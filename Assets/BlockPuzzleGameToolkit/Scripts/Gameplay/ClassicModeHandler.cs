using BlockPuzzleGameToolkit.Scripts.Data;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Enums;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace BlockPuzzleGameToolkit.Scripts.Gameplay
{
    public class ClassicModeHandler : BaseModeHandler
    {
        public Image rhombusImage;
        
        // 새로운 UI 경로에서 bestScoreText를 찾기 위한 변수
        private TextMeshProUGUI _externalBestScoreText;
        
        // bestScore 변수를 직접 선언하여 BaseModeHandler의 중복 제거
        [HideInInspector]
        public int bestScore;

        protected override void LoadScores()
        {
            // 권위 소스에서 최고 점수 로드
            bestScore = HighScoreService.GetBest(EGameMode.Classic);
            
            // 새로운 UI 경로에서 bestScoreText 찾기
            FindExternalBestScoreText();
            
            // bestScoreText가 null인지 확인
            if (_externalBestScoreText != null)
            {
                _externalBestScoreText.text = bestScore.ToString();
            }
            else if (bestScoreText != null)
            {
                bestScoreText.text = bestScore.ToString();
            }
            else
            {
                Debug.LogWarning("[ClassicModeHandler] bestScoreText is null");
            }

            // 게임 상태에서 현재 점수만 로드 (최고 점수는 서비스만 신뢰)
            var state = GameState.Load(EGameMode.Classic) as ClassicGameState;
            if (state != null)
            {
                score = state.score;
                // state.bestScore는 무시
                
                // 게임 시작 시 최고 점수가 저장되지 않았다면 현재 최고 점수로 저장
                if (state.highScoreAtStart == 0)
                {
                    state.highScoreAtStart = bestScore;
                    // 상태 저장
                    var fieldManager = _levelManager.GetFieldManager();
                    if (fieldManager != null)
                    {
                        GameState.Save(state, fieldManager);
                        Debug.Log($"[DEBUG] 게임 시작 시 최고 점수 저장 - highScoreAtStart: {state.highScoreAtStart}");
                    }
                }
                
                // 리워드 사용 정보 로그 출력
                Debug.Log($"[DEBUG] 게임 상태 로드 - Score: {score}, hasUsedReward: {state.hasUsedReward}, scoreBeforeReward: {state.scoreBeforeReward}, highScoreAtStart: {state.highScoreAtStart}");
                
                if (scoreText != null)
                {
                    scoreText.text = score.ToString();
                }
                
                // 최고 점수 UI 업데이트
                UpdateBestScoreDisplay();
            }
            else
            {
                score = 0;
                Debug.Log($"[DEBUG] 게임 상태 없음 - Score: {score}, BestScore: {bestScore}");
                if (scoreText != null)
                {
                    scoreText.text = "0";
                }
                
                // 새 게임 시작 시 현재 최고 점수를 시작 시 최고 점수로 저장
                var newState = new ClassicGameState();
                newState.score = 0;
                newState.highScoreAtStart = bestScore;
                
                // 기존 게임 상태가 있다면 리워드 광고 사용 상태 보존
                var existingState = GameState.Load(EGameMode.Classic) as ClassicGameState;
                if (existingState != null)
                {
                    newState.hasUsedReward = existingState.hasUsedReward;
                    newState.hasUsedHighScoreBonus = existingState.hasUsedHighScoreBonus;
                    newState.scoreBeforeReward = existingState.scoreBeforeReward;
                    Debug.Log($"[DEBUG] 새 게임 시작 - 기존 리워드 상태 보존: hasUsedReward={newState.hasUsedReward}, hasUsedHighScoreBonus={newState.hasUsedHighScoreBonus}");
                }
                
                var fieldManager = _levelManager.GetFieldManager();
                if (fieldManager != null)
                {
                    GameState.Save(newState, fieldManager);
                    Debug.Log($"[DEBUG] 새 게임 시작 - highScoreAtStart 저장: {newState.highScoreAtStart}");
                }
            }
            
            Debug.Log($"[DEBUG] LoadScores 완료 - 최종 Score: {score}, 최종 BestScore: {bestScore}");
        }

        /// <summary>
        /// 리워드 광고 사용 상태를 완전히 리셋합니다.
        /// 게임 오버 후 FailedClassic 팝업에서 자동으로 호출됩니다.
        /// </summary>
        public void ResetRewardAdUsage()
        {
            var existingState = GameState.Load(EGameMode.Classic) as ClassicGameState;
            if (existingState != null)
            {
                existingState.hasUsedReward = false;
                existingState.hasUsedHighScoreBonus = false;
                existingState.scoreBeforeReward = 0;
                existingState.highScoreAtStart = HighScoreService.GetBest(EGameMode.Classic);
                
                var fieldManager = _levelManager?.GetFieldManager();
                if (fieldManager != null)
                {
                    GameState.Save(existingState, fieldManager);
                    Debug.Log("[ClassicModeHandler] 리워드 광고 사용 상태 리셋 완료");
                }
            }
        }

        // 튜토리얼에서 전환될 때 호출할 public 메서드
        public void InitializeForTutorialTransition()
        {
            Debug.Log($"[DEBUG] InitializeForTutorialTransition 시작");
            
            // BaseModeHandler의 OnEnable 로직을 수동으로 실행
            _levelManager = FindObjectOfType<LevelManager>(true);
            if (_levelManager != null)
            {
                _levelManager.OnLose += OnLose;
                _levelManager.OnScored += OnScored;
            }
            
            // 튜토리얼에서 전환될 때는 게임 상태를 무시하고 기본값으로 초기화
            bestScore = ResourceManager.instance.GetResource("Score").GetValue();
            score = 0;
            
            Debug.Log($"[DEBUG] 튜토리얼 전환 초기화 - Score: {score}, BestScore: {bestScore}");
            
            // UI 업데이트
            FindExternalBestScoreText();
            UpdateBestScoreDisplay();
            
            // Classic_score 오브젝트 활성화 및 scoreText 찾기
            Transform gameCanvas = GameObject.Find("GameCanvas")?.transform;
            if (gameCanvas != null)
            {
                Transform safeArea = gameCanvas.Find("SafeArea");
                if (safeArea != null)
                {
                    Transform ui = safeArea.Find("UI");
                    if (ui != null)
                    {
                        // Classic_score 오브젝트 활성화
                        Transform classicScore = ui.Find("Classic_score");
                        if (classicScore != null)
                        {
                            classicScore.gameObject.SetActive(true);
                            Debug.Log("[ClassicModeHandler] Classic_score object activated");
                            
                            // scoreText 찾기
                            if (scoreText == null)
                            {
                                scoreText = classicScore.GetComponent<TextMeshProUGUI>();
                                if (scoreText == null)
                                {
                                    scoreText = classicScore.GetComponentInChildren<TextMeshProUGUI>();
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[ClassicModeHandler] Classic_score object not found");
                        }
                        
                        // Score_Adventure 오브젝트 비활성화
                        Transform scoreAdventure = ui.Find("Score_Adventure");
                        if (scoreAdventure != null)
                        {
                            scoreAdventure.gameObject.SetActive(false);
                            Debug.Log("[ClassicModeHandler] Score_Adventure object deactivated");
                        }
                    }
                }
            }
            
            if (scoreText != null)
            {
                scoreText.text = "0";
                Debug.Log("[ClassicModeHandler] ScoreText found and updated to 0");
            }
            else
            {
                Debug.LogWarning("[ClassicModeHandler] ScoreText not found in Classic_score");
            }
            
            Debug.Log("[ClassicModeHandler] Initialized for tutorial transition - Score: 0, Best Score: " + bestScore);
        }

        private void FindExternalBestScoreText()
        {
            // GameCanvas/SafeArea/UI/crown-icon_1 경로에서 TextMeshProUGUI 컴포넌트 찾기
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
                            _externalBestScoreText = crownIcon.GetComponent<TextMeshProUGUI>();
                            if (_externalBestScoreText == null)
                            {
                                // 자식 오브젝트에서 TextMeshProUGUI 찾기
                                _externalBestScoreText = crownIcon.GetComponentInChildren<TextMeshProUGUI>();
                            }
                        }
                    }
                }
            }
            
            if (_externalBestScoreText == null)
            {
                Debug.LogWarning("[ClassicModeHandler] crown-icon_1에서 TextMeshProUGUI를 찾을 수 없습니다.");
            }
        }

        private void UpdateBestScoreDisplay()
        {
            Debug.Log($"[DEBUG] UpdateBestScoreDisplay - 현재 BestScore: {bestScore}");
            
            if (_externalBestScoreText != null)
            {
                _externalBestScoreText.text = bestScore.ToString();
                Debug.Log($"[DEBUG] 외부 BestScore UI 업데이트: {bestScore}");
            }
            else if (bestScoreText != null)
            {
                bestScoreText.text = bestScore.ToString();
                Debug.Log($"[DEBUG] 기본 BestScore UI 업데이트: {bestScore}");
            }
            else
            {
                Debug.LogWarning("[DEBUG] UpdateBestScoreDisplay - BestScore UI 컴포넌트를 찾을 수 없음");
            }
        }

        protected override void SaveGameState()
        {
            Debug.Log($"[DEBUG] SaveGameState 시작 - 현재 Score: {score}, 현재 BestScore: {bestScore}");
            
            var fieldManager = _levelManager.GetFieldManager();
            if (fieldManager != null)
            {
                // 권위 소스에서 베스트 확인 후 상태에 기록
                int authoritativeBest = HighScoreService.GetBest(EGameMode.Classic);
                Debug.Log($"[DEBUG] 게임 상태 저장 - Score: {score}, 저장할 BestScore: {authoritativeBest}");
                
                // 기존 게임 상태를 로드하여 리워드 광고 관련 상태와 필드 상태 유지
                var existingState = GameState.Load(EGameMode.Classic) as ClassicGameState;
                
                var state = new ClassicGameState
                {
                    score = score,
                    bestScore = authoritativeBest,
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
                
                GameState.Save(state, fieldManager);
                Debug.Log($"[DEBUG] SaveGameState 완료 - 게임 상태 저장됨 (리워드 상태 포함)");
            }
            else
            {
                Debug.LogWarning("[DEBUG] SaveGameState 실패 - FieldManager를 찾을 수 없음");
            }
        }

        protected override void DeleteGameState()
        {
            Debug.Log($"[DEBUG] DeleteGameState 시작 - 현재 Score: {score}, 현재 BestScore: {bestScore}");
            GameState.Delete(EGameMode.Classic);
            Debug.Log($"[DEBUG] DeleteGameState 완료 - 게임 상태 삭제됨");
        }
        
        /// <summary>
        /// 새 게임 시작 시 리워드 상태 초기화
        /// </summary>
        public void ResetRewardState()
        {
            var state = GameState.Load(EGameMode.Classic) as ClassicGameState;
            if (state != null)
            {
                state.hasUsedReward = false;
                state.scoreBeforeReward = 0;
                state.hasUsedHighScoreBonus = false;
                state.highScoreAtStart = HighScoreService.GetBest(EGameMode.Classic); // 새 게임 시작 시 최고 점수 갱신
                
                var fieldManager = _levelManager.GetFieldManager();
                if (fieldManager != null)
                {
                    GameState.Save(state, fieldManager);
                    Debug.Log($"[ClassicModeHandler] 리워드 상태 초기화 완료 - highScoreAtStart: {state.highScoreAtStart}");
                }
            }
        }

        public override void OnLose()
        {
            Debug.Log($"[DEBUG] OnLose 시작 - 현재 Score: {score}, 현재 BestScore: {bestScore}");
            
            int currentBest = HighScoreService.GetBest(EGameMode.Classic);
            int todayBest = DailyScoreService.GetTodayBest(EGameMode.Classic);
            int weekBest = DailyScoreService.GetThisWeekBest(EGameMode.Classic);
            Debug.Log($"[LEADERBOARD] 게임 종료 - 현재 점수: {score}, 저장된 최고 점수: {currentBest}, 오늘의 최고 점수: {todayBest}, 이번주의 최고 점수: {weekBest}");
            
            // 전체 기간 최고 점수 업데이트
            bool isNewAllTimeBest = HighScoreService.TryUpdateBest(EGameMode.Classic, score);
            // 오늘의 최고 점수 업데이트
            bool isNewTodayBest = DailyScoreService.TryUpdateTodayBest(EGameMode.Classic, score);
            // 이번주의 최고 점수 업데이트
            bool isNewWeekBest = DailyScoreService.TryUpdateThisWeekBest(EGameMode.Classic, score);
            
            if (isNewAllTimeBest)
            {
                bestScore = score;
                Debug.Log($"[LEADERBOARD] 새로운 전체 기간 최고 점수 설정: {bestScore}");
            }
            else
            {
                bestScore = currentBest;
                Debug.Log($"[LEADERBOARD] 전체 기간 최고 점수 유지: {bestScore}");
            }
            
            if (isNewTodayBest)
            {
                Debug.Log($"[LEADERBOARD] 새로운 오늘의 최고 점수 설정: {score}");
            }
            else
            {
                Debug.Log($"[LEADERBOARD] 오늘의 최고 점수 유지: {todayBest}");
            }
            
            if (isNewWeekBest)
            {
                Debug.Log($"[LEADERBOARD] 새로운 이번주의 최고 점수 설정: {score}");
            }
            else
            {
                Debug.Log($"[LEADERBOARD] 이번주의 최고 점수 유지: {weekBest}");
            }
            
            // UI 업데이트
            UpdateBestScoreDisplay();
            
            // 리더보드에 오늘의 최고 점수 제출 (일간 리더보드용)
            if (GPGSLoginManager.instance != null && GPGSLoginManager.instance.IsAuthenticated)
            {
                int todayScoreToSubmit = DailyScoreService.GetTodayBest(EGameMode.Classic);
                Debug.Log($"[LEADERBOARD] ===== 일간 리더보드 점수 제출 =====");
                Debug.Log($"[LEADERBOARD] 현재 점수: {score}");
                Debug.Log($"[LEADERBOARD] 오늘의 최고 점수: {todayScoreToSubmit}");
                Debug.Log($"[LEADERBOARD] 일간 리더보드에 제출할 점수: {todayScoreToSubmit}");
                
                // 새로고침 없이 즉시 제출만 수행
                GPGSLoginManager.instance.SubmitScore(todayScoreToSubmit, true);
                Debug.Log($"[LEADERBOARD] 일간 점수 제출 요청 완료: {todayScoreToSubmit}");
                Debug.Log($"[LEADERBOARD] ======================================");
            }
            else
            {
                Debug.Log($"[LEADERBOARD] GPGS 로그인이 되지 않아 일간 리더보드 제출 실패");
            }
            
            // 주간 리더보드 점수 제출
            if (GPGSLoginManager.instance != null && GPGSLoginManager.instance.IsAuthenticated)
            {
                int weekScoreToSubmit = DailyScoreService.GetThisWeekBest(EGameMode.Classic);
                Debug.Log($"[LEADERBOARD] ===== 주간 리더보드 점수 제출 =====");
                Debug.Log($"[LEADERBOARD] 현재 점수: {score}");
                Debug.Log($"[LEADERBOARD] 이번주의 최고 점수: {weekScoreToSubmit}");
                Debug.Log($"[LEADERBOARD] 주간 리더보드에 제출할 점수: {weekScoreToSubmit}");
                
                GPGSLoginManager.instance.SubmitWeeklyScore(weekScoreToSubmit, true);
                Debug.Log($"[LEADERBOARD] 주간 점수 제출 요청 완료: {weekScoreToSubmit}");
                Debug.Log($"[LEADERBOARD] ======================================");
            }
            else
            {
                Debug.Log($"[LEADERBOARD] GPGS 로그인이 되지 않아 주간 리더보드 제출 실패");
            }
            
            // 전체 기간 리더보드 점수 제출
            if (GPGSLoginManager.instance != null && GPGSLoginManager.instance.IsAuthenticated)
            {
                int allTimeScoreToSubmit = HighScoreService.GetBest(EGameMode.Classic);
                Debug.Log($"[LEADERBOARD] ===== 전체 기간 리더보드 점수 제출 =====");
                Debug.Log($"[LEADERBOARD] 현재 점수: {score}");
                Debug.Log($"[LEADERBOARD] 전체 기간 최고 점수: {allTimeScoreToSubmit}");
                Debug.Log($"[LEADERBOARD] 전체 기간 리더보드에 제출할 점수: {allTimeScoreToSubmit}");
                
                GPGSLoginManager.instance.SubmitAllTimeScore(allTimeScoreToSubmit, true);
                Debug.Log($"[LEADERBOARD] 전체 기간 점수 제출 요청 완료: {allTimeScoreToSubmit}");
                Debug.Log($"[LEADERBOARD] ==========================================");
            }
            else
            {
                Debug.Log($"[LEADERBOARD] GPGS 로그인이 되지 않아 전체 기간 리더보드 제출 실패");
            }

            // 점수 제출 검증
            StartCoroutine(VerifyScoreSubmission(DailyScoreService.GetTodayBest(EGameMode.Classic)));
            Debug.Log($"[DEBUG] OnLose 완료 - 최종 Score: {score}, 최종 BestScore: {bestScore}");
            // 클래식 모드에서는 즉시 GameState를 삭제하지 않음 (리워드 상태 보존)
            // base.OnLose(); // DeleteGameState는 Failed_Classic에서만 호출
        }

        public override void OnScored(int scoreToAdd)
        {
            Debug.Log($"[DEBUG] OnScored 시작 - 현재 Score: {score}, 현재 BestScore: {bestScore}, 추가 점수: {scoreToAdd}");
            
            int previousScore = this.score;
            this.score += scoreToAdd;

            Debug.Log($"[DEBUG] 점수 업데이트 - 이전 Score: {previousScore}, 새로운 Score: {score}");

            // 점수 UI 즉시 갱신
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }

            // 점수 애니메이션
            if (_counterCoroutine != null)
            {
                StopCoroutine(_counterCoroutine);
            }
            _counterCoroutine = StartCoroutine(CountScore(previousScore, this.score));

            int authoritativeBest = HighScoreService.GetBest(EGameMode.Classic);
            Debug.Log($"[DEBUG] 최고 점수 비교 - 현재 Score: {score}, 저장된 BestScore: {authoritativeBest}");
            
            if (score > authoritativeBest)
            {
                int previousBest = bestScore;
                bestScore = score;
                Debug.Log($"[DEBUG] 최고 점수 업데이트 - 이전 BestScore: {previousBest}, 새로운 BestScore: {score}");
                
                HighScoreService.TryUpdateBest(EGameMode.Classic, score);
                Debug.Log($"[DEBUG] 새로운 최고 점수를 저장 시도: {score}");
                
                if (_bestScoreCoroutine != null)
                {
                    StopCoroutine(_bestScoreCoroutine);
                }
                _bestScoreCoroutine = StartCoroutine(CountBestScore(previousBest, bestScore));
            }
            else
            {
                Debug.Log($"[DEBUG] 최고 점수 갱신 안됨 - Score({score}) <= 저장된 BestScore({authoritativeBest})");
            }
            
            Debug.Log($"[DEBUG] OnScored 완료 - 최종 Score: {score}, 최종 BestScore: {bestScore}");
        }

        private Coroutine _bestScoreCoroutine;
        private IEnumerator CountBestScore(int startValue, int endValue)
        {
            Debug.Log($"[DEBUG] CountBestScore 시작 - 시작값: {startValue}, 목표값: {endValue}");
            
            int displayedBest = startValue;
            int scoreDifference = endValue - startValue;
            
            // 점수 차이에 따른 동적 속도 계산
            float baseSpeed = counterSpeed * 2.5f; // 기본적으로 BestScore는 Score보다 조금 느리게
            float actualSpeed;
            
            if (scoreDifference <= 10)
            {
                // 작은 점수 차이: 기본 속도
                actualSpeed = baseSpeed;
            }
            else if (scoreDifference <= 50)
            {
                // 중간 점수 차이: 약간 빠르게
                actualSpeed = baseSpeed * 0.7f;
            }
            else if (scoreDifference <= 100)
            {
                // 큰 점수 차이: 빠르게
                actualSpeed = baseSpeed * 0.4f;
            }
            else if (scoreDifference <= 500)
            {
                // 매우 큰 점수 차이: 매우 빠르게
                actualSpeed = baseSpeed * 0.15f;
            }
            else
            {
                // 엄청 큰 점수 차이: 극도로 빠르게
                actualSpeed = baseSpeed * 0.08f;
            }
            
            // 점진적 가속 효과를 위한 변수
            float currentSpeed = actualSpeed;
            int stepSize = 1;
            
            // 점수 차이가 클 때는 단계별로 증가
            if (scoreDifference > 100)
            {
                stepSize = Mathf.Max(1, scoreDifference / 80); // BestScore는 Score보다 조금 더 세밀하게
            }
            
            while (displayedBest < endValue)
            {
                // 점수 차이가 줄어들수록 속도 증가 (가속 효과)
                float progress = (float)(displayedBest - startValue) / scoreDifference;
                float speedMultiplier = 1f + (progress * 1.5f); // 진행할수록 1.5배까지 빨라짐
                currentSpeed = actualSpeed / speedMultiplier;
                
                // 점수 증가
                int nextValue = Mathf.Min(displayedBest + stepSize, endValue);
                displayedBest = nextValue;
                
                Debug.Log($"[DEBUG] CountBestScore 진행 중 - displayedBest: {displayedBest}, bestScore: {bestScore}");
                
                bestScore = displayedBest;
                UpdateBestScoreDisplay();
                
                // 점수 차이가 작아지면 더 세밀하게 증가
                if (endValue - displayedBest <= 15)
                {
                    stepSize = 1;
                    currentSpeed = baseSpeed * 0.25f; // 마지막에는 적당한 속도로
                }
                
                yield return new WaitForSeconds(currentSpeed);
            }
            
            bestScore = endValue;
            Debug.Log($"[DEBUG] CountBestScore 완료 - 최종 bestScore: {bestScore}");
            UpdateBestScoreDisplay();
            
            // 애니메이션 완료 후 권위 소스와 동기화
            int savedBestScore = HighScoreService.GetBest(EGameMode.Classic);
            if (endValue == savedBestScore)
            {
                Debug.Log($"[LEADERBOARD] 최고 점수 애니메이션 완료 후 동기화: {bestScore}");
            }
            else
            {
                Debug.LogWarning($"[LEADERBOARD] 애니메이션 완료 후 값 불일치: bestScore={bestScore}, savedBestScore={savedBestScore}");
                bestScore = savedBestScore; // 강제로 동기화
                Debug.Log($"[DEBUG] 🚨 bestScore를 savedBestScore로 강제 동기화: {bestScore}");
            }
        }
        
        /// <summary>
        /// 점수 제출 확인
        /// </summary>
        private IEnumerator VerifyScoreSubmission(int submittedScore)
        {
            Debug.Log($"[LEADERBOARD] 점수 제출 확인 시작: {submittedScore}");
            yield return new WaitForSeconds(3f); // 3초 대기
            
            if (GPGSLoginManager.instance != null)
            {
                Debug.Log($"[LEADERBOARD] 제출된 점수 확인 중: {submittedScore}");
                GPGSLoginManager.instance.LoadLeaderboardScores((scores) => {
                    Debug.Log($"[LEADERBOARD] 확인된 점수 개수: {scores.Length}");
                    if (scores.Length > 0)
                    {
                        Debug.Log($"[LEADERBOARD] 리더보드 최고 점수: {scores[0].value}");
                        Debug.Log($"[LEADERBOARD] 제출한 점수: {submittedScore}");
                        
                        if (scores[0].value >= submittedScore)
                        {
                            Debug.Log($"[LEADERBOARD] ✅ 점수 제출 성공 확인: {submittedScore}");
                        }
                        else
                        {
                            Debug.Log($"[LEADERBOARD] ❌ 점수 제출 실패 또는 아직 반영 안됨: {submittedScore}");
                        }
                    }
                    else
                    {
                        Debug.Log("[LEADERBOARD] 리더보드에 점수가 없습니다.");
                    }
                });
            }
        }
    }
}