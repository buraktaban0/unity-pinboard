using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pinboard.Entries
{
	[System.Serializable]
	[EntryType("Asset Shortcut", false, BoardAccessibility.Global)]
	public class ObjectShortcutEntry : Entry
	{
		public const string INVALID_ASSET = "Invalid Asset";

		public const int ID_NULL = 0;
		public const int ID_IMPORTED_ASSET = 1;
		public const int ID_SCENE_OBJECT = 2;
		public const int ID_SOURCE_ASSET = 3;


		[MenuItem("Assets/Pinboard/Save Shortcut")]
		public static void SaveAssetShortcutToPinboard()
		{
			PinboardCore.TryCreateEntry<ObjectShortcutEntry>();
		}

		[MenuItem("Assets/Pinboard/Save Shortcut", true)]
		public static bool SaveAssetShortcutToPinboardValidation()
		{
			return Selection.activeObject != null && Selection.objects.Length == 1;
		}

		[MenuItem("GameObject/Pinboard/Save Shortcut", false, 0)]
		public static void SaveGameObjectShortcutToPinboard()
		{
			PinboardCore.TryCreateEntry<ObjectShortcutEntry>();
		}

		[MenuItem("GameObject/Pinboard/Save Shortcut", true, 0)]
		public static bool SaveGameObjectShortcutToPinboardValidation()
		{
			return Selection.activeObject != null && Selection.objects.Length == 1;
		}


		public override string ShortVisibleName
		{
			get
			{
				var obj = Object;
				if (obj)
				{
					return obj.name;
				}

				return "Missing Object";
			}
		}

		public GlobalObjectId ObjectId
		{
			get
			{
				if (string.IsNullOrEmpty(guid))
					return default;

				GlobalObjectId.TryParse(guid, out var globalObjectId);
				return globalObjectId;
			}
		}


		public UnityEngine.Object Object => GlobalObjectId.GlobalObjectIdentifierToObjectSlow(ObjectId);

		public GlobalObjectId SceneAssetId
		{
			get
			{
				if (string.IsNullOrEmpty(sceneAssetGuid))
					return default;

				GlobalObjectId.TryParse(sceneAssetGuid, out var globalObjectId);
				return globalObjectId;
			}
		}

		public SceneAsset SceneAsset => GlobalObjectId.GlobalObjectIdentifierToObjectSlow(SceneAssetId) as SceneAsset;


		public bool IsSceneObject => !string.IsNullOrEmpty(sceneAssetGuid);

		public bool IsFolder => System.IO.Directory.Exists(SystemAssetPath);

		public string AssetPath => AssetDatabase.GetAssetPath(Object);

		public string SystemAssetPath => Application.dataPath.Replace("Assets", "") + AssetPath;


		public string guid = "";
		public string sceneAssetGuid = "";

		public string cachedName = "";
		public string cachedType = "";
		public string cachedPath = "";
		public string cachedSceneName = "";
		public ulong cachedTransformLocalIdentifier = 0;

		public ObjectShortcutEntry()
		{
			guid = "";
		}

		public ObjectShortcutEntry(string guid)
		{
			this.IsDirty = true;

			this.guid = guid;
		}


		public override void BindVisualElement(VisualElement el)
		{
			var lbl = el.Q<Label>();
			lbl?.RemoveFromHierarchy();
			lbl = new Label();
			lbl.style.textOverflow = TextOverflow.Ellipsis;
			lbl.name = "asset-shortcut-name";
			el.Add(lbl);

			// el.tooltip =
			// 	$"{id} - {cachedName} - {cachedType} - {cachedPath} - {cachedSceneName} - {guid} - {sceneAssetGuid}";

			var img = el.Q<Image>();

			var obj = Object;

			if (obj is GameObject go && go != null)
			{
				cachedTransformLocalIdentifier = GlobalObjectId.GetGlobalObjectIdSlow(go.transform).targetObjectId;
			}

			if ((!IsSceneObject && obj) || (IsSceneObject && obj && SceneAsset &&
			                                EditorSceneManager.GetActiveScene().name == SceneAsset.name))
			{
				cachedName = obj.name;
				cachedType = obj.GetType().Name;
				var sceneAsset = SceneAsset;

				if (IsSceneObject)
				{
					cachedSceneName = sceneAsset.name;
				}
				else
				{
					cachedPath = AssetDatabase.GetAssetPath(obj);
				}

				lbl.text = obj.name;
				img.image = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;
				el.tooltip = $"{obj.GetType().Name} {obj.name} @ {(IsSceneObject ? sceneAsset.name : AssetPath)}";
				return;
			}


			if (IsSceneObject)
			{
				var sceneAsset = SceneAsset;

				if (!sceneAsset)
				{
					// scene deleted;
					lbl.text = $"Missing {cachedType} {cachedName} (Scene missing)";
					img.image = PinboardResources.ICON_INVALID_SCENE;
					el.tooltip =
						$"Missing {cachedType} {cachedName} @ {cachedSceneName}\nThe Scene containing this GameObject is missing.";
					return;
				}

				if (EditorSceneManager.GetActiveScene().name == sceneAsset.name)
				{
					// Check if converted to prefab
					var wasConverted = CheckAndHandleConversionToPrefab();
					if (wasConverted)
					{
						this.BindVisualElement(el);
						return;
					}

					// deleted scene object
					lbl.text = $"Missing {cachedType} {cachedName}";
					img.image = PinboardResources.ICON_DELETE;
					el.tooltip =
						$"Missing {cachedType} {cachedName} @ {cachedSceneName}\nThe GameObject was probably deleted or a prefab pack/unpack operation took place. Pinboard cannot track these events.";
				}
				else
				{
					lbl.text = $"{cachedType} {cachedName}\t(In scene '{sceneAsset.name}')";
					img.image = PinboardResources.ICON_INVALID_SCENE;
					el.tooltip =
						$"{cachedType} {cachedName} @ {cachedSceneName}\nOpen the scene to access {cachedName}.";
				}
			}
			else
			{
				// Asset or folder deleted
				lbl.text = $"Missing {cachedType} {cachedName}";
				img.image = PinboardResources.ICON_DELETE;
				el.tooltip = $"Missing {cachedType} {cachedName} @ {cachedPath}\nThe asset was probably deleted.";
			}
		}

		public override void UnbindVisualElement(VisualElement el)
		{
			var lbl = el.Q<Label>("asset-shortcut-name");
			if (lbl != null)
			{
				el.Remove(lbl);
			}
		}

		public override void Create(Action<bool> onResult)
		{
			var obj = Selection.activeObject;
			Debug.Log(obj);
			if (!obj)
			{
				Debug.Log("Selected asset was null, cannot create shortcut.");
				onResult?.Invoke(false);
				return;
			}

			TrySetupForObject(obj, onResult);
		}

		private bool TrySetupForObject(UnityEngine.Object obj, Action<bool> onResult = null)
		{
			if (obj is GameObject go)
			{
				var scene = go.scene;
				if (scene.IsValid())
				{
					var sceneAssetContaining = Utility.LoadAssets<SceneAsset>().First(s => s.name == scene.name);
					sceneAssetGuid = GlobalObjectId.GetGlobalObjectIdSlow(sceneAssetContaining).ToString();
				}

				cachedTransformLocalIdentifier = GlobalObjectId.GetGlobalObjectIdSlow(go.transform).targetObjectId;
			}

			var id = GlobalObjectId.GetGlobalObjectIdSlow(obj);

			if (id.identifierType == ID_NULL)
			{
				Debug.Log("Id was null for object, cannot create shortcut.");
				onResult?.Invoke(false);
				return false;
			}

			guid = id.ToString();

			cachedName = obj.name;
			cachedType = obj.GetType().Name;

			var sceneAsset = SceneAsset;

			if (IsSceneObject)
			{
				cachedSceneName = sceneAsset.name;
			}
			else
			{
				cachedPath = AssetDatabase.GetAssetPath(obj);
			}

			onResult?.Invoke(true);
			return true;
		}

		public override bool EditOrUpdate(bool recordUndoState, Action<bool> onResult = null)
		{
			return false;
		}

		public override void OnClick()
		{
			base.OnClick();

			var obj = Object;

			if (!obj)
				return;

			if (IsFolder && !IsSceneObject)
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

			TrySelectObject();
		}


		public override void PopulateContextualMenu(ContextualMenuPopulateEvent evt)
		{
			base.PopulateContextualMenu(evt);

			var obj = Object;

			if ((!IsSceneObject && obj))
			{
				evt.menu.AppendAction("Select Asset", action => TrySelectObject());
				//evt.menu.AppendAction("Edit Asset", action => { UnityObjectEditorContainerWindow.Show(obj); });
			}
			else if (IsSceneObject)
			{
				if (SceneAsset == null)
				{
					if (EditorSceneManager.GetActiveScene().name == SceneAsset.name)
					{
						if (obj)
						{
							evt.menu.AppendAction("Select Asset", action => TrySelectObject());
						}
					}
				}
				else
				{
					if (EditorSceneManager.GetActiveScene().name == SceneAsset.name)
					{
						if (obj)
						{
							evt.menu.AppendAction("Select Asset", action => TrySelectObject());
						}
					}
					else
					{
						evt.menu.AppendAction("Open Containing Scene and Select GameObject", action =>
						{
							void OnSceneLoaded(Scene scene, OpenSceneMode mode)
							{
								EditorSceneManager.sceneOpened -= OnSceneLoaded;
								TrySelectObject();
								//PinboardCore.RunNextFrame(TrySelectObject);
							}

							EditorSceneManager.sceneOpened += OnSceneLoaded;
							var wasSaved = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
							EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(SceneAsset));
						});
					}

					evt.menu.AppendAction("Select Containing Scene", action => SelectAsset(SceneAsset));
				}
			}

			//evt.menu.AppendAction("Edit", action => { this.EditOrUpdate(true); });
		}
		//

		public override void CopySelfToClipboard()
		{
			PinboardClipboard.Entry = this;
			if (IsSceneObject)
			{
				PinboardClipboard.SystemBuffer = cachedName;
				if (Object is GameObject go)
				{
					PinboardClipboard.UnityObject = go;
				}
			}
			else
			{
				PinboardClipboard.SystemBuffer = cachedPath;
				PinboardClipboard.UnityObject = Object;
			}
		}

		public override Entry Clone()
		{
			var clone = new ObjectShortcutEntry(this.guid);
			clone.sceneAssetGuid = sceneAssetGuid;
			clone.cachedName = cachedName;
			clone.cachedPath = cachedPath;
			clone.cachedType = cachedType;
			clone.cachedSceneName = cachedSceneName;
			clone.IsDirty = true;
			return clone;
		}

		// public override bool IsValidForSearch(List<string>filters)
		// {
		// 	if (IsSceneObject)
		// 	{
		// 		return Utility.DoStringSearch(cachedName, filters) || Utility.DoStringSearch(cachedType, filters) ||
		// 		       Utility.DoStringSearch(cachedSceneName, filters);
		// 	}
		// 	else
		// 	{
		// 		return Utility.DoStringSearch(cachedName, filters) || Utility.DoStringSearch(cachedType, filters) ||
		// 		       Utility.DoStringSearch(cachedPath, filters);
		// 	}
		// }

		public override IEnumerable<string> GetSearchKeywords()
		{
			yield return cachedName;
			yield return cachedType;
			if (IsSceneObject)
			{
				yield return "scene";
				yield return cachedSceneName;
			}
			else
			{
				if (IsFolder)
				{
					yield return "folder";
				}

				yield return cachedPath;
			}
		}

		public void TrySelectObject()
		{
			var obj = Object;

			if (!obj)
			{
				var sceneAsset = SceneAsset;
				if (SceneAsset)
				{
					SelectAsset(sceneAsset);
				}

				return;
			}

			if (IsFolder && !IsSceneObject)
			{
				Utility.ShowFolderContents(obj.GetInstanceID());
			}
			else if (IsSceneObject)
			{
				var sceneAsset = SceneAsset;
				if (EditorSceneManager.GetActiveScene().name == sceneAsset.name)
				{
					SelectAsset();
				}
				else
				{
					SelectAsset(sceneAsset);
				}
			}
			else
			{
				SelectAsset();
			}
		}


		public void SelectAsset() => SelectAsset(Object);

		public void SelectAsset(UnityEngine.Object obj)
		{
			Selection.activeObject = obj;
			EditorGUIUtility.PingObject(obj);
		}


		private bool CheckAndHandleConversionToPrefab()
		{
			return false;
			// Cannot find conversions, no local file id for prefab instances in scene files. 
			// TODO: Try PrefabUtility.prefabInstanceUpdated

			// Debug.Log("A");
			// GlobalObjectId.TryParse(guid, out var id);
			//
			// if (id.identifierType == ID_NULL)
			// 	return false;
			//
			// Debug.Log("B " + id);
			// var targetObjectId = cachedTransformLocalIdentifier;
			// Debug.Log("IDIDID " + cachedTransformLocalIdentifier);
			//
			// var convertedObj = GameObject.FindObjectsOfType<Transform>().FirstOrDefault(t =>
			// {
			// 	var prefabHandle = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
			// 	if (prefabHandle)
			// 	{
			// 		Debug.Log(prefabHandle.GetType());
			// 		t = ((GameObject)prefabHandle).transform;
			// 	}
			// 	var otherId = GlobalObjectId.GetGlobalObjectIdSlow(t);
			//
			// 	Debug.Log("Other " + otherId + "  " + otherId.targetObjectId);
			// 	return otherId.targetObjectId == targetObjectId;
			// });
			//
			// if (!convertedObj)
			// {
			// 	// GO was simply deleted.
			// 	return false;
			// }
			// Debug.Log("C");
			//
			// return TrySetupForObject(convertedObj.gameObject);
		}
	}
}
