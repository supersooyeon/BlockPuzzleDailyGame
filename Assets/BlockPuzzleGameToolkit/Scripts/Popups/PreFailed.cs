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

using BlockPuzzleGameToolkit.Scripts.Audio;
using BlockPuzzleGameToolkit.Scripts.Data;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.GUI;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Gameplay;
using DG.Tweening;
using TMPro;
using UnityEngine;
using System.Collections;

namespace BlockPuzzleGameToolkit.Scripts.Popups
{
    public class PreFailed : PopupWithCurrencyLabel
    {
        public TextMeshProUGUI continuePrice;
        public TextMeshProUGUI timerText;
        public CustomButton continueButton;
        public CustomButton rewardButton;
        
        private bool isWaitingForRewardAd = false; // 리워드 광고 대기 상태
        public TextMeshProUGUI timeLeftText;
        protected int timer;
        protected int price;
        protected bool hasContinued = false;

        protected virtual void OnEnable()
        {
            price = GameManager.instance.GameSettings.continuePrice;
            continuePrice.text = price.ToString();
            continueButton.onClick.AddListener(Continue);
            
            // 리워드 버튼에 클릭 리스너 추가 (광고 시작 감지)
            var rewardedButtonHandler = rewardButton.GetComponent<BlockPuzzleGameToolkit.Scripts.Popups.Reward.RewardedButtonHandler>();
            if (rewardedButtonHandler != null)
            {
                Debug.Log("[PreFailed] RewardedButtonHandler 컴포넌트 발견됨");
                rewardButton.onClick.AddListener(() => {
                    Debug.Log("[PreFailed] 리워드 버튼 클릭됨");
                    isWaitingForRewardAd = true;
                    Debug.Log("[PreFailed] 리워드 광고 시작 - 대기 상태로 설정");
                });
            }
            else
            {
                Debug.LogWarning("[PreFailed] RewardedButtonHandler 컴포넌트를 찾을 수 없음!");
            }
            
            InitializeTimer();
            
            timerText.text = timer.ToString();
            SoundBase.instance.PlaySound(SoundBase.instance.warningTime);
            InvokeRepeating(nameof(UpdateTimer), 1, 1);
            
            // 리워드 광고 사용 가능 여부 확인
            bool canUseRewardAd = CanUseRewardAd();
            rewardButton.gameObject.SetActive(GameManager.instance.GameSettings.enableAds && canUseRewardAd);
            
            if (!canUseRewardAd)
            {
                Debug.Log("[PreFailed] 리워드 광고 사용 불가 - 이미 사용했거나 조건을 만족하지 않음");
            }
            
            if(GameDataManager.GetLevel().enableTimer && timeLeftText != null)
            {
                timeLeftText.gameObject.SetActive(true);
            }
            
            // 광고 취소 감지를 위한 Application focus 이벤트 구독
            Application.focusChanged += OnApplicationFocusChanged;
        }
        
        protected virtual void OnDisable()
        {
            // Application focus 이벤트 구독 해제
            Application.focusChanged -= OnApplicationFocusChanged;
        }
        
        private void OnApplicationFocusChanged(bool hasFocus)
        {
            Debug.Log($"[PreFailed] Application focus changed: {hasFocus}, isWaitingForRewardAd: {isWaitingForRewardAd}");
            
            if (hasFocus && isWaitingForRewardAd)
            {
                Debug.Log("[PreFailed] 앱 포커스 복귀 감지 - 광고 취소 확인 시작");
                // 앱이 다시 포커스를 받았을 때 리워드 광고 대기 중이었다면
                // 잠시 후 팝업을 닫음 (광고 취소로 간주)
                StartCoroutine(CheckRewardAdCancellation());
            }
        }
        
