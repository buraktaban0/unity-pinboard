// using System.Linq;
// using UnityEngine;
// using UnityEngine.UIElements;
//
// namespace Pinboard.Items
// {
// 	[System.Serializable]
// 	[EntryType(visibleName = "Menu Item Shortcut")]
// 	public class MenuItemShortcutEntry : Entry
// 	{
// 		public override string ShortVisibleName => $"/{Name}".Truncate();
//
// 		public string Name => path.Split('/').Last();
//
// 		public string path = "";
// 		
// 		
// 		public MenuItemShortcutEntry()
// 		{
// 			path = "";
// 		}
//
// 		public MenuItemShortcutEntry(string path)
// 		{
// 			this.IsDirty = true;
//
// 			this.path = path;
// 		}
//
// 		public override Texture GetIcon() => PinboardResources.ICON_MENU_OPEN;
//
// 		public override void BindVisualElement(VisualElement el)
// 		{
// 			var lbl = new Label(path);
// 			lbl.style.textOverflow = TextOverflow.Ellipsis;
// 			lbl.style.unityTextOverflowPosition = TextOverflowPosition.Middle;
// 			lbl.name = "menu-item-shortcut-path";
// 			el.Add(lbl);
//
// 			el.tooltip = $"Shortcut for menu item '{path}'";
// 		}
//
// 		public override void UnbindVisualElement(VisualElement el)
// 		{
// 			var lbl = el.Q<Label>("menu-item-shortcut-path");
// 			if (lbl != null)
// 			{
// 				el.Remove(lbl);
// 			}
// 		}
//
// 		public override bool Create()
// 		{
// 			popupTitle = "Create Note";
// 			return EditOrUpdate(false);
// 		}
//
// 		public override bool EditOrUpdate(bool recordUndoState)
// 		{
// 			if (isBeingEdited)
// 				return false;
//
// 			isBeingEdited = true;
//
// 			if (recordUndoState)
// 				PinboardDatabase.Current.WillModifyEntry(this);
//
// 			var wasEdited = TextEditPopup.ShowPopup(popupTitle, this.content, s =>
// 			{
// 				popupTitle = "Update Note";
// 				this.content = s;
// 			});
// 			isBeingEdited = false;
//
// 			if (wasEdited)
// 				IsDirty = true;
//
// 			return wasEdited;
// 		}
//
// 		public override void OnDoubleClick()
// 		{
// 			base.OnDoubleClick();
//
// 			if (this.EditOrUpdate(true))
// 			{
// 				//PinboardDatabase.SaveBoards();
// 			}
// 		}
//
// 		public override void PopulateContextualMenu(ContextualMenuPopulateEvent evt)
// 		{
// 			base.PopulateContextualMenu(evt);
//
// 			evt.menu.AppendAction("Edit", action => { this.EditOrUpdate(true); });
// 			evt.menu.AppendAction("Copy", action =>
// 			{
// 				PinboardClipboard.Entry = this;
// 				PinboardClipboard.SystemBuffer = this.content;
// 			});
// 		}
//
// 		public override Entry Clone()
// 		{
// 			var clone = new NoteEntry(content);
// 			clone.IsDirty = true;
// 			return clone;
// 		}
//
// 		public override bool IsValidForSearch(string[] filters)
// 		{
// 			return Utility.DoStringSearch(content, filters);
// 		}
// 	}
// }
