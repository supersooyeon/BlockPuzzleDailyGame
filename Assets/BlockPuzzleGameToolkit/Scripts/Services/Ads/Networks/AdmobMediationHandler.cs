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

using BlockPuzzleGameToolkit.Scripts.Services.Ads.AdUnits;
using BlockPuzzleGameToolkit.Scripts.Services.Ads;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
#if ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif

namespace BlockPuzzleGameToolkit.Scripts.Services.Ads.Networks
{
    [CreateAssetMenu(fileName = "AdmobMediationHandler", menuName = "BlockPuzzleGameToolkit/Ads/AdmobMediationHandler")]
    public class AdmobMediationHandler : AdsHandlerBase
    {
        private IAdsListener _listener;
        #if ADMOB
        private Dictionary<string, InterstitialAd> _interstitialAds = new Dictionary<string, InterstitialAd>();
        private Dictionary<string, RewardedAd> _rewardedAds = new Dictionary<string, RewardedAd>();
        private Dictionary<string, BannerView> _bannerAds = new Dictionary<string, BannerView>();
        private bool _isInitialized = false;
        private bool _mediationEnabled = true;
        #endif

        public override void Init(string _id, bool adSettingTestMode, IAdsListener listener)
        {
            #if ADMOB
            _listener = listener;
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            // Configure mediation settings
            var requestConfiguration = new RequestConfiguration();
            requestConfiguration.TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified;
            requestConfiguration.TagForUnderAgeOfConsent = TagForUnderAgeOfConsent.Unspecified;
            requestConfiguration.MaxAdContentRating = MaxAdContentRating.T;

            // Enable mediation
            if (_mediationEnabled)
            {
                requestConfiguration.TestDeviceIds = new List<string>();
                if (adSettingTestMode)
                {
                    requestConfiguration.TestDeviceIds.Add("TEST_DEVICE_ID");
                }
            }

            MobileAds.SetRequestConfiguration(requestConfiguration);

            MobileAds.Initialize(initstatus =>
            {
                if (initstatus == null)
                {
                    Debug.LogError("Google Mobile Ads Mediation initialization failed.");
                    return;
                }

                _isInitialized = true;

                // Check mediation adapter status
                var adapterStatusMap = initstatus.getAdapterStatusMap();
                if (adapterStatusMap != null)
                {
                    foreach (var item in adapterStatusMap)
                    {
                        Debug.Log($"Mediation Adapter {item.Key} is {item.Value.InitializationState}");
                        if (item.Value.InitializationState == AdapterState.Ready)
                        {
                            Debug.Log($"Mediation Adapter {item.Key} is ready for ads");
                        }
                        else if (item.Value.InitializationState == AdapterState.NotReady)
                        {
                            Debug.LogWarning($"Mediation Adapter {item.Key} is not ready: {item.Value.Description}");
                        }
                    }
                }

                Debug.Log("Google Mobile Ads Mediation initialization complete.");
                _listener?.OnAdsInitialized();
            });
            #endif
        }

        public override void Show(AdUnit adUnit)
        {
            #if ADMOB
            if (!_isInitialized)
            {
                Debug.LogError("AdMob Mediation Handler is not initialized.");
                return;
            }

            _listener?.Show(adUnit);

            if (adUnit.AdReference.adType == EAdType.Interstitial)
            {
                if (_interstitialAds.TryGetValue(adUnit.PlacementId, out var interstitialAd) && 
                    interstitialAd != null && interstitialAd.CanShowAd())
                {
                    Debug.Log($"Showing interstitial ad via mediation for placement: {adUnit.PlacementId}");
                    interstitialAd.Show();
                }
                else
                {
                    Debug.LogWarning($"Interstitial ad is not ready for placement: {adUnit.PlacementId}");
                    Load(adUnit);
                }
            }
            else if (adUnit.AdReference.adType == EAdType.Rewarded)
            {
                if (_rewardedAds.TryGetValue(adUnit.PlacementId, out var rewardedAd) && 
                    rewardedAd != null && rewardedAd.CanShowAd())
                {
                    Debug.Log($"Showing rewarded ad via mediation for placement: {adUnit.PlacementId}");
                    rewardedAd.Show(reward =>
                    {
                        Debug.Log($"Rewarded ad granted a reward: {reward.Amount} {reward.Type}");
                        _listener?.OnAdsShowComplete();
                    });
                }
                else
                {
                    Debug.LogWarning($"Rewarded ad is not ready for placement: {adUnit.PlacementId}");
                    Load(adUnit);
                }
            }
            else if (adUnit.AdReference.adType == EAdType.Banner)
            {
                if (_bannerAds.TryGetValue(adUnit.PlacementId, out var bannerAd) && bannerAd != null)
                {
                    bannerAd.Show();
                    Debug.Log($"Showing banner ad via mediation for placement: {adUnit.PlacementId}");
                }
                else
                {
                    Debug.LogWarning($"Banner ad not found for placement: {adUnit.PlacementId}");
                    Load(adUnit);
                }
            }
            #endif
        }

