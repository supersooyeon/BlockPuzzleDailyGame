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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlockPuzzleGameToolkit.Scripts.Audio;
using BlockPuzzleGameToolkit.Scripts.Data;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay.FX;
using BlockPuzzleGameToolkit.Scripts.Gameplay.Managers;
using BlockPuzzleGameToolkit.Scripts.Gameplay.Pool;
using BlockPuzzleGameToolkit.Scripts.LevelsData;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Utils;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using Random = UnityEngine.Random;
using UnityEngine.InputSystem;

namespace BlockPuzzleGameToolkit.Scripts.Gameplay
{
    public partial class LevelManager : MonoBehaviour
    {
        public int currentLevel;
        public int completedLevel; // 완료한 레벨 번호 저장용
        public LineExplosion lineExplosionPrefab;
        public ComboText comboTextPrefab;
        public Transform pool;
        public Transform fxPool;

        public int comboCounter;
        private int missCounter;

        [SerializeField]
        private RectTransform gameCanvas;

        [SerializeField]
        private RectTransform shakeCanvas;

        [SerializeField]
        private GameObject scorePrefab;

        [SerializeField]
        private GameObject[] words;

        [SerializeField]
        private TutorialManager tutorialManager;

        [SerializeField]
        private GameObject timerPanel;

        public EGameMode gameMode;
        public Level _levelData;

        private Cell[] emptyCells;

        public UnityEvent<Level> OnLevelLoaded;
        public Action<int> OnScored;
        public Action OnLose;
        private FieldManager field;
        public CellDeckManager cellDeck;
        private ItemFactory itemFactory;
        private TargetManager targetManager;

        private ObjectPool<ComboText> comboTextPool;
        private ObjectPool<LineExplosion> lineExplosionPool;
        private ObjectPool<ScoreText> scoreTextPool;
        private ObjectPool<GameObject> wordsPool;
        private ClassicModeHandler classicModeHandler;
        private TimedModeHandler timedModeHandler;
        public TimerManager timerManager;
        private int timerDuration;
        
        private Vector3 cachedFieldCenter;
        private bool isFieldCenterCached;
        
        // 튜토리얼에서 일반 레벨로 전환되는 상황을 감지하기 위한 플래그
        private bool isTransitioningFromTutorial = false;
        public bool isLineClearEffectPlaying = false;
        
        // 필드 상태 복원을 위한 변수들
        private bool shouldRestoreFieldState = false;
        private LevelRow[] savedFieldState = null;

        private void OnEnable()
        {
            StateManager.instance.CurrentState = EScreenStates.Game;
            EventManager.GetEvent(EGameEvent.RestartLevel).Subscribe(RestartLevel);
            EventManager.GetEvent<Shape>(EGameEvent.ShapePlaced).Subscribe(CheckLines);
            EventManager.OnGameStateChanged += HandleGameStateChange;
            targetManager = FindObjectOfType<TargetManager>();
            itemFactory = FindObjectOfType<ItemFactory>();
            cellDeck = FindObjectOfType<CellDeckManager>();
            field = FindObjectOfType<FieldManager>();
            // Get or add the TimerManager component
            timerManager = GetComponent<TimerManager>();
            if (timerManager == null)
            {
                timerManager = gameObject.AddComponent<TimerManager>();
            }

            if (timerManager != null && timerPanel != null)
            {
                timerManager.OnTimerExpired += OnTimerExpired;
            }

            // GameDataManager에서 현재 게임 모드 가져오기
            var previousGameMode = gameMode;
            gameMode = GameDataManager.GetGameMode();
            Debug.Log($"[LevelManager] 모드 초기화: {gameMode} (이전: {previousGameMode})");
            
            // 모드가 변경된 경우 필드를 완전히 초기화
            // previousGameMode가 0(Classic)이고 gameMode도 Classic인 경우는 첫 실행이므로 제외
            bool isModeChanged = previousGameMode != gameMode;
            bool isFirstRun = (previousGameMode == default(EGameMode) || previousGameMode == EGameMode.Classic) && gameMode == EGameMode.Classic;
            
            if (isModeChanged && !isFirstRun)
            {
                Debug.Log($"[LevelManager] 모드 전환 감지: {previousGameMode} → {gameMode} - 필드 초기화");
                // 모드 전환 시에는 필드 상태 복원을 건너뛰도록 설정
                shouldRestoreFieldState = false;
                savedFieldState = null;
                // 에디터 'PlayLevel' 테스트 중에는 선택한 레벨을 유지해야 함
                if (!GameDataManager.isTestPlay)
                {
                    // 레벨 캐시 무효화하여 새 모드에서 올바른 레벨이 로드되도록 함
                    GameDataManager.SetLevel(null);
                }
            }
            
            // 모드 전환 시 이전 모드의 데이터를 저장하고 정리
            if (isModeChanged && !isFirstRun)
            {
                // 이전 모드의 데이터를 저장 (모드 전환 전에). 각 모드별 진행 데이터는 보존해야 함
                SaveCurrentModeData(previousGameMode);
            }
            
            // 어드벤처 모드에서 클래식 모드로 전환하는 경우 명시적으로 처리
            if (previousGameMode == EGameMode.Adventure && gameMode == EGameMode.Classic)
            {
                Debug.Log("[LevelManager] 어드벤처 → 클래식 모드 전환 - 필드 완전 초기화");
                shouldRestoreFieldState = false;
                savedFieldState = null;
            }

            comboTextPool = new ObjectPool<ComboText>(
                () => Instantiate(comboTextPrefab, fxPool),
                obj => obj.gameObject.SetActive(true),
                obj => obj.gameObject.SetActive(false),
                Destroy
            );

            lineExplosionPool = new ObjectPool<LineExplosion>(
                () => Instantiate(lineExplosionPrefab, pool),
                obj => obj.gameObject.SetActive(true),
                obj => obj.gameObject.SetActive(false),
                Destroy
            );

            scoreTextPool = new ObjectPool<ScoreText>(
                () => Instantiate(scorePrefab, fxPool).GetComponent<ScoreText>(),
                obj => obj.gameObject.SetActive(true),
                obj => obj.gameObject.SetActive(false),
                Destroy
            );

            wordsPool = new ObjectPool<GameObject>(
                () => Instantiate(words[Random.Range(0, words.Length)], fxPool),
                obj => obj.SetActive(true),
                obj => obj.SetActive(false),
                Destroy
            );
            // 게임 상태 복원을 먼저 수행 (RestartLevel 전에)
            if (gameMode == EGameMode.Classic)
                RestoreGameState();
            else if (gameMode == EGameMode.Timed)
                RestoreTimedGameState();
            
            RestartLevel();
        }

