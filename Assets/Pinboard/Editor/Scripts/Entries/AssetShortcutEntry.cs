using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Pinboard.Entries
{
	[System.Serializable]
	[EntryType(visibleName = "Asset Shortcut")]
	public class AssetShortcutEntry : Entry
	{
		public const string INVALID_ASSET = "Invalid Asset";


		[MenuItem("Assets/Pinboard/Save Shortcut")]
		public static void SaveAssetShortcutToPinboard()
		{
			PinboardCore.TryCreateEntry<AssetShortcutEntry>();
		}

		[MenuItem("Assets/Pinboard/Save Shortcut", true)]
		public static bool SaveAssetShortcutToPinboardValidation()
		{
			return Selection.activeObject != null;
		}

		[MenuItem("GameObject/Pinboard/Save Shortcut", false, 0)]
		public static void SaveGameObjectShortcutToPinboard()
		{
			PinboardCore.TryCreateEntry<AssetShortcutEntry>();
		}

		[MenuItem("GameObject/Pinboard/Save Shortcut", true, 0)]
		public static bool SaveGameObjectShortcutToPinboardValidation()
		{
			return Selection.activeObject != null;
		}

		public int instanceId = -1;

		public int sceneInstanceId = -1;

		public UnityEngine.Object Object => EditorUtility.InstanceIDToObject(instanceId);

		public bool IsValid => Object;

		public bool IsAsset => AssetDatabase.Contains(instanceId);

		public SceneAsset SceneAsset => EditorUtility.InstanceIDToObject(sceneInstanceId) as SceneAsset;

		public bool IsSceneObject => SceneAsset;

		public bool IsSceneOpen => EditorSceneManager.GetActiveScene().name == SceneAsset.name;

		public string cachedPath = "";
		public string AssetPath => IsValid ? AssetDatabase.GetAssetPath(Object) : string.Empty;

		// TODO: Validate on macos and linux 
		public bool IsFolder => System.IO.Directory.Exists(Application.dataPath.Replace("Assets", "") + AssetPath);

		public override string ShortVisibleName => IsValid ? Object.name.Truncate() : INVALID_ASSET;

		public string cachedType = ""; 
		public Type AssetType => IsValid ? Object.GetType() : null;

		public string cachedName = "";
		public string Name
		{
			get
			{

				return IsValid ? Object.name : IsSceneObject ? cachedName : $"Deleted asset: {cachedName}";

			}
		}


		public AssetShortcutEntry()
		{
			instanceId = -1;
		}

		public AssetShortcutEntry(int instanceId)
		{
			this.IsDirty = true;

			this.instanceId = instanceId;
		}

		public override Texture GetIcon()
		{
			if (IsValid)
				return EditorGUIUtility.ObjectContent(Object, typeof(Object)).image;

			// not currently valid
			
			if (IsSceneObject)
			{
				if (EditorSceneManager.GetActiveScene().name == SceneAsset.name)
				{
					// deleted scene object
					Debug.Log(instanceId);
					return PinboardResources.ICON_DELETE;
				}
				else
				{
					return PinboardResources.ICON_INVALID_SCENE;
					// scene not loaded (maybe object is deleted too, not sure at this point)
				}
			}
			else
			{
				// deleted asset
				return PinboardResources.ICON_DELETE;
			}
		}

		public override void BindVisualElement(VisualElement el)
		{
			if (IsValid)
			{
				cachedName = Name;
				cachedPath = IsAsset ? AssetPath : "";
				cachedType = Object.GetType().Name;
			}

			var lbl = new Label(Name);
			lbl.style.textOverflow = TextOverflow.Ellipsis;
			lbl.name = "asset-shortcut-name";
			el.Add(lbl);

			el.tooltip = IsValid
				? IsAsset ? $"{AssetType.Name} @ {AssetPath}" : $"{AssetType.Name} @ Scene {SceneAsset.name}"
				: IsAsset ? $"Deleted {cachedType} @ {cachedPath}" : IsSceneOpen ? $"Deleted {cachedType} {cachedName} @ {(SceneAsset ? SceneAsset.name : "Unknown Scene")}":  $"{cachedType} {cachedName} @ {(SceneAsset ? SceneAsset.name : "Unknown Scene")}";
		}

		public override void UnbindVisualElement(VisualElement el)
		{
			var lbl = el.Q<Label>("asset-shortcut-name");
			if (lbl != null)
			{
				el.Remove(lbl);
			}
		}

		public override bool Create()
		{
			var obj = Selection.activeObject;
			if (!obj)
			{
				Debug.Log("Selected asset was null, cannot create shortcut.");
				return false;
			}

			if (obj is GameObject go)
			{
				var scene = go.scene;
				if (scene.IsValid())
				{
					Debug.Log(obj.GetInstanceID());
					var sceneAsset = Utility.LoadAssets<SceneAsset>().First(s => s.name == scene.name);
					sceneInstanceId = sceneAsset.GetInstanceID();
				}
			}

			// var isAsset = AssetDatabase.Contains(obj);
			// if (!isAsset)
			// {
			// 	Debug.Log("Selected object is not an asset, cannot create shortcut.");
			// 	return false;
			// }


			instanceId = obj.GetInstanceID();


			if (instanceId <= 0)
			{
				Debug.Log("Instance id for asset was null, cannot create shortcut.");
				return false;
			}

			cachedName = obj.name;
			cachedType = obj.GetType().FullName;

			if (IsAsset)
				cachedPath = AssetPath;
			
			return true;
		}

		public override bool EditOrUpdate(bool recordUndoState)
		{
			return false;
		}

		public override void OnClick()
		{
			base.OnClick();

			if (!IsValid)
			{
				return;
			}

			if (IsFolder)
			{
				SelectAsset();
			}
			else if (IsSceneObject)
			{
				
			}
		}

		public override void OnDoubleClick()
		{
			base.OnDoubleClick();

			if (!IsValid)
			{
				Debug.Log("Cannot select asset, asset is null.");
				return;
			}

			if (IsFolder)
			{
				Utility.ShowFolderContents(instanceId);
			}
			else if (IsSceneObject)
			{
				if (EditorSceneManager.GetActiveScene().name == SceneAsset.name)
				{
					SelectAsset();
				}
				else
				{
					SelectAsset(SceneAsset);
				}
			}
			else
			{
				SelectAsset();
			}
		}


		public override void PopulateContextualMenu(ContextualMenuPopulateEvent evt)
		{
			base.PopulateContextualMenu(evt);

			if (IsValid)
				evt.menu.AppendAction("Select Asset", action => SelectAsset());
			else
			{
				evt.menu.AppendAction("Cannot Select Asset", action => { }, DropdownMenuAction.Status.Disabled);
			}

			//evt.menu.AppendAction("Edit", action => { this.EditOrUpdate(true); });
		}

		public override void CopySelfToClipboard()
		{
			PinboardClipboard.Entry = this;
			if (IsValid)
				PinboardClipboard.SystemBuffer = AssetPath;
		}

		public override Entry Clone()
		{
			var clone = new AssetShortcutEntry(this.instanceId);
			clone.IsDirty = true;
			return clone;
		}

		public override bool IsValidForSearch(string[] filters)
		{
			return Utility.DoStringSearch(AssetPath, filters) || Utility.DoStringSearch(instanceId.ToString(), filters);
		}


		public void SelectAsset() => SelectAsset(Object);

		public void SelectAsset(UnityEngine.Object obj)
		{
			Selection.activeObject = obj;
			EditorGUIUtility.PingObject(obj);	
		}

	}
}
