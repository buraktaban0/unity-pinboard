using System;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	public class PinboardAssetPostprocessor : AssetPostprocessor
	{
		public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
		                                          string[] movedFromAssetPaths)
		{
			PinboardCore.RunNextFrame(PinboardCore.OnAssetDatabaseModifiedExternally);
		}
		
	}
}
