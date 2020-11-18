using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	[System.Serializable]
	public class Board
	{
		public List<BoardItem> items;


		[MenuItem("Tools/TestMachineName")]
		public static void Test()
		{
			Debug.Log(Utility.GetGitUserName());
		}
	}
}
