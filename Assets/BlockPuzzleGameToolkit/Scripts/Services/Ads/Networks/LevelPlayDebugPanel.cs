// // ©2015 - 2025 Candy Smith
// // All rights reserved

using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.Services.Ads.Networks
{
	public class LevelPlayDebugPanel : MonoBehaviour
	{
		[SerializeField] private string[] placementsToWatch = new string[] {"Interstitial", "Rewarded", "Banner"};
		[SerializeField] private bool showInReleaseBuilds = false;
		[SerializeField] private Vector2 panelSize = new Vector2(520, 150);
		[SerializeField] private float yStep = 22f;

		private void OnGUI()
		{
			#if !UNITY_EDITOR
			if (!showInReleaseBuilds) return;
			#endif

			var area = new Rect(10, 10, panelSize.x, panelSize.y);
			GUI.BeginGroup(area, GUI.skin.box);
			GUI.Label(new Rect(8, 6, area.width - 16, 20), "LevelPlay Debug Panel");
			float y = 28f;
			for (int i = 0; i < placementsToWatch.Length; i++)
			{
				var pid = placementsToWatch[i];
				string summary;
				if (LevelPlayDebugInfo.TryGetLastImpression(pid, out summary))
				{
					GUI.Label(new Rect(8, y, area.width - 16, 20), pid + ": " + summary);
				}
				else
				{
					GUI.Label(new Rect(8, y, area.width - 16, 20), pid + ": (no impression yet)");
				}
				y += yStep;
			}
			GUI.EndGroup();
		}
	}
}


