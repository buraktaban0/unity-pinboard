using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard.Entries
{
	[System.Serializable]
	public class GenericControlDefinition
	{
		public string typeName = "";
		public string methodName = "";
		public string name = "";
	}


	public class GenericControlEntry : Entry
	{
		public GenericControlDefinition definition;

		public string ControlName => definition.name.Split('/').Last();

		public override string ShortVisibleName => ControlName;

		private Type containingType;
		private Type valueType;
		private MethodInfo getter;
		private MethodInfo setter;
		private MethodInfo validator;

		public GenericControlEntry(GenericControlDefinition definition)
		{
			this.definition = definition;

			containingType = Type.GetType(definition.typeName);
		}


		public override void BindVisualElement(VisualElement el)
		{
			throw new NotImplementedException();
		}

		public override void UnbindVisualElement(VisualElement el)
		{
			throw new NotImplementedException();
		}

		public override void Create(Action<bool> onResult)
		{
			throw new NotImplementedException();
		}

		public override bool EditOrUpdate(bool recordUndoState, Action<bool> onResult = null)
		{
			throw new NotImplementedException();
		}

		public override void CopySelfToClipboard()
		{
			PinboardClipboard.Entry = this;
		}

		public override Entry Clone()
		{
			var clone = new GenericControlEntry(this.definition);
			return clone;
		}
	}
}
