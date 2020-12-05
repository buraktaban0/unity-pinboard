using System;
using System.Collections.Generic;
using System.Linq;
using Pinboard.Entries;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Pinboard
{
	[System.Serializable]
	public class Board
	{
		public bool IsDirty { get; set; } = false;

		public string id = Guid.Get();

		public BoardAccessibility accessibility = BoardAccessibility.Global;

		public string title = "New Board";

		public string createdBy = PinboardCore.User;

		public long createdAt = Utility.GetUnixTimestamp();

		public DateTime CreationTime => Utility.FromUnixToLocal(createdAt);

		[SerializeReference]
		public List<Entry> entries = new List<Entry>();

		public static Board Create()
		{
			var board = new Board();
			board.IsDirty = true;
			return board;
		}

		public static Board Create(CreateBoardOptions options)
		{
			var board = Create();
			board.title = options.title.CorrectlyEnumerate(PinboardDatabase.Current.Boards.Select(b => b.title));
			board.accessibility = options.accessibility;
			return board;
		}

		public Board()
		{
			
		}

		public Board(SerializedBoard serializedBoard)
		{
			id = serializedBoard.id;
			accessibility = serializedBoard.accessibility;
			title = serializedBoard.title;
			createdBy = serializedBoard.createdBy;
			createdAt = serializedBoard.createdAt;
		}

		public void Add(Entry entry)
		{
			entry.board = this;
			entry.IsDirty = true;
			entries.Add(entry);
			IsDirty = true;
		}

		public void Remove(Entry entry)
		{
			entry.board = null;
			entries.Remove(entry);
			IsDirty = true;
		}

		[MenuItem("Tools/TestMachineName")]
		public static void Test()
		{
			Debug.Log(Utility.GetUserName());
			Debug.Log(Utility.GetProjectID());
		}
	}


	[System.Serializable]
	public class SerializedBoard
	{
		public string id;

		[HideInInspector]
		public BoardAccessibility accessibility;

		public string title;
		public string createdBy;
		public long createdAt;

		public string[] entryIds;

		public SerializedBoard(Board board)
		{
			id = board.id;
			accessibility = board.accessibility;
			title = board.title;
			createdBy = board.createdBy;
			createdAt = board.createdAt;
			entryIds = board.entries.Select(item => item.id).ToArray();
		}
	}
}
