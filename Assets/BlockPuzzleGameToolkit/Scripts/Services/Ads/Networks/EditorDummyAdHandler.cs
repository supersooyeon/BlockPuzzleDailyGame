using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzleGameToolkit.Scripts.Services.Ads.AdUnits;

namespace BlockPuzzleGameToolkit.Services.Ads.Networks
{
    [CreateAssetMenu(fileName = "EditorDummyAdHandler", menuName = "BlockPuzzleGameToolkit/Ads/EditorDummyAdHandler")]
    public class EditorDummyAdHandler : AdsHandlerBase
    {
        [Header("Editor Dummy Ad Settings")]
        [SerializeField] private float adShowDelay = 2f;
        [SerializeField] private bool showDebugUI = true;
        
        private Dictionary<string, bool> _interstitialLoaded = new Dictionary<string, bool>();
        private Dictionary<string, bool> _rewardedLoaded = new Dictionary<string, bool>();
        private Dictionary<string, bool> _bannerLoaded = new Dictionary<string, bool>();
        private bool _isInitialized = false;
        private IAdsListener _listener;
        private MonoBehaviour _coroutineRunner;
        private EditorAdDebugUI _debugUI;

        public override void Init(string appId, bool adSettingTestMode, IAdsListener listener)
        {
            Debug.Log($"[에디터더미] 앱ID {appId}, 테스트모드 {adSettingTestMode}로 초기화 호출됨");
            
            if (_isInitialized)
            {
                Debug.Log("[에디터더미] 이미 초기화됨");
                return;
            }

            Debug.Log($"[에디터더미] 에디터용 더미 광고 시스템 초기화 중");
            Debug.Log($"[에디터더미] 플랫폼: {Application.platform}");
            
            _listener = listener;
            
            // 코루틴을 실행할 GameObject 생성
            var runnerObj = new GameObject("EditorDummyAdRunner");
            _coroutineRunner = runnerObj.AddComponent<MonoBehaviour>();
            _debugUI = runnerObj.AddComponent<EditorAdDebugUI>();
            UnityEngine.Object.DontDestroyOnLoad(runnerObj);
            
            _isInitialized = true;
            Debug.Log("[에디터더미] 초기화 완료");
            
            // 즉시 초기화 완료 이벤트 발생
            _listener?.OnAdsInitialized();
        }

        public override void Load(AdUnit adUnit)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[에디터더미] 초기화되지 않음. Init() 먼저 호출하세요.");
                return;
            }

            Debug.Log($"[에디터더미] 배치ID {adUnit.PlacementId}, 광고타입 {adUnit.AdReference.adType} 로드 중");