        private IEnumerator CheckRewardAdCancellation()
        {
            // 에디터에서는 짧게, 실제 디바이스에서는 길게 대기
            float waitTime = Application.isEditor ? 0.5f : 2f;
            Debug.Log($"[PreFailed] 광고 취소 확인 대기 시작 - {waitTime}초 대기");
            yield return new WaitForSeconds(waitTime);
            
            if (isWaitingForRewardAd)
            {
                // 아직 대기 중이라면 광고가 취소된 것으로 간주
                Debug.Log("[PreFailed] 리워드 광고 취소됨 - Failed_Classic 강제 호출");
                isWaitingForRewardAd = false;
                
                // 리워드 광고 취소 시에는 게임 종료 처리
                // hasContinued를 true로 설정하여 더 이상 Continue가 불가능하도록 함
                hasContinued = true;
                continueButton.interactable = false;
                rewardButton.interactable = false;
                
                // Failed 상태로 강제 전환
                EventManager.GameStatus = EGameState.Failed;
                Close();
            }
        }

        protected virtual void InitializeTimer()
        {
            timer = GameManager.instance.GameSettings.failedTimerStart;
        }

        protected virtual void UpdateTimer()
        {
            if (MenuManager.instance.GetLastPopup() == this)
            {
                timer--;
                SaveTimerState();
            }
            else
            {
                timer = GameManager.instance.GameSettings.failedTimerStart;
            }

            timerText.text = timer.ToString();
            if (timer <= 0)
            {
                continueButton.interactable = false;
                rewardButton.interactable = false;
                hasContinued = true;

                CancelInvoke(nameof(UpdateTimer));
                EventManager.GameStatus = EGameState.Failed;
                Close();
            }
        }

        protected virtual void SaveTimerState() { }

        /// <summary>
        /// 리워드 광고 사용 가능 여부를 확인하는 메서드
        /// </summary>
        private bool CanUseRewardAd()
        {
            if (GameDataManager.GetGameMode() != EGameMode.Classic)
            {
                Debug.Log("[PreFailed] 클래식 모드 아님 - 리워드 광고 불가");
                return false;
            }

            var state = GameState.Load(EGameMode.Classic) as ClassicGameState;
            if (state == null)
            {
                Debug.Log("[PreFailed] 게임 상태 없음 - 첫 번째 리워드 광고 가능");
                return true;
            }

            if (state.hasUsedReward)
            {
                if (state.hasUsedHighScoreBonus)
                {
                    Debug.Log("[PreFailed] 리워드+보너스 모두 사용됨 - 리워드 광고 불가");
                    return false;
                }

                var classicModeHandler = FindObjectOfType<ClassicModeHandler>();
                if (classicModeHandler != null)
                {
                    bool isNewHighScore = classicModeHandler.score > state.highScoreAtStart;
                    Debug.Log($"[PreFailed] 리워드 사용됨 - 현재점수: {classicModeHandler.score}, 시작시최고: {state.highScoreAtStart}, 새최고: {isNewHighScore}");
                    return isNewHighScore;
                }
                else
                {
                    Debug.LogWarning("[PreFailed] ClassicModeHandler 없음 - 리워드 광고 불가");
                    return false;
                }
            }

            Debug.Log("[PreFailed] 첫 번째 리워드 광고 가능");
            return true;
        }

        public void PauseTimer()
        {
            CancelInvoke(nameof(UpdateTimer));
        }

        protected virtual void Continue()
        {
            if (timer <= 0 || hasContinued)
            {
                return;
            }

            var coinsResource = ResourceManager.instance.GetResource("Coins");
            if (coinsResource.Consume(price))
            {
                hasContinued = true;
                continueButton.interactable = false;
                rewardButton.interactable = false;
                                
                CancelInvoke(nameof(UpdateTimer));
                ShowCoinsSpendFX(continueButton.transform.position);
                StopInteration();
                OnContinue();
            }
        }

        public void OnContinue()
        {
            DOTween.Kill(this);
            DOVirtual.DelayedCall(0.5f, ContinueGame);
        }

