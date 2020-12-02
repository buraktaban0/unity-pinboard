using UnityEngine;

namespace Pinboard
{
	public static class PinboardClipboard
	{

		public static Entry Entry { get; set; } = null;

		public static string Content { get; set; } = "";

		public static string SystemBuffer
		{
			get => GUIUtility.systemCopyBuffer;
			set => GUIUtility.systemCopyBuffer = value;
		}

		public static bool HasContent => string.IsNullOrEmpty(Content);


	}
}
