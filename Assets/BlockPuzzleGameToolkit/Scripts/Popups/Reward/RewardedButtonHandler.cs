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

using BlockPuzzleGameToolkit.Scripts.GUI;
using BlockPuzzleGameToolkit.Scripts.Services;
using BlockPuzzleGameToolkit.Scripts.Services.Ads.AdUnits;
using UnityEngine;
using UnityEngine.Events;

namespace BlockPuzzleGameToolkit.Scripts.Popups.Reward
{
    public class RewardedButtonHandler : MonoBehaviour
    {
        [SerializeField]
        private AdReference adReference;

        [SerializeField]
        private CustomButton rewardedButton;

        [SerializeField]
        private UnityEvent onRewardedAdComplete;

        [SerializeField]
        private UnityEvent onRewardedShow;

        private void Awake()
        {
            rewardedButton.onClick.AddListener(ShowRewardedAd);
        }

        private void ShowRewardedAd()
        {
            Debug.Log($"[RewardedButtonHandler] ShowRewardedAd 호출됨 - adReference: {adReference}");
            Debug.Log($"[RewardedButtonHandler] AdsManager.instance null: {AdsManager.instance == null}");
            
            if (AdsManager.instance != null)
            {
                bool isAvailable = AdsManager.instance.IsRewardedAvailable(adReference);
                Debug.Log($"[RewardedButtonHandler] IsRewardedAvailable 결과: {isAvailable}");
                
                if (isAvailable)
                {
                    Debug.Log("[RewardedButtonHandler] 리워드 광고 표시 시작");
                    onRewardedShow?.Invoke();
                    AdsManager.instance.ShowAdByType(adReference, _ => {
                        Debug.Log("[RewardedButtonHandler] onRewardedAdComplete 콜백 실행");
                        onRewardedAdComplete?.Invoke();
                    });
                }
                else
                {
                    Debug.LogWarning("[RewardedButtonHandler] 리워드 광고를 사용할 수 없음");
                }
            }
            else
            {
                Debug.LogError("[RewardedButtonHandler] AdsManager.instance가 null임");
            }
        }
    }
}