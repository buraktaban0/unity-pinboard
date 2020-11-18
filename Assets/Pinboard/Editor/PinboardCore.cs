using UnityEngine;

namespace Pinboard
{
	public static class PinboardCore
	{
		private static string gitUser = null;

		public static string GitUser
		{
			get
			{
				if (string.IsNullOrEmpty(gitUser))
				{
					gitUser = Utility.GetGitUserName();
				}

				return gitUser;
			}
		}


		static PinboardCore()
		{
			gitUser = Utility.GetGitUserName();
		}
	}
}
