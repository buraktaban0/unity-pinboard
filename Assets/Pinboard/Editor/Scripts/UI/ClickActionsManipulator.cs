using System;
using UnityEngine.UIElements;

namespace Pinboard
{
	public class ClickActionsManipulator : MouseManipulator
	{

		public Action onClick;
		public Action onDoubleClick;
		
		private long clickTimestamp;
		private int clickCount = 0;
	
		public ClickActionsManipulator(Action onClick, Action onDoubleClick)
		{
			this.onClick = onClick;
			this.onDoubleClick = onDoubleClick;
		}
		
		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<ClickEvent>(OnClicked);
		}


		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<ClickEvent>(OnClicked);
		}


		private void OnClicked(ClickEvent evt)
		{
			if (clickCount == 1 && (evt.timestamp - clickTimestamp) < 500)
			{
				// double click
				clickCount = 0;
				onDoubleClick?.Invoke();
			}
			else
			{
				clickCount = 1;
				clickTimestamp = evt.timestamp;
				onClick?.Invoke();
			}
		}
	}
}
