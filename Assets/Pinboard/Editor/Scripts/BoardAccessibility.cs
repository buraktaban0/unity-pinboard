using System;

namespace Pinboard
{
	public enum BoardAccessibility
	{
		Global = 1,
		ProjectPrivate = 2,
		ProjectPublic = 3
	}

	public static class BoardTypeDesc
	{
		public const string Global = "All projects on the machine";
		public const string ProjectPrivate = "This project, private";
		public const string ProjectPublic = "This project, shared with source control";

		public static string For(BoardAccessibility accessibility)
		{
			switch (accessibility)
			{
				case BoardAccessibility.Global:
					return Global;
				case BoardAccessibility.ProjectPrivate:
					return ProjectPrivate;
				case BoardAccessibility.ProjectPublic:
					return ProjectPublic;
			}

			throw new Exception("Unknown project type: " + accessibility);
		}
	}
	
	
	
}
