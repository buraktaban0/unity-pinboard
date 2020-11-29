using UnityEngine.UIElements;

namespace Pinboard.Items
{
	[System.Serializable]
	public class InvalidEntry : BoardEntry
	{
		public override void BindVisualElement(VisualElement el)
		{
			
		}

		public override void UnbindVisualElement(VisualElement el)
		{
			
		}

		public override bool Create()
		{
			return false;
		}

		public override bool EditOrUpdate()
		{
			return false;
		}
	}
}
