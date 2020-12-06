using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	public static class PinboardResources
	{		
		public static readonly Texture ICON_PINBOARD;
		public static readonly Texture ICON_UNITY;
		public static readonly Texture ICON_TEXT;
		public static readonly Texture ICON_GO;
		public static readonly Texture ICON_PREFAB;
		public static readonly Texture ICON_VIEW;
		public static readonly Texture ICON_IMAGE;
		public static readonly Texture ICON_ADD;
		public static readonly Texture ICON_MENU_OPEN;
		public static readonly Texture ICON_BROKEN;
		public static readonly Texture ICON_INVALID_SCENE;
		public static readonly Texture ICON_DELETE;
		public static readonly Texture ICON_CLIPBOARD;
		public static readonly Texture ICON_COPY;
		public static readonly Texture ICON_PASTE;

		static PinboardResources()
		{
			var iconsDir = PinboardWindow.DIR_UI + "/Icons/";
			ICON_PINBOARD = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/pinboard.png");
			ICON_TEXT = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/text.png");
			ICON_UNITY = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/scene.png");
			ICON_GO = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/go.png");
			ICON_PREFAB = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/prefab.png");
			ICON_VIEW = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/view.png");
			ICON_IMAGE = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/image.png");
			ICON_ADD = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/add.png");
			ICON_MENU_OPEN = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/menu_open.png");
			ICON_BROKEN = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/broken.png");
			ICON_INVALID_SCENE = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/invalid_scene.png");
			ICON_DELETE = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/delete.png");
			ICON_CLIPBOARD = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/clipboard.png");
			ICON_COPY = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/copy.png");
			ICON_PASTE = AssetDatabase.LoadAssetAtPath<Texture>(iconsDir + "/paste.png");
			
		}
	}
}
