using System;
using UnityEngine;

namespace Pinboard
{
	public struct TypedJson
	{
		public string type;
		public string data;

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
			return JsonUtility.FromJson(data, Type.GetType(type)) as T;
		}
	}
}
