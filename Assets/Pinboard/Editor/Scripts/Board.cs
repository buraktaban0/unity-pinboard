using System;
using System.Collections.Generic;
using System.Linq;
using Pinboard.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Pinboard
{
	public class Board
	{
		public string id = Guid.Get();

		public BoardAccessibility accessibility = BoardAccessibility.Global;

		public string title = "New Board";

		public string createdBy = PinboardCore.User;

		public long createdAt = Utility.GetUnixTimestamp();

		public DateTime CreationTime => Utility.FromUnixToLocal(createdAt);

		public List<BoardEntry> items = new List<BoardEntry>();

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

		public void Add(BoardEntry entry)
		{
			items.Add(entry);
		}

		public void Remove(BoardEntry item)
		{
			items.Remove(item);
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

		public string[] itemIds;

		public SerializedBoard(Board board)
		{
			id = board.id;
			accessibility = board.accessibility;
			title = board.title;
			createdBy = board.createdBy;
			createdAt = board.createdAt;
			itemIds = board.items.Select(item => item.id).ToArray();
		}
	}
}
