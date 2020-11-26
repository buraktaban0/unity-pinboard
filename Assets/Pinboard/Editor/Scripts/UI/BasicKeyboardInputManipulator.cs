using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard
{
	public class BasicKeyboardInputManipulator : Manipulator
	{
		public EditorWindow window;
		public Action done;


		public BasicKeyboardInputManipulator(EditorWindow window, Action done)
		{
			this.window = window;
			this.done = done;
		}


		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<KeyDownEvent>(evt =>
			{
				switch (evt.keyCode)
				{
					case KeyCode.Escape:
						window.Close();
						break;
					case KeyCode.Return:
						done?.Invoke();
						window.Close();
						break;
				}
			});
		}


		protected override void UnregisterCallbacksFromTarget()
		{
		}
	}
}