        public override void Load(AdUnit adUnit)
        {
            #if ADMOB
            if (!_isInitialized)
            {
                Debug.LogError("AdMob Mediation Handler is not initialized.");
                return;
            }

            var adRequest = new AdRequest();

            if (adUnit.AdReference.adType == EAdType.Interstitial)
            {
                InterstitialAd.Load(adUnit.PlacementId, adRequest, (ad, error) =>
                {
                    if (error != null)
                    {
                        Debug.LogError($"Interstitial ad failed to load via mediation for placement {adUnit.PlacementId} with error: {error.GetMessage()}");
                        _listener?.OnAdsLoadFailed();
                        return;
                    }

                    if (ad == null)
                    {
                        Debug.LogError($"Unexpected error: Interstitial load event fired with null ad for placement {adUnit.PlacementId}");
                        _listener?.OnAdsLoadFailed();
                        return;
                    }

                    _interstitialAds[adUnit.PlacementId] = ad;
                    Debug.Log($"Interstitial ad loaded successfully via mediation for placement: {adUnit.PlacementId}");

                    // Set up event handlers
                    ad.OnAdFullScreenContentClosed += () => 
                    {
                        Debug.Log($"Interstitial ad closed for placement: {adUnit.PlacementId}");
                        _listener?.OnAdsShowComplete();
                        Load(adUnit);
                    };

                    ad.OnAdFullScreenContentFailed += (AdError adError) =>
                    {
                        Debug.LogError($"Interstitial ad failed to show for placement {adUnit.PlacementId}: {adError.GetMessage()}");
                        _listener?.OnAdsLoadFailed();
                    };

                    ad.OnAdClicked += () =>
                    {
                        Debug.Log($"Interstitial ad clicked for placement: {adUnit.PlacementId}");
                        _listener?.OnAdsShowClick();
                    };

                    ad.OnAdImpressionRecorded += () =>
                    {
                        Debug.Log($"Interstitial ad impression recorded for placement: {adUnit.PlacementId}");
                        _listener?.OnAdsShowStart();
                    };

                    _listener?.OnAdsLoaded(adUnit.PlacementId);
                });
            }
            else if (adUnit.AdReference.adType == EAdType.Rewarded)
            {
                RewardedAd.Load(adUnit.PlacementId, adRequest, (ad, error) =>
                {
                    if (error != null)
                    {
                        Debug.LogError($"Rewarded ad failed to load via mediation for placement {adUnit.PlacementId} with error: {error.GetMessage()}");
                        _listener?.OnAdsLoadFailed();
                        return;
                    }

                    if (ad == null)
                    {
                        Debug.LogError($"Unexpected error: Rewarded load event fired with null ad for placement {adUnit.PlacementId}");
                        _listener?.OnAdsLoadFailed();
                        return;
                    }

                    _rewardedAds[adUnit.PlacementId] = ad;
                    Debug.Log($"Rewarded ad loaded successfully via mediation for placement: {adUnit.PlacementId}");

                    // Set up event handlers
                    ad.OnAdFullScreenContentClosed += () => 
                    {
                        Debug.Log($"Rewarded ad closed for placement: {adUnit.PlacementId}");
                        _listener?.OnAdsShowComplete();
                        Load(adUnit);
                    };

                    ad.OnAdFullScreenContentFailed += (AdError adError) =>
                    {
                        Debug.LogError($"Rewarded ad failed to show for placement {adUnit.PlacementId}: {adError.GetMessage()}");
                        _listener?.OnAdsLoadFailed();
                    };

                    ad.OnAdClicked += () =>
                    {
                        Debug.Log($"Rewarded ad clicked for placement: {adUnit.PlacementId}");
                        _listener?.OnAdsShowClick();
                    };

                    ad.OnAdImpressionRecorded += () =>
                    {
                        Debug.Log($"Rewarded ad impression recorded for placement: {adUnit.PlacementId}");
                        _listener?.OnAdsShowStart();
                    };

                    _listener?.OnAdsLoaded(adUnit.PlacementId);
                });
            }
            else if (adUnit.AdReference.adType == EAdType.Banner)
            {
                // Destroy any existing banner for this placement
                if (_bannerAds.TryGetValue(adUnit.PlacementId, out var existingBanner) && existingBanner != null)
                {
                    existingBanner.Destroy();
                    _bannerAds.Remove(adUnit.PlacementId);
                }

                var bannerView = new BannerView(adUnit.PlacementId, AdSize.Banner, AdPosition.Bottom);

                bannerView.OnBannerAdLoaded += () =>
                {
                    Debug.Log($"Banner ad loaded successfully via mediation for placement: {adUnit.PlacementId}");
                    _listener?.OnAdsLoaded(adUnit.PlacementId);
                };

                bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
                {
                    Debug.LogError($"Banner ad failed to load via mediation for placement {adUnit.PlacementId} with error: {error.GetMessage()}");
                    _listener?.OnAdsLoadFailed();
                    
                    if (_bannerAds.ContainsKey(adUnit.PlacementId))
                    {
                        _bannerAds.Remove(adUnit.PlacementId);
                    }
                };

                bannerView.OnAdPaid += (AdValue adValue) => 
                { 
                    Debug.Log($"Banner ad paid {adValue.Value} {adValue.CurrencyCode} for placement: {adUnit.PlacementId}"); 
                };

                bannerView.OnAdClicked += () =>
                {
                    Debug.Log($"Banner ad clicked for placement: {adUnit.PlacementId}");
                    _listener?.OnAdsShowClick();
                };

                bannerView.OnAdImpressionRecorded += () =>
                {
                    Debug.Log($"Banner ad impression recorded for placement: {adUnit.PlacementId}");
                    _listener?.OnAdsShowStart();
                };

                _bannerAds[adUnit.PlacementId] = bannerView;
                bannerView.LoadAd(adRequest);
                Debug.Log($"Requested banner ad load via mediation for placement: {adUnit.PlacementId}");
            }
            #endif
        }

