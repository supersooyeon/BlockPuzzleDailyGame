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

using System;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay;
using BlockPuzzleGameToolkit.Scripts.GUI;
using BlockPuzzleGameToolkit.Scripts.System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzleGameToolkit.Scripts.Popups
{
    public class Settings : PopupWithCurrencyLabel
    {
        [SerializeField]
        private CustomButton back;

        // privacypolicy button
        [SerializeField]
        private CustomButton privacypolicy;

        //shop button
        [SerializeField]
        private CustomButton shop;

        [SerializeField]
        private CustomButton retryButton;

        [SerializeField]
        private Button restorePurchase;

        // Rate Us button
        [SerializeField]
        private CustomButton rateUsButton;

        // Language button
        [SerializeField]
        private CustomButton languageButton;

        // Delete Account button
        [SerializeField]
        private CustomButton deleteAccountButton;

        // Main Menu button
        [SerializeField]
        private CustomButton mainMenuButton;

        [SerializeField]
        private Slider vibrationSlider;

        private MenuManager menuManager;

        private const string VibrationPrefKey = "VibrationLevel";

        private void OnEnable()
        {
            var fieldManager = FindObjectOfType<FieldManager>();
            // Save current game state when settings is opened
            if (StateManager.instance.CurrentState == EScreenStates.Game)
            {
                var currentMode = GameDataManager.GetGameMode();
                GameState currentState = null;

                // Create appropriate state based on game mode
                if (currentMode == EGameMode.Classic)
                {
                    var classicHandler = FindObjectOfType<ClassicModeHandler>();
                    if (classicHandler != null)
                    {
                        // 기존 게임 상태를 로드하여 리워드 광고 관련 상태와 필드 상태 유지
                        var existingState = GameState.Load(EGameMode.Classic) as ClassicGameState;
                        
                        currentState = new ClassicGameState
                        {
                            score = classicHandler.score,
                            bestScore = classicHandler.bestScore,
                            gameMode = EGameMode.Classic,
                            gameStatus = EventManager.GameStatus,
                            // 리워드 광고 관련 상태 유지
                            hasUsedReward = existingState?.hasUsedReward ?? false,
                            hasUsedHighScoreBonus = existingState?.hasUsedHighScoreBonus ?? false,
                            scoreBeforeReward = existingState?.scoreBeforeReward ?? 0,
                            highScoreAtStart = existingState?.highScoreAtStart ?? 0,
                            // 필드 상태 저장 강제 트리거 (리워드 광고 후가 아닌 일반 저장)
                            levelRows = new BlockPuzzleGameToolkit.Scripts.LevelsData.LevelRow[1]
                        };
                    }
                }
                else if (currentMode == EGameMode.Timed)
                {
                    var timedHandler = FindObjectOfType<TimedModeHandler>();
                    if (timedHandler != null)
                    {
                        currentState = new TimedGameState
                        {
                            score = timedHandler.score,
                            bestScore = timedHandler.bestScore,
                            remainingTime = timedHandler.GetRemainingTime(),
                            gameMode = EGameMode.Timed,
                            gameStatus = EventManager.GameStatus
                        };
                    }
                }

                if (currentState != null && fieldManager != null)
                {
                    // 필드 상태를 포함하여 저장
                    GameState.Save(currentState, fieldManager);
                    Debug.Log("[Settings] OnEnable에서 게임 상태 저장 완료 (필드 상태 포함)");
                }
            }

            back.onClick.AddListener(BackToMain);
            // privacypolicy.onClick.AddListener(PrivacyPolicy);
            // shop.onClick.AddListener(Shop);
            retryButton.onClick.AddListener(Retry);

            // Add listeners for new buttons
            rateUsButton.onClick.AddListener(RateUs);
            languageButton.onClick.AddListener(Language);
            deleteAccountButton.onClick.AddListener(DeleteAccount);
            mainMenuButton.onClick.AddListener(GoToMainMenu);

            // Load the saved vibration level
            LoadVibrationLevel();

            // Register the OnValueChanged event
            vibrationSlider.onValueChanged.AddListener(SaveVibrationLevel);
            menuManager = GetComponentInParent<MenuManager>();
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(BackToGame);
            // restorePurchase.onClick.AddListener(RestorePurchase);
            // restorePurchase.gameObject.SetActive(GameManager.instance.GameSettings.enableInApps);
            // shop.gameObject.SetActive(GameManager.instance.GameSettings.enableInApps);
        }

        /*
        private void RestorePurchase()
        {
             GameManager.instance.RestorePurchases(((b, list) =>
            {
                if (b)
                    Close();
            }));
        }
        */

        private void BackToGame()
        {
            DisablePause();
            Close();
        }

        private void OnDisable()
        {
            // Unregister the OnValueChanged event
            vibrationSlider.onValueChanged.RemoveListener(SaveVibrationLevel);
        }

        private void SaveVibrationLevel(float value)
        {
            PlayerPrefs.SetFloat(VibrationPrefKey, value);
            PlayerPrefs.Save();
        }

        private void LoadVibrationLevel()
        {
            if (PlayerPrefs.HasKey(VibrationPrefKey))
            {
                vibrationSlider.value = PlayerPrefs.GetFloat(VibrationPrefKey);
            }
            else
            {
                vibrationSlider.value = 1.0f;
                SaveVibrationLevel(1.0f);
            }
        }

        private void Retry()
        {
            var classicHandler = FindObjectOfType<ClassicModeHandler>();
            if (classicHandler != null)
            {
                classicHandler.ResetScore();
            }
            var timedHandler = FindObjectOfType<TimedModeHandler>();
            if (timedHandler != null)
            {
                timedHandler.ResetScore();
            }
            GameManager.instance.RestartLevel();
            MenuManager.instance.FadeOut();
        }

        /*
        private void Shop()
        {
            StopInteration();

            DisablePause();
            MenuManager.instance.ShowPopup<CoinsShop>();
            Close();
        }
        */

        /*
        private void PrivacyPolicy()
        {
            StopInteration();

            DisablePause();
            MenuManager.instance.ShowPopup<GDPR>();
            Close();
        }
        */

        private void DisablePause()
        {
            if (StateManager.instance.CurrentState == EScreenStates.Game)
            {
                EventManager.GameStatus = EGameState.Playing;
            }
        }

        private void BackToMain()
        {
            StopInteration();

            Close();
            GameManager.instance.MainMenu();
        }

        private void GoToMainMenu()
        {
            StopInteration();

            // 현재 게임 상태를 저장하고 메인 메뉴로 전환
            if (StateManager.instance.CurrentState == EScreenStates.Game)
            {
                // 게임 상태 저장
                var fieldManager = FindObjectOfType<FieldManager>();
                if (fieldManager != null)
                {
                    var currentMode = GameDataManager.GetGameMode();
                    GameState currentState = null;

                    if (currentMode == EGameMode.Classic)
                    {
                        var classicHandler = FindObjectOfType<ClassicModeHandler>();
                        if (classicHandler != null)
                        {
                            // 기존 게임 상태를 로드하여 리워드 광고 관련 상태와 필드 상태 유지
                            var existingState = GameState.Load(EGameMode.Classic) as ClassicGameState;
                            
                            currentState = new ClassicGameState
                            {
                                score = classicHandler.score,
                                bestScore = classicHandler.bestScore,
                                gameMode = EGameMode.Classic,
                                gameStatus = EventManager.GameStatus,
                                // 리워드 광고 관련 상태 유지
                                hasUsedReward = existingState?.hasUsedReward ?? false,
                                hasUsedHighScoreBonus = existingState?.hasUsedHighScoreBonus ?? false,
                                scoreBeforeReward = existingState?.scoreBeforeReward ?? 0,
                                highScoreAtStart = existingState?.highScoreAtStart ?? 0,
                                // 필드 상태 저장 강제 트리거 (리워드 광고 후가 아닌 일반 저장)
                                levelRows = new BlockPuzzleGameToolkit.Scripts.LevelsData.LevelRow[1]
                            };
                        }
                    }
                    else if (currentMode == EGameMode.Timed)
                    {
                        var timedHandler = FindObjectOfType<TimedModeHandler>();
                        if (timedHandler != null)
                        {
                            currentState = new TimedGameState
                            {
                                score = timedHandler.score,
                                bestScore = timedHandler.bestScore,
                                remainingTime = timedHandler.GetRemainingTime(),
                                gameMode = EGameMode.Timed,
                                gameStatus = EventManager.GameStatus
                            };
                        }
                    }

                    if (currentState != null)
                    {
                        // 필드 상태를 포함하여 저장
                        GameState.Save(currentState, fieldManager);
                        Debug.Log("[Settings] GoToMainMenu에서 게임 상태 저장 완료 (필드 상태 포함)");
                    }
                }
            }

            // StateManager 상태 변경 대신 SceneLoader를 통해 메인메뉴 씬으로 전환
            SceneLoader.instance.GoMain();
            
            // 팝업 닫기
            Close();
            
            Debug.Log("[Settings] 메인 메뉴 씬으로 전환되었습니다.");
        }

        private void RateUs()
        {
            StopInteration();

            DisablePause();
            // 여기서 Rate Us 팝업을 열도록 합니다
            // MenuManager.instance.ShowPopup<RateUsPopup>();
            
            // 임시로 URL을 열도록 구현 (실제 Rate Us 팝업이 없는 경우)
            #if UNITY_ANDROID
                Application.OpenURL("market://details?id=" + Application.identifier);
            #elif UNITY_IOS
                Application.OpenURL("itms-apps://itunes.apple.com/app/id" + "YOUR_APP_ID");
            #else
                Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
            #endif
            
            Close();
        }

        private void Language()
        {
            StopInteration();

            DisablePause();
            // Language 설정 팝업을 열도록 합니다
            MenuManager.instance.ShowPopup<LanguagePopup>();
            
            Close();
        }

        private void DeleteAccount()
        {
            StopInteration();

            DisablePause();
            // Delete Account 확인 팝업을 엽니다
            MenuManager.instance.ShowPopup<DeleteAccountPopup>();
            
            Close();
        }
    }
}