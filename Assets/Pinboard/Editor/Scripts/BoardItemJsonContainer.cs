using UnityEngine;

namespace Pinboard
{
	public class BoardItemJsonContainer : ScriptableObject
	{
		public string type;
		
		[Multiline]
		public string data;
	}
}
