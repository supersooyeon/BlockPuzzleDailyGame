using UnityEngine;

namespace BlockPuzzleGameToolkit.Services.Ads.Networks
{
    public class EditorAdDebugUI : MonoBehaviour
    {
        [Header("Debug UI Settings")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private Vector2 windowPosition = new Vector2(10, 10);
        [SerializeField] private Vector2 windowSize = new Vector2(300, 200);
        
        private string currentAdType = "없음";
        private string currentPlacementId = "없음";
        private string adStatus = "대기 중";
        private float adShowTime = 0f;
        private bool isShowingAd = false;

        private void OnGUI()
        {
            if (!showDebugUI || !Application.isEditor) return;

            GUI.Window(0, new Rect(windowPosition, windowSize), DrawDebugWindow, "에디터 광고 디버그");
        }

        private void DrawDebugWindow(int windowID)
        {
            GUILayout.BeginVertical();
            
            GUILayout.Label("🎬 에디터 광고 디버그", GUI.skin.box);
            GUILayout.Space(10);
            
            GUILayout.Label($"광고 타입: {currentAdType}");
            GUILayout.Label($"배치 ID: {currentPlacementId}");
            GUILayout.Label($"상태: {adStatus}");
            
            if (isShowingAd)
            {
                GUILayout.Label($"남은 시간: {adShowTime:F1}초");
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("전면광고 테스트"))
            {
                TestInterstitialAd();
            }
            
            if (GUILayout.Button("보상형 광고 테스트"))
            {
                TestRewardedAd();
            }
            
            if (GUILayout.Button("배너 광고 테스트"))
            {
                TestBannerAd();
            }
            
            GUILayout.EndVertical();
        }

        public void ShowAd(string adType, string placementId, float duration)
        {
            currentAdType = adType;
            currentPlacementId = placementId;
            adStatus = "표시 중";
            adShowTime = duration;
            isShowingAd = true;
            
            StartCoroutine(AdShowCoroutine(duration));
        }

        private System.Collections.IEnumerator AdShowCoroutine(float duration)
        {
            while (adShowTime > 0)
            {
                adShowTime -= Time.deltaTime;
                yield return null;
            }
            
            adStatus = "완료";
            isShowingAd = false;
        }

        private void TestInterstitialAd()
        {
            ShowAd("전면광고", "Interstitial_Test", 3f);
        }

        private void TestRewardedAd()
        {
            ShowAd("보상형 광고", "Rewarded_Test", 5f);
        }

        private void TestBannerAd()
        {
            ShowAd("배너 광고", "Banner_Test", 2f);
        }
    }
} 