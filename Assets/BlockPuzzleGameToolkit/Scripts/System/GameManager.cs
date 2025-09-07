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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay;
using BlockPuzzleGameToolkit.Scripts.GUI;
using BlockPuzzleGameToolkit.Scripts.Popups;
using BlockPuzzleGameToolkit.Scripts.Popups.Daily;
using BlockPuzzleGameToolkit.Scripts.Services;
using BlockPuzzleGameToolkit.Scripts.Services.IAP;
using BlockPuzzleGameToolkit.Scripts.Settings;
using DG.Tweening;
using UnityEngine;
using ResourceManager = BlockPuzzleGameToolkit.Scripts.Data.ResourceManager;

namespace BlockPuzzleGameToolkit.Scripts.System
{
    public class GameManager : SingletonBehaviour<GameManager>
    {
        public Action<string> purchaseSucceded;
        public DebugSettings debugSettings;
        public DailyBonusSettings dailyBonusSettings;
        public GameSettings GameSettings;
        public SpinSettings luckySpinSettings;
        public CoinsShopSettings coinsShopSettings;
        private (string id, ProductTypeWrapper.ProductType productType)[] products;
        private int lastBackgroundIndex = -1;
        private bool isTutorialMode;
        private MainMenu mainMenu;
        public Action<bool, List<string>> OnPurchasesRestored;
        public ProductID noAdsProduct;
        private bool blockButtons;

        // Score는 클래식 모드의 "최고 점수"로 취급한다. 낮은 값으로는 덮어쓰지 않는다.
        public int Score
        {
            get => HighScoreService.GetBest(EGameMode.Classic);
            set => HighScoreService.TryUpdateBest(EGameMode.Classic, value);
        }

        public override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this);
            
            // 프레임 레이트 설정을 더 안정적으로
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

			// 앱 업데이트/초기 구동 시 하이스코어 마이그레이션 및 백업 복원
			MigrateAndRestoreHighScores();
            
            // WebView 관련 설정 (Android)
            #if UNITY_ANDROID
            // WebView 프레임 레이트 문제 방지
            if (Application.platform == RuntimePlatform.Android)
            {
                // WebView 관련 설정
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                
                // WebView 디버그 로그 비활성화
                Debug.unityLogger.logEnabled = false;
                
                // 프레임 레이트 안정화
                Time.fixedDeltaTime = 0.01666667f; // 60 FPS
            }
            #endif
            
            DOTween.SetTweensCapacity(1250, 512);

            // 앱 업데이트/재설치 등으로 PlayerPrefs가 일부 초기화된 경우 스냅샷에서 복원
            ProgressBackup.LoadSnapshotIfMissing();

