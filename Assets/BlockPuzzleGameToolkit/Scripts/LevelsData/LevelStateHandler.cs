using UnityEngine;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Popups;

namespace BlockPuzzleGameToolkit.Scripts.LevelsData
{
    public abstract class LevelStateHandler : ScriptableObject
    {
        public virtual void HandleState(EGameState state, LevelManager levelManager)
        {
            switch (state)
            {
                case EGameState.PrepareGame:
                    HandlePrepareGame(levelManager);
                    break;
                case EGameState.Playing:
                    HandlePlaying(levelManager);
                    break;
                case EGameState.PreFailed:
                    HandlePreFailed(levelManager);
                    break;
                case EGameState.Failed:
                    HandleFailed(levelManager);
                    break;
                case EGameState.PreWin:
                    HandlePreWin(levelManager);
                    break;
                case EGameState.Win:
                    HandleWin(levelManager);
                    break;
            }
        }
        
        private protected virtual void HandlePrepareGame(LevelManager levelManager)
        {
            var level = levelManager.GetCurrentLevel();
            var prePlayPopup = level.levelType.prePlayPopup;

            if (prePlayPopup != null)
            {
                MenuManager.instance.ShowPopup(prePlayPopup, null, _ => EventManager.GameStatus = EGameState.Playing);
            }
            else
            {
                EventManager.GameStatus = EGameState.Playing;
            }
        }

        private protected virtual void HandlePlaying(LevelManager levelManager) {}

        private protected virtual void HandlePreFailed(LevelManager levelManager) {}

        private protected virtual void HandleFailed(LevelManager levelManager)
        {
            var failedPopup = levelManager.GetCurrentLevel().levelType.failedPopup;
            if (failedPopup != null)
            {
                MenuManager.instance.ShowPopup(failedPopup);
            }
        }

        private protected virtual void HandlePreWin(LevelManager levelManager)
        {
            // 스테이지 클리어 확정 시점: 완료한 스테이지 번호로 리더보드 제출
            Debug.Log("=== [LEADERBOARD] PreWin - 스테이지 클리어 처리 시작 ===");
            Debug.Log($"[LEADERBOARD] LevelManager: {(levelManager != null ? "존재" : "null")}");
            Debug.Log($"[LEADERBOARD] GPGSLoginManager: {(GPGSLoginManager.instance != null ? "존재" : "null")}");
            
            try
            {
                if (GPGSLoginManager.instance != null && !string.IsNullOrEmpty(GPGSLoginManager.instance.leaderboardIdHighestStage))
                {
                    // SetWin()에서 저장한 완료한 레벨 사용
                    int stageToSubmit = levelManager != null ? levelManager.completedLevel : levelManager.GetCurrentLevel()?.Number ?? GameDataManager.GetLevelNum();
                    Debug.Log($"[LEADERBOARD] LevelManager.currentLevel: {(levelManager != null ? levelManager.currentLevel.ToString() : "null")}");
                    Debug.Log($"[LEADERBOARD] LevelManager.completedLevel: {(levelManager != null ? levelManager.completedLevel.ToString() : "null")}");
                    Debug.Log($"[LEADERBOARD] GetCurrentLevel().Number: {(levelManager?.GetCurrentLevel()?.Number.ToString() ?? "null")}");
                    Debug.Log($"[LEADERBOARD] GameDataManager.GetLevelNum(): {GameDataManager.GetLevelNum()}");
                    Debug.Log($"[LEADERBOARD] 최종 제출할 스테이지: {stageToSubmit}");
                    Debug.Log($"[LEADERBOARD] 리더보드 ID: {GPGSLoginManager.instance.leaderboardIdHighestStage}");
                    
                    GPGSLoginManager.instance.SubmitScoreToBoard(stageToSubmit, GPGSLoginManager.instance.leaderboardIdHighestStage);
                }
                else
                {
                    Debug.LogWarning("[LEADERBOARD] GPGSLoginManager가 null이거나 리더보드 ID가 설정되지 않음");
                    Debug.Log($"[LEADERBOARD] GPGSLoginManager.instance: {GPGSLoginManager.instance}");
                    Debug.Log($"[LEADERBOARD] leaderboardIdHighestStage: {(GPGSLoginManager.instance?.leaderboardIdHighestStage ?? "null")}");
                }
            }
            catch (global::System.Exception e)
            {
                Debug.LogError($"[LEADERBOARD] 최고 스테이지 제출(PreWin) 중 예외: {e.Message}");
                Debug.LogError($"[LEADERBOARD] 예외 타입: {e.GetType()}");
                Debug.LogError($"[LEADERBOARD] 스택 트레이스: {e.StackTrace}");
            }
            
            Debug.Log("=== [LEADERBOARD] PreWin - 스테이지 클리어 처리 완료 ===");
            var preWinPopup = levelManager.GetCurrentLevel().levelType.preWinPopup;
            if (preWinPopup != null)
            {
                MenuManager.instance.ShowPopupDelayed(preWinPopup, null, _ => EventManager.GameStatus = EGameState.Win);
            }
            else
            {
                EventManager.GameStatus = EGameState.Win;
            }
        }

        private protected virtual void HandleWin(LevelManager levelManager)
        {
            var winPopup = levelManager.GetCurrentLevel().levelType.winPopup;
            if (winPopup != null)
            {
                MenuManager.instance.ShowPopup(winPopup);
            }
            else
            {
                GameManager.instance.OpenMap();
            }
        }
    }
} 