using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	public delegate void PinboardEvent();
	
	public static class Database
	{
		private const string TOKEN_BREAK = ";BREAK;";
		private const string KEY_IDS = "PINBOARD_BOARD_IDS";

		private const string KEY_ID = "PINBOARD_BOARD_";
		public static string PINBOARD_ROOT_DIR => Application.dataPath + "/Pinboard/";

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
			
			onBoardsModified.Invoke();
		}

		public static void DeleteBoard(Board board)
		{
			if (boards.Contains(board) == false)
			{
				Debug.LogWarning("Pinboard tried to delete a board that does not exist in the database");
				return;
			}

			boards.Remove(board);
			
			onBoardsModified.Invoke();
		}

		public static void LoadBoards()
		{
			var joined = EditorPrefs.GetString(KEY_IDS, "");

			boards = new List<Board>();

			List<string> boardIds;
			if (string.IsNullOrEmpty(joined))
			{
				boardIds = new List<string>();
			}
			else
			{
				boardIds = joined.Split(new[] {TOKEN_BREAK}, StringSplitOptions.None).ToList();
			}

			foreach (var boardId in boardIds)
			{
				var key = $"{KEY_ID}{boardId}";
				var json = EditorPrefs.GetString(key, "");

				SerializedBoard serializedBoard = null;
				try
				{
					serializedBoard = JsonUtility.FromJson<SerializedBoard>(json);
				}
				catch (Exception e)
				{
					throw new Exception("Pinboard - Corrupt board json in prefs: " + json + "" + e);
				}

				var board = new Board(serializedBoard);
				boards.Add(board);
			}
			
			onBoardsModified.Invoke();
		}

		public static void SaveBoards()
		{
			var joinedIds = string.Join(TOKEN_BREAK, boards.Select(b => b.id).ToArray());
			EditorPrefs.SetString(KEY_IDS, joinedIds);

			foreach (var board in boards)
			{
				var serializedBoard = new SerializedBoard(board);
				var json = JsonUtility.ToJson(serializedBoard);
				var key = $"{KEY_ID}{board.id}";
				EditorPrefs.SetString(key, json);
			}
			
			onBoardsModified.Invoke();
		}


		public static void SaveText(string folder, string filename, string text)
		{
			var dir = PINBOARD_ROOT_DIR + folder;

			if (Directory.Exists(dir) == false)
				Directory.CreateDirectory(dir);

			var path = dir + "/" + filename;

			File.WriteAllText(path, text);
		}
	}
}
