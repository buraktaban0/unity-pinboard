using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Pinboard
{
	public class TextEditPopup : EditorWindow
	{
		private static HashSet<object> currentTargets = new HashSet<object>(); 
		
		public static void ShowPopup(object target, string title, string initialValue, Action<string> onEditDone)
		{
			if (currentTargets.Add(target) == false)
				return;
			
			var window = ScriptableObject.CreateInstance<TextEditPopup>();

			window.titleContent = new GUIContent(title);

			var size = new Vector2(320, 110);
			window.minSize = size;
			window.maxSize = size;
			//window.maxSize = new Vector2(640, 500);
			//window.maxSize = size;

			window.target = target;

			window.onEditDone = onEditDone;

			window.textField.value = initialValue;

			window.ShowUtility();
		}


		private object target;
		
		private Action<string> onEditDone;

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
			row.style.flexGrow = 1f;

			row.style.alignItems = Align.FlexStart;

			textField = new TextField(256, true, false, '*');
			textField.style.flexGrow = 1f;
			textField.style.flexShrink = 1f;
			textField.style.alignSelf = Align.Stretch;
			textField.Q("unity-text-input").style.whiteSpace = WhiteSpace.Normal;
			textField.Q("unity-text-input").style.unityTextAlign = TextAnchor.UpperLeft;
			textField.Q("unity-text-input").style.alignSelf = Align.Stretch;
			row.Add(textField);

			var copyButton = new Button(() =>
			{
				PinboardClipboard.SystemBuffer = textField.value;
				textField.Q("unity-text-input").Focus();
			});
			copyButton.AddToClassList("icon-button");
			copyButton.tooltip = "Copy";
			copyButton.style.width = 24;
			copyButton.style.height = 23;
			copyButton.style.paddingBottom = 2;
			copyButton.style.paddingTop = 2;
			copyButton.style.paddingRight = 2;
			copyButton.style.paddingLeft = 2;

			var pasteButton = new Button(() =>
			{
				textField.value = PinboardClipboard.SystemBuffer;
				textField.Q("unity-text-input").Focus();
			});
			pasteButton.AddToClassList("icon-button");
			//
			pasteButton.tooltip = "Paste";
			pasteButton.style.width = 24;
			pasteButton.style.height = 23;
			pasteButton.style.paddingBottom = 2;
			pasteButton.style.paddingTop = 2;
			pasteButton.style.paddingRight = 2;
			pasteButton.style.paddingLeft = 2;
			
			var pasteImg = new Image();
			pasteImg.image = PinboardResources.ICON_PASTE;
			pasteButton.Add(pasteImg);

			var copyImg = new Image();
			copyImg.image = PinboardResources.ICON_COPY;
			copyButton.Add(copyImg);

			var col = new VisualElement();
			col.Add(copyButton);
			col.Add(pasteButton);

			row.Add(col);

			var rowButton = new VisualElement();
			rowButton.style.flexDirection = FlexDirection.Row;
			rowButton.style.alignItems = Align.Center;
			rowButton.style.justifyContent = Justify.FlexEnd;

			var button = new Button(() =>
			{
				var val = textField.value.Trim();
				PinboardCore.RunNextFrame(()=> onEditDone.Invoke(val));
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
				//size.x = this.minSize.x;

				if (size.y > this.minSize.y)
				{
				}
				this.minSize = size;
				//this.maxSize = size;

				button.SetEnabled(textField.value.Trim().Length >= 1);
			});
		}

		private void OnDisable()
		{
			currentTargets.Remove(this.target);
		}


		private static void OnClickCreate()
		{
		}
	}
}