        public override bool IsAvailable(AdUnit adUnit)
        {
            #if ADMOB
            if (!_isInitialized) return false;

            if (adUnit.AdReference.adType == EAdType.Interstitial)
            {
                return _interstitialAds.TryGetValue(adUnit.PlacementId, out var ad) && 
                       ad != null && ad.CanShowAd();
            }
            else if (adUnit.AdReference.adType == EAdType.Rewarded)
            {
                return _rewardedAds.TryGetValue(adUnit.PlacementId, out var ad) && 
                       ad != null && ad.CanShowAd();
            }
            else if (adUnit.AdReference.adType == EAdType.Banner)
            {
                return _bannerAds.TryGetValue(adUnit.PlacementId, out var ad) && ad != null;
            }
            #endif
            return false;
        }

        public override void Hide(AdUnit adUnit)
        {
            #if ADMOB
            if (adUnit.AdReference.adType == EAdType.Banner)
            {
                if (_bannerAds.TryGetValue(adUnit.PlacementId, out var bannerView) && bannerView != null)
                {
                    bannerView.Hide();
                    Debug.Log($"Banner ad hidden for placement: {adUnit.PlacementId}");
                }
            }
            #endif
        }

        private void OnDestroy()
        {
            #if ADMOB
            // Clean up ads
            foreach (var ad in _interstitialAds.Values)
            {
                ad?.Destroy();
            }
            _interstitialAds.Clear();

            foreach (var ad in _rewardedAds.Values)
            {
                ad?.Destroy();
            }
            _rewardedAds.Clear();

            foreach (var ad in _bannerAds.Values)
            {
                ad?.Destroy();
            }
            _bannerAds.Clear();
            #endif
        }
    }
}