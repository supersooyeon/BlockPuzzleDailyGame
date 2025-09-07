using BlockPuzzleGameToolkit.Scripts.Data;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Enums;
using TMPro;
using UnityEngine;
using System.Collections;

namespace BlockPuzzleGameToolkit.Scripts.Gameplay
{
    public abstract class BaseModeHandler : MonoBehaviour
    {
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI bestScoreText;

        // bestScore 변수 완전 제거 - 각 모드 핸들러에서 직접 관리
        // [HideInInspector]
        // public int bestScore;

        [HideInInspector]
        public int score;

        protected LevelManager _levelManager;
        protected Coroutine _counterCoroutine;
        protected int _displayedScore = 0;
        [SerializeField]
        protected float counterSpeed = 0.01f;

        protected virtual void OnEnable()
        {
            _levelManager = FindObjectOfType<LevelManager>(true);
            
            if (_levelManager == null)
            {
                Debug.LogError("LevelManager not found!");
                return;
            }

            _levelManager.OnLose += OnLose;
            _levelManager.OnScored += OnScored;

            // ResetScore();
            LoadScores();
        }

        protected virtual void OnDisable()
        {
            if (_levelManager != null)
            {
                _levelManager.OnLose -= OnLose;
                _levelManager.OnScored -= OnScored;
            }
        }

        protected virtual void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && EventManager.GameStatus == EGameState.Playing)
            {
                SaveGameState();
            }
        }

        protected virtual void OnApplicationQuit()
        {
            if (EventManager.GameStatus == EGameState.Playing)
            {
                SaveGameState();
            }
        }

        public virtual void OnScored(int scoreToAdd)
        {
            // null 체크 추가
            if (this == null)
            {
                Debug.LogWarning("BaseModeHandler.OnScored called on destroyed object");
                return;
            }
            
            int previousScore = this.score;
            this.score += scoreToAdd;

            // Update UI immediately
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }

            if (_counterCoroutine != null)
            {
                StopCoroutine(_counterCoroutine);
            }
            _counterCoroutine = StartCoroutine(CountScore(previousScore, this.score));
        }

        protected IEnumerator CountScore(int startValue, int endValue)
        {
            _displayedScore = startValue;
            int scoreDifference = endValue - startValue;
            
            // 점수 차이에 따른 동적 속도 계산
            float baseSpeed = counterSpeed;
            float actualSpeed;
            
            if (scoreDifference <= 10)
            {
                // 작은 점수 차이: 기본 속도
                actualSpeed = baseSpeed;
            }
            else if (scoreDifference <= 50)
            {
                // 중간 점수 차이: 약간 빠르게
                actualSpeed = baseSpeed * 0.8f;
            }
            else if (scoreDifference <= 100)
            {
                // 큰 점수 차이: 빠르게
                actualSpeed = baseSpeed * 0.5f;
            }
            else if (scoreDifference <= 500)
            {
                // 매우 큰 점수 차이: 매우 빠르게
                actualSpeed = baseSpeed * 0.2f;
            }
            else
            {
                // 엄청 큰 점수 차이: 극도로 빠르게
                actualSpeed = baseSpeed * 0.1f;
            }
            
            // 점진적 가속 효과를 위한 변수
            float currentSpeed = actualSpeed;
            int stepSize = 1;
            
            // 점수 차이가 클 때는 단계별로 증가
            if (scoreDifference > 100)
            {
                stepSize = Mathf.Max(1, scoreDifference / 100);
            }
            
            while (_displayedScore < endValue)
            {
                // 점수 차이가 줄어들수록 속도 증가 (가속 효과)
                float progress = (float)(_displayedScore - startValue) / scoreDifference;
                float speedMultiplier = 1f + (progress * 2f); // 진행할수록 2배까지 빨라짐
                currentSpeed = actualSpeed / speedMultiplier;
                
                // 점수 증가
                int nextValue = Mathf.Min(_displayedScore + stepSize, endValue);
                _displayedScore = nextValue;
                
                // UI 업데이트
                if (scoreText != null)
                {
                    scoreText.text = _displayedScore.ToString();
                }
                
                // 점수 차이가 작아지면 더 세밀하게 증가
                if (endValue - _displayedScore <= 20)
                {
                    stepSize = 1;
                    currentSpeed = baseSpeed * 0.3f; // 마지막에는 적당한 속도로
                }
                
                yield return new WaitForSeconds(currentSpeed);
            }
            
            // 최종 값으로 정확하게 설정
            _displayedScore = endValue;
            if (scoreText != null)
            {
                scoreText.text = endValue.ToString();
            }
        }

        public virtual void OnLose()
        {
            DeleteGameState();
        }

        public virtual void UpdateScore(int newScore)
        {
            int previousScore = this.score;
            this.score = newScore;
            
            // Update UI immediately
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }
            
            // Animate the change
            if (_counterCoroutine != null)
            {
                StopCoroutine(_counterCoroutine);
            }
            _counterCoroutine = StartCoroutine(CountScore(previousScore, this.score));
        }

        public virtual void ResetScore()
        {
            // Stop any ongoing score animation
            if (_counterCoroutine != null)
            {
                StopCoroutine(_counterCoroutine);
                _counterCoroutine = null;
            }

            // Reset score and displayed score
            score = 0;
            _displayedScore = 0;

            // Update UI
            if (scoreText != null)
            {
                scoreText.text = "0";
            }

            // Delete the game state since we're resetting
            DeleteGameState();
        }

        protected abstract void LoadScores();
        protected abstract void SaveGameState();
        protected abstract void DeleteGameState();
    }
}