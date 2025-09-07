using UnityEngine;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Popups;

namespace BlockPuzzleGameToolkit.Scripts.LevelsData
{
    // [CreateAssetMenu(fileName = "AdventureStateHandler", menuName = "BlockPuzzleGameToolkit/Levels/AdventureStateHandler")]
    public class AdventureLevelStateHandler : LevelStateHandler
    {
        private protected override void HandlePreFailed(LevelManager levelManager)
        {
            Debug.Log("Adventure mode: Game is about to end...");
            var level = levelManager.GetCurrentLevel();
            var preFailedPopup = level.levelType.preFailedPopup;
            levelManager.timerManager?.StopTimer();

            levelManager.StartCoroutine(levelManager.EndAnimations(() =>
            {
                // PreFailed 팝업 호출 부분을 주석 처리
                /*
                if (preFailedPopup != null && GameManager.instance.GameSettings.enablePreFailedPopup)
                {
                    MenuManager.instance.ShowPopup(preFailedPopup, levelManager.ClearEmptyCells, result =>
                    {
                        if (result == EPopupResult.Continue)
                        {
                            if (level.enableTimer)
                            {
                                float newTime = levelManager.timerManager.RemainingTime > 0 ?
                                    levelManager.timerManager.RemainingTime :
                                    GameManager.instance.GameSettings.continueTimerBonus;
                                levelManager.timerManager.InitializeTimer(newTime);
                            }
                            levelManager.cellDeck.UpdateCellDeckAfterFail();
                            EventManager.GameStatus = EGameState.Playing;
                        }
                    });
                }
                else
                {
                    EventManager.GameStatus = EGameState.Failed;
                }
                */
                
                // PreFailed 팝업 대신 바로 Failed 상태로 변경
                EventManager.GameStatus = EGameState.Failed;
            }));
        }
    }
}