        private void RestoreGameState()
        {
            var state = GameState.Load(EGameMode.Classic) as ClassicGameState;
            if (state != null)
            {
                Debug.Log($"[LevelManager] 클래식 게임 상태 복원 - Score: {state.score}, hasUsedReward: {state.hasUsedReward}, hasUsedHighScoreBonus: {state.hasUsedHighScoreBonus}, LevelRows: {(state.levelRows != null ? state.levelRows.Length : 0)}");
                GameManager.instance.Score = state.score;

                // 필드 상태 복원을 위한 플래그 설정
                if (state.levelRows != null && state.levelRows.Length > 0)
                {
                    shouldRestoreFieldState = true;
                    savedFieldState = state.levelRows;
                    Debug.Log("[LevelManager] 필드 상태 복원 예약됨");
                }
                else
                {
                    Debug.Log("[LevelManager] 저장된 필드 상태 없음 - 새 필드로 시작");
                }
            }
            else
            {
                Debug.Log("[LevelManager] 저장된 클래식 게임 상태 없음 - 새로 시작");
            }
        }

        private void RestoreTimedGameState()
        {
            var state = GameState.Load(EGameMode.Timed) as TimedGameState;
            if (state != null)
            {
                Debug.Log($"Restoring timed game state with {(state.levelRows?.Length ?? 0)} rows");
                timedModeHandler = FindObjectOfType<TimedModeHandler>();
                if (timedModeHandler != null)
                {
                    timedModeHandler.score = state.score;
                    timedModeHandler.bestScore = state.bestScore;
                    
                    // Initialize timer with saved remaining time
                    if (timerManager != null)
                    {
                        timerManager.InitializeTimer(state.remainingTime);
                    }

                    // Restore field state if we have saved rows
                    if (state.levelRows != null && state.levelRows.Length > 0)
                    {
                        var fieldManager = FindObjectOfType<FieldManager>();
                        if (fieldManager != null)
                        {
                            Debug.Log("Restoring field state from saved state");
                            fieldManager.RestoreFromState(state.levelRows);
                        }
                        else
                        {
                            Debug.LogError("Could not find FieldManager component to restore field state");
                        }
                    }

                    // Let TimedModeHandler handle the timer start
                    timedModeHandler.ResumeGame();
                }
            }
            else
            {
                // If no saved state, start fresh timer
                if (timerManager != null && timedModeHandler != null)
                {
                    timerManager.InitializeTimer(GameManager.instance.GameSettings.globalTimedModeSeconds);
                }
            }
        }

        // Adventure 모드는 별도 진행 데이터 시스템을 사용하는 것으로 가정. GameState를 통한 복원은 생략.

        private void RestartLevel()
        {
            comboCounter = 0;
            missCounter = 0;
            field.ShowOutline(false);
            Load();
        }

        private void SaveCurrentModeData(EGameMode modeToSave)
        {
            var fieldManager = FindObjectOfType<FieldManager>();
            if (fieldManager == null) return;
            
            GameState state = null;
            
            switch (modeToSave)
            {
                case EGameMode.Classic:
                    var classicHandler = FindObjectOfType<ClassicModeHandler>();
                    if (classicHandler != null)
                    {
                        // 기존 게임 상태를 로드하여 리워드 광고 관련 상태와 필드 상태 유지
                        var existingState = GameState.Load(EGameMode.Classic) as ClassicGameState;
                        
                        state = new ClassicGameState
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
                            // 필드 상태 유지 (리워드 광고 후가 아닌 경우)
                            levelRows = existingState?.levelRows
                        };
                    }
                    break;
                    
                case EGameMode.Timed:
                    var timedHandler = FindObjectOfType<TimedModeHandler>();
                    if (timedHandler != null)
                    {
                        state = new TimedGameState
                        {
                            score = timedHandler.score,
                            bestScore = timedHandler.bestScore,
                            remainingTime = timedHandler.GetRemainingTime(),
                            gameMode = EGameMode.Timed,
                            gameStatus = EventManager.GameStatus
                        };
                    }
                    break;
                    
                case EGameMode.Adventure:
                    // Adventure 모드는 GameState를 사용하지 않으므로 저장 생략
                    Debug.Log("[LevelManager] Adventure 모드 진행 데이터 저장은 별도 시스템 사용 - GameState 저장 생략");
                    return;
            }
            
