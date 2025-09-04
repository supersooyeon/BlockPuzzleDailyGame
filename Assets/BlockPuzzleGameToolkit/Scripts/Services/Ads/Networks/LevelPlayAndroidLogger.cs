// // ©2015 - 2025 Candy Smith
// // All rights reserved

using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.Services.Ads.Networks
{
	internal static class LevelPlayAndroidLogger
	{
		private const string AndroidTag = "LevelPlay";

		public static void Log(string message)
		{
			if (string.IsNullOrEmpty(message)) return;
			Debug.Log(message);
			#if UNITY_ANDROID && !UNITY_EDITOR
			try
			{
				using (var log = new AndroidJavaClass("android.util.Log"))
				{
					log.CallStatic<int>("i", AndroidTag, message);
				}
			}
			catch {}
			#endif
		}
	}
}


