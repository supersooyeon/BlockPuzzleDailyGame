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

using BlockPuzzleGameToolkit.Scripts.Gameplay;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Enums;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace BlockPuzzleGameToolkit.Scripts.Popups
{
    public class FailedClassic : Failed
    {
        public GameObject failedStuff;
        public GameObject bestScoreStuff;

        public TextMeshProUGUI[] scoreText;
        public TextMeshProUGUI bestScoreText;
        // modeHandler 변수 제거 - ClassicModeHandler를 직접 사용

        protected override void OnEnable()
        {
            base.OnEnable();
            // BaseModeHandler 대신 ClassicModeHandler를 직접 찾기
            var classicModeHandler = FindObjectOfType<ClassicModeHandler>(false);
            if (classicModeHandler != null)
            {
                // Failed_Classic 팝업 표시 = 한 판의 게임이 완전히 끝남 = 게임 상태 삭제
                GameState.Delete(EGameMode.Classic);
                Debug.Log("[FailedClassic] 게임 종료 - 게임 상태 삭제");
                
                // 리워드 광고 사용 상태도 리셋 (다음 게임을 위해)
                classicModeHandler.ResetRewardAdUsage();
                
                var score = classicModeHandler.score;
                var bestScore = classicModeHandler.bestScore;
                scoreText[0].text = score.ToString();
                // DoTween을 사용한 점진적 증가 효과
                if (score > 0)
                {
                    float displayed = 0;
                    DOTween.To(() => displayed, x => {
                        displayed = x;
                        scoreText[1].text = Mathf.FloorToInt(displayed).ToString();
                    }, score, 0.8f).SetEase(Ease.OutCubic).OnComplete(() => {
                        scoreText[1].text = bestScore.ToString();
                    });
                }
                else
                {
                    scoreText[1].text = "0";
                }
                bestScoreText.text = bestScore.ToString();
                if (score >= bestScore)
                {
                    bestScoreStuff.SetActive(true);
                    failedStuff.SetActive(false);
                }
                else
                {
                    failedStuff.SetActive(true);
                    bestScoreStuff.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("[FailedClassic] ClassicModeHandler를 찾을 수 없습니다.");
            }
        }
    }
}