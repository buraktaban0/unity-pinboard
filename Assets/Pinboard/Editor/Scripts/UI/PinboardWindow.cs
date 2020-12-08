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
	[DefaultExecutionOrder(-2)]
	public class PinboardWindow : EditorWindow
	{
		public const string DIR_ROOT = "Assets/Pinboard/";
		public const string DIR_EDITOR = DIR_ROOT + "Editor/";
		public const string DIR_UI = DIR_EDITOR + "UI/";

		public const string PATH_PINBOARD_UXML = DIR_UI + "Pinboard.uxml";
		public const string PATH_PINBOARD_USS = DIR_UI + "Pinboard.uss";

		public const string CLASS_LIST_ITEM_ROOT = "list-item-root";
		public const string CLASS_BOARD_TOOLBAR = "board-toolbar";
		public const string CLASS_GHOST_LAYER = "ghost-layer";


		private static PinboardWindow _instance = null;

		public static PinboardWindow Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = Resources.FindObjectsOfTypeAll<PinboardWindow>().FirstOrDefault();
				}

				return _instance;
			}
			set { _instance = value; }
		}

		[MenuItem("Pinboard/Open &B")]
		public static void Open()
		{
			var window = GetWindow<PinboardWindow>();

			window.minSize = new Vector2(250, 100);
			window.titleContent =
				new GUIContent("Pinboard", PinboardResources.ICON_PINBOARD, "Ease your editor sessions.");

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
		private ContextualMenuManipulator boardDropdownContextualManipulator;

		private ListView itemsList;

		private VisualElement ghostLayer;
		private Image ghostLayerIcon;

		private List<Entry> visibleItems;

		private Queue<Action> updateActions = new Queue<Action>();

		private void OnEnable()
		{
			Instance = this;

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

			AddGhostLayer();

			RegisterDragAndDropCallbacks();

			PinboardDatabase.onBoardAdded += OnBoardAdded;
			PinboardDatabase.onBoardDeleted += OnBoardDeleted;
			PinboardDatabase.onDatabaseModified += OnBoardsModified;
			PinboardDatabase.onDatabaseSaved += OnDatabaseSaved;

			EditorSceneManager.sceneLoaded += OnEditorSceneLoaded;
			EditorSceneManager.sceneOpened += OnEditorSceneOpened;

			EditorApplication.update += EditorUpdate;

			this.Refresh();
		}

		private void OnDisable()
		{
			UnregisterDragAndDropCallbacks();

			PinboardDatabase.onBoardAdded -= OnBoardAdded;
			PinboardDatabase.onBoardDeleted -= OnBoardDeleted;
			PinboardDatabase.onDatabaseModified -= OnBoardsModified;
			PinboardDatabase.onDatabaseSaved -= OnDatabaseSaved;

			EditorSceneManager.sceneLoaded -= OnEditorSceneLoaded;
			EditorSceneManager.sceneOpened -= OnEditorSceneOpened;

			EditorApplication.update -= EditorUpdate;
		}

		private void EditorUpdate()
		{
			if (updateActions.Count > 0)
			{
				var act = updateActions.Dequeue();
				act?.Invoke();
			}
		}

		public void RunNextFrame(Action action)
		{
			updateActions.Enqueue(action);
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


		private ContextualMenuManipulator GetBoardContextualMenuManipulator()
		{
			return new ContextualMenuManipulator(pop =>
			{
				pop.menu.AppendAction("Rename Board", action =>
				{
					var board = currentBoard;
					TextEditPopup.ShowPopup(currentBoard, "Rename Board", currentBoard.title, s =>
					{
						if (board.title == s.Trim())
							return;

						PinboardDatabase.Current.WillModifyBoard(board);
						board.title =
							s.Trim().CorrectlyEnumerate(PinboardDatabase.Current.Boards.Select(b => b.title));
						board.IsDirty = true;
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
		}


		private void AddMainToolbar()
		{
			var toolbar = new Toolbar();

			boardsDropdown = new ToolbarMenu();
			boardsDropdown.style.minWidth = 84;
			toolbar.Add(boardsDropdown);

			boardDropdownContextualManipulator = GetBoardContextualMenuManipulator();

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

			if (currentBoard == null)
			{
				boardsDropdown.RemoveManipulator(boardDropdownContextualManipulator);
			}
			else
			{
				boardsDropdown.AddManipulator(boardDropdownContextualManipulator);
			}
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

			boardToolbarContextualManipulator = GetBoardContextualMenuManipulator();

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
			DetermineListViewEntries();
		}

		private void DetermineListViewEntries()
		{
			var str = searchField.value;

			if (currentBoard == null)
			{
				itemsList.itemsSource = new List<Entry>();
				return;
			}

			if (string.IsNullOrEmpty(str))
			{
				visibleItems = currentBoard.entries;
			}
			else
			{
				str = str.ToLower().Trim();
				var filters = str.Split(' ').SelectMany(s => s.Split(','))
				                 .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
				filters.Add(str);


				visibleItems =
					currentBoard.entries.Where(
						entry =>
						{
							var keywords = entry.GetSearchKeywords().ToList();
							keywords.Add(entry.GetType().Name.ToLower());
							keywords = keywords.Select(kw => kw.ToLower().Trim()).ToList();
							return Utility.CrossCompareStrings(keywords, filters);
						}).ToList();
			}

			itemsList.itemsSource = visibleItems;
			// Refreshes already at set source line
			//itemsList.Refresh();
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

				evt.menu.AppendAction("Comment...", action =>
				{
					TextEditPopup.ShowPopup(entry, "Comment", entry.comment, newVal =>
					{
						if (newVal == entry.comment)
							return;

						PinboardDatabase.Current.WillModifyEntry(entry);
						entry.comment = newVal;
					});
				});

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

			root.AddManipulator(new ClickActionsManipulator(() => { RunNextFrame((root.userData as Entry).OnClick); },
			                                                () =>
			                                                {
				                                                RunNextFrame((root.userData as Entry).OnDoubleClick);
			                                                }));
			var img = new Image();
			img.style.minWidth = 15;
			img.style.minHeight = 15;
			img.style.width = 15;
			img.style.height = 15;
			img.style.marginLeft = 2;
			img.style.marginRight = 6;
			root.Add(img);
			return root;
		}

		private void BindItem(VisualElement element, int index)
		{
			var entry = visibleItems[index];
			element.userData = entry;

			entry.BindVisualElement(element);

			if (string.IsNullOrEmpty(entry.comment) == false)
			{
				var tooltip = element.tooltip;
				if (string.IsNullOrEmpty(tooltip) == false)
					tooltip += "\n\nP.S. ";

				tooltip += entry.comment;

				element.tooltip = tooltip;
			}

			var lbl = element.Q<Label>();
			if (lbl != null)
			{
				lbl.style.textOverflow = TextOverflow.Ellipsis;
				lbl.style.unityTextOverflowPosition = TextOverflowPosition.End;
			}
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


		private void AddGhostLayer()
		{
			ghostLayer = new VisualElement();
			ghostLayer.AddToClassList(CLASS_GHOST_LAYER);
			ghostLayer.visible = false;

			ghostLayerIcon = new Image();
			ghostLayerIcon.image = PinboardResources.ICON_ADD;

			ghostLayer.Add(ghostLayerIcon);
			root.Add(ghostLayer);
		}


		private void RegisterDragAndDropCallbacks()
		{
			root.RegisterCallback<DragEnterEvent>(OnDragEnter);
			root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
			root.RegisterCallback<DragLeaveEvent>(OnDragLeave);
			root.RegisterCallback<DragPerformEvent>(OnDragPerform);
			root.RegisterCallback<DragExitedEvent>(OnDragExited);
		}

		private void UnregisterDragAndDropCallbacks()
		{
			root.UnregisterCallback<DragEnterEvent>(OnDragEnter);
			root.UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
			root.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
			root.UnregisterCallback<DragPerformEvent>(OnDragPerform);
			root.UnregisterCallback<DragExitedEvent>(OnDragExited);
		}


		private bool isDragging = false;

		private void OnDragEnter(DragEnterEvent evt)
		{
			var canAccept = DragAndDrop.objectReferences.Length == 1;
			// ghostLayer.visible = canAccept;
			isDragging = true;
		}

		private void OnDragUpdated(DragUpdatedEvent evt)
		{
			if (!isDragging)
				return;

			var canAccept = DragAndDrop.objectReferences.Length == 1;
			ghostLayer.visible = canAccept;
			DragAndDrop.visualMode = canAccept ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
		}

		private void OnDragLeave(DragLeaveEvent evt)
		{
			ghostLayer.visible = false;
		}

		private void OnDragExited(DragExitedEvent evt)
		{
			isDragging = false;
			ghostLayer.visible = false;
		}

		private void OnDragPerform(DragPerformEvent evt)
		{
			isDragging = false;
			ghostLayer.visible = false;
			var obj = DragAndDrop.objectReferences[0];
			Selection.activeObject = obj;
			PinboardCore.TryCreateEntry<ObjectShortcutEntry>();
		}


		public void Refresh()
		{
			updateActions.Enqueue(RefreshInternal);
		}

		public void RefreshNow()
		{
			RefreshInternal();
		}

		private void RefreshInternal()
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
		}


		public void SetBoard(Board board)
		{
			currentBoard = board;

			if (board == null)
			{
				PinboardPrefs.LastOpenBoardID = string.Empty;
				visibleItems = new List<Entry>();
				//itemsList.itemsSource = new List<Entry>();
			}
			else
			{
				PinboardPrefs.LastOpenBoardID = board.id;
				visibleItems = board.entries;
				//itemsList.itemsSource = board.entries;
			}


			PinboardCore.SetSelectedBoard(board);

			UpdateBoardToolbar();

			UpdateBoardsMenu();

			DetermineListViewEntries();

			// Refreshes at source set
			//itemsList.Refresh();
		}
	}
}
