using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard
{
	// public abstract class Entry<T> : Entry where T : Entry<T>, new()
	// {
	// 	public static T CreateInstance()
	// 	{
	// 		var t = new T();
	// 		t.IsDirty = true;
	// 		return t;
	// 	}
	// }

	[System.Serializable]
	public abstract class Entry
	{
		public bool IsDirty { get; set; } = false;

		public abstract string ShortVisibleName { get; }

		[NonSerialized]
		public Board board;

		public string id = Guid.Get();

		public string author = PinboardCore.User;

		public long createdAt = Utility.GetUnixTimestamp();
		public DateTime CreationTime => Utility.FromUnixToLocal(createdAt);

		public abstract Texture GetIcon();

		public abstract void BindVisualElement(VisualElement el);

		public abstract void UnbindVisualElement(VisualElement el);

		public abstract bool Create();

		public abstract bool EditOrUpdate(bool recordUndoState);

		public virtual void OnClick()
		{
		}

		public virtual void OnDoubleClick()
		{
		}

		public virtual bool IsValidForSearch(string[] filters)
		{
			return false;
		}

		public virtual void PopulateContextualMenu(ContextualMenuPopulateEvent evt)
		{
			
		}

		public abstract void CopySelfToClipboard();

		public abstract Entry Clone();
	}
}
