using UnityEngine.UIElements;

namespace Pinboard.Items
{
	public class SimpleTextItem : BoardItem
	{
		public string content;
		
		public SimpleTextItem(string content)
		{
			this.content = content;
		}

		public override void BindVisualElement(VisualElement el)
		{
			var lbl = new Label(content);
			el.Add(lbl);
		}
	}
}
