using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard
{
	public class CreateBoardOptions
	{
		public string title = "New Board";
		public BoardAccessibility accessibility = BoardAccessibility.ProjectPrivate;
	}

	public class CreateBoardPopup : EditorWindow
	{
		private static Action<CreateBoardOptions> boardCreateResultCallback;

		private static void Show(Action<CreateBoardOptions> boardCreateResultCallback)
		{
			var window = GetWindow<CreateBoardPopup>();
			window.titleContent = new GUIContent("Create Board");

			window.ShowModal();

			var uxmlPath = PinboardCore.DIR_UI + "/CreateBoard.uxml";
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);

			var root = window.rootVisualElement;
			uxml.CloneTree(root);

			root.Q<Button>("cancel");

		}

		private void OnGUI()
		{
		}
	}
}
