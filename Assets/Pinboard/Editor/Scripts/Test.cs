using System;
using System.Collections.Generic;
using System.Reflection;
using Pinboard.Entries;
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
		
		[MenuItem("Test/log id ")]
		public static void LogId()
		{
			Debug.Log(Selection.activeObject.GetInstanceID() + "  " + Selection.activeObject.name);
		}


		[MenuItem("Test/Log menu items")]
		public static void LogMenuItems()
		{
			var attrType = typeof(MenuItem);
			int a = 0;
			int t = 0;
			int m = 0;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				a++;
				foreach (var type in assembly.GetTypes())
				{
					t++;
					foreach (var method in type.GetMethods())
					{
						var attr = method.GetCustomAttribute(typeof(MenuItem));

						if (attr == null)
						{
							continue;
						}

						var menuAttr = attr as MenuItem;

						if (menuAttr == null)
							continue;

						
						var path = menuAttr.menuItem;
						Debug.Log(path);
					}
				}
			}

			Debug.Log(EditorGUIUtility.SerializeMainMenuToString());

			Debug.Log(a + "  " + t + "  " + m);
		}

		[MenuItem("Test/Count databases")]
		public static void CountDatabases()
		{
			Debug.Log(Resources.FindObjectsOfTypeAll<PinboardDatabase>().Length);
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
