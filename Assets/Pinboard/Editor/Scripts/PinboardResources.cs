using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	public static class PinboardResources
	{
		public static readonly Texture ICON_UNITY;
		public static readonly Texture ICON_TEXT;
		public static readonly Texture ICON_GO;
		public static readonly Texture ICON_PREFAB;
		public static readonly Texture ICON_VIEW;
		public static readonly Texture ICON_IMAGE;

		static PinboardResources()
		{
			var iconsDir = PinboardCore.DIR_UI + "/Icons/";
			ICON_TEXT = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/text.png");
			ICON_UNITY = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/scene.png");
			ICON_GO = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/go.png");
			ICON_PREFAB = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/prefab.png");
			ICON_VIEW = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/view.png");
			ICON_IMAGE = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/image.png");
		}
	}
}
