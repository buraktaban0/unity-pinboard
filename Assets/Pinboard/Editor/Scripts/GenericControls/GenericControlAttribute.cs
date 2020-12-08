using System;

namespace Pinboard
{
	public class GenericControlAttribute : Attribute
	{
		public string name;
		public bool isValidateMethod;


		public GenericControlAttribute(string name, bool isValidateMethod = false)
		{
			this.name = name;
			this.isValidateMethod = isValidateMethod;
		}
	}
}
