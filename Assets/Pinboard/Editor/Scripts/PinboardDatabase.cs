using System;
using System.Collections.Generic;
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

	public static class PinboardDatabase
	{
		private const string TOKEN_BREAK = ";";
		private const string KEY_IDS = "PINBOARD_BOARD_IDS";

		private const string KEY_ID = "PINBOARD_BOARD";
		public static string DIR_PINBOARD_ROOT => Application.dataPath + "/Pinboard/";

		public static string DIR_BOARDS = DIR_PINBOARD_ROOT + "Boards/";

		public static PinboardBoardEvent onBoardAdded = delegate { };
		public static PinboardBoardEvent onBoardDeleted = delegate { };
		public static PinboardEvent onBoardsModified = delegate { };

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

		public static void DeleteItemFromBoard(BoardItem item, Board board)
		{
			board.Remove(item);
			
			SaveBoards();

			if (board.accessibility == BoardAccessibility.ProjectPublic)
			{
				DeleteItemFromAssetDatabase(item);
			}
			else
			{
				DeleteIdFromEditorPrefs(item.id);
			}
			

			AssetDatabase.SaveAssets();
		}


		private static void DeleteItemFromAssetDatabase(BoardItem item)
		{
			var boardItemContainers = LoadAssets<BoardItemJsonContainer>();

			var container =
				boardItemContainers.FirstOrDefault(c => new TypedJson(c.type, c.data).ToObject<BoardItem>().id ==
				                                        item.id);

			if (container != null)
			{
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(container));
			}
		}

		public static void DeleteBoard(Board board)
		{
			if (boards.Contains(board) == false)
			{
				Debug.LogWarning("Pinboard tried to delete a board that does not exist in the database");
				return;
			}

			boards.Remove(board);

			if (board.accessibility == BoardAccessibility.ProjectPublic)
			{
				DeleteBoardFromAssetDatabase(board);
			}
			else
			{
				DeleteBoardFromEditorPrefsWithContext(board, board.accessibility == BoardAccessibility.Global
					                                      ? ""
					                                      : PinboardCore.ProjectID);
			}

			AssetDatabase.SaveAssets();

			onBoardDeleted.Invoke(board);

			board = null;
		}

		private static void DeleteBoardFromAssetDatabase(Board board)
		{
			var serializedBoardContainers = LoadAssets<SerializedBoardContainer>();

			var cont = serializedBoardContainers.FirstOrDefault(c => c.serializedBoard.id == board.id);
			if (cont != null)
			{
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(cont));
			}

			var boardItemContainers = LoadAssets<BoardItemJsonContainer>();

			boardItemContainers
				.Where(c => board.items.Any(i => i.id == new TypedJson(c.type, c.data).ToObject<BoardItem>().id))
				.ToList().ForEach(c => AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(c)));
		}


		private static void DeleteBoardFromEditorPrefsWithContext(Board board, string ctx)
		{
			string keyIDs = KEY_IDS;
			if (string.IsNullOrEmpty(ctx) == false)
			{
				keyIDs += $"_{ctx}";
			}

			var ids = EditorPrefs.GetString(keyIDs, "");

			if (string.IsNullOrEmpty(ids))
				return;

			var idsSplit = ids.Split(new string[] {TOKEN_BREAK}, StringSplitOptions.None).ToList();

			if (idsSplit.Contains(board.id))
				idsSplit.Remove(board.id);

			var joinedIds = string.Join(TOKEN_BREAK, idsSplit);
			EditorPrefs.SetString(keyIDs, joinedIds);

			board.items.Select(item => item.id).ToList().ForEach(DeleteIdFromEditorPrefs);
			DeleteIdFromEditorPrefs(board.id);
		}

		public static void LoadBoards()
		{
			boards = new List<Board>();

			LoadBoardsFromAssetDatabase();
			LoadBoardsFromEditorPrefsWithContext("");
			LoadBoardsFromEditorPrefsWithContext(PinboardCore.ProjectID);
		}

		private static void LoadBoardsFromEditorPrefsWithContext(string ctx)
		{
			string keyIDs = KEY_IDS;
			if (string.IsNullOrEmpty(ctx) == false)
			{
				keyIDs += $"_{ctx}";
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

		private static void DeleteIdFromEditorPrefs(string id)
		{
			var key = $"{KEY_ID}_{id}";

			if (EditorPrefs.HasKey(key))
				EditorPrefs.DeleteKey(key);
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
			var pathBoard = PinboardCore.DIR_DATA + $"/{board.id}.asset";
			var boardContainer = AssetDatabase.LoadAssetAtPath<SerializedBoardContainer>(pathBoard);
			if (boardContainer == null)
			{
				boardContainer = ScriptableObject.CreateInstance<SerializedBoardContainer>();
				CreateAsset(boardContainer, pathBoard);
			}

			boardContainer.serializedBoard = serializedBoard;
			EditorUtility.SetDirty(boardContainer);

			foreach (var item in items)
			{
				var path = PinboardCore.DIR_DATA + $"/{item.id}.asset";
				var itemContainer = AssetDatabase.LoadAssetAtPath<BoardItemJsonContainer>(path);
				if (itemContainer == null)
				{
					itemContainer = ScriptableObject.CreateInstance<BoardItemJsonContainer>();
					CreateAsset(itemContainer, path);
				}

				itemContainer.type = item.GetType().FullName;
				itemContainer.data = JsonUtility.ToJson(item);

				EditorUtility.SetDirty(itemContainer);
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


		private static List<T> LoadAssets<T>() where T : UnityEngine.Object
		{
			var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

			if (guids == null || guids.Length < 1)
				return new List<T>();

			var assets =
				guids.Select(
					guid => AssetDatabase.LoadAssetAtPath<T>(
						AssetDatabase.GUIDToAssetPath(guid))).ToList();

			return assets;
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