            mainMenu = FindObjectOfType<MainMenu>();
            if (mainMenu != null)
            {
                mainMenu.OnAnimationEnded += OnMainMenuAnimationEnded;
            }
        }

		private void MigrateAndRestoreHighScores()
		{
			// Classic
			int bestClassic = HighScoreService.GetBest(EGameMode.Classic);
			// Timed
			int bestTimed = HighScoreService.GetBest(EGameMode.Timed);
			// 두 호출 자체가 내부적으로 백업 복원을 수행하므로 별도 로직 불필요
		}

        private void OnEnable()
        {
            IAPManager.SubscribeToPurchaseEvent(PurchaseSucceeded);
            if (StateManager.instance.CurrentState == EScreenStates.MainMenu)
            {
                if (!GameDataManager.isTestPlay && CheckDailyBonusConditions())
                {
                    blockButtons = true;
                }
            }

            // 튜토리얼 모드 설정 로직
            if (!GameDataManager.isTestPlay)
            {
                Debug.Log($"GameManager.OnEnable: PostTutorialGameMode 키 존재: {PlayerPrefs.HasKey("PostTutorialGameMode")}");
                Debug.Log($"GameManager.OnEnable: IsTutorialShown: {IsTutorialShown()}");
                
                // 신규 유저: PostTutorialGameMode가 설정되지 않은 경우
                if (!PlayerPrefs.HasKey("PostTutorialGameMode"))
                {
                    if (!IsTutorialShown())
                    {
                        SetTutorialMode(true);
                        Debug.Log("GameManager: 신규 유저 - 튜토리얼 모드 활성화");
                    }
                    else
                    {
                        SetTutorialMode(false);
                        Debug.Log("GameManager: 신규 유저이지만 이미 튜토리얼 완료 - 일반 모드");
                    }
                }
                // 기존 유저의 튜토리얼 미완료 상황: PostTutorialGameMode는 있지만 IsTutorialShown이 false
                else if (!IsTutorialShown())
                {
                    SetTutorialMode(true);
                    Debug.Log("GameManager: 기존 유저 튜토리얼 미완료 - 튜토리얼 모드 활성화");
                }
                // 기존 유저의 튜토리얼 완료 상황: PostTutorialGameMode가 있고 IsTutorialShown도 true
                else
                {
                    SetTutorialMode(false);
                    Debug.Log("GameManager: 기존 유저 튜토리얼 완료 - 일반 모드");
                }
            }
        }

        private void OnDisable()
        {
            IAPManager.UnsubscribeFromPurchaseEvent(PurchaseSucceeded);
            if (mainMenu != null)
            {
                mainMenu.OnAnimationEnded -= OnMainMenuAnimationEnded;
            }
            GameDataManager.isTestPlay = false; // Reset isTestPlay
        }

        public bool IsTutorialShown()
        {
            return PlayerPrefs.GetInt("tutorial", 0) == 1;
        }

        public void SetTutorialCompleted()
        {
            PlayerPrefs.SetInt("tutorial", 1);
            PlayerPrefs.Save();
        }

        private async void Start()
        {
            // 앱 업데이트 체크를 가장 먼저 실행
            CheckForAppUpdate();
            
            if (GameSettings.enableInApps) {
                products = Resources.LoadAll<ProductID>("ProductIDs")
                    .Select(p => (p.ID, p.productType))
                    .ToArray();

                // Initialize gaming services
                await InitializeGamingServices.instance?.Initialize(
                    OnInitializeSuccess,
                    OnInitializeError
                );
                // Initialize IAP directly if InitializeGamingServices is not used
                await IAPManager.instance?.InitializePurchasing(products);
            }

            if (GameSettings.enableAds && IsNoAdsPurchased())
            {
                AdsManager.instance.RemoveAds();
            }

            if (GameDataManager.isTestPlay)
            {
                GameDataManager.SetLevel(GameDataManager.GetLevel());
            }
        }
        
        /// <summary>
        /// 앱 업데이트를 체크하고 필요시 업데이트를 시작합니다.
        /// </summary>
        private void CheckForAppUpdate()
        {
            Debug.Log("[AppUpdate] 앱 업데이트 체크 시작");
            
            // InAppUpdateManager 컴포넌트 찾기 또는 생성
            InAppUpdateManager updateManager = FindObjectOfType<InAppUpdateManager>();
            
            if (updateManager == null)
            {
                Debug.Log("[AppUpdate] InAppUpdateManager 생성");
                GameObject updateManagerObj = new GameObject("InAppUpdateManager");
                updateManager = updateManagerObj.AddComponent<InAppUpdateManager>();
                
                // GameManager와 함께 유지되도록 설정
                DontDestroyOnLoad(updateManagerObj);
            }
            
            // 업데이트 체크 시작
            if (updateManager != null)
            {
                updateManager.StartUpdateCheck();
            }
            else
            {
                Debug.LogError("[AppUpdate] InAppUpdateManager 생성 실패");
            }
        }

        private void OnInitializeSuccess()
        {
            Debug.Log("Gaming services initialized successfully");
        }

        private void OnInitializeError(string errorMessage)
        {
            Debug.LogError($"Failed to initialize gaming services: {errorMessage}");
        }

        private void HandleDailyBonus()
        {
            // 임시로 데일리 보너스 비활성화
            blockButtons = false;
            return;
            
            if (StateManager.instance.CurrentState != EScreenStates.MainMenu || !dailyBonusSettings.dailyBonusEnabled || !GameSettings.enableInApps)
            {
                return;
            }

            var shouldShowDailyBonus = CheckDailyBonusConditions();

            if (shouldShowDailyBonus)
            {
                var daily = MenuManager.instance.ShowPopup<DailyBonus>(()=>
                {
                    blockButtons = false;
                });
            }
        }

        private bool CheckDailyBonusConditions()
        {
            var today = DateTime.Today;
            var lastRewardDate = DateTime.Parse(PlayerPrefs.GetString("DailyBonusDay", today.Subtract(TimeSpan.FromDays(1)).ToString(CultureInfo.CurrentCulture)));
            return today.Date > lastRewardDate.Date && dailyBonusSettings.dailyBonusEnabled;
        }

        public void RestartLevel()
        {
            DOTween.KillAll();
            MenuManager.instance.CloseAllPopups();
            EventManager.GetEvent(EGameEvent.RestartLevel).Invoke();
        }

        public void RemoveAds()
        {
            if (GameSettings.enableAds) {
                MenuManager.instance.ShowPopup<NoAds>();
            }
        }

        public void MainMenu()
        {
            DOTween.KillAll();
            if (StateManager.instance.CurrentState == EScreenStates.Game && GameDataManager.GetGameMode() == EGameMode.Classic)
            {
                SceneLoader.instance.GoMain();
            }
            else if (StateManager.instance.CurrentState == EScreenStates.Game && GameDataManager.GetGameMode() == EGameMode.Adventure)
            {
                SceneLoader.instance.StartMapScene();
            }
            else if (StateManager.instance.CurrentState == EScreenStates.Map)
            {
                SceneLoader.instance.GoMain();
            }
            else if (StateManager.instance.CurrentState == EScreenStates.MainMenu)
            {
                MenuManager.instance.ShowPopup<Quit>();
            }
            else
            {
                SceneLoader.instance.GoMain();
            }
        }

        public void OpenMap()
        {
            if (blockButtons && StateManager.instance.CurrentState == EScreenStates.MainMenu)
                return;
            if (GetGameMode() == EGameMode.Classic)
            {
                SceneLoader.instance.StartGameSceneClassic();
            }
            else if (GetGameMode() == EGameMode.Timed)
            {
                SceneLoader.instance.StartGameSceneTimed();
            }
            else
            {
                SceneLoader.instance.StartMapScene();
            }
        }

        public void OpenGame()
        {
            SceneLoader.instance.StartGameScene();
        }

        public void PurchaseSucceeded(string id)
        {
            purchaseSucceded?.Invoke(id);
        }

        public bool IsNoAdsPurchased()
        {
            return !GameSettings.enableAds || IsPurchased(noAdsProduct.ID);
        }

        public void SetGameMode(EGameMode gameMode)
        {
            GameDataManager.SetGameMode(gameMode);
        }

        private EGameMode GetGameMode()
        {
            return GameDataManager.GetGameMode();
        }

        public int GetLastBackgroundIndex()
        {
            return lastBackgroundIndex;
        }

        public void SetLastBackgroundIndex(int index)
        {
            lastBackgroundIndex = index;
        }

        public void NextLevel()
        {
            GameDataManager.LevelNum++;
            OpenGame();
            RestartLevel();
        }

        public void SetTutorialMode(bool tutorial)
        {
            Debug.Log("Tutorial mode set to " + tutorial);
            isTutorialMode = tutorial;
        }

        public bool IsTutorialMode()
        {
            return isTutorialMode;
        }

        private void OnMainMenuAnimationEnded()
        {
            if (StateManager.instance.CurrentState == EScreenStates.MainMenu)
            {

                HandleDailyBonus();
            }
        }

        internal void RestorePurchases(Action<bool, List<string>> OnPurchasesRestored)
        {
            if (!GameSettings.enableInApps) return;
            
            this.OnPurchasesRestored = OnPurchasesRestored;
            IAPManager.instance?.RestorePurchases(OnPurchasesRestored);
        }

        public bool IsPurchased(string id)
        {
            if (!GameSettings.enableInApps) return false;
            return IAPManager.instance?.IsProductPurchased(id) ?? false;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveAllProgress();
                ProgressBackup.SaveSnapshot();
            }
        }

        private void OnApplicationQuit()
        {
            SaveAllProgress();
            // 종료 직전에 스냅샷 백업
            ProgressBackup.SaveSnapshot();
        }

        private void SaveAllProgress()
        {
            try
            {
                var mode = GameDataManager.GetGameMode();
                var fieldManager = FindObjectOfType<FieldManager>();

                if (fieldManager != null)
                {
                    if (mode == EGameMode.Classic)
                    {
                        var classicHandler = FindObjectOfType<ClassicModeHandler>();
                        if (classicHandler != null)
                        {
                            var existing = GameState.Load(EGameMode.Classic) as ClassicGameState;
                            var state = new ClassicGameState
                            {
                                score = classicHandler.score,
                                bestScore = HighScoreService.GetBest(EGameMode.Classic),
                                gameMode = EGameMode.Classic,
                                gameStatus = EventManager.GameStatus,
                                hasUsedReward = existing?.hasUsedReward ?? false,
                                hasUsedHighScoreBonus = existing?.hasUsedHighScoreBonus ?? false,
                                scoreBeforeReward = existing?.scoreBeforeReward ?? 0,
                                highScoreAtStart = existing?.highScoreAtStart ?? 0,
                                // 필드 스냅샷 저장 강제 트리거
                                levelRows = new BlockPuzzleGameToolkit.Scripts.LevelsData.LevelRow[1]
                            };
                            GameState.Save(state, fieldManager);
                        }
                    }
                    else if (mode == EGameMode.Timed)
                    {
                        var timedHandler = FindObjectOfType<TimedModeHandler>();
                        if (timedHandler != null)
                        {
                            var state = new TimedGameState
                            {
                                score = timedHandler.score,
                                bestScore = HighScoreService.GetBest(EGameMode.Timed),
                                remainingTime = timedHandler.GetRemainingTime(),
                                gameMode = EGameMode.Timed,
                                gameStatus = EventManager.GameStatus
                            };
                            GameState.Save(state, fieldManager);
                        }
                    }
                    else if (mode == EGameMode.Adventure)
                    {
                        // 어드벤처 진행도(레벨 번호) 보존
                        PlayerPrefs.SetInt("Level", GameDataManager.GetLevelNum());
                        PlayerPrefs.Save();
                    }
                }
                else
                {
                    // 필드가 없더라도 최소한 현재 모드/레벨 정보는 저장
                    PlayerPrefs.SetInt("Level", GameDataManager.GetLevelNum());
                    PlayerPrefs.Save();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] SaveAllProgress 예외: {e.Message}");
            }
        }
    }
}
