using System;
using System.Collections.Generic;
using System.Linq;
using Pinboard.Items;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pinboard
{
	public class PinboardCore : EditorWindow
	{
		public const string DIR_ROOT = "Assets/Pinboard/";
		public const string DIR_EDITOR = DIR_ROOT + "Editor/";
		public const string DIR_UI = DIR_EDITOR + "UI/";

		public const string PATH_PINBOARD_UXML = DIR_UI + "Pinboard.uxml";
		public const string PATH_PINBOARD_USS = DIR_UI + "Pinboard.uss";

		private static string user = null;

		public static string User
		{
			get
			{
				if (string.IsNullOrEmpty(user))
				{
					user = Utility.GetUserName();
				}

				return user;
			}
		}


		static PinboardCore()
		{
			user = Utility.GetUserName();
		}


		[MenuItem("Pinboard/Test")]
		public static void Open()
		{
			var window = GetWindow<PinboardCore>();

			window.minSize = new Vector2(250, 100);
			window.titleContent = new GUIContent("Pinboard");

			window.Refresh();
		}


		public Board currentBoard;

		private VisualElement root;
		private ToolbarMenu boardsDropdown;
		private ToolbarSearchField searchField;
		private ListView itemsList;


		private void OnEnable()
		{
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PATH_PINBOARD_UXML);

			root = rootVisualElement;
			uxml.CloneTree(root);

			var css = AssetDatabase.LoadAssetAtPath<StyleSheet>(PATH_PINBOARD_USS);
			root.styleSheets.Add(css);

			root = root.Q<VisualElement>("root");

			AddToolbar();

			itemsList = new ListView();

			itemsList.makeItem = MakeItem;
			itemsList.bindItem = BindItem;
			// itemsList.unbindItem = UnbindItem;
			itemsList.reorderable = false;
			itemsList.itemHeight = 32;
			itemsList.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
			itemsList.selectionType = SelectionType.None;
			itemsList.onSelectionChange += OnItemSelectionChange;

			root.Add(itemsList);

			var board = new Board();
			board.Add(new SimpleTextItem("Test Item xxx"));
			board.Add(new SimpleTextItem("Test Item xxx1"));
			board.Add(new SimpleTextItem("Test Item xxx2"));
			board.Add(new SimpleTextItem("Test Item xxx 3"));

			SetBoard(board);

			Database.onBoardsModified += OnBoardsModified;
		}


		private void AddToolbar()
		{
			var toolbar = new Toolbar();

			// var addButton = new ToolbarButton(OnAddClicked);
			// addButton.text = "Add";
			// toolbar.Add(addButton);

			boardsDropdown = new ToolbarMenu();
			toolbar.Add(boardsDropdown);
			UpdateBoardsMenu();

			toolbar.Add(new ToolbarSpacer());
			toolbar.Add(new ToolbarSpacer());

			searchField = new ToolbarSearchField();
			toolbar.Add(searchField);

			rootVisualElement.Insert(0, toolbar);
		}


		private VisualElement MakeItem()
		{
			var root = new VisualElement();
			root.style.flexDirection = FlexDirection.Row;
			root.style.justifyContent = Justify.Center;
			root.style.alignContent = Align.FlexStart;
			return root;
		}

		private void BindItem(VisualElement element, int index)
		{
			currentBoard.items[index].BindVisualElement(element);
		}

		// private void UnbindItem(VisualElement element, int index)
		// {
		// 	currentBoard.items[index].UnbindVisualElement(element)
		// }


		private void OnItemSelectionChange(IEnumerable<object> selection)
		{
			if (!selection.Any())
				return;

			var item = selection.First();
		}

		private void UpdateBoardsMenu()
		{
			boardsDropdown.text = currentBoard != null ? currentBoard.title : "Boards";
			boardsDropdown.variant = ToolbarMenu.Variant.Default;

			boardsDropdown.menu.MenuItems().Clear();

			for (int i = 0; i < Database.boards.Count; i++)
			{
				var board = Database.boards[i];
				boardsDropdown.menu.AppendAction(board.title, action =>
				                                 {
					                                 if (action.name == board.title)
					                                 {
						                                 SetBoard(board);
					                                 }
				                                 },
				                                 board == currentBoard
					                                 ? DropdownMenuAction.Status.Checked
					                                 : DropdownMenuAction.Status.Normal);
			}

			boardsDropdown.menu.AppendSeparator();
			boardsDropdown.menu.AppendAction("New...", TryCreateNewBoard);
		}

		private void TryCreateNewBoard(DropdownMenuAction action)
		{
			Debug.Log("Create board");
		}

		private void OnBoardsModified()
		{
			Refresh();
		}


		public void Refresh()
		{
			if (Database.boards.Count < 1)
			{
				SetBoard(null);
				return;
			}

			var lastOpenId = PinboardPrefs.LastOpenBoardID;
			Board firstBoard = Database.boards.FirstOrDefault();
			if (string.IsNullOrEmpty(lastOpenId))
			{
				if (firstBoard == null)
				{
					SetBoard(null);
					return;
				}

				SetBoard(firstBoard);
			}
			else
			{
				var lastOpenBoard = Database.GetBoard(lastOpenId);
				if (lastOpenBoard == null)
				{
					SetBoard(null);
					return;
				}

				SetBoard(lastOpenBoard);
			}
		}


		public void SetBoard(Board board)
		{
			currentBoard = board;

			if (board == null)
			{
				PinboardPrefs.LastOpenBoardID = string.Empty;
				itemsList.itemsSource = new List<BoardItem>();
			}
			else
			{
				PinboardPrefs.LastOpenBoardID = board.id;
				itemsList.itemsSource = board.items;
			}

			itemsList.Refresh();
			UpdateBoardsMenu();
		}
	}
}
