using System;
using System.Collections.Generic;
using Pinboard.Items;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	[System.Serializable]
	public class A
	{
		[SerializeReference]
		public List<B> bs = new List<B>() {new B()};
	}

	[System.Serializable]
	public class B
	{
		public string s = "testString";
		public int x = 7;
	}

	public class Test : ScriptableObject, ISerializationCallbackReceiver
	{
		public A a;

		public void OnBeforeSerialize()
		{
			//TestPrint();
		}

		public void OnAfterDeserialize()
		{
			//TestPrint();
		}


		private static Test obj;

		[MenuItem("Test/setclip")]
		public static void SetClipboard()
		{
			GUIUtility.systemCopyBuffer = "Test!!!";
		}

		[MenuItem("Test/getclip")]
		public static void GetClipboard()
		{
			Debug.Log(GUIUtility.systemCopyBuffer);
		}


		[MenuItem("Test/loop")]
		public static void TestLoop()
		{
			Debug.Log("before");

			PinboardDatabase.Current.WillModifyEntry(new NoteEntry("This is my content"));

			TextEditPopup.ShowPopup("asd", "qwe", s => { Debug.Log("done"); });
			Debug.Log("after");
		}


		[MenuItem("Test/A")]
		public static void TestA()
		{
			obj = ScriptableObject.CreateInstance<Test>();
			obj.a = new A();
			TestPrint();
		}


		[MenuItem("Test/Record")]
		public static void Record()
		{
			Undo.RegisterCompleteObjectUndo(obj, "asdasdasd");
			TestPrint();
		}


		[MenuItem("Test/SetA")]
		public static void SetA()
		{
			obj.a.bs[0].s = "testA";
			obj.a.bs[0].x = 10;
			TestPrint();
		}

		[MenuItem("Test/Remove")]
		public static void Remove()
		{
			obj.a.bs.RemoveAt(0);
		}


		[MenuItem("Test/SetB")]
		public static void SetB()
		{
			obj.a.bs[0].s = "testB";
			obj.a.bs[0].x = 11;

			TestPrint();
		}

		[MenuItem("Test/Print")]
		public static void TestPrint()
		{
			Debug.Log(obj.a.bs[0].s);
			Debug.Log(obj.a.bs[0].x);
		}
	}
}
