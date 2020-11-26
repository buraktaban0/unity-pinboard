using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard
{
	public class NotePopup : EditorWindow
	{
		public static bool ShowPopup(Action<string> onEditDone)
		{
			var window = ScriptableObject.CreateInstance<NotePopup>();

			window.titleContent = new GUIContent("Create Note");

			var size = new Vector2(320, 80);
			window.minSize = size;
			window.maxSize = size;

			window.onEditDone = onEditDone;

			window.ShowModal();

			return window.wasDone;
		}


		private Action<string> onEditDone;

		private bool wasDone = false;

		private void OnEnable()
		{
			var root = this.rootVisualElement;

			root.style.paddingLeft = 8;
			root.style.paddingRight = 8;

			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.justifyContent = Justify.Center;
			row.style.alignContent = Align.Center;
			row.style.alignItems = Align.Center;

			var inputField = new TextField(256, true, false, '*');
			row.Add(inputField);

			var rowButton = new VisualElement();
			rowButton.style.flexDirection = FlexDirection.Row;
			rowButton.style.alignItems = Align.FlexEnd;

			var button = new Button(() =>
			{
				wasDone = true;
				onEditDone.Invoke(inputField.value.Trim());
				this.Close();
			}) {text = "Done"};

			button.SetEnabled(false);

			rowButton.Add(button);

			inputField.RegisterCallback<ChangeEvent<string>>(evt =>
			{
				var s = evt.newValue.Trim();
				if (s.Length < 1)
				{
					button.SetEnabled(false);
					return;
				}

				button.SetEnabled(true);
			});

			root.Add(row);
			root.Add(rowButton);
		}


		private static void OnClickCreate()
		{
		}
	}
}
