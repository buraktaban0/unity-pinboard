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

		public const string CLASS_LIST_ITEM_ROOT = "list-item-root";
		public const string CLASS_BOARD_TOOLBAR = "board-toolbar";

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

		private Toolbar boardToolbar;
		private Label boardNameLabel;

		private ListView itemsList;

		private List<BoardItem> visibleItems;


		private void OnEnable()
		{
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PATH_PINBOARD_UXML);

			root = rootVisualElement;
			uxml.CloneTree(root);

			var css = AssetDatabase.LoadAssetAtPath<StyleSheet>(PATH_PINBOARD_USS);
			root.styleSheets.Add(css);

			root = root.Q<VisualElement>("root");

			AddMainToolbar();

			AddBoardToolbar();

			visibleItems = currentBoard?.items;

			MakeScrollList();

			Database.onBoardsModified += OnBoardsModified;
		}

		private void ListGeoChanged(GeometryChangedEvent evt)
		{
			var board = new Board();
			board.Add(new SimpleTextItem("Test Item xxx"));
			board.Add(new SimpleTextItem("Test Item xxx1"));
			board.Add(new SimpleTextItem("Test Item xxx2"));
			board.Add(new SimpleTextItem("Test Item xxx 3"));

			var ser = new SerializedBoard(board);
			var json = JsonUtility.ToJson(ser, true);
			Debug.Log(json);
			
			SetBoard(board);
		}

		private void OnDisable()
		{
			Database.onBoardsModified -= OnBoardsModified;
		}


		private void AddMainToolbar()
		{
			var toolbar = new Toolbar();

			boardsDropdown = new ToolbarMenu();
			boardsDropdown.style.minWidth = 84;
			toolbar.Add(boardsDropdown);
			UpdateBoardsMenu();

			toolbar.Add(new ToolbarSpacer());
			toolbar.Add(new ToolbarSpacer());

			searchField = new ToolbarSearchField();
			searchField.style.maxWidth = 164;
			searchField.RegisterValueChangedCallback(OnSearchValueChanged);
			toolbar.Add(searchField);

			rootVisualElement.Insert(0, toolbar);
		}

		public void AddBoardToolbar()
		{
			var toolbar = new Toolbar();
			boardToolbar = toolbar;
			toolbar.AddToClassList(CLASS_BOARD_TOOLBAR);

			toolbar.AddManipulator(new ContextualMenuManipulator(pop =>
			{
				pop.menu.AppendAction("Log1", action => { Debug.Log(action.name); });
				pop.menu.AppendAction("Log2", action => { Debug.Log(action.name); });
			}));

			toolbar.AddManipulator(new ClickActionsManipulator(
				                       () => {  }, () => { Debug.Log("double click"); }));

			boardNameLabel = new Label("");
			boardNameLabel.style.marginLeft = 4;
			toolbar.Add(boardNameLabel);

			rootVisualElement.Insert(1, toolbar);
		}

		public void UpdateBoardToolbar()
		{
			if (currentBoard == null)
			{
				boardNameLabel.text = "";
				boardToolbar.tooltip = "";
			}
			else
			{
				boardNameLabel.text = currentBoard.title;
				boardToolbar.tooltip = $"Title: {currentBoard.title}" +
				                         $"\nCreated by: {currentBoard.createdBy}" +
				                         $"\nCreation date: {currentBoard.CreationTime.ToShortDateString()}, {currentBoard.CreationTime.ToShortTimeString()}" 
				                         /*+ $"\nUnique ID: {currentBoard.id}"*/;
			}
			
		}


		private void OnSearchValueChanged(ChangeEvent<string> evt)
		{
			var str = evt.newValue;

			if (currentBoard == null)
				return;

			if (string.IsNullOrEmpty(str))
			{
				visibleItems = currentBoard.items;
			}
			else
			{
				var filters = new string[] {str.ToLower()};
				visibleItems = currentBoard.items.Where(item => item.IsValidForSearch(filters)).ToList();
			}

			itemsList.itemsSource = visibleItems;
			itemsList.Refresh();
		}


		private void MakeScrollList()
		{
			itemsList = new ListView();
			itemsList.style.flexGrow = 1f;
			itemsList.makeItem = MakeItem;
			itemsList.bindItem = BindItem;
			// itemsList.unbindItem = UnbindItem;
			itemsList.reorderable = false;
			itemsList.itemHeight = 22;
			itemsList.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
			itemsList.selectionType = SelectionType.None;
			itemsList.onSelectionChange += OnItemSelectionChange;
			itemsList.RegisterCallback<GeometryChangedEvent>(ListGeoChanged);

			root.Add(itemsList);
		}

		private VisualElement MakeItem()
		{
			var root = new VisualElement();
			root.AddToClassList(CLASS_LIST_ITEM_ROOT);


			var img = new Image();
			img.style.width = 15;
			img.style.height = 15;
			img.style.marginLeft = 2;
			img.style.marginRight = 6;
			root.Add(img);
			return root;
		}

		private void BindItem(VisualElement element, int index)
		{
			visibleItems[index].BindVisualElement(element);
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
				visibleItems = board.items;
				itemsList.itemsSource = board.items;
			}
			
			UpdateBoardToolbar();

			itemsList.Refresh();
			UpdateBoardsMenu();
		}
	}
}
