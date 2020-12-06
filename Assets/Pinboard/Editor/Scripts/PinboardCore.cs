using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Pinboard.Entries;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	[InitializeOnLoad]
	public static class PinboardCore
	{
		public static string DIR_ROOT = "Assets/Pinboard";
		public static string DIR_EDITOR = DIR_ROOT + "/Editor";
		public static string DIR_UI = DIR_EDITOR + "/UI";
		public static string DIR_DATA = DIR_EDITOR + "/Data";

		public static string PATH_CFG = DIR_ROOT + "/PinboardConfig.asset";
		public static string DIR_PROJECT => Application.dataPath.Replace("Assets", "");

		private static PinboardConfig _config;

		public static PinboardConfig Config
		{
			get
			{
				if (_config == null)
				{
					InitializeConfig();
				}

				return _config;
			}
		}


		private static string _projectID;

		public static string ProjectID
		{
			get
			{
				if (string.IsNullOrEmpty(_projectID))
				{
					_projectID = Utility.GetProjectID();
				}

				return _projectID;
			}
		}


		private static string _user;

		public static string User
		{
			get
			{
				if (string.IsNullOrEmpty(_user))
				{
					_user = Utility.GetUserName();
				}

				return _user;
			}
		}


		public static List<Type>   EntryTypes     { get; private set; }
		public static List<string> EntryTypeNames { get; private set; }


		private static Board selectedBoard = null;


		static PinboardCore()
		{
			InitializeTypes();

			updateActions.Enqueue(Initialize);

			EditorApplication.update += EditorUpdate;
		}

		private static Queue<Action> updateActions = new Queue<Action>();

		public static void RunNextFrame(Action action)
		{
			updateActions.Enqueue(action);
		}


		private static void EditorUpdate()
		{
			if (updateActions.Count > 0)
			{
				var act = updateActions.Dequeue();
				act?.Invoke();
			}
		}

		public static void OnAssetDatabaseModifiedExternally()
		{
			var db = PinboardDatabase.Current;
			db.Load();

			PinboardWindow.Instance?.RefreshNow();
		}

		public static void Initialize()
		{
			//InitializeConfig();

			var currentDatabase = PinboardDatabase.Current;
			currentDatabase.Load();

			PinboardWindow.Instance?.Refresh();
		}


		private static void InitializeConfig()
		{
			_config = AssetDatabase.LoadAssetAtPath<PinboardConfig>(PATH_CFG);

			if (_config == null)
			{
				_config = ScriptableObject.CreateInstance<PinboardConfig>();
				var dir = DIR_PROJECT + DIR_ROOT;
				Debug.Log(dir);
				if (Directory.Exists(dir) == false)
				{
					Directory.CreateDirectory(dir);
				}

				PinboardDatabase.CreateAsset(_config, PATH_CFG);
				AssetDatabase.SaveAssets();
			}
		}

		private static void InitializeTypes()
		{
			EntryTypes = new List<Type>(typeof(PinboardCore).Assembly.GetTypes()
			                                                .Where(type => type.IsSubclassOf(typeof(Entry)) &&
			                                                               type.IsDefined(
				                                                               typeof(EntryTypeAttribute), true) &&
			                                                               type.GetCustomAttribute<EntryTypeAttribute>()
			                                                                   .canBeCreatedExplicitly));
			EntryTypeNames = EntryTypes
			                 .Select(type => ((EntryTypeAttribute) type.GetCustomAttribute(
				                         typeof(EntryTypeAttribute), true)).visibleName).ToList();
		}


		public static Board TryCreateBoard(CreateBoardOptions options)
		{
			var board = Board.Create(options);

			board.Add(new NoteEntry("Fresh board!"));

			PinboardDatabase.Current.AddBoard(board);

			return board;
		}

		public static void TryCreateEntry<T>() where T : Entry => TryCreateEntry(typeof(T));

		public static void TryCreateEntry(Type entryType)
		{
			if (selectedBoard == null)
			{
				Debug.LogError("No board selected, cannot create entries.");
				return;
			}

			var entry = System.Activator.CreateInstance(entryType) as Entry;

			if (entry == null)
			{
				throw new Exception("Tried to create an entry of unknown type " + entryType.FullName);
			}

			bool wasCreated = entry.Create();

			if (!wasCreated)
			{
				return;
			}

			PinboardDatabase.Current.WillModifyBoard(selectedBoard, "Create entry");
			selectedBoard.Add(entry);
		}

		public static void TryCreateEntry(Entry template)
		{
			updateActions.Enqueue(() =>
			{
				if (selectedBoard == null)
				{
					Debug.LogError("No board selected, cannot create entries.");
					return;
				}

				var entry = template.Clone();
				entry.IsDirty = true;

				PinboardDatabase.Current.WillModifyBoard(selectedBoard, "Paste entry");
				selectedBoard.Add(entry);

				PinboardDatabase.Current.Save();
			});
		}

		public static void TryDeleteEntry(Entry item, Board board)
		{
			if (board == null || item == null)
				return;

			PinboardDatabase.Current.DeleteEntryFromBoard(item, board);
		}


		public static void SetSelectedBoard(Board board)
		{
			selectedBoard = board;
		}


		[MenuItem("Pinboard/Clear All")]
		public static void ClearAllBoards()
		{
			PinboardCore.Initialize();
			foreach (var board in PinboardDatabase.Current.Boards.ToList())
			{
				PinboardDatabase.Current.DeleteBoard(board);
			}

			//PinboardDatabase.boards.ToList().ForEach(PinboardDatabase.DeleteBoard);
			PinboardWindow.Instance?.Refresh();
		}
	}
}
