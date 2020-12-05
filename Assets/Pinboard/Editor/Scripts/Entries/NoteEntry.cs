using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard.Entries
{
	[System.Serializable]
	[EntryType(visibleName = "Note")]
	public class NoteEntry : Entry
	{
		public override string ShortVisibleName => content.Truncate();

		public string content;


		private string popupTitle = "Update Note";

		private bool isBeingEdited = false;

		public NoteEntry()
		{
			content = "Empty note";
		}

		public NoteEntry(string content)
		{
			this.IsDirty = true;

			this.content = content;
		}

		public override Texture GetIcon() => PinboardResources.ICON_TEXT;

		public override void BindVisualElement(VisualElement el)
		{
			var lbl = new Label(content);
			lbl.style.textOverflow = TextOverflow.Ellipsis;
			lbl.name = "simple-text-content";
			el.Add(lbl);

			el.tooltip = content;
		}

		public override void UnbindVisualElement(VisualElement el)
		{
			var lbl = el.Q<Label>("simple-text-content");
			if (lbl != null)
			{
				el.Remove(lbl);
			}
		}

		public override bool Create()
		{
			popupTitle = "Create Note";
			return EditOrUpdate(false);
		}

		public override bool EditOrUpdate(bool recordUndoState)
		{
			if (isBeingEdited)
				return false;

			isBeingEdited = true;

			if (recordUndoState)
				PinboardDatabase.Current.WillModifyEntry(this);

			var wasEdited = TextEditPopup.ShowPopup(popupTitle, this.content, s =>
			{
				popupTitle = "Update Note";
				this.content = s;
			});
			isBeingEdited = false;

			if (wasEdited)
				IsDirty = true;

			return wasEdited;
		}

		public override void OnDoubleClick()
		{
			base.OnDoubleClick();

			if (this.EditOrUpdate(true))
			{
				//PinboardDatabase.SaveBoards();
			}
		}

		public override void PopulateContextualMenu(ContextualMenuPopulateEvent evt)
		{
			base.PopulateContextualMenu(evt);

			evt.menu.AppendAction("Edit", action => { this.EditOrUpdate(true); });
			
		}

		public override void CopySelfToClipboard()
		{
			PinboardClipboard.Entry = this;
			PinboardClipboard.SystemBuffer = this.content;
		}

		public override Entry Clone()
		{
			var clone = new NoteEntry(content);
			clone.IsDirty = true;
			return clone;
		}

		public override bool IsValidForSearch(string[] filters)
		{
			return Utility.DoStringSearch(content, filters);
		}
	}
}
