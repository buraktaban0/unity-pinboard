using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard
{
	public class TextEditPopup : EditorWindow
	{
		public static bool ShowPopup(string title, string initialValue, Action<string> onEditDone)
		{
			var window = ScriptableObject.CreateInstance<TextEditPopup>();

			window.titleContent = new GUIContent(title);

			var size = new Vector2(320, 80);
			window.minSize = size;
			window.maxSize = size;

			window.onEditDone = onEditDone;

			window.textField.value = initialValue;

			window.ShowModal();			

			return window.wasDone;
		}


		private Action<string> onEditDone;

		private bool wasDone = false;

		private TextField textField;


		private void OnEnable()
		{
			var root = this.rootVisualElement;

			root = new VisualElement();
			root.style.flexGrow = 1f;
			root.style.flexShrink = 0f;

			rootVisualElement.Add(root);

			root.style.paddingLeft = 8;
			root.style.paddingTop = 8;
			root.style.paddingBottom = 8;
			root.style.paddingRight = 8;

			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.justifyContent = Justify.Center;
			row.style.alignContent = Align.Center;
			row.style.alignItems = Align.Center;
			row.style.marginBottom = 8;

			textField = new TextField(256, true, false, '*');
			textField.style.flexGrow = 1f;
			textField.Q("unity-text-input").style.whiteSpace = WhiteSpace.Normal;
			row.Add(textField);

			var rowButton = new VisualElement();
			rowButton.style.flexDirection = FlexDirection.Row;
			rowButton.style.alignItems = Align.Center;
			rowButton.style.justifyContent = Justify.FlexEnd;

			var button = new Button(() =>
			{
				wasDone = true;
				onEditDone.Invoke(textField.value.Trim());
				this.Close();
			}) {text = "Done"};


			rowButton.Add(button);

			textField.RegisterCallback<ChangeEvent<string>>(evt =>
			{
				var s = evt.newValue.Trim();
				if (s.Length < 1)
				{
					button.SetEnabled(false);
					return;
				}

				button.SetEnabled(true);
			});

			textField.RegisterCallback<AttachToPanelEvent>(evt => { textField.Q("unity-text-input").Focus(); });

			root.Add(row);
			root.Add(rowButton);

			root.RegisterCallback<GeometryChangedEvent>(evt =>
			{
				var w = root.resolvedStyle.width;
				var h = root.resolvedStyle.height;

				var size = new Vector2(w, h);
				this.minSize = size;
				this.maxSize = size;
				
				button.SetEnabled(textField.value.Trim().Length >= 1);
			});
		}


		private static void OnClickCreate()
		{
		}
	}
}
