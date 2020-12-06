using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	public static class Utility
	{
		public static string GetUserName()
		{
			var name = System.Environment.MachineName.Trim();

			if (string.IsNullOrEmpty(name))
			{
				name = "Unknown User";
			}

			try
			{
				var process = new System.Diagnostics.Process();
				process.StartInfo.FileName = "git";
				process.StartInfo.Arguments = "config user.name";
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.UseShellExecute = false;
				process.Start();

				var error = process.StandardError.ReadToEnd();
				var data = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				if (error.Length > 0)
				{
					Debug.LogWarning(error);
					return name.Trim();
				}

				name = data;
			}
			catch (Exception e)
			{
				Debug.LogWarning(e);
			}

			return name.Trim();
		}

		public static string GetProjectID()
		{
			var segments = Application.dataPath.Split('/');
			var complexName = segments[segments.Length - 2].Trim().ToLower();
			var chars = complexName.Replace(" ", "-").ToCharArray();

			chars = chars.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
			var simpleName = new string(chars);
			return simpleName;
		}

		public static long GetUnixTimestamp()
		{
			return DateTimeOffset.UtcNow.ToUniversalTime().ToUnixTimeSeconds();
		}

		public static DateTime FromUnixToLocal(string seconds)
		{
			if (long.TryParse(seconds, out long s))
			{
				return FromUnixToLocal(s);
			}

			return new DateTime(1900, 1, 1);
		}

		public static DateTime FromUnixToLocal(long seconds)
		{
			return DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
		}


		public static bool DoStringSearch(string val, List<string> filters)
		{
			val = val.ToLower();

			var result = filters.Any(f => val.Contains(f));

			return result;
		}

		public static bool CrossCompareStrings(IEnumerable<string> keywords, IEnumerable<string> filters)
		{
			return keywords.Any(keyword => filters.Any(keyword.Contains));
			//return filters.Any(filter => keywords.Any(filter.Contains));
		}

		public static string SplitCamelCase(this string input)
		{
			return System.Text.RegularExpressions.Regex
			             .Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
		}

		public static string Truncate(this string s, int maxChars = 16, string truncateString = "…")
		{
			return s.Length <= maxChars ? s : $"{s.Substring(0, maxChars - truncateString.Length)}{truncateString}";
		}


		public static string CorrectlyEnumerate(this string s, IEnumerable<string> others)
		{
			var list = others.ToList();
			var s1 = s;
			int index = 1;
			while (list.Contains(s1))
			{
				s1 = s + "_" + index++;
			}

			return s1;
		}


		public static void MakeDirs(string path)
		{
			path = path.Replace("\\", "/");

			var segments = path.Split('/').ToList();

			if (segments.Last().Contains("."))
				segments.RemoveAt(segments.Count - 1);

			if (segments.Count < 2)
				return;

			var parent = segments[0];

			for (int i = 1; i < segments.Count; i++)
			{
				if (AssetDatabase.IsValidFolder(parent + "/" + segments[i]) == false)
				{
					AssetDatabase.CreateFolder(parent, segments[i]);
				}

				parent = parent + "/" + segments[i];
			}
		}

		public static List<T> LoadAssets<T>() where T : UnityEngine.Object
		{
			var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

			if (guids == null || guids.Length < 1)
				return new List<T>();

			var assets =
				guids.Select(
					guid => AssetDatabase.LoadAssetAtPath<T>(
						AssetDatabase.GUIDToAssetPath(guid))).Where(a => a != null).ToList();

			return assets;
		}


		public static void ShowFolderContents(int folderInstanceID)
		{
			// Find the internal ProjectBrowser class in the editor assembly.
			Assembly editorAssembly = typeof(Editor).Assembly;
			System.Type projectBrowserType = editorAssembly.GetType("UnityEditor.ProjectBrowser");

			// This is the internal method, which performs the desired action.
			// Should only be called if the project window is in two column mode.
			MethodInfo showFolderContents = projectBrowserType.GetMethod(
				"ShowFolderContents", BindingFlags.Instance | BindingFlags.NonPublic);

			// Find any open project browser windows.
			UnityEngine.Object[] projectBrowserInstances = Resources.FindObjectsOfTypeAll(projectBrowserType);

			if (projectBrowserInstances.Length > 0)
			{
				for (int i = 0; i < projectBrowserInstances.Length; i++)
					ShowFolderContentsInternal(projectBrowserInstances[i], showFolderContents, folderInstanceID);
			}
			else
			{
				EditorWindow projectBrowser = OpenNewProjectBrowser(projectBrowserType);
				ShowFolderContentsInternal(projectBrowser, showFolderContents, folderInstanceID);
			}
		}

		private static void ShowFolderContentsInternal(UnityEngine.Object projectBrowser, MethodInfo showFolderContents,
		                                               int folderInstanceID)
		{
			// Sadly, there is no method to check for the view mode.
			// We can use the serialized object to find the private property.
			SerializedObject serializedObject = new SerializedObject(projectBrowser);
			bool inTwoColumnMode = serializedObject.FindProperty("m_ViewMode").enumValueIndex == 1;

			if (!inTwoColumnMode)
			{
				// If the browser is not in two column mode, we must set it to show the folder contents.
				MethodInfo setTwoColumns = projectBrowser.GetType().GetMethod(
					"SetTwoColumns", BindingFlags.Instance | BindingFlags.NonPublic);
				setTwoColumns.Invoke(projectBrowser, null);
			}

			bool revealAndFrameInFolderTree = true;
			showFolderContents.Invoke(projectBrowser, new object[] {folderInstanceID, revealAndFrameInFolderTree});
		}

		private static EditorWindow OpenNewProjectBrowser(System.Type projectBrowserType)
		{
			EditorWindow projectBrowser = EditorWindow.GetWindow(projectBrowserType);
			projectBrowser.Show();

			// Unity does some special initialization logic, which we must call,
			// before we can use the ShowFolderContents method (else we get a NullReferenceException).
			MethodInfo init = projectBrowserType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
			init.Invoke(projectBrowser, null);

			return projectBrowser;
		}
	}
}
