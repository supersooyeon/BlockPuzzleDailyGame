using BlockPuzzleGameToolkit.Scripts.Data;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using System.Collections;

namespace BlockPuzzleGameToolkit.Scripts.Gameplay
{
    public class TimedModeHandler : BaseModeHandler
    {
        [SerializeField]
        private float gameDuration = 180f; // 3 minutes default game duration
        
        // bestScore 변수를 직접 선언하여 BaseModeHandler의 중복 제거
        [HideInInspector]
        public int bestScore;
        
        private TimerManager _timerManager;
        private Sequence _pulseSequence;

        public TimerManager TimerManager
        {
            get
            {
                if (_timerManager == null)
                {
                    _timerManager = FindObjectOfType<TimerManager>();
                }
                return _timerManager;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (TimerManager == null)
            {
                Debug.LogError("TimerManager not found!");
                return;
            }

            EventManager.OnGameStateChanged += HandleGameStateChange;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.OnGameStateChanged -= HandleGameStateChange;
        }

        protected override void LoadScores()
        {
            // 권위 소스에서 최고 점수 로드
            bestScore = HighScoreService.GetBest(EGameMode.Timed);
            if (bestScoreText != null)
            {
                bestScoreText.text = bestScore.ToString();
            }

            // 현재 점수와 타이머는 상태에서 로드하되, state.bestScore는 신뢰하지 않음
            var state = GameState.Load(EGameMode.Timed) as TimedGameState;
            if (state != null)
            {
                score = state.score;
                if (scoreText != null)
                {
                    scoreText.text = score.ToString();
                }
                TimerManager.InitializeTimer(state.remainingTime > 0 ? state.remainingTime : gameDuration);
            }
            else
            {
                score = 0;
                if (scoreText != null)
                {
                    scoreText.text = "0";
                }
                TimerManager.InitializeTimer(gameDuration);
            }
        }

        protected override void SaveGameState()
        {
            var fieldManager = _levelManager.GetFieldManager();
            if (fieldManager != null)
            {
                var state = new TimedGameState
                {
                    score = score,
                    bestScore = HighScoreService.GetBest(EGameMode.Timed),
                    remainingTime = GetRemainingTime(),
                    gameMode = EGameMode.Timed,
                    gameStatus = EventManager.GameStatus
                };
                GameState.Save(state, fieldManager);
            }
        }

        protected override void DeleteGameState()
        {
            GameState.Delete(EGameMode.Timed);
        }

        public override void OnScored(int scoreToAdd)
        {
            base.OnScored(scoreToAdd);
            // AddBonusTime(scoreToAdd);
        }

        private void AddBonusTime(int scoreValue)
        {
            // Add 1 second for every 10 points scored
            float bonusTime = scoreValue / 10f;
            float currentTime = TimerManager.RemainingTime;
            TimerManager.InitializeTimer(Mathf.Min(currentTime + bonusTime, gameDuration));
        }

        public override void OnLose()
        {
            // Only update best score if timer actually reached 0
            if (TimerManager != null && TimerManager.RemainingTime <= 0)
            {
                int currentBest = HighScoreService.GetBest(EGameMode.Timed);
                if (HighScoreService.TryUpdateBest(EGameMode.Timed, score))
                {
                    bestScore = score;
                }
                else
                {
                    bestScore = currentBest;
                }
            }
            
            base.OnLose();
        }

        // Optional: Pause functionality
        public void PauseGame()
        {
            if (TimerManager != null)
            {
                TimerManager.PauseTimer(true);
            }
        }

        public void ResumeGame()
        {
            if (TimerManager != null)
            {
                TimerManager.PauseTimer(false);
            }
        }

        public float GetRemainingTime()
        {
            return TimerManager != null ? TimerManager.RemainingTime : 0f;
        }

        private void HandleGameStateChange(EGameState newState)
        {
            if (TimerManager != null)
            {
                if (newState == EGameState.Playing)
                {
                    TimerManager.PauseTimer(false);
                }
                else if (newState == EGameState.Paused)
                {
                    TimerManager.PauseTimer(true);
                }
            }
        }
    }
}