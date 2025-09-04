using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay;
using BlockPuzzleGameToolkit.Scripts.System;
using TMPro;
using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.Popups
{
    public class FailedTimed : FailedClassic
    {
        public TextMeshProUGUI timeText;
        private TimedModeHandler timedModeHandler;

        protected override void OnEnable()
        {
            base.OnEnable();
            // modeHandler 대신 TimedModeHandler를 직접 찾기
            timedModeHandler = FindObjectOfType<TimedModeHandler>(false);
            if (timedModeHandler != null)
            {
                var remainingTime = timedModeHandler.GetRemainingTime();

                bool isTimerFinished = remainingTime <= 0;
                if (!isTimerFinished)
                {
                    scoreText[1].text = "0";
                    bestScoreStuff.SetActive(false);
                    failedStuff.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("[FailedTimed] TimedModeHandler를 찾을 수 없습니다.");
            }
        }
    }
}