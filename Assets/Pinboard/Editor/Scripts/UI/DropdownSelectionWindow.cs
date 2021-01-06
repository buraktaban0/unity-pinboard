using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard.Editor.UI
{
	public class DropdownSelectionWindow : EditorWindow
	{
		[MenuItem("Test/Test dropdown window")]
		public static void ShowTest()
		{
			Show(new List<string> {"Test 1", "Test 2/Child 1"}, s => { Debug.Log("Selected: " + s); },
			     "Select Control");
		}

		public static void Show(List<string> paths, Action<string> onResult, string title = null)
		{
			var window = ScriptableObject.CreateInstance<DropdownSelectionWindow>();

			var size = new Vector2(200, 100);
			window.minSize = size;
			window.maxSize = size;

			if (string.IsNullOrEmpty(title))
			{
				title = "Select";
			}

			window.titleContent = new GUIContent(title, PinboardResources.ICON_PINBOARD);

			window.Show();
		}


		//private string selection = null;

		private void OnEnable()
		{
			var root = rootVisualElement;

			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.alignItems = Align.Center;
			row.style.justifyContent = Justify.Center;

			var dropdown = new ToolbarMenu();
			dropdown.style.flexGrow = 1f;
			dropdown.style.height = 32f;
			dropdown.style.marginBottom = 4f;
			dropdown.style.marginTop = 4f;
			dropdown.style.marginLeft = 4f;
			dropdown.style.marginRight = 4f;
			dropdown.menu.AppendAction("Test 1", action => { });
			dropdown.menu.AppendAction("Test 2/Child 1", action => { });
			dropdown.text = "Select control...";

			row.Add(dropdown);


			root.Add(row);
		}

		private void OnDisable()
		{
		}
	}
}
