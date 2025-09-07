using UnityEngine;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Popups;

namespace BlockPuzzleGameToolkit.Scripts.LevelsData
{
    // [CreateAssetMenu(fileName = "ClassicStateHandler", menuName = "BlockPuzzleGameToolkit/Levels/ClassicStateHandler")]
    public class ClassicLevelStateHandler : LevelStateHandler
    {
        private protected override void HandlePreFailed(LevelManager levelManager)
        {
            var level = levelManager.GetCurrentLevel();
            var preFailedPopup = level.levelType.preFailedPopup;
            levelManager.timerManager?.StopTimer();

            levelManager.StartCoroutine(levelManager.EndAnimations(() =>
            {
                // 클래식 모드의 특별한 게임 오버 로직
                var classicModeHandler = FindObjectOfType<ClassicModeHandler>();
                var gameState = GameState.Load(EGameMode.Classic) as ClassicGameState;
                bool shouldShowPreFailed = false; // 기본값을 false로 변경
                bool isNewHighScore = false;
                
                if (classicModeHandler != null)
                {
                    int currentScore = classicModeHandler.score;
                    int gameStartHighScore = gameState?.highScoreAtStart ?? 0;
                    
                    // 게임 시작 시의 최고 점수와 비교
                    isNewHighScore = currentScore > gameStartHighScore;
                    
                    Debug.Log($"[ClassicStateHandler] 게임 오버 - 현재점수: {currentScore}, 시작시최고: {gameStartHighScore}, 새최고: {isNewHighScore}");
                    Debug.Log($"[ClassicStateHandler] 게임 상태 - 리워드사용: {gameState?.hasUsedReward}, 보너스사용: {gameState?.hasUsedHighScoreBonus}");
                    Debug.Log($"[ClassicStateHandler] 게임 상태 상세 - scoreBeforeReward: {gameState?.scoreBeforeReward}, highScoreAtStart: {gameState?.highScoreAtStart}");
                    
                    // 리워드 광고 사용 가능 여부 판단
                    if (gameState == null || !gameState.hasUsedReward)
                    {
                        shouldShowPreFailed = true;
                        Debug.Log("[ClassicStateHandler] → PreFailed (첫 번째 게임 오버)");
                    }
                    else if (gameState.hasUsedReward && !gameState.hasUsedHighScoreBonus && isNewHighScore)
                    {
                        shouldShowPreFailed = true;
                        Debug.Log("[ClassicStateHandler] → PreFailed (새로운 최고점수 보너스)");
                    }
                    else
                    {
                        shouldShowPreFailed = false;
                        Debug.Log($"[ClassicStateHandler] → Failed_Classic (리워드 사용 불가) - 리워드: {gameState.hasUsedReward}, 보너스: {gameState.hasUsedHighScoreBonus}, 새최고: {isNewHighScore}");
                    }
                }
                
                if (shouldShowPreFailed && preFailedPopup != null && GameManager.instance.GameSettings.enablePreFailedPopup)
                {
                    MenuManager.instance.ShowPopup(preFailedPopup, levelManager.ClearGameOverAnimationBlocks, result =>
                    {
                        if (result == EPopupResult.Continue)
                        {
                            levelManager.cellDeck.UpdateCellDeckAfterFail();
                            EventManager.GameStatus = EGameState.Playing;
                        }
                        else
                        {
                            // Close 버튼 클릭 시 전면 광고 후 Failed_classic 호출
                            EventManager.GameStatus = EGameState.Failed;
                        }
                    });
                }
                else
                {
                    // PreFailed를 건너뛰고 바로 Failed 상태로
                    EventManager.GameStatus = EGameState.Failed;
                }
            }));
        }
    }
}