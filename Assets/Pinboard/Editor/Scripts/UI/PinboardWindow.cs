using System;
using System.Collections.Generic;
using System.Linq;
using Pinboard.Entries;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pinboard
{
	public class PinboardWindow : EditorWindow
	{
		public const string DIR_ROOT = "Assets/Pinboard/";
		public const string DIR_EDITOR = DIR_ROOT + "Editor/";
		public const string DIR_UI = DIR_EDITOR + "UI/";

		public const string PATH_PINBOARD_UXML = DIR_UI + "Pinboard.uxml";
		public const string PATH_PINBOARD_USS = DIR_UI + "Pinboard.uss";

		public const string CLASS_LIST_ITEM_ROOT = "list-item-root";
		public const string CLASS_BOARD_TOOLBAR = "board-toolbar";

		public static PinboardWindow Instance;

		[MenuItem("Pinboard/Test")]
		public static void Open()
		{
			var window = GetWindow<PinboardWindow>();

			window.minSize = new Vector2(250, 100);
			window.titleContent = new GUIContent("Pinboard");

			Instance = window;

			window.Show();
		}


		public Board currentBoard;

		private VisualElement root;

		private ToolbarMenu boardsDropdown;
		private ToolbarSearchField searchField;

		private Toolbar boardToolbar;
		private Label boardNameLabel;
		private ToolbarMenu addMenu;

		private ContextualMenuManipulator boardToolbarContextualManipulator;

		private ListView itemsList;

		private List<Entry> visibleItems;


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

			visibleItems = currentBoard?.entries;

			MakeScrollList();

			PinboardDatabase.onBoardAdded += OnBoardAdded;
			PinboardDatabase.onBoardDeleted += OnBoardDeleted;
			PinboardDatabase.onDatabaseModified += OnBoardsModified;
			PinboardDatabase.onDatabaseSaved += OnDatabaseSaved;

			EditorSceneManager.sceneLoaded += OnEditorSceneLoaded;
			EditorSceneManager.sceneOpened += OnEditorSceneOpened;

			this.Refresh();
		}

		private void OnDisable()
		{
			PinboardDatabase.onBoardAdded -= OnBoardAdded;
			PinboardDatabase.onBoardDeleted -= OnBoardDeleted;
			PinboardDatabase.onDatabaseModified -= OnBoardsModified;
			PinboardDatabase.onDatabaseSaved -= OnDatabaseSaved;

			EditorSceneManager.sceneLoaded -= OnEditorSceneLoaded;
			EditorSceneManager.sceneOpened -= OnEditorSceneOpened;

		}

		private void OnEditorSceneOpened(Scene scene, OpenSceneMode mode)
		{
			Refresh();
		}

		private void OnEditorSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Refresh();
		}


		private void OnDatabaseSaved()
		{
			Refresh();
		}

		private void OnBoardsModified()
		{
			Refresh();
		}


		private void OnBoardDeleted(Board board)
		{
			Refresh();
		}

		private void OnBoardAdded(Board board)
		{
			Refresh();
			SetBoard(board);
		}

		private void ListGeoChanged(GeometryChangedEvent evt)
		{
			// var board = new Board();
			// board.Add(new SimpleTextItem("Test Item xxx"));
			// board.Add(new SimpleTextItem("Test Item xxx1"));
			// board.Add(new SimpleTextItem("Test Item xxx2"));
			// board.Add(new SimpleTextItem("Test Item xxx 3"));
			//
			// var ser = new SerializedBoard(board);
			// var json = JsonUtility.ToJson(ser, true);
			// Debug.Log(json);
			//
			// SetBoard(board);
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


		private void UpdateBoardsMenu()
		{
			boardsDropdown.text = currentBoard != null ? currentBoard.title : "Boards";
			boardsDropdown.variant = ToolbarMenu.Variant.Default;

			boardsDropdown.menu.MenuItems().Clear();

			for (int i = 0; i < PinboardDatabase.Current.BoardCount; i++)
			{
				var board = PinboardDatabase.Current.Boards[i];
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
			CreateBoardPopup.ShowDialog();
		}


		public void AddBoardToolbar()
		{
			var toolbar = new Toolbar();
			boardToolbar = toolbar;
			toolbar.AddToClassList(CLASS_BOARD_TOOLBAR);

			boardToolbarContextualManipulator = new ContextualMenuManipulator(pop =>
			{
				pop.menu.AppendAction("Rename Board", action =>
				{
					TextEditPopup.ShowPopup("Rename Board", currentBoard.title, s =>
					{
						if (currentBoard.title == s.Trim())
							return;

						PinboardDatabase.Current.WillModifyBoard(currentBoard);
						currentBoard.title =
							s.Trim().CorrectlyEnumerate(PinboardDatabase.Current.Boards.Select(b => b.title));
						currentBoard.IsDirty = true;
					});
				});
				pop.menu.AppendAction("Delete Board", action =>
				{
					var delete = EditorUtility.DisplayDialog("Delete Board",
					                                         $"Do you want to continue to delete \"{currentBoard.title}\"?",
					                                         "Yes", "No");

					if (delete)
					{
						PinboardDatabase.Current.DeleteBoard(currentBoard);
					}
				});
			});

			toolbar.AddManipulator(boardToolbarContextualManipulator);

			toolbar.AddManipulator(new ClickActionsManipulator(
				                       () => { }, () => { Debug.Log("double click"); }));

			boardNameLabel = new Label("");
			boardNameLabel.style.marginLeft = 4;
			//toolbar.Add(boardNameLabel);

			addMenu = new ToolbarMenu();
			addMenu.Q<TextElement>().RemoveFromHierarchy();

			var addIcon = new Image();
			addIcon.style.width = 10;
			addIcon.style.height = 10;
			addIcon.image = PinboardResources.ICON_ADD;
			addMenu.Insert(0, addIcon);
			addMenu.style.alignItems = Align.Center;

			for (var i = 0; i < PinboardCore.EntryTypes.Count; i++)
			{
				var entryType = PinboardCore.EntryTypes[i];
				var entryName = PinboardCore.EntryTypeNames[i];
				addMenu.menu.AppendAction(entryName, action => { PinboardCore.TryCreateEntry(entryType); });
			}

			toolbar.Add(addMenu);

			rootVisualElement.Insert(1, toolbar);
		}

		public void UpdateBoardToolbar()
		{
			if (currentBoard == null)
			{
				boardNameLabel.text = "";
				boardToolbar.tooltip = "";
				addMenu.SetEnabled(false);
				boardToolbar.RemoveManipulator(boardToolbarContextualManipulator);
			}
			else
			{
				boardNameLabel.text = currentBoard.title;
				boardToolbar.tooltip = $"Title: {currentBoard.title}" +
				                       $"\nCreated by: {currentBoard.createdBy}" +
				                       $"\nCreation date: {currentBoard.CreationTime.ToShortDateString()}, {currentBoard.CreationTime.ToShortTimeString()}"
					/*+ $"\nUnique ID: {currentBoard.id}"*/;
				addMenu.SetEnabled(true);
				boardToolbar.AddManipulator(boardToolbarContextualManipulator);
			}
		}


		private void OnSearchValueChanged(ChangeEvent<string> evt)
		{
			ValidateSearch();
		}

		private void ValidateSearch()
		{
			var str = searchField.value;

			if (currentBoard == null)
				return;

			if (string.IsNullOrEmpty(str))
			{
				visibleItems = currentBoard.entries;
			}
			else
			{
				var filters = new string[] {str.ToLower()};
				visibleItems = currentBoard.entries.Where(item => item.IsValidForSearch(filters)).ToList();
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
			//itemsList.reorderable = true;
			itemsList.itemHeight = 22;
			//itemsList.showBorder = true;
			//itemsList.showBoundCollectionSize = true;
			//itemsList.selectionType = SelectionType.Single;
			//itemsList.selectionType = SelectionType.Multiple;
			itemsList.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
			itemsList.onSelectionChange += OnEntrySelectionChange;
			itemsList.RegisterCallback<GeometryChangedEvent>(ListGeoChanged);

			root.Add(itemsList);
		}

		private VisualElement MakeItem()
		{
			var root = new VisualElement();
			root.AddToClassList(CLASS_LIST_ITEM_ROOT);

			root.AddManipulator(new ContextualMenuManipulator(evt =>
			{
				var entry = root.userData as Entry;
				entry.PopulateContextualMenu(evt);

				evt.menu.AppendSeparator();

				evt.menu.AppendAction("Copy", action => { entry.CopySelfToClipboard(); });

				evt.menu.AppendAction("Delete",
				                      action =>
				                      {
					                      var b = EditorUtility.DisplayDialog(
						                      "Deleting Board Entry",
						                      "Are you sure you want to delete this entry?",
						                      "Yes", "No");

					                      if (b)
					                      {
						                      PinboardCore.TryDeleteEntry(
							                      root.userData as Entry,
							                      currentBoard);
						                      Refresh();
					                      }
				                      });
				if (PinboardClipboard.Entry != null)
				{
					evt.menu.AppendAction($"Paste ({PinboardClipboard.Entry.ShortVisibleName})", action =>
					{
						PinboardCore.TryCreateEntry(PinboardClipboard.Entry);
						Refresh();
					});
				}
			}));

			root.AddManipulator(new ClickActionsManipulator(() => { (root.userData as Entry).OnClick(); },
			                                                () => { (root.userData as Entry).OnDoubleClick(); }));
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
			element.userData = visibleItems[index];
			element.Q<Image>().image = visibleItems[index].GetIcon();
			visibleItems[index].BindVisualElement(element);
		}


		// private void UnbindItem(VisualElement element, int index)
		// {
		// 	currentBoard.items[index].UnbindVisualElement(element)
		// }

		private void OnEntrySelectionChange(IEnumerable<object> selection)
		{
			if (!selection.Any())
				return;

			var item = selection.First();
		}


		public void Refresh()
		{
			//PinboardDatabase.Current.Load();

			if (PinboardDatabase.Current.BoardCount < 1)
			{
				SetBoard(null);
				return;
			}

			var lastOpenId = PinboardPrefs.LastOpenBoardID;
			Board firstBoard = PinboardDatabase.Current.Boards.FirstOrDefault();
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
				var lastOpenBoard = PinboardDatabase.Current.GetBoard(lastOpenId);
				if (lastOpenBoard == null)
				{
					SetBoard(null);
					return;
				}

				SetBoard(lastOpenBoard);
			}

			ValidateSearch();
		}


		public void SetBoard(Board board)
		{
			currentBoard = board;

			if (board == null)
			{
				PinboardPrefs.LastOpenBoardID = string.Empty;
				itemsList.itemsSource = new List<Entry>();
			}
			else
			{
				PinboardPrefs.LastOpenBoardID = board.id;
				visibleItems = board.entries;
				itemsList.itemsSource = board.entries;
			}

			PinboardCore.SetSelectedBoard(board);

			UpdateBoardToolbar();

			UpdateBoardsMenu();

			itemsList.Refresh();
		}
	}
}
