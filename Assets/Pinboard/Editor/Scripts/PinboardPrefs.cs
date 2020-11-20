using UnityEditor;

namespace Pinboard
{
	public static class PinboardPrefs
	{
		private const string KEY_LAST_OPEN_BOARD = "PINBOARD_LAST_OPEN_BOARD_ID";

		public static string LastOpenBoardID
		{
			get => EditorPrefs.GetString(KEY_LAST_OPEN_BOARD, "");
			set => EditorPrefs.SetString(KEY_LAST_OPEN_BOARD, value);
		}
	}
}
