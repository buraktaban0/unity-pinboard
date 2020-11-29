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
				if (_current == null)
				{
					_current = ScriptableObject.CreateInstance<PinboardDatabase>();
				}

				return _current;
			}
		}

		public PinboardBoardEvent onBoardAdded = delegate { };
		public PinboardBoardEvent onBoardDeleted = delegate { };
		public PinboardEvent onDatabaseModified = delegate { };

		private Dictionary<string, SerializedBoardContainer> serializedBoardContainers;
		private Dictionary<string, BoardEntryJsonContainer> boardEntryContainers;

		private Dictionary<string, Board> boardsById;
		private Dictionary<string, BoardEntry> entriesById;

		[SerializeField]
		private List<Board> boards;

		public ReadOnlyCollection<Board> Boards => boards.AsReadOnly();

		public int BoardCount => boards.Count;

		public void OnBeforeSerialize()
		{
			Debug.Log("Before database serialize");
		}

		public void OnAfterDeserialize()
		{
			Debug.Log("After database serialize");
			SaveAll();
		}

		public bool HasBoard(string id)
		{
			return boards.Any(b => b.id == id);
		}

		public Board GetBoard(string id)
		{
			var board = boards.FirstOrDefault(b => b.id == id);

			if (board == null)
				throw new Exception($"Board with id {id} not found! Check with 'HasBoard' before.");

			return board;
		}


		public void AddBoard(Board board)
		{
			Undo.RegisterCompleteObjectUndo(this, $"Create Board {board.title}");

			boards.Add(board);
			boardsById.Add(board.id, board);

			SaveAll();
		}

		public void SaveAll()
		{
		}


		public void DeleteBoard(Board board) => DeleteBoard(board.id);

		public void DeleteBoard(string id)
		{
			var index = boards.FindIndex(b => b.id == id);
			if (index < 0)
				throw new Exception("Board to be deleted does not exist in database.");

			var board = boards[index];

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
				var entryContainer = boardEntryContainers[entry.id];
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(entryContainer));
				boardEntryContainers.Remove(entry.id);
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

		public void LoadAll()
		{
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
			              .Select(entryData => new TypedJson(entryData.type, entryData.data).ToObject<BoardEntry>())
			              .ToList();

			this.serializedBoardContainers = new Dictionary<string, SerializedBoardContainer>();
			for (var i = 0; i < boardContainers.Count; i++)
			{
				serializedBoardContainers[serializedBoards[i].id] = boardContainers[i];
			}

			this.boardEntryContainers = new Dictionary<string, BoardEntryJsonContainer>();
			for (var i = 0; i < entryContainers.Count; i++)
			{
				this.boardEntryContainers[entries[i].id] = entryContainers[i];
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
			var boardIds = idsJoined.Split(new[] {TOKEN_BREAK}, StringSplitOptions.None);

			var boardData = boardIds.Select(id => EditorPrefs.GetString($"{KEY_ID}_{id}", null))
			                        .Where(data => data != null).ToList();
			var serializedBoards = boardData.Select(data => JsonUtility.FromJson<SerializedBoard>(data));
			var boards = serializedBoards.Select(serBoard =>
			{
				var board = new Board(serBoard);
				foreach (var entryId in serBoard.entryIds)
				{
					var key = $"{KEY_ID}_{entryId}";
					var entryData = EditorPrefs.GetString(key, null);
					if (entryData == null)
						continue;
					var entry = JsonUtility.FromJson<TypedJson>(entryData).ToObject<BoardEntry>();
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
