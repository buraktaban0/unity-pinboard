using System;
using UnityEngine;

namespace Pinboard
{
	[System.Serializable]
	public struct TypedJson
	{
		public string type;
		public string data;

		public TypedJson(string type, string json)
		{
			this.type = type;
			this.data = json;
		}

		public static TypedJson Create(object obj)
		{
			return new TypedJson
			       {
				       type = obj.GetType().FullName,
				       data = JsonUtility.ToJson(obj)
			       };
		}

		public T ToObject<T>() where T : class
		{
			var t = Type.GetType(type);
			if (t == null)
				return null;
			return JsonUtility.FromJson(data, Type.GetType(type)) as T;
		}
	}
}
