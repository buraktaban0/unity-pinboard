﻿using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pinboard
{
	[InitializeOnLoad]
	public static class PinboardCore
	{
		public static string DIR_ROOT = "Assets/Pinboard";
		public static string DIR_EDITOR = "Assets/Pinboard";
		public static string DIR_UI = DIR_EDITOR + "/UI";
		public static string DIR_DATA = DIR_EDITOR + "/Data";

		public static string PATH_CFG = DIR_ROOT + "/PinboardConfig.asset";
		public static string DIR_PROJECT => Application.dataPath.Replace("Assets", "");

		private static PinboardConfig _config;

		public static PinboardConfig Config
		{
			get
			{
				if (_config == null)
				{
					InitializeConfig();
				}

				return _config;
			}
		}


		private static string _projectID;

		public static string ProjectID
		{
			get
			{
				if (string.IsNullOrEmpty(_projectID))
				{
					_projectID = Utility.GetProjectID();
				}

				return _projectID;
			}
		}


		private static string _user;

		public static string User
		{
			get
			{
				if (string.IsNullOrEmpty(_user))
				{
					_user = Utility.GetUserName();
				}

				return _user;
			}
		}

		static PinboardCore()
		{
			Initialize();
		}


		public static void Initialize()
		{
			InitializeConfig();

			PinboardDatabase.LoadBoards();

			PinboardWindow.Instance?.Refresh();
		}


		private static void InitializeConfig()
		{
			_config = AssetDatabase.LoadAssetAtPath<PinboardConfig>(PATH_CFG);

			if (_config == null)
			{
				_config = ScriptableObject.CreateInstance<PinboardConfig>();
				var dir = DIR_PROJECT + DIR_ROOT;
				Debug.Log(dir);
				if (Directory.Exists(dir) == false)
				{
					Directory.CreateDirectory(dir);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}

				AssetDatabase.CreateAsset(_config, PATH_CFG);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}


		public static Board TryCreateBoard(CreateBoardOptions options)
		{
			var board = new Board();
			board.title = options.title;
			board.accessibility = options.accessibility;

			PinboardDatabase.AddBoard(board);
			PinboardDatabase.SaveBoards();

			return board;
		}


		[MenuItem("Pinboard/Clear All")]
		public static void ClearAllBoards()
		{
			PinboardCore.Initialize();
			PinboardDatabase.boards.ToList().ForEach(PinboardDatabase.DeleteBoard);
			PinboardWindow.Instance?.Refresh();
		}
	}
}
