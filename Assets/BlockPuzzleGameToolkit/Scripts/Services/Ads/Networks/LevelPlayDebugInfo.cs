// // ©2015 - 2025 Candy Smith
// // All rights reserved

using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.Services.Ads.Networks
{
	public static class LevelPlayDebugInfo
	{
		// placementId -> last impression summary
		private static readonly Dictionary<string, string> s_LastImpressionByPlacement = new Dictionary<string, string>();

		public static void SetLastImpression(string placementId, string summary)
		{
			if (string.IsNullOrEmpty(placementId)) return;
			s_LastImpressionByPlacement[placementId] = summary;
		}

		public static bool TryGetLastImpression(string placementId, out string summary)
		{
			return s_LastImpressionByPlacement.TryGetValue(placementId, out summary);
		}

		public static void Clear()
		{
			s_LastImpressionByPlacement.Clear();
		}
	}
}


