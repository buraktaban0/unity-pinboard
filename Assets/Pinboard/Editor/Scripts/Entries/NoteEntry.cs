using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard.Items
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
			this.content = content;
		}

		public override void BindVisualElement(VisualElement el)
		{
			var icon = el.Q<Image>();
			icon.image = PinboardResources.ICON_TEXT;

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
			return EditOrUpdate();
		}

		public override bool EditOrUpdate()
		{
			if (isBeingEdited)
				return false;

			isBeingEdited = true;

			PinboardDatabase.Current.WillModifyEntry(this);

			var wasEdited = NotePopup.ShowPopup(popupTitle, this.content, s =>
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

			if (this.EditOrUpdate())
			{
				//PinboardDatabase.SaveBoards();
			}
		}

		public override bool IsValidForSearch(string[] filters)
		{
			return Utility.DoStringSearch(content, filters);
		}
	}
}
