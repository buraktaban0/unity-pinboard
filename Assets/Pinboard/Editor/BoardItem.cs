using System;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	[System.Serializable]
	public class BoardItem
	{
		public string author = "";

		public long createdAtUnix = 0;
		public DateTime CreatedAt => Utility.FromUnixToLocal(createdAtUnix);
		
		
		
	}
}
