using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard
{
	public class CreateBoardOptions
	{
		public string title;
		public BoardAccessibility accessibility = BoardAccessibility.ProjectPrivate;

		public CreateBoardOptions(string title, BoardAccessibility accessibility)
		{
			this.title = title;
			this.accessibility = accessibility;
		}
	}

	public class CreateBoardPopup : EditorWindow
	{
		private static Action<CreateBoardOptions> boardCreateResultCallback;

		public static void ShowDialog()
		{
			var window = GetWindow<CreateBoardPopup>();

			window.titleContent = new GUIContent("Create Board");

			var size = new Vector2(350, 110);
			window.minSize = size;
			window.maxSize = size;

			window.ShowUtility();
		}


		private void OnEnable()
		{
			var root = this.rootVisualElement;

			root.style.paddingLeft = 8;
			root.style.paddingRight = 8;

			var dropdown = new ToolbarMenu();
			dropdown.style.flexGrow = 0;
			dropdown.style.flexShrink = 0;
			dropdown.style.width = 110;

			var values = (Enum.GetValues(typeof(BoardAccessibility)) as BoardAccessibility[]).ToList();
			var names = Enum.GetNames(typeof(BoardAccessibility)).Select(n => n.SplitCamelCase()).ToList();
			var descs = values.Select(BoardAccessibilityDesc.Get).ToList();

			dropdown.text = names[0];
			dropdown.variant = ToolbarMenu.Variant.Popup;

			BoardAccessibility accessibility = values[0];

			var descLbl = new Label(descs[0]);
			descLbl.style.marginLeft = 8;
			descLbl.style.flexShrink = 1;
			descLbl.style.flexWrap = Wrap.Wrap;
			descLbl.style.whiteSpace = WhiteSpace.Normal;

			for (var i = 0; i < values.Count; i++)
			{
				var val = values[i];
				var name = names[i];
				dropdown.menu.AppendAction(name, action =>
				                           {
					                           accessibility = val;
					                           descLbl.text = BoardAccessibilityDesc.Get(val);
					                           dropdown.text = name;
				                           },
				                           action => action.name == dropdown.text
					                           ? DropdownMenuAction.Status.Checked
					                           : DropdownMenuAction.Status.Normal,
				                           DropdownMenuAction.Status.Normal);
			}


			var row0 = new VisualElement();
			row0.style.height = 20;
			row0.style.marginTop = 8;
			row0.style.marginBottom = 8;
			row0.style.flexDirection = FlexDirection.Row;
			row0.style.alignItems = Align.Center;
			//row0.style.justifyContent = Justify.SpaceBetween;


			row0.Add(dropdown);
			row0.Add(descLbl);
			root.Add(row0);

			var row1 = new VisualElement();
			row1.style.marginTop = 8;
			row1.style.marginBottom = 8;
			var nameField = new TextField(64, false, false, '*') {value = "New Board"};

			row1.Add(nameField);

			root.Add(row1);

			var row2 = new VisualElement();
			row2.style.alignSelf = Align.FlexEnd;
			row2.style.marginTop = 8;
			row2.style.marginBottom = 8;
			row2.style.flexDirection = FlexDirection.Row;
			row2.style.justifyContent = Justify.SpaceBetween;

			var cancelButton = new Button(() => { this.Close(); }) {text = "Cancel"};
			var createButton = new Button(() =>
			{
				var createBoardOptions = new CreateBoardOptions(nameField.value, accessibility);
				PinboardCore.TryCreateBoard(createBoardOptions);
				this.Close();
			}) {text = "Create"};


			//row2.Add(cancelButton);
			row2.Add(createButton);

			root.Add(row2);
		}


		private static void OnClickCreate()
		{
		}
	}
}
