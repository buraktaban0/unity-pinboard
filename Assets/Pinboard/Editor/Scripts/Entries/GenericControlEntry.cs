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
		public string validatorName = "";
		public string getterName = "";
		public string setterName = "";
		public string name = "";
	}


	public class GenericControlEntry : Entry, ISerializationCallbackReceiver
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

			BindReflectedData();
		}

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			BindReflectedData();
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
			onResult?.Invoke(true);
		}

		public override bool EditOrUpdate(bool recordUndoState, Action<bool> onResult = null)
		{
			onResult?.Invoke(true);
			return false;
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


		private void BindReflectedData()
		{
			containingType = Type.GetType(definition.typeName);
			validator = containingType.GetMethod(definition.validatorName);
			getter = containingType.GetMethod(definition.getterName);
			setter = containingType.GetMethod(definition.setterName);
		}

		private object GetReflectedValue()
		{
			return getter.Invoke(null, null);
		}

		private void SetReflectedValue(object obj)
		{
			setter.Invoke(null, new[] {obj});
		}
	}
}
