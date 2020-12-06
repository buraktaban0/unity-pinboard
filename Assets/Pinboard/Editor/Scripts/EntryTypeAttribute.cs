using System;

namespace Pinboard
{
	public class EntryTypeAttribute : Attribute
	{
		public string visibleName = "Not defined";

		public bool canBeCreatedExplicitly = false;

		public BoardAccessibility[] disallowedAccessibilities = { };


		public EntryTypeAttribute(string visibleName, bool canBeCreatedExplicitly,
		                          params BoardAccessibility[] disallowedAccessibilities)
		{
			this.visibleName = visibleName;
			this.canBeCreatedExplicitly = canBeCreatedExplicitly;
			this.disallowedAccessibilities = disallowedAccessibilities;
		}
	}
}
