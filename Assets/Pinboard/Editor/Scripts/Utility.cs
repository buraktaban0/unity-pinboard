using System;
using System.Linq;
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


		public static bool DoStringSearch(string val, string[] filters)
		{
			val = val.ToLower();

			var result = filters.Any(f => val.Contains(f));

			return result;
		}

		public static string SplitCamelCase(this string input)
		{
			return System.Text.RegularExpressions.Regex
			             .Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
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
	}
}