            if (state != null)
            {
                GameState.Save(state, fieldManager);
                Debug.Log($"[LevelManager] {modeToSave} 모드 데이터 저장 완료");
            }
        }

        private void SaveGameState()
        {
            if (gameMode == EGameMode.Classic)
            {
                classicModeHandler = FindObjectOfType<ClassicModeHandler>();
                if (classicModeHandler != null)
                {
                    // 기존 게임 상태를 로드하여 리워드 광고 관련 상태 보존
                    var existingState = GameState.Load(EGameMode.Classic) as ClassicGameState;
                    
                    var state = new ClassicGameState
                    {
                        score = classicModeHandler.score,
                        bestScore = classicModeHandler.bestScore,
                        gameMode = EGameMode.Classic,
                        gameStatus = EventManager.GameStatus,
                        // 리워드 광고 관련 상태 보존
                        hasUsedReward = existingState?.hasUsedReward ?? false,
                        hasUsedHighScoreBonus = existingState?.hasUsedHighScoreBonus ?? false,
                        scoreBeforeReward = existingState?.scoreBeforeReward ?? 0,
                        highScoreAtStart = existingState?.highScoreAtStart ?? 0
                    };
                    GameState.Save(state, field);
                    Debug.Log($"[LevelManager] 게임 상태 저장 - 리워드 상태 보존: hasUsedReward={state.hasUsedReward}, hasUsedHighScoreBonus={state.hasUsedHighScoreBonus}");
                }
            }
            else if (gameMode == EGameMode.Timed)
            {
                timedModeHandler = FindObjectOfType<TimedModeHandler>();
                if (timedModeHandler != null)
                {
                    var state = new TimedGameState
                    {
                        score = timedModeHandler.score,
                        bestScore = timedModeHandler.bestScore,
                        remainingTime = timedModeHandler.GetRemainingTime(),
                        gameMode = EGameMode.Timed,
                        gameStatus = EventManager.GameStatus
                    };
                    GameState.Save(state, field);
                }
            }
        }

        private void OnDisable()
        {
            // 유니티 에디터에서 플레이 모드 종료 시에도 게임 상태 저장
            if ((gameMode == EGameMode.Classic || gameMode == EGameMode.Timed) && EventManager.GameStatus == EGameState.Playing)
            {
                SaveGameState();
                Debug.Log("[LevelManager] OnDisable - 게임 상태 저장 완료 (에디터 플레이 모드 종료)");
            }

            EventManager.GetEvent(EGameEvent.RestartLevel).Unsubscribe(RestartLevel);
            EventManager.GetEvent<Shape>(EGameEvent.ShapePlaced).Unsubscribe(CheckLines);
            EventManager.OnGameStateChanged -= HandleGameStateChange;

            // Unsubscribe from timer events
            if (timerManager != null)
            {
                timerManager.OnTimerExpired -= OnTimerExpired;
            }
        }