        public void ContinueGame()
        {
            // 리워드 광고가 성공적으로 완료됨
            isWaitingForRewardAd = false;
            Debug.Log("[PreFailed] 리워드 광고 완료 - 대기 상태 해제");
            
            result = EPopupResult.Continue;
            
            // 클래식 모드에서 리워드 사용 상태 업데이트
            if (GameDataManager.GetGameMode() == EGameMode.Classic)
            {
                var classicModeHandler = FindObjectOfType<ClassicModeHandler>();
                if (classicModeHandler != null)
                {
                    // 현재 점수를 저장하고 리워드 사용 플래그 설정
                    var state = GameState.Load(EGameMode.Classic) as ClassicGameState;
                    if (state == null)
                    {
                        state = new ClassicGameState();
                    }
                    
                    if (state.hasUsedReward)
                    {
                        // 이미 리워드를 사용한 상태 - 게임 시작 시 최고점수와 비교
                        int gameStartHighScore = state.highScoreAtStart;
                        bool isNewHighScore = classicModeHandler.score > gameStartHighScore;
                        
                        if (isNewHighScore)
                        {
                            state.hasUsedHighScoreBonus = true;
                            Debug.Log($"[PreFailed] 최고점수 보너스 사용됨 - 현재점수({classicModeHandler.score}) > 게임시작시최고점수({gameStartHighScore})");
                        }
                        else
                        {
                            Debug.Log($"[PreFailed] 리워드 사용 - 현재점수({classicModeHandler.score}) <= 게임시작시최고점수({gameStartHighScore})");
                        }
                    }
                    else
                    {
                        // 첫 번째 리워드 사용
                        state.hasUsedReward = true;
                        state.scoreBeforeReward = classicModeHandler.score;
                        // 게임 시작 시 최고 점수 설정 (아직 설정되지 않은 경우)
                        if (state.highScoreAtStart == 0)
                        {
                            state.highScoreAtStart = classicModeHandler.bestScore;
                        }
                        Debug.Log($"[PreFailed] 첫 번째 리워드 사용됨 - 게임시작시최고점수: {state.highScoreAtStart}");
                    }
                    
                    state.score = classicModeHandler.score;
                    state.bestScore = classicModeHandler.bestScore;
                    
                    // 리워드 광고 후에는 필드 상태를 저장하지 않음 (빈 필드로 시작해야 함)
                    state.levelRows = null;
                    
                    // 리워드 광고 사용 후 현재 필드 상태를 완전히 초기화
                    var fieldManager = FindObjectOfType<FieldManager>();
                    if (fieldManager != null)
                    {
                        var allCells = fieldManager.GetAllCells();
                        if (allCells != null)
                        {
                            int clearedCells = 0;
                            for (int i = 0; i < allCells.GetLength(0); i++)
                            {
                                for (int j = 0; j < allCells.GetLength(1); j++)
                                {
                                    var cell = allCells[i, j];
                                    if (cell != null && !cell.IsEmpty())
                                    {
                                        cell.ClearCell();
                                        clearedCells++;
                                    }
                                }
                            }
                            Debug.Log($"[PreFailed] 리워드 광고 후 필드 초기화 완료 - {clearedCells}개 셀 클리어");
                        }
                        
                        // 필드 초기화 후 게임 상태를 저장 (levelRows = null로 설정됨)
                        GameState.Save(state, fieldManager);
                        Debug.Log($"[PreFailed] 리워드 사용 상태 저장 - Score: {state.score}, hasUsedReward: {state.hasUsedReward}, hasUsedHighScoreBonus: {state.hasUsedHighScoreBonus}, levelRows: null");
                    }
                }
            }
            
            // 리워드 광고 후 게임을 이어 진행할 때 시작 애니메이션 재생
            var fieldManagerForAnimation = FindObjectOfType<FieldManager>();
            if (fieldManagerForAnimation != null && !fieldManagerForAnimation.IsAnimationPlaying)
            {
                Debug.Log("[PreFailed] 리워드 광고 후 게임 시작 애니메이션 재생");
                fieldManagerForAnimation.StartCoroutine(fieldManagerForAnimation.PlayGameStartEffect());
            }
            
            EventManager.GameStatus = EGameState.Playing;
            Close();
        }
    }
}