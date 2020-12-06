using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;



namespace Pinboard.UI
{
	public class UnityObjectEditorContainerWindow : EditorWindow
	{
		
		private static UnityEngine.Object s_target;

		public static void Show(UnityEngine.Object target)
		{
			s_target = ((GameObject)target).transform;

			var win = ScriptableObject.CreateInstance<UnityObjectEditorContainerWindow>();


			win.Show();
		}

		public Object target;
		public Editor editor;

		private void OnEnable()
		{
			
			if (target == null)
				target = s_target;

			editor = Editor.CreateEditor(target);

			var inspectorRoot = editor.CreateInspectorGUI();

			if (inspectorRoot != null)
			{
				this.rootVisualElement.Add(inspectorRoot);

				return;
			}

			IMGUIContainer imguiContainer = null;
			imguiContainer = new IMGUIContainer(() =>
			{
				
				//EditorGUIUtility.LookLikeInspector();
				editor.OnInspectorGUI();
				
				imguiContainer.MarkDirtyLayout();
				imguiContainer.MarkDirtyRepaint();
			});
			

			rootVisualElement.Add(imguiContainer);
		}

		private void OnDisable()
		{
			if (editor)
				Editor.DestroyImmediate(editor);
		}
	}
}
