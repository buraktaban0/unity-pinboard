using System;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	public class TypedJsonContainer : ScriptableObject
	{
		public static TypedJsonContainer Serialize(object obj)
		{
			var type = obj.GetType().FullName;
			var json = JsonUtility.ToJson(obj);
			var container = ScriptableObject.CreateInstance<TypedJsonContainer>();
			container.type = type;
			container.json = json;

			return container;
		}

		public static void SerializeAndSave(object obj, string path)
		{
			var container = Serialize(obj);
			path = AssetDatabase.GenerateUniqueAssetPath(path);
			AssetDatabase.CreateAsset(container, path);
			AssetDatabase.SaveAssets();
		}

		public string type;
		public string json;


		public object Deserialize()
		{
			var type = System.Type.GetType(this.type);
			var obj = JsonUtility.FromJson(json, type);
			return obj;
		}
	}
}
