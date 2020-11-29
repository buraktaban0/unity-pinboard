using UnityEngine;

namespace Pinboard
{
	public class BoardEntryJsonContainer : ScriptableObject
	{
		public string type;
		
		[Multiline]
		public string data;
	}
}
