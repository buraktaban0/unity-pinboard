using System;
using UnityEngine;

namespace Pinboard
{
	public static class Utility
	{
		public static string GetGitUserName()
		{
			var name = System.Environment.MachineName;

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
					return name;
				}

				name = data;
			}
			catch (Exception e)
			{
				Debug.LogWarning(e);
			}

			return name;
		}

		public static string GetUnixTimestamp()
		{
			return DateTimeOffset.UtcNow.ToUniversalTime().ToUnixTimeSeconds().ToString();
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
	}
}
