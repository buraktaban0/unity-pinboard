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
	[System.Serializable]
	public class Board
	{
		public string id = UID.Get();

		public BoardAccessibility accessibility = BoardAccessibility.Global;

		public string title = "New Board";

		public string createdBy = PinboardCore.User;

		[FormerlySerializedAs("createdAtUnix")]
		public long createdAt = Utility.GetUnixTimestamp();

		public DateTime CreationTime => Utility.FromUnixToLocal(createdAt);

		public List<BoardItem> items = new List<BoardItem>();

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
			items = serializedBoard.items.Select(typedJson => typedJson.ToObject<BoardItem>()).ToList();
		}

		public void Add(BoardItem item)
		{
			items.Add(item);
		}

		[MenuItem("Tools/TestMachineName")]
		public static void Test()
		{
			Debug.Log(Utility.GetUserName());
			Debug.Log(Utility.GetProjectName());
		}
	}


	[System.Serializable]
	public class SerializedBoard
	{
		public string id;
		public BoardAccessibility accessibility;
		public string title;
		public string createdBy;
		public long createdAt;
		public TypedJson[] items;

		public SerializedBoard(Board board)
		{
			id = board.id;
			accessibility = board.accessibility;
			title = board.title;
			createdBy = board.createdBy;
			createdAt = board.createdAt;
			items = board.items.Select(TypedJson.Create).ToArray();
		}
	}
}
