using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pinboard.Entries
{
	[System.Serializable]
	[EntryType("Scene View Record", true)]
	public class SceneViewRecordEntry : Entry
	{
		public override string ShortVisibleName => "Scene View Record";

		public Vector3 pivot;
		public float size;
		public Quaternion rotation;
		public bool orthographic;
		public bool in2DMode;

		public SceneViewRecordEntry()
		{
		}


		public override void BindVisualElement(VisualElement el)
		{
			var lbl = el.Q<Label>();
			lbl?.RemoveFromHierarchy();
			lbl = new Label(hasExplicitName ? explicitName : "Scene View Record");
			lbl.style.textOverflow = TextOverflow.Ellipsis;
			lbl.name = "scene-view-record-name";
			el.Add(lbl);

			el.Q<Image>().image = PinboardResources.ICON_VIEW;

			var tooltip = $"{(hasExplicitName ? explicitName + " | " : "")}Scene View Record\n\n";

			if (in2DMode)
			{
				tooltip += "2D";
			}
			else
			{
				tooltip += (orthographic ? "Orthographic" : "Perspective") + "";
			}
			
			el.tooltip = tooltip;
		}

		public override void UnbindVisualElement(VisualElement el)
		{
			var lbl = el.Q<Label>("scene-view-record-name");
			lbl?.RemoveFromHierarchy();
		}

		public override void Create(Action<bool> onResult)
		{
			EditOrUpdate(false, onResult);
		}

		public override bool EditOrUpdate(bool recordUndoState, Action<bool> onResult = null)
		{
			if (recordUndoState)
			{
				PinboardDatabase.Current.WillModifyEntry(this);
			}

			var view = SceneView.lastActiveSceneView;
			pivot = view.pivot;
			size = view.size;
			rotation = view.rotation;
			orthographic = view.orthographic;
			in2DMode = view.in2DMode;

			IsDirty = true;

			onResult?.Invoke(true);
			return true;
		}

		public override void OnDoubleClick()
		{
			base.OnDoubleClick();

			ApplyToSceneView();
		}

		public override void PopulateContextualMenu(ContextualMenuPopulateEvent evt)
		{
			base.PopulateContextualMenu(evt);

			evt.menu.AppendAction("Apply to scene view", action => { ApplyToSceneView(); });

			evt.menu.AppendAction("Update from scene view", action =>
			{
				var yes = EditorUtility.DisplayDialog("Update scene view record",
				                                      "Are you sure you want to update and override this scene view record?",
				                                      "Yes", "No");

				if (yes)
				{
					this.EditOrUpdate(true);
				}
			});
		}

		public override void CopySelfToClipboard()
		{
			PinboardClipboard.Entry = this;
			//PinboardClipboard.SystemBuffer = this.content;
		}

		public override Entry Clone()
		{
			var clone = new SceneViewRecordEntry();
			clone.IsDirty = true;
			clone.orthographic = orthographic;
			clone.in2DMode = in2DMode;
			clone.pivot = pivot;
			clone.rotation = rotation;
			clone.size = size;
			return clone;
		}

		// public override bool IsValidForSearch(List<string> filters)
		// {
		// 	return Utility.DoStringSearch(content, filters);
		// }

		public override IEnumerable<string> GetSearchKeywords()
		{
			yield return "scene";
			yield return "view";
			yield return "position";
			yield return "rotation";
			yield return "record";
			yield return "orientation";
			yield return "pivot";
			yield return "camera";
		}


		public void ApplyToSceneView()
		{
			var view = SceneView.lastActiveSceneView;
			view.in2DMode = in2DMode;
			view.orthographic = orthographic;
			view.pivot = pivot;
			view.rotation = rotation;
			view.size = size;

			view.Repaint();
		}
	}
}
