using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Pinboard.Items;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pinboard
{
	public delegate void PinboardEvent();

	public delegate void PinboardBoardEvent(Board board);

	[InitializeOnLoad]
	public class PinboardDatabase : ScriptableObject, ISerializationCallbackReceiver
	{
		private const string TOKEN_BREAK = ";";
		private const string KEY_IDS = "PINBOARD_BOARD_IDS";
		private const string KEY_ID = "PINBOARD_BOARD";


		private static PinboardDatabase _current;

		public static PinboardDatabase Current
		{
			get
			{
				if (!_current)
				{
					var databases = Resources.FindObjectsOfTypeAll<PinboardDatabase>().ToList();
					for (int i = databases.Count - 1; i >= 1; i--)
					{
						if (databases[i] == null)
							continue;

						DestroyImmediate(databases[i]);

						databases.RemoveAt(i);
					}

					if (databases.Count > 0)
					{
						_current = databases.First();
					}
					else
					{
						_current = ScriptableObject.CreateInstance<PinboardDatabase>();
					}
				}

				return _current;
			}
		}

		static PinboardDatabase()
		{
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
		}

		private static void OnBeforeAssemblyReload()
		{
			//Debug.Log("OnBeforeAssemblyReload ");
			if (_current)
				Current.Save();
		}

		public static PinboardBoardEvent onBoardAdded = delegate { };
		public static PinboardBoardEvent onBoardDeleted = delegate { };
		public static PinboardEvent onDatabaseModified = delegate { };
		public static PinboardEvent onDatabaseSaved = delegate { };

		
		private Dictionary<string, SerializedBoardContainer> serializedBoardContainers = new Dictionary<string, SerializedBoardContainer>();
		private Dictionary<string, BoardEntryJsonContainer> entryContainers = new Dictionary<string, BoardEntryJsonContainer>();

		private Dictionary<string, Board> boardsById = new Dictionary<string, Board>();
		private Dictionary<string, Entry> entriesById = new Dictionary<string, Entry>();

		[SerializeField]
		private List<Board> boards = new List<Board>();

		public ReadOnlyCollection<Board> Boards => boards.AsReadOnly();

		public int BoardCount => boards.Count;

		private bool shouldSaveOnEditorUpdate = false;

		private void OnEnable()
		{
			EditorApplication.update += EditorUpdate;

			RefreshBoardEntryConnections();
		}

		private void OnDisable()
		{
			EditorApplication.update -= EditorUpdate;
		}

		private void EditorUpdate()
		{
			if (shouldSaveOnEditorUpdate)
			{
				shouldSaveOnEditorUpdate = false;
				Save();
			}
		}

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			//Save();

			RefreshBoardEntryConnections();
			shouldSaveOnEditorUpdate = true;
		}


		private void RefreshBoardEntryConnections()
		{
			foreach (var board in boards)
			{
				foreach (var boardEntry in board.entries)
				{
					boardEntry.board = board;
				}
			}
		}


		public void WillModifyEntry(Entry entry)
		{
			Undo.RegisterCompleteObjectUndo(this, $"Modify Entry '{entry.ShortVisibleName}'");
			shouldSaveOnEditorUpdate = true;
			//EditorApplication.QueuePlayerLoopUpdate();
		}

		public void WillModifyBoard(Board board, string reason = null)
		{
			if (reason is null)
			{
				reason = $"Modify board '{board.title}'";
			}

			Undo.RegisterCompleteObjectUndo(this, reason);
			shouldSaveOnEditorUpdate = true;
			//EditorApplication.QueuePlayerLoopUpdate();
		}

		public bool HasBoard(string id)
		{
			return boards.Any(b => b.id == id);
		}

		public Board GetBoard(string id)
		{
			var board = boards.FirstOrDefault(b => b.id == id);
			return board;
		}


		public void AddBoard(Board board)
		{
			Undo.RegisterCompleteObjectUndo(this, $"Create Board {board.title}");

			boards.Add(board);
			boardsById.Add(board.id, board);

			Save();

			onBoardAdded.Invoke(board);
		}

		public bool NeedsSave()
		{
			return boards.Any(b => b.IsDirty || b.entries.Any(e => e.IsDirty));
		}

		public void Save()
		{
			foreach (var board in boards)
			{
				if (board.IsDirty)
				{
					SaveBoard(board);
					board.IsDirty = false;
				}

				foreach (var entry in board.entries)
				{
					if (entry.IsDirty)
					{
						SaveEntry(entry);
						entry.IsDirty = false;
					}
				}
			}

			AssetDatabase.SaveAssets();

			onDatabaseSaved.Invoke();
		}

		private void SaveBoard(Board board)
		{
			switch (board.accessibility)
			{
				case BoardAccessibility.ProjectPublic:
					SaveBoardToAssetDatabase(board);
					break;
				case BoardAccessibility.Global:
					SaveBoardToEditorPrefsWithContext("", board);
					break;
				case BoardAccessibility.ProjectPrivate:
					SaveBoardToEditorPrefsWithContext(PinboardCore.ProjectID, board);
					break;
			}
		}

		private void SaveBoardToAssetDatabase(Board board)
		{
			if (serializedBoardContainers == null)
				serializedBoardContainers = new Dictionary<string, SerializedBoardContainer>();

			if (!serializedBoardContainers.TryGetValue(board.id, out var container))
			{
				container = ScriptableObject.CreateInstance<SerializedBoardContainer>();
				container.serializedBoard = new SerializedBoard(board);
				serializedBoardContainers[board.id] = container;
				AssetDatabase.CreateAsset(container, PinboardCore.DIR_DATA + "/" + board.id + ".asset");
			}
			else
			{
				if (container == null)
				{
					container = Utility.LoadAssets<SerializedBoardContainer>().First(c => c.name == board.id);
				}

				container.serializedBoard = new SerializedBoard(board);
				EditorUtility.SetDirty(container);
			}
		}

		private void SaveBoardToEditorPrefsWithContext(string ctx, Board board)
		{
			var keyIds = GetPrefsIdsKeyWithContext(ctx);
			var idsJoined = EditorPrefs.GetString(keyIds);
			var ids = idsJoined.Split(new[] {TOKEN_BREAK}, StringSplitOptions.None).ToList();
			if (!ids.Contains(board.id))
				ids.Add(board.id);

			idsJoined = string.Join(TOKEN_BREAK, ids);
			EditorPrefs.SetString(keyIds, idsJoined);

			var serializedBoard = new SerializedBoard(board);
			var key = GetPrefsIdKey(board.id);
			var json = JsonUtility.ToJson(serializedBoard);

			EditorPrefs.SetString(key, json);
		}

		private void SaveEntry(Entry entry)
		{
			switch (entry.board.accessibility)
			{
				case BoardAccessibility.ProjectPublic:
					SaveEntryToAssetDatabase(entry);
					break;
				case BoardAccessibility.Global:
				case BoardAccessibility.ProjectPrivate:
					SaveEntryToEditorPrefs(entry);
					break;
			}
		}

		private void SaveEntryToAssetDatabase(Entry entry)
		{
			if (!entryContainers.TryGetValue(entry.id, out var container))
			{
				container = ScriptableObject.CreateInstance<BoardEntryJsonContainer>();
				AssetDatabase.CreateAsset(container, PinboardCore.DIR_DATA + "/" + entry.id + ".asset");
			}

			container.type = entry.GetType().FullName;
			container.data = JsonUtility.ToJson(entry);
			EditorUtility.SetDirty(container);
		}

		private void SaveEntryToEditorPrefs(Entry entry)
		{
			var typedJson = new TypedJson(entry.GetType().FullName, JsonUtility.ToJson(entry));
			var json = JsonUtility.ToJson(typedJson);
			var key = GetPrefsIdKey(entry.id);
			EditorPrefs.SetString(key, json);
		}


		public void DeleteBoard(Board board) => DeleteBoard(board.id);

		public void DeleteEntryFromBoard(Entry entry, Board board)
		{
			Undo.RegisterCompleteObjectUndo(this, $"Delete '{entry.ShortVisibleName}' from '{board.title}'");
			board.Remove(entry);
			DeleteEntry(entry, board);
			Save();
		}

		public void DeleteBoard(string id)
		{
			var index = boards.FindIndex(b => b.id == id);
			if (index < 0)
				throw new Exception("Board to be deleted does not exist in database.");

			var board = boards[index];

			Undo.RegisterCompleteObjectUndo(this, $"Delete board '{board.title}'");

			switch (board.accessibility)
			{
				case BoardAccessibility.ProjectPublic:
					DeleteBoardFromAssetDatabase(id);
					break;
				case BoardAccessibility.Global:
					DeleteBoardFromEditorPrefsWithContext("", id);
					break;
				case BoardAccessibility.ProjectPrivate:
					DeleteBoardFromEditorPrefsWithContext(PinboardCore.ProjectID, id);
					break;
			}

			boards.RemoveAt(index);
			boardsById.Remove(board.id);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			onBoardDeleted.Invoke(board);
			onDatabaseModified.Invoke();
		}

		private void DeleteBoardFromAssetDatabase(string id)
		{
			var container = serializedBoardContainers[id];
			AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(container));
			serializedBoardContainers.Remove(id);

			var board = boardsById[id];
			foreach (var entry in board.entries)
			{
				var entryContainer = this.entryContainers[entry.id];
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(entryContainer));
				this.entryContainers.Remove(entry.id);
			}
		}

		private void DeleteBoardFromEditorPrefsWithContext(string ctx, string id)
		{
			var keyIds = GetPrefsIdsKeyWithContext(ctx);
			var idsJoined = EditorPrefs.GetString(keyIds, null);
			var ids = idsJoined.Split(new[] {TOKEN_BREAK}, StringSplitOptions.None).ToList();
			ids.Remove(id);
			idsJoined = string.Join(TOKEN_BREAK, ids);
			EditorPrefs.SetString(keyIds, idsJoined);

			if (EditorPrefs.HasKey(id))
				EditorPrefs.DeleteKey(id);

			var board = boardsById[id];
			foreach (var entry in board.entries)
			{
				if (EditorPrefs.HasKey(id))
					EditorPrefs.DeleteKey(id);
			}
		}

		public void DeleteEntry(Entry entry, Board board)
		{
			switch (board.accessibility)
			{
				case BoardAccessibility.ProjectPublic:
					DeleteEntryFromAssetDatabase(entry);
					break;
				case BoardAccessibility.ProjectPrivate:
				case BoardAccessibility.Global:
					DeleteEntryFromEditorPrefs(entry);
					break;
			}
		}

		private void DeleteEntryFromAssetDatabase(Entry entry)
		{
			if (entryContainers.TryGetValue(entry.id, out var container))
			{
				entryContainers.Remove(entry.id);
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(container));
			}
		}

		private void DeleteEntryFromEditorPrefs(Entry entry)
		{
			var key = GetPrefsIdKey(entry.id);
			if (EditorPrefs.HasKey(key))
			{
				EditorPrefs.DeleteKey(key);
			}
		}

		public void Unload()
		{
			boards?.Clear();
			boardsById?.Clear();
			entriesById?.Clear();
			serializedBoardContainers?.Clear();
			entryContainers?.Clear();
		}

		public void Load()
		{

			Unload();

			LoadAllFromAssetDatabase();
			LoadAllFromEditorPrefsWithContext("");
			LoadAllFromEditorPrefsWithContext(PinboardCore.ProjectID);

			boardsById = boards.ToDictionary(board => board.id);
		}


		private void LoadAllFromAssetDatabase()
		{
			var boardContainers = Utility.LoadAssets<SerializedBoardContainer>();
			var serializedBoards = boardContainers.Select(container => container.serializedBoard).ToList();

			var entryContainers = Utility.LoadAssets<BoardEntryJsonContainer>();
			var entries = entryContainers
			              .Select(entryData => new TypedJson(entryData.type, entryData.data).ToObject<Entry>())
			              .ToList();

			this.serializedBoardContainers = new Dictionary<string, SerializedBoardContainer>();
			for (var i = 0; i < boardContainers.Count; i++)
			{
				serializedBoardContainers[serializedBoards[i].id] = boardContainers[i];
			}

			this.entryContainers = new Dictionary<string, BoardEntryJsonContainer>();
			for (var i = 0; i < entryContainers.Count; i++)
			{
				this.entryContainers[entries[i].id] = entryContainers[i];
			}


			var boards = serializedBoards.Select(serBoard =>
			{
				var board = new Board(serBoard);
				foreach (var entryId in serBoard.entryIds)
				{
					var entry = entries.FirstOrDefault(e => e.id == entryId);
					if (entry == null)
						continue;

					board.Add(entry);
				}

				return board;
			}).ToList();


			this.boards.AddRange(boards);
		}

		private void LoadAllFromEditorPrefsWithContext(string ctx)
		{
			var keyIds = KEY_IDS;
			if (string.IsNullOrEmpty(ctx) == false)
			{
				keyIds += $"_{ctx}";
			}

			var idsJoined = EditorPrefs.GetString(keyIds);
			var boardIds = string.IsNullOrEmpty(idsJoined)
				? new string[] { }
				: idsJoined.Split(new[] {TOKEN_BREAK}, StringSplitOptions.None);

			var boardData = boardIds.Select(id => EditorPrefs.GetString($"{KEY_ID}_{id}", null))
			                        .Where(data => data != null).ToList();
			var serializedBoards = boardData.Select(data => JsonUtility.FromJson<SerializedBoard>(data))
			                                .Where(s => s != null && s.entryIds != null);
			var boards = serializedBoards.Select(serBoard =>
			{
				var board = new Board(serBoard);

				foreach (var entryId in serBoard.entryIds)
				{
					var key = $"{KEY_ID}_{entryId}";
					var entryData = EditorPrefs.GetString(key, null);
					if (entryData == null)
						continue;
					var entry = JsonUtility.FromJson<TypedJson>(entryData).ToObject<Entry>();
					board.Add(entry);
				}

				return board;
			});

			this.boards.AddRange(boards);
		}


		private string GetPrefsIdsKeyWithContext(string ctx)
		{
			return string.IsNullOrEmpty(ctx) ? KEY_IDS : $"{KEY_IDS}_{ctx}";
		}

		private string GetPrefsIdKey(string id)
		{
			return $"{KEY_ID}_{id}";
		}

		public static void CreateAsset(UnityEngine.Object asset, string path)
		{
			if (AssetDatabase.IsMainAsset(asset))
			{
				EditorUtility.SetDirty(asset);
				return;
			}

			Utility.MakeDirs(path);
			AssetDatabase.CreateAsset(asset, path);
		}
	}
}
