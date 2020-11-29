using UnityEngine.UIElements;

namespace Pinboard.Items
{
	[System.Serializable]
	public class InvalidEntry : Entry
	{
		public override string ShortVisibleName => "Invalid Entry";

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
