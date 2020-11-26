using UnityEngine.UIElements;

namespace Pinboard.Items
{
	[EntryType(visibleName = "Note")]
	public class NoteEntry: BoardEntry
	{
		public string content;

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
			return EditOrUpdate();
		}

		public override bool EditOrUpdate()
		{
			return NotePopup.ShowPopup(s =>
			{
				this.content = s;
			});
		}

		public override bool IsValidForSearch(string[] filters)
		{
			return Utility.DoStringSearch(content, filters);
		}
	}
}
