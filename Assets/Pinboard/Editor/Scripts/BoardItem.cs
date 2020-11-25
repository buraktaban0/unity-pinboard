using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard
{
	[System.Serializable]
	public abstract class BoardItem
	{
		
		public string id = Guid.Get();

		public string author = PinboardCore.User;

		public long createdAt = Utility.GetUnixTimestamp();
		public DateTime CreationTime => Utility.FromUnixToLocal(createdAt);

		public abstract void BindVisualElement(VisualElement el);

		public abstract void UnbindVisualElement(VisualElement el);
		
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

	}
}
