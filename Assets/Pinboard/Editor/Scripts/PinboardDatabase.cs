using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pinboard.Items;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	public delegate void PinboardEvent();

	public delegate void PinboardBoardEvent(Board board);

	public static class PinboardDatabase
	{
		private const string TOKEN_BREAK = ";BREAK;";
		private const string KEY_IDS = "PINBOARD_BOARD_IDS";

		private const string KEY_ID = "PINBOARD_BOARD";
		public static string DIR_PINBOARD_ROOT => Application.dataPath + "/Pinboard/";

		public static string DIR_BOARDS = DIR_PINBOARD_ROOT + "Boards/";

		public static PinboardBoardEvent onBoardAdded;
		public static PinboardBoardEvent onBoardDeleted;
		public static PinboardEvent onBoardsModified;

		public static List<Board> boards = new List<Board>(32);

		public static Board GetBoard(string id)
		{
			return boards.FirstOrDefault(board => board.id == id);
		}

		public static void AddBoard(Board board)
		{
			if (boards.Contains(board))
			{
				Debug.LogWarning("Pinboard tried to add a board to the database that already exists");
				return;
			}

			boards.Add(board);

			SaveBoards();

			onBoardAdded.Invoke(board);
		}

		public static void DeleteBoard(Board board)
		{
			if (boards.Contains(board) == false)
			{
				Debug.LogWarning("Pinboard tried to delete a board that does not exist in the database");
				return;
			}

			boards.Remove(board);

			SaveBoards();

			onBoardDeleted.Invoke(board);
		}

		public static void LoadBoards()
		{
			boards = new List<Board>();

			LoadBoardsFromAssetDatabase();
			LoadBoardsFromEditorPrefsWithContext("");
			LoadBoardsFromEditorPrefsWithContext(PinboardCore.ProjectID);
		}

		private static void LoadBoardsFromEditorPrefsWithContext(string cxt)
		{
			string keyIDs = KEY_IDS;
			if (string.IsNullOrEmpty(cxt) == false)
			{
				keyIDs += $"_{cxt}";
			}

			var ids = EditorPrefs.GetString(keyIDs, "");

			if (string.IsNullOrEmpty(ids))
				return;

			var idsSplit = ids.Split(new string[] {TOKEN_BREAK}, StringSplitOptions.None);

			foreach (var id in idsSplit)
			{
				var board = LoadBoardFromPrefs(id);
				if (board == null)
					continue;

				boards.Add(board);
			}
		}


		private static Board LoadBoardFromPrefs(string id)
		{
			var key = $"{KEY_ID}_{id}";
			var boardJson = EditorPrefs.GetString(key, "");

			if (string.IsNullOrEmpty(boardJson))
			{
				Debug.Log("Board was missing from EditorPrefs. - " + id);
				return null;
			}

			var serializedBoard = JsonUtility.FromJson<SerializedBoard>(boardJson);
			var board = new Board(serializedBoard);

			foreach (var itemId in serializedBoard.itemIds)
			{
				var item = LoadItemFromEditorPrefs(itemId);

				if (item == null)
				{
					Debug.Log("Item was missing from EditorPrefs. - " + id);
					continue;
				}

				board.items.Add(item);
			}

			return board;
		}

		private static BoardItem LoadItemFromEditorPrefs(string id)
		{
			var key = $"{KEY_ID}_{id}";

			var typedJsonContent = EditorPrefs.GetString(key, "");

			if (string.IsNullOrEmpty(typedJsonContent))
			{
				return null;
			}

			var typedJson = JsonUtility.FromJson<TypedJson>(typedJsonContent);
			var item = typedJson.ToObject<BoardItem>();
			return item;
		}

		private static void LoadBoardsFromAssetDatabase()
		{
			var guids = AssetDatabase.FindAssets("t:SerializedBoardContainer");

			if (guids == null || guids.Length < 1)
				return;

			var containers =
				guids.Select(
					guid => AssetDatabase.LoadAssetAtPath<SerializedBoardContainer>(
						AssetDatabase.GUIDToAssetPath(guid))).ToList();

			var serializedBoards = containers.Select(c => c.serializedBoard).ToList();
			var boards = containers.Select(bc => new Board(bc.serializedBoard)).ToList();


			guids = AssetDatabase.FindAssets("t:BoardItemJsonContainer");
			var itemContainers = guids
			                     .Select(
				                     guid => AssetDatabase.LoadAssetAtPath<BoardItemJsonContainer>(
					                     AssetDatabase.GUIDToAssetPath(guid))).ToList();

			var items = itemContainers.Select(ic =>
			{
				var typedJson = new TypedJson(ic.type, ic.data);
				var item = typedJson.ToObject<BoardItem>();
				return item;
			}).ToList();


			for (var i = 0; i < boards.Count; i++)
			{
				var board = boards[i];
				var serializedBoard = serializedBoards[i];

				for (var j = 0; j < serializedBoard.itemIds.Length; j++)
				{
					var targetId = serializedBoard.itemIds[j];
					var item = items.FirstOrDefault(it => it.id == targetId);
					items.Remove(item);

					if (item == null)
					{
						Debug.Log("Board item could not be loaded. - " + targetId);
						continue;
					}

					board.items.Add(item);
				}
			}

			PinboardDatabase.boards.AddRange(boards);
		}

		public static void SaveBoards()
		{
			List<string> globalBoardIDs = new List<string>();
			List<string> projectPrivateBoardIDs = new List<string>();

			foreach (var board in boards)
			{
				if (board.accessibility == BoardAccessibility.ProjectPublic)
				{
					SaveBoardToAssetDatabase(board);
				}
				else if (board.accessibility == BoardAccessibility.Global)
				{
					SaveBoardToEditorPrefs(board);
					globalBoardIDs.Add(board.id);
				}
				else if (board.accessibility == BoardAccessibility.ProjectPrivate)
				{
					SaveBoardToEditorPrefs(board);
					projectPrivateBoardIDs.Add(board.id);
				}
			}

			string globalIDsJoined = string.Join(TOKEN_BREAK, globalBoardIDs);
			string projectPrivateIDsJoined = string.Join(TOKEN_BREAK, projectPrivateBoardIDs);

			EditorPrefs.SetString(KEY_IDS, globalIDsJoined);
			EditorPrefs.SetString($"{KEY_IDS}_{PinboardCore.ProjectID}", projectPrivateIDsJoined);

			onBoardsModified.Invoke();

			AssetDatabase.SaveAssets();
		}


		private static void SaveBoardToAssetDatabase(Board board)
		{
			var serializedBoard = new SerializedBoard(board);
			var items = board.items;
			var pathBoard = PinboardCore.DIR_DATA + $"/{board.id}.board";
			var boardContainer = AssetDatabase.LoadAssetAtPath<SerializedBoardContainer>(pathBoard);
			if (boardContainer == null)
			{
				boardContainer = ScriptableObject.CreateInstance<SerializedBoardContainer>();
				AssetDatabase.CreateAsset(boardContainer, pathBoard);
			}

			boardContainer.serializedBoard = serializedBoard;

			foreach (var item in items)
			{
				var path = PinboardCore.DIR_DATA + $"/{item.id}.boarditem";
				var itemContainer = AssetDatabase.LoadAssetAtPath<BoardItemJsonContainer>(path);
				if (itemContainer == null)
				{
					itemContainer = ScriptableObject.CreateInstance<BoardItemJsonContainer>();
					AssetDatabase.CreateAsset(itemContainer, path);
				}

				itemContainer.type = item.GetType().FullName;
				itemContainer.data = JsonUtility.ToJson(item);
			}
		}


		private static void SaveBoardToEditorPrefs(Board board)
		{
			var serializedBoard = new SerializedBoard(board);
			var items = board.items;
			var key = $"{KEY_ID}_{board.id}";
			var val = JsonUtility.ToJson(serializedBoard);

			EditorPrefs.SetString(key, val);

			foreach (var boardItem in items)
			{
				SaveBoardItemToEditorPrefs(boardItem);
			}
		}


		private static void SaveBoardItemToEditorPrefs(BoardItem item)
		{
			var key = $"{KEY_ID}_{item.id}";
			var val = JsonUtility.ToJson(TypedJson.Create(item));
			EditorPrefs.SetString(key, val);
		}


	}
}
