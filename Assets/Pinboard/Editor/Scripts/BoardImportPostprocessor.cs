using System;
using UnityEditor;

namespace Pinboard
{
	public class BoardImportPostprocessor : AssetPostprocessor
	{
		public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
		                                          string[] movedFromAssetPaths)
		{
			PinboardCore.Initialize();
		}
	}
}