        private void OnTimerExpired()
        {
            // Check if level is complete before triggering a loss
            if (targetManager != null && targetManager.IsLevelComplete())
            {
                // Level complete, trigger win
                SetWin();
            }
            else
            {
                // Level not complete, trigger loss
                SetLose();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if ((gameMode == EGameMode.Classic || gameMode == EGameMode.Timed) && EventManager.GameStatus == EGameState.Playing)
                SaveGameState();

            PauseTimer(pauseStatus);
        }

        private void OnApplicationQuit()
        {
            if ((gameMode == EGameMode.Classic || gameMode == EGameMode.Timed) && EventManager.GameStatus == EGameState.Playing)
                SaveGameState();
        }

        public void Load()
        {
            // GameDataManager에서 현재 게임 모드 가져오기
            gameMode = GameDataManager.GetGameMode();
            Debug.Log($"LevelManager.Load: Current game mode is {gameMode}");
            
            if (GameManager.instance.IsTutorialMode())
            {
                _levelData = tutorialManager.GetLevelForPhase();
                Debug.Log($"Tutorial: Loading level for phase {tutorialManager.currentPhase}, level data: {(_levelData != null ? _levelData.name : "null")}");
            }
            else
            {
                // 현재 게임 모드에 따라 적절한 레벨 데이터 로드
                if (gameMode == EGameMode.Classic)
                {
                    _levelData = Resources.Load<Level>("Misc/ClassicLevel");
                    if (_levelData == null)
                    {
                        Debug.LogError("ClassicLevel.asset not found in Resources/Misc");
                        _levelData = GameDataManager.GetLevel();
                        if (_levelData == null)
                        {
                            Debug.LogError("Failed to load any level data");
                            return;
                        }
                    }
                    currentLevel = _levelData.Number;
                    Debug.Log("Classic 모드: " + _levelData.name + " 로드 완료");
                    
                    // 클래식 모드에서 crown-icon_1 활성화
                    SetCrownIconVisibility(EGameMode.Classic);
                }
                else if (gameMode == EGameMode.Adventure)
                {
                    _levelData = GameDataManager.GetLevel();
                    if (_levelData == null)
                    {
                        Debug.LogError("Failed to load Adventure level data");
                        return;
                    }
                    currentLevel = _levelData.Number;
                    Debug.Log("Adventure 모드: " + _levelData.name + " 로드 완료");
                    
                    // 어드벤처 모드에서 crown-icon_1 비활성화
                    SetCrownIconVisibility(EGameMode.Adventure);
                }
                else if (gameMode == EGameMode.Timed)
                {
                    _levelData = Resources.Load<Level>("Misc/TimedLevel");
                    if (_levelData == null)
                    {
                        Debug.LogError("TimedLevel.asset not found in Resources/Misc");
                        _levelData = GameDataManager.GetLevel();
                        if (_levelData == null)
                        {
                            Debug.LogError("Failed to load any level data");
                            return;
                        }
                    }
                    currentLevel = _levelData.Number;
                    Debug.Log("Timed 모드: " + _levelData.name + " 로드 완료");
                    
                    // Timed 모드에서 crown-icon_1 비활성화
                    SetCrownIconVisibility(EGameMode.Timed);
                }
                else
                {
                    // 기본값으로 Classic 모드 사용
                    gameMode = EGameMode.Classic;
                    _levelData = Resources.Load<Level>("Misc/ClassicLevel");
                    if (_levelData == null)
                    {
                        Debug.LogError("ClassicLevel.asset not found in Resources/Misc");
                        _levelData = GameDataManager.GetLevel();
                        if (_levelData == null)
                        {
                            Debug.LogError("Failed to load any level data");
                            return;
                        }
                    }
                    currentLevel = _levelData.Number;
                    Debug.Log("기본 Classic 모드: " + _levelData.name + " 로드 완료");
                    
                    // 기본 Classic 모드에서 crown-icon_1 활성화
                    SetCrownIconVisibility(EGameMode.Classic);
                }
            }
            if(_levelData == null)
            {
                Debug.LogError("Level data is null");
                return;
            }

            // Apply global time settings if timed mode is enabled
            if (GameManager.instance.GameSettings.enableTimedMode && _levelData.enableTimer)
            {
                timerDuration = _levelData.timerDuration;
                if(_levelData.timerDuration == 0)
                    timerDuration = GameManager.instance.GameSettings.globalTimedModeSeconds;
            }

            FindObjectsOfType<MonoBehaviour>().OfType<IBeforeLevelLoadable>().ToList().ForEach(x => x.OnLevelLoaded(_levelData));
            LoadLevel(_levelData);
            FindObjectsOfType<MonoBehaviour>().OfType<ILevelLoadable>().ToList().ForEach(x => x.OnLevelLoaded(_levelData));
            Invoke(nameof(StartGame), 0.5f);
            if (GameManager.instance.IsTutorialMode())
            {
                tutorialManager.StartTutorial();
            }

            // Initialize timer if enabled for this level or if global timed mode is enabled
            if (_levelData.enableTimer && timerManager != null)
            {
                timerManager.InitializeTimer(timerDuration);
                if (timerPanel != null)
                {
                    timerPanel.SetActive(true);
                }
            }
            else if (timerManager != null)
            {
                timerManager.StopTimer();
                if (timerPanel != null)
                {
                    timerPanel.SetActive(false);
                }
            }
        }

        private void StartGame()
        {
            EventManager.GameStatus = EGameState.PrepareGame;
            classicModeHandler = FindObjectOfType<ClassicModeHandler>();
        }

        private void LoadLevel(Level levelData)
        {
            // 튜토리얼 레벨인지 확인
            bool isTutorialLevel = GameManager.instance.IsTutorialMode();
            
            // 튜토리얼에서 전환되는 경우 시작 애니메이션을 건너뛰도록 플래그 전달
            bool skipStartAnimation = isTransitioningFromTutorial;
            
            field.Generate(levelData, isTutorialLevel, skipStartAnimation);
            
            // 저장된 필드 상태가 있으면 복원
            if (shouldRestoreFieldState && savedFieldState != null)
            {
                Debug.Log("Restoring field state after level generation");
                field.RestoreFromState(savedFieldState);
                shouldRestoreFieldState = false;
                savedFieldState = null;
            }
            
            // 전환 플래그 리셋
            isTransitioningFromTutorial = false;
            
            // Reset field center cache when loading new level
            isFieldCenterCached = false;
            EventManager.GetEvent<Level>(EGameEvent.LevelLoaded).Invoke(levelData);
            OnLevelLoaded?.Invoke(levelData);
        }

        private void CheckLines(Shape obj)
        {
            // shape 패턴(블록 개수)만큼 점수 증가
            if (OnScored != null && obj != null)
            {
                try
                {
                    int shapeScore = obj.GetActiveItems().Count;
                    if (shapeScore > 0)
                        OnScored.Invoke(shapeScore);
                }
                catch (global::System.Exception e)
                {
                    Debug.LogWarning($"OnScored (shape place) event failed: {e.Message}");
                }
            }
            var lines = field.GetFilledLines(false, false);
            if (lines.Count > 0)
            {
                comboCounter++;
                // 콤보 달성 시 미스카운트 초기화
                missCounter = 0;
                shakeCanvas.DOShakePosition(0.2f, 35f, 50);

                // 튜토리얼 마지막 라인에서는 AfterMoveProcessing을 호출하지 않음
                if (!(isTransitioningFromTutorial && field.IsAllBlocksCleared()))
                {
                    StartCoroutine(AfterMoveProcessing(obj, lines));
                }

                if (comboCounter > 1)
                {
                    field.ShowOutline(true);
                }
            }
            else
            {
                missCounter++;
                if (missCounter >= GameManager.instance.GameSettings.ResetComboAfterMoves)
                {
                    field.ShowOutline(false);
                    missCounter = 0;
                    comboCounter = 0;
                }

                StartCoroutine(CheckLose());
            }
        }

        private Vector3 GetFieldCenter()
        {
            if (isFieldCenterCached)
                return cachedFieldCenter;

            Vector3 fieldCenter = Vector3.zero;
            int rowCount = field.cells.GetLength(0);
            int colCount = field.cells.GetLength(1);
            
            if (rowCount > 0 && colCount > 0)
            {
                Cell centerCell = field.cells[rowCount/2, colCount/2];
                if (centerCell != null)
                {
                    fieldCenter = centerCell.transform.position;
                }
            }
            
            cachedFieldCenter = fieldCenter;
            isFieldCenterCached = true;
            return fieldCenter;
        }

        private void ShowComboText(int comboCount)
        {
            if (GameManager.instance.IsTutorialMode()) return; // 튜토리얼에서는 콤보 UI 표시 안함
            Vector3 center = GetFieldCenter();
            Vector3 comboPosition = center + new Vector3(0, 0.75f, 0); // Same height as score text
            var comboText = comboTextPool.Get();
            comboText.transform.position = comboPosition;
            comboText.Show(comboCount);
            DOVirtual.DelayedCall(0.75f, () => { comboTextPool.Release(comboText); }); // Adjusted to match faster animation
        }

        private IEnumerator AfterMoveProcessing(Shape shape, List<List<Cell>> lines)
        {
            Vector3 center = GetFieldCenter();
            Vector3 scorePosition = center + new Vector3(0, 0.75f, 0); // Move score higher
            Vector3 gratzPosition = center + new Vector3(0, 0.35f, 0); // Position gratz between score and center

            yield return new WaitForSeconds(0.1f);
            if (gameMode == EGameMode.Adventure)
            {
                StartCoroutine(targetManager.AnimateTarget(lines));
            }

            yield return StartCoroutine(DestroyLines(lines, shape));

            var scoreTarget = CalculateScore(lines.Count, comboCounter);
            
            // OnScored 이벤트 호출 전에 null 체크
            if (OnScored != null)
            {
                try
                {
                    OnScored.Invoke(scoreTarget);
                }
                catch (global::System.Exception e)
                {
                    Debug.LogWarning($"OnScored event failed: {e.Message}");
                }
            }
            if (gameMode == EGameMode.Adventure)
            {
                targetManager.UpdateScoreTarget(scoreTarget);
            }
            
            // Show combo first if active
            if (!GameManager.instance.IsTutorialMode() && comboCounter > 1)
            {
                ShowComboText(comboCounter);
                yield return new WaitForSeconds(0.5f);
            }

            // Then show score at higher position
            if (!GameManager.instance.IsTutorialMode())
            {
                var scoreText = scoreTextPool.Get();
                scoreText.transform.position = scorePosition;
                scoreText.ShowScore(scoreTarget, scorePosition);
                DOVirtual.DelayedCall(0.75f, () => { scoreTextPool.Release(scoreText); }); // Halved from 1.5f to match faster animation
            }

            // Show congratulatory words below score
            if (!GameManager.instance.IsTutorialMode() && Random.Range(0, 3) == 0)
            {
                var txt = wordsPool.Get();
                txt.transform.position = gratzPosition;

                // Ensure txt is within the bounds of the gameCanvas
                var canvasCorners = new Vector3[4];
                gameCanvas.GetWorldCorners(canvasCorners);

                var txtPosition = txt.transform.position;
                txtPosition.x = Mathf.Clamp(txtPosition.x, canvasCorners[0].x, canvasCorners[2].x);
                txtPosition.y = Mathf.Clamp(txtPosition.y, canvasCorners[0].y, canvasCorners[2].y);
                txt.transform.position = txtPosition;

                DOVirtual.DelayedCall(1.5f, () => { wordsPool.Release(txt); });
            }

            // 모든 블록이 제거되었는지 확인하고 애니메이션 실행
            // 튜토리얼에서 전환되는 경우에는 애니메이션을 실행하지 않음 (EndTutorialCoroutine에서만 실행)
            if (field.IsAllBlocksCleared() && !isTransitioningFromTutorial)
            {
                yield return StartCoroutine(field.StartLineClearEffect());
            }

            // 튜토리얼에서 전환되는 경우에는 게임 오버 체크도 실행하지 않음
            if (EventManager.GameStatus == EGameState.Playing && !isTransitioningFromTutorial)
                yield return StartCoroutine(CheckLose());
        }

        private IEnumerator CheckLose()
        {
            // 라인 클리어 애니메이션 중에는 게임 오버 체크를 건너뜀
            if (isLineClearEffectPlaying)
            {
                yield break;
            }
            // 튜토리얼 모드일 때는 게임 오버 체크를 건너뛰기
            if (GameManager.instance.IsTutorialMode())
            {
                yield break;
            }

            if (gameMode != EGameMode.Classic && targetManager != null && targetManager.WillLevelBeComplete())
            {
                EventManager.GameStatus = EGameState.WinWaiting;
            }

            yield return new WaitForSeconds(0.5f); // Keep a small delay for game flow
            var lose = true;
            var availableShapes = cellDeck.GetShapes();
            foreach (var shape in availableShapes)
            {
                if (field.CanPlaceShape(shape))
                {
                    lose = false;
                    break;
                }
            }
            
            if (gameMode != EGameMode.Classic && targetManager != null && targetManager.WillLevelBeComplete())
            {
                yield return new WaitForSeconds(0.5f);
                SetWin();
                lose = false;
            }

            if (lose)
            {
                SetLose();
            }

            yield return null;
        }

        private void SetWin()
        {
            // 현재 완료한 레벨을 저장 (currentLevel이 실제 완료한 레벨)
            completedLevel = currentLevel;
            // 다음 레벨을 언락 (currentLevel + 1)
            GameDataManager.UnlockLevel(currentLevel + 1);
            EventManager.GameStatus = EGameState.PreWin;
        }

        private void SetLose()
        {
            // 게임 상태 삭제를 제거하여 리워드 상태 보존
            // 클래식 모드에서는 리워드 정보를 유지해야 함
            OnLose?.Invoke();
            EventManager.GameStatus = EGameState.PreFailed;
        }

        public IEnumerator EndAnimations(Action action)
        {
            yield return StartCoroutine(FillEmptyCellsFailed());
            action?.Invoke();
        }

        private IEnumerator FillEmptyCellsFailed()
        {
            SoundBase.instance.PlaySound(SoundBase.instance.fillEmpty);
            // 게임 오버 애니메이션용 템플릿을 고정 (Items/ItemTemplate 0)
            var template = Resources.Load<BlockPuzzleGameToolkit.Scripts.LevelsData.ItemTemplate>("Items/ItemTemplate 0");

            // 모든 셀에 대해 동일한 색상으로 FillCellFailed 호출
            int rows = field.cells.GetLength(0);
            int cols = field.cells.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var cell = field.cells[i, j];
                    if (cell != null && !cell.IsEmpty())
                    {
                        cell.FillCellFailed(template);
                        yield return new WaitForSeconds(0.01f);
                    }
                }
            }
            // 기존처럼 빈 셀에도 동일하게 적용
            emptyCells = field.GetEmptyCells();
            foreach (var cell in emptyCells)
            {
                cell.FillCellFailed(template);
                yield return new WaitForSeconds(0.01f);
            }
        }

        public void ClearEmptyCells()
        {
            foreach (var cell in emptyCells)
            {
                cell.ClearCell();
            }
        }

        /// <summary>
        /// 게임 오버 애니메이션으로 채워진 블록들을 제거하는 메서드
        /// </summary>
        public void ClearGameOverAnimationBlocks()
        {
            Debug.Log("[LevelManager] 게임 오버 애니메이션 블록들 제거 시작");
            
            // 먼저 모든 셀을 강제로 클리어
            var allCells = field.GetAllCells();
            for (int i = 0; i < allCells.GetLength(0); i++)
            {
                for (int j = 0; j < allCells.GetLength(1); j++)
                {
                    var cell = allCells[i, j];
                    if (cell != null)
                    {
                        // 게임 오버 애니메이션 블록을 강제로 제거
                        cell.ForceClearGameOverAnimation();
                    }
                }
            }
            
            // 게임 오버 시에는 필드를 비운 상태로 유지 (복원하지 않음)
            Debug.Log("[LevelManager] 게임 오버 - 필드를 비운 상태로 유지");
            
            Debug.Log("[LevelManager] 게임 오버 애니메이션 블록들 제거 완료");
        }

        private IEnumerator DestroyLines(List<List<Cell>> lines, Shape shape)
        {
            // 튜토리얼에서 전환 중이 아닐 때만 사운드 재생
            if (!isTransitioningFromTutorial)
                SoundBase.instance.PlayLimitSound(SoundBase.instance.combo[Mathf.Min(comboCounter, SoundBase.instance.combo.Length - 1)]);
            EventManager.GetEvent<Shape>(EGameEvent.LineDestroyed).Invoke(shape);

            // Mark cells as destroying immediately at the start
            foreach (var line in lines)
            {
                foreach (var cell in line)
                {
                    cell.SetDestroying(true);
                }
            }
            
            foreach (var line in lines)
            {
                if (line.Count == 0) continue;
                
                var lineExplosion = lineExplosionPool.Get();
                lineExplosion.Play(line, shape, RectTransformUtils.GetMinMaxAndSizeForCanvas(line, gameCanvas.GetComponent<Canvas>()), GetExplosionColor(shape));
                DOVirtual.DelayedCall(1.5f, () => { lineExplosionPool.Release(lineExplosion); });
                foreach (var cell in line)
                {
                    cell.DestroyCell();
                }
            }
            
            yield return null;
        }

        private Color GetExplosionColor(Shape shape)
        {
            var itemTemplateTopColor = shape.GetActiveItems()[0].itemTemplate.overlayColor;
            if (_levelData.levelType.singleColorMode)
            {
                itemTemplateTopColor = itemFactory.GetOneColor().overlayColor;
            }

            return itemTemplateTopColor;
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                // Debug keys for win/lose
                if(Keyboard.current[GameManager.instance.debugSettings.Win].wasPressedThisFrame)
                {
                    SetWin();
                }

                if(Keyboard.current[GameManager.instance.debugSettings.Lose].wasPressedThisFrame)
                {
                    SetLose();
                }

                // Other debug keys
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    // Fill the first row with tiles
                    var rowCells = new List<Cell>();
                    for (int col = 0; col < field.cells.GetLength(1); col++)
                    {
                        rowCells.Add(field.cells[0, col]);
                    }

                    var itemTemplate = Resources.Load<ItemTemplate>("Items/ItemTemplate 0");
                    
                    // Get all available bonus items from the level data
                    var availableBonuses = _levelData.targetInstance
                        .Where(t => t.targetScriptable.bonusItem != null)
                        .Select(t => t.targetScriptable.bonusItem)
                        .ToList();

                    foreach (var cell in rowCells)
                    {
                        if (cell != null && cell.IsEmpty())
                        {
                            cell.FillCell(itemTemplate);
                            
                            // 30% chance to add a bonus to the cell
                            if (availableBonuses.Count > 0 && Random.Range(0f, 1f) < 0.3f)
                            {
                                var randomBonus = availableBonuses[Random.Range(0, availableBonuses.Count)];
                                cell.SetBonus(randomBonus);
                            }
                        }
                    }

                    // Increment combo and show effects
                    comboCounter++;
                    // 콤보 달성 시 미스카운트 초기화
                    missCounter = 0;
                    field.ShowOutline(true);
                    
                    int scoreToAdd = CalculateScore(1, comboCounter);
                    
                    // Add score based on game mode
                    if (gameMode == EGameMode.Classic)
                    {
                        if (classicModeHandler != null)
                            classicModeHandler.UpdateScore(classicModeHandler.score + scoreToAdd);
                    }
                    else if (gameMode == EGameMode.Timed)
                    {
                        if (timedModeHandler != null)
                            timedModeHandler.UpdateScore(timedModeHandler.score + scoreToAdd);
                    }

                    // Create a dummy shape for the animation position
                    var dummyShape = itemFactory.CreateRandomShape(null, PoolObject.GetObject(cellDeck.shapePrefab.gameObject));
                    dummyShape.transform.position = rowCells[0].transform.position;
                    
                    // Screen shake effect
                    shakeCanvas.DOShakePosition(0.2f, 35f, 50);
                    
                    // Process the row destruction with proper animations
                    StartCoroutine(AfterMoveProcessing(dummyShape, new List<List<Cell>> { rowCells }));
                    
                    // Clean up the dummy shape
                    Destroy(dummyShape.gameObject);
                }

                // Use the configurable UpdateDeck key from debug settings instead of hardcoded dKey
                if (Keyboard.current[GameManager.instance.debugSettings.UpdateDeck].wasPressedThisFrame)
                {
                    cellDeck.ClearCellDecks();
                    cellDeck.FillCellDecks();
                }

                if (Keyboard.current[GameManager.instance.debugSettings.ClearAllBlocks].wasPressedThisFrame)
                {
                    ClearAllBlocks();
                }

                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    GameManager.instance.RestartLevel();
                }
            }
        }

        public Level GetCurrentLevel()
        {
            return _levelData;
        }

        public EGameMode GetGameMode()
        {
            return gameMode;
        }

        public FieldManager GetFieldManager()
        {
            return field;
        }

        public void PauseTimer(bool pause)
        {
            if (timerManager != null)
            {
                timerManager.PauseTimer(pause);
            }
        }
        
        /// <summary>
        /// 튜토리얼에서 일반 레벨로 전환될 때 호출되는 메서드
        /// </summary>
        public void SetTransitioningFromTutorial()
        {
            isTransitioningFromTutorial = true;
        }

        public void ClearAllBlocks()
        {
            if (field == null)
            {
                Debug.LogWarning("FieldManager is null, cannot clear blocks");
                return;
            }

            Debug.Log("Clearing all blocks from the field");
            
            // 모든 셀에서 블록 제거
            var allCells = new List<Cell>();
            for (int row = 0; row < field.cells.GetLength(0); row++)
            {
                for (int col = 0; col < field.cells.GetLength(1); col++)
                {
                    var cell = field.cells[row, col];
                    if (cell != null && !cell.IsEmpty())
                    {
                        allCells.Add(cell);
                    }
                }
            }

            if (allCells.Count > 0)
            {
                // 모든 블록을 한 번에 제거하는 애니메이션 실행
                StartCoroutine(ClearAllBlocksAnimation(allCells));
            }
            else
            {
                Debug.Log("No blocks to clear");
            }
        }

        /// <summary>
        /// 어드벤처 모드에서 crown-icon_1을 비활성화하는 메서드
        /// </summary>
        private void DisableCrownIcon()
        {
            // GameCanvas/SafeArea/UI/crown-icon_1 경로에서 crown-icon_1 오브젝트 찾기 및 비활성화
            Transform gameCanvas = GameObject.Find("GameCanvas")?.transform;
            if (gameCanvas != null)
            {
                Transform safeArea = gameCanvas.Find("SafeArea");
                if (safeArea != null)
                {
                    Transform ui = safeArea.Find("UI");
                    if (ui != null)
                    {
                        Transform crownIcon = ui.Find("crown-icon_1");
                        if (crownIcon != null)
                        {
                            crownIcon.gameObject.SetActive(false);
                            Debug.Log("[LevelManager] Adventure 모드에서 crown-icon_1 비활성화됨");
                        }
                        else
                        {
                            Debug.LogWarning("[LevelManager] crown-icon_1을 찾을 수 없습니다.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 게임 모드에 따라 crown-icon_1을 활성화하거나 비활성화하는 메서드
        /// </summary>
        private void SetCrownIconVisibility(EGameMode gameMode)
        {
            // GameCanvas/SafeArea/UI/crown-icon_1 경로에서 crown-icon_1 오브젝트 찾기
            Transform gameCanvas = GameObject.Find("GameCanvas")?.transform;
            if (gameCanvas != null)
            {
                Transform safeArea = gameCanvas.Find("SafeArea");
                if (safeArea != null)
                {
                    Transform ui = safeArea.Find("UI");
                    if (ui != null)
                    {
                        Transform crownIcon = ui.Find("crown-icon_1");
                        if (crownIcon != null)
                        {
                            // 클래식 모드일 때만 활성화, 다른 모드들은 비활성화
                            bool shouldBeActive = (gameMode == EGameMode.Classic);
                            crownIcon.gameObject.SetActive(shouldBeActive);
                            Debug.Log($"[LevelManager] {gameMode} 모드에서 crown-icon_1 {(shouldBeActive ? "활성화" : "비활성화")}됨");
                        }
                        else
                        {
                            Debug.LogWarning("[LevelManager] crown-icon_1을 찾을 수 없습니다.");
                        }
                    }
                }
            }
        }

        private IEnumerator ClearAllBlocksAnimation(List<Cell> cells)
        {
            // 화면 흔들림 효과
            shakeCanvas.DOShakePosition(0.3f, 50f, 100);
            
            // 모든 셀을 한 번에 제거
            foreach (var cell in cells)
            {
                if (cell != null && !cell.IsEmpty())
                {
                    cell.DestroyCell();
                }
            }

            // 애니메이션 완료 대기
            yield return new WaitForSeconds(0.5f);
            
            // 모든 블록이 제거되었는지 확인하고 전체 라인 클리어 애니메이션 실행
            if (field.IsAllBlocksCleared())
            {
                Debug.Log("All blocks cleared! Starting line clear effect animation");
                field.StartLineClearEffect();
            }
            
            Debug.Log($"Cleared {cells.Count} blocks from the field");
        }

        /// <summary>
        /// 게임 모드 전환 시 필드를 완전히 초기화하는 메서드
        /// </summary>
        private void ClearFieldCompletely()
        {
            if (field == null)
            {
                Debug.LogWarning("FieldManager is null, cannot clear field");
                return;
            }

            Debug.Log("Clearing field completely for game mode transition");
            
            // 모든 셀에서 블록 제거
            var allCells = field.GetAllCells();
            if (allCells != null)
            {
                for (int i = 0; i < allCells.GetLength(0); i++)
                {
                    for (int j = 0; j < allCells.GetLength(1); j++)
                    {
                        var cell = allCells[i, j];
                        if (cell != null && !cell.IsEmpty())
                        {
                            cell.ClearCell();
                        }
                    }
                }
            }
            
            // 콤보 카운터 초기화
            comboCounter = 0;
            missCounter = 0;
            
            // 아웃라인 숨기기
            field.ShowOutline(false);
            
            Debug.Log("Field cleared completely for game mode transition");
        }

        /// <summary>
        /// 새로운 점수 계산 공식에 따른 점수를 계산합니다.
        /// </summary>
        /// <param name="linesCount">제거된 라인 수</param>
        /// <param name="comboCount">현재 콤보 수</param>
        /// <returns>계산된 총 점수</returns>
        private int CalculateScore(int linesCount, int comboCount)
        {
            // 라인 제거 점수: 한줄 = 10, 두줄 = 20*2, 세줄 = 30*3, 네줄 = 40*4
            int lineScore = 0;
            if (linesCount == 1) lineScore = 10;
            else if (linesCount == 2) lineScore = 20 * 2;
            else if (linesCount == 3) lineScore = 30 * 3;
            else if (linesCount == 4) lineScore = 40 * 4;
            else lineScore = linesCount * 10 * linesCount; // 4줄 이상일 경우
            
            // 콤보 점수: 1콤보 = 10, 2콤보 = 20+10, 3콤보 = 30+10, 4콤보 = 40+10
            int comboScore = 0;
            if (comboCount == 1) comboScore = 10;
            else if (comboCount == 2) comboScore = 20 + 10;
            else if (comboCount == 3) comboScore = 30 + 10;
            else if (comboCount == 4) comboScore = 40 + 10;
            else comboScore = comboCount * 10 + 10; // 4콤보 이상일 경우
            
            return lineScore + comboScore;
        }
    }
}