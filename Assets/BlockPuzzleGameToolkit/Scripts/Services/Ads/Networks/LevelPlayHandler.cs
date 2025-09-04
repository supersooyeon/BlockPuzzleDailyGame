// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System.Collections.Generic;
using BlockPuzzleGameToolkit.Scripts.Services.Ads.AdUnits;
// LevelPlay SDK은 선택 패키지이므로 전처리 심볼로 가드
#if IRONSOURCE
using Unity.Services.LevelPlay;
#endif
using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.Services.Ads.Networks
{
	[CreateAssetMenu(fileName = "LevelPlayHandler", menuName = "BlockPuzzleGameToolkit/Ads/LevelPlayHandler")]
	public class LevelPlayHandler : AdsHandlerBase
	{
		private IAdsListener _listener;
		private bool _isInitialized;

#if IRONSOURCE
		private readonly Dictionary<string, LevelPlayInterstitialAd> _interstitialAds = new Dictionary<string, LevelPlayInterstitialAd>();
		private readonly Dictionary<string, LevelPlayRewardedAd> _rewardedAds = new Dictionary<string, LevelPlayRewardedAd>();
		private readonly Dictionary<string, LevelPlayBannerAd> _bannerAds = new Dictionary<string, LevelPlayBannerAd>();
#endif

		public override void Init(string _id, bool adSettingTestMode, IAdsListener listener)
		{
			_listener = listener;

#if IRONSOURCE
			LevelPlay.OnInitSuccess += OnSdkInitSuccess;
			LevelPlay.OnInitFailed += OnSdkInitFailed;
			LevelPlay.OnImpressionDataReady += OnImpressionDataReady;

			#if UNITY_EDITOR
			LevelPlay.ValidateIntegration();
			#endif

			LevelPlay.Init(_id);
#else
			Debug.LogWarning("[LevelPlayHandler] LevelPlay 패키지가 설치되지 않았습니다. Ads 초기화를 건너뜁니다.");
			_listener?.OnInitFailed();
#endif
		}

		public override void Show(AdUnit adUnit)
		{
			if (!_isInitialized) return;
			_listener?.Show(adUnit);

			switch (adUnit.AdReference.adType)
			{
				case EAdType.Interstitial:
				{
#if IRONSOURCE
					var ad = GetOrCreateInterstitial(adUnit.PlacementId);
					if (ad.IsAdReady())
					{
						ad.ShowAd();
					}
					else
					{
						ad.LoadAd();
					}
#endif
					break;
				}
				case EAdType.Rewarded:
				{
#if IRONSOURCE
					var ad = GetOrCreateRewarded(adUnit.PlacementId);
					if (ad.IsAdReady())
					{
						ad.ShowAd();
					}
					else
					{
						ad.LoadAd();
					}
#endif
					break;
				}
				case EAdType.Banner:
				{
#if IRONSOURCE
					var ad = GetOrCreateBanner(adUnit.PlacementId);
					ad.LoadAd();
#endif
					break;
				}
			}
		}

		public override void Load(AdUnit adUnit)
		{
			if (!_isInitialized) return;

			switch (adUnit.AdReference.adType)
			{
				case EAdType.Interstitial:
					#if IRONSOURCE
					GetOrCreateInterstitial(adUnit.PlacementId).LoadAd();
					#endif
					break;
				case EAdType.Rewarded:
					#if IRONSOURCE
					GetOrCreateRewarded(adUnit.PlacementId).LoadAd();
					#endif
					break;
				case EAdType.Banner:
					#if IRONSOURCE
					GetOrCreateBanner(adUnit.PlacementId).LoadAd();
					#endif
					break;
			}
		}

		public override bool IsAvailable(AdUnit adUnit)
		{
			Debug.Log($"[LevelPlayHandler] IsAvailable 호출됨 - _isInitialized: {_isInitialized}, adType: {adUnit.AdReference.adType}, PlacementId: {adUnit.PlacementId}");
			
			if (!_isInitialized) 
			{
				Debug.LogWarning("[LevelPlayHandler] SDK가 초기화되지 않음");
				return false;
			}

			switch (adUnit.AdReference.adType)
			{
				case EAdType.Interstitial:
					#if IRONSOURCE
					bool interstitialAvailable = _interstitialAds.ContainsKey(adUnit.PlacementId) && _interstitialAds[adUnit.PlacementId].IsAdReady();
					Debug.Log($"[LevelPlayHandler] Interstitial 광고 - ContainsKey: {_interstitialAds.ContainsKey(adUnit.PlacementId)}, IsAdReady: {(_interstitialAds.ContainsKey(adUnit.PlacementId) ? _interstitialAds[adUnit.PlacementId].IsAdReady() : false)}");
					return interstitialAvailable;
					#else
					Debug.Log("[LevelPlayHandler] IRONSOURCE 정의되지 않음");
					return false;
					#endif
				case EAdType.Rewarded:
					#if IRONSOURCE
					bool rewardedAvailable = _rewardedAds.ContainsKey(adUnit.PlacementId) && _rewardedAds[adUnit.PlacementId].IsAdReady();
					Debug.Log($"[LevelPlayHandler] Rewarded 광고 - ContainsKey: {_rewardedAds.ContainsKey(adUnit.PlacementId)}, IsAdReady: {(_rewardedAds.ContainsKey(adUnit.PlacementId) ? _rewardedAds[adUnit.PlacementId].IsAdReady() : false)}");
					return rewardedAvailable;
					#else
					Debug.Log("[LevelPlayHandler] IRONSOURCE 정의되지 않음");
					return false;
					#endif
				case EAdType.Banner:
					#if IRONSOURCE
					bool bannerAvailable = _bannerAds.ContainsKey(adUnit.PlacementId);
					Debug.Log($"[LevelPlayHandler] Banner 광고 - ContainsKey: {_bannerAds.ContainsKey(adUnit.PlacementId)}");
					return bannerAvailable;
					#else
					Debug.Log("[LevelPlayHandler] IRONSOURCE 정의되지 않음");
					return false;
					#endif
				default:
					Debug.LogWarning($"[LevelPlayHandler] 알 수 없는 광고 타입: {adUnit.AdReference.adType}");
					return false;
			}
		}

		public override void Hide(AdUnit adUnit)
		{
			if (adUnit.AdReference.adType != EAdType.Banner) return;
			if (_bannerAds.TryGetValue(adUnit.PlacementId, out var banner))
			{
				banner.HideAd();
			}
		}

		private void OnSdkInitSuccess(LevelPlayConfiguration config)
		{
			_isInitialized = true;
			_listener?.OnAdsInitialized();
		}

		private void OnSdkInitFailed(LevelPlayInitError error)
		{
			Debug.LogError($"[LevelPlayHandler] Init Failed: {error}");
			_listener?.OnInitFailed();
		}

#if IRONSOURCE
		private void OnImpressionDataReady(LevelPlayImpressionData impressionData)
		{
			LevelPlayAndroidLogger.Log($"[LevelPlay][Impression] ToString(): {impressionData}");
			LevelPlayAndroidLogger.Log($"[LevelPlay][Impression] AllData: {impressionData.AllData}");
			// 저장: placement, network, instanceName 등 간단 요약
			try
			{
				var placement = impressionData?.Placement ?? string.Empty;
				var network = impressionData?.AdNetwork;
				var instance = impressionData?.InstanceName;
				var revenue = impressionData?.Revenue.HasValue == true ? impressionData.Revenue.Value.ToString() : "n/a";
				var precision = impressionData?.Precision;
				var summary = $"network={network}, instance={instance}, revenue={revenue}, precision={precision}";
				LevelPlayDebugInfo.SetLastImpression(placement, summary);
				LevelPlayAndroidLogger.Log($"[LevelPlay][Impression] placement={placement}, {summary}");
			}
			catch { }
		}
#endif

		private LevelPlayInterstitialAd GetOrCreateInterstitial(string adUnitId)
		{
			if (_interstitialAds.TryGetValue(adUnitId, out var ad)) return ad;

			ad = new LevelPlayInterstitialAd(adUnitId);
			ad.OnAdLoaded += info => { Debug.Log($"[LevelPlay][Interstitial][Loaded] {info}"); _listener?.OnAdsLoaded(adUnitId); };
			ad.OnAdLoadFailed += error => { Debug.LogWarning($"[LevelPlay][Interstitial][LoadFailed] {error}"); _listener?.OnAdsLoadFailed(); };
			ad.OnAdDisplayed += info =>
			{
				Debug.Log($"[LevelPlay][Interstitial][Displayed] {info}");
				_listener?.OnAdsShowStart();
			};
			ad.OnAdDisplayFailed += error =>
			{
				Debug.LogWarning($"[LevelPlay][Interstitial][DisplayFailed] {error}");
				_listener?.OnAdsShowFailed();
			};
			ad.OnAdClicked += info => { Debug.Log($"[LevelPlay][Interstitial][Clicked] {info}"); _listener?.OnAdsShowClick(); };
			ad.OnAdClosed += info =>
			{
				Debug.Log($"[LevelPlay][Interstitial][Closed] {info}");
				_listener?.OnAdsShowComplete();
				ad.LoadAd();
			};
			_interstitialAds[adUnitId] = ad;
			return ad;
		}

		private LevelPlayRewardedAd GetOrCreateRewarded(string adUnitId)
		{
			if (_rewardedAds.TryGetValue(adUnitId, out var ad)) return ad;

			ad = new LevelPlayRewardedAd(adUnitId);
			ad.OnAdLoaded += info => { Debug.Log($"[LevelPlay][Rewarded][Loaded] {info}"); _listener?.OnAdsLoaded(adUnitId); };
			ad.OnAdLoadFailed += error => { Debug.LogWarning($"[LevelPlay][Rewarded][LoadFailed] {error}"); _listener?.OnAdsLoadFailed(); };
			ad.OnAdDisplayed += info =>
			{
				Debug.Log($"[LevelPlay][Rewarded][Displayed] {info}");
				_listener?.OnAdsShowStart();
			};
			ad.OnAdDisplayFailed += error =>
			{
				Debug.LogWarning($"[LevelPlay][Rewarded][DisplayFailed] {error}");
				_listener?.OnAdsShowFailed();
			};
			ad.OnAdClicked += info => { Debug.Log($"[LevelPlay][Rewarded][Clicked] {info}"); _listener?.OnAdsShowClick(); };
			ad.OnAdRewarded += (info, reward) =>
			{
				Debug.Log($"[LevelPlay][Rewarded][Rewarded] {info}, Reward: {reward}");
				_listener?.OnAdsShowComplete();
			};
			ad.OnAdClosed += info =>
			{
				Debug.Log($"[LevelPlay][Rewarded][Closed] {info}");
				// 광고 재로드
				ad.LoadAd();
			};
			_rewardedAds[adUnitId] = ad;
			return ad;
		}

		private LevelPlayBannerAd GetOrCreateBanner(string adUnitId)
		{
			if (_bannerAds.TryGetValue(adUnitId, out var ad)) return ad;

			ad = new LevelPlayBannerAd(adUnitId);
			ad.OnAdLoaded += info => { Debug.Log($"[LevelPlay][Banner][Loaded] {info}"); _listener?.OnAdsLoaded(adUnitId); };
			ad.OnAdLoadFailed += error => { Debug.LogWarning($"[LevelPlay][Banner][LoadFailed] {error}"); _listener?.OnAdsLoadFailed(); };
			ad.OnAdDisplayed += info => { Debug.Log($"[LevelPlay][Banner][Displayed] {info}"); };
			ad.OnAdDisplayFailed += error => { Debug.LogWarning($"[LevelPlay][Banner][DisplayFailed] {error}"); };
			ad.OnAdClicked += info => { Debug.Log($"[LevelPlay][Banner][Clicked] {info}"); _listener?.OnAdsShowClick(); };
			_bannerAds[adUnitId] = ad;
			return ad;
		}
	}
}