            switch (adUnit.AdReference.adType)
            {
                case EAdType.Interstitial:
                    LoadInterstitial(adUnit.PlacementId);
                    break;
                case EAdType.Rewarded:
                    LoadRewarded(adUnit.PlacementId);
                    break;
                case EAdType.Banner:
                    LoadBanner(adUnit.PlacementId);
                    break;
                default:
                    Debug.LogWarning($"[에디터더미] 지원하지 않는 광고 타입: {adUnit.AdReference.adType}");
                    break;
            }
        }

        private void LoadInterstitial(string placementId)
        {
            if (_interstitialLoaded.ContainsKey(placementId) && _interstitialLoaded[placementId])
            {
                Debug.Log($"[에디터더미] 배치ID {placementId} 전면광고 이미 로드됨");
                return;
            }

            Debug.Log($"[에디터더미] 배치ID {placementId} 전면광고 로드 중");
            
            // 1초 후 로드 완료 시뮬레이션
            _coroutineRunner.StartCoroutine(SimulateAdLoad(placementId, EAdType.Interstitial));
        }

        private void LoadRewarded(string placementId)
        {
            if (_rewardedLoaded.ContainsKey(placementId) && _rewardedLoaded[placementId])
            {
                Debug.Log($"[에디터더미] 배치ID {placementId} 보상형 광고 이미 로드됨");
                return;
            }

            Debug.Log($"[에디터더미] 배치ID {placementId} 보상형 광고 로드 중");
            
            // 1초 후 로드 완료 시뮬레이션
            _coroutineRunner.StartCoroutine(SimulateAdLoad(placementId, EAdType.Rewarded));
        }

        private void LoadBanner(string placementId)
        {
            if (_bannerLoaded.ContainsKey(placementId) && _bannerLoaded[placementId])
            {
                Debug.Log($"[에디터더미] 배치ID {placementId} 배너 이미 로드됨");
                return;
            }

            Debug.Log($"[에디터더미] 배치ID {placementId} 배너 로드 중");
            
            // 즉시 로드 완료 시뮬레이션
            _coroutineRunner.StartCoroutine(SimulateAdLoad(placementId, EAdType.Banner));
        }

        private IEnumerator SimulateAdLoad(string placementId, EAdType adType)
        {
            yield return new WaitForSeconds(1f);
            
            switch (adType)
            {
                case EAdType.Interstitial:
                    _interstitialLoaded[placementId] = true;
                    break;
                case EAdType.Rewarded:
                    _rewardedLoaded[placementId] = true;
                    break;
                case EAdType.Banner:
                    _bannerLoaded[placementId] = true;
                    break;
            }
            
            Debug.Log($"[에디터더미] 배치ID {placementId} {adType} 광고 로드 완료");
            _listener?.OnAdsLoaded(placementId);
        }

        public override void Show(AdUnit adUnit)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[에디터더미] 초기화되지 않음");
                return;
            }

            Debug.Log($"[에디터더미] 배치ID {adUnit.PlacementId}, 광고타입 {adUnit.AdReference.adType} 표시 중");

            switch (adUnit.AdReference.adType)
            {
                case EAdType.Interstitial:
                    ShowInterstitial(adUnit.PlacementId);
                    break;
                case EAdType.Rewarded:
                    ShowRewarded(adUnit.PlacementId);
                    break;
                case EAdType.Banner:
                    ShowBanner(adUnit.PlacementId);
                    break;
                default:
                    Debug.LogWarning($"[에디터더미] 지원하지 않는 광고 타입: {adUnit.AdReference.adType}");
                    break;
            }
        }

        private void ShowInterstitial(string placementId)
        {
            if (!_interstitialLoaded.ContainsKey(placementId) || !_interstitialLoaded[placementId])
            {
                Debug.LogWarning($"[에디터더미] 배치ID {placementId} 전면광고 로드되지 않음");
                return;
            }

            Debug.Log($"[에디터더미] 배치ID {placementId} 전면광고 표시 중");
            
            // 에디터에서 전면광고 표시 시뮬레이션
            _coroutineRunner.StartCoroutine(SimulateInterstitialAd(placementId));
        }

        private void ShowRewarded(string placementId)
        {
            if (!_rewardedLoaded.ContainsKey(placementId) || !_rewardedLoaded[placementId])
            {
                Debug.LogWarning($"[에디터더미] 배치ID {placementId} 보상형 광고 로드되지 않음");
                return;
            }

            Debug.Log($"[에디터더미] 배치ID {placementId} 보상형 광고 표시 중");
            
            // 에디터에서 보상형 광고 표시 시뮬레이션
            _coroutineRunner.StartCoroutine(SimulateRewardedAd(placementId));
        }

        private void ShowBanner(string placementId)
        {
            if (!_bannerLoaded.ContainsKey(placementId) || !_bannerLoaded[placementId])
            {
                Debug.LogWarning($"[에디터더미] 배치ID {placementId} 배너 로드되지 않음");
                return;
            }

            Debug.Log($"[에디터더미] 배치ID {placementId} 배너 표시 중");
            
            // 에디터에서 배너 광고 표시 시뮬레이션
            _coroutineRunner.StartCoroutine(SimulateBannerAd(placementId));
        }

        private IEnumerator SimulateInterstitialAd(string placementId)
        {
            Debug.Log($"[에디터더미] 🎬 전면광고 표시 시작: {placementId}");
            
            // 디버그 UI에 광고 표시 상태 업데이트
            _debugUI?.ShowAd("전면광고", placementId, adShowDelay);
            
            yield return new WaitForSeconds(adShowDelay);
            
            Debug.Log($"[에디터더미] ✅ 전면광고 표시 완료: {placementId}");
            _listener?.OnAdsShowComplete();
            
            // 로드 상태 초기화
            _interstitialLoaded[placementId] = false;
        }

        private IEnumerator SimulateRewardedAd(string placementId)
        {
            Debug.Log($"[에디터더미] 🎬 보상형 광고 표시 시작: {placementId}");
            
            // 디버그 UI에 광고 표시 상태 업데이트
            _debugUI?.ShowAd("보상형 광고", placementId, adShowDelay);
            
            yield return new WaitForSeconds(adShowDelay);
            
            Debug.Log($"[에디터더미] ✅ 보상형 광고 표시 완료: {placementId}");
            _listener?.OnAdsShowComplete();
            
            // 로드 상태 초기화
            _rewardedLoaded[placementId] = false;
        }

        private IEnumerator SimulateBannerAd(string placementId)
        {
            Debug.Log($"[에디터더미] 🎬 배너 광고 표시 시작: {placementId}");
            
            // 디버그 UI에 광고 표시 상태 업데이트
            _debugUI?.ShowAd("배너 광고", placementId, 1f);
            
            yield return new WaitForSeconds(1f);
            
            Debug.Log($"[에디터더미] ✅ 배너 광고 표시 완료: {placementId}");
            _listener?.OnAdsShowComplete();
        }

        public override bool IsAvailable(AdUnit adUnit)
        {
            switch (adUnit.AdReference.adType)
            {
                case EAdType.Interstitial:
                    return _interstitialLoaded.ContainsKey(adUnit.PlacementId) && _interstitialLoaded[adUnit.PlacementId];
                case EAdType.Rewarded:
                    return _rewardedLoaded.ContainsKey(adUnit.PlacementId) && _rewardedLoaded[adUnit.PlacementId];
                case EAdType.Banner:
                    return _bannerLoaded.ContainsKey(adUnit.PlacementId) && _bannerLoaded[adUnit.PlacementId];
                default:
                    return false;
            }
        }

        public override void Hide(AdUnit adUnit)
        {
            if (adUnit.AdReference.adType == EAdType.Banner)
            {
                Debug.Log($"[에디터더미] 배치ID {adUnit.PlacementId} 배너 숨김");
                
                if (_bannerLoaded.ContainsKey(adUnit.PlacementId))
                {
                    _bannerLoaded[adUnit.PlacementId] = false;
                }
            }
        }

        private void OnDestroy()
        {
            if (_coroutineRunner != null)
            {
                UnityEngine.Object.DestroyImmediate(_coroutineRunner.gameObject);
            }
        }
    }
} 