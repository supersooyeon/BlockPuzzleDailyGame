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
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.GUI;
using BlockPuzzleGameToolkit.Scripts.LevelsData;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Data;
using BlockPuzzleGameToolkit;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzleGameToolkit.Scripts.Popups
{
    public class MainMenu : Popup
    {
        public CustomButton timedMode;
        public CustomButton classicMode;
        public CustomButton adventureMode;
        public CustomButton settingsButton;
        // public CustomButton luckySpin;
        public CustomButton leaderboardButton;
        public GameObject playObject;

        // [SerializeField]
        // private GameObject freeSpinMarker;

        [SerializeField]
        private Image background;

        public Action OnAnimationEnded;

        // private const string LastFreeSpinTimeKey = "LastFreeSpinTime";

        private void Start()
        {
            timedMode.onClick.AddListener(PlayTimedMode);
            classicMode.onClick.AddListener(PlayClassicMode);
            adventureMode.onClick.AddListener(PlayAdventureMode);
            settingsButton.onClick.AddListener(SettingsButtonClicked);
            // luckySpin.onClick.AddListener(LuckySpinButtonClicked);
            leaderboardButton.onClick.AddListener(LeaderboardButtonClicked);
            // UpdateFreeSpinMarker();
            GameDataManager.LevelNum = PlayerPrefs.GetInt("Level", 1);
            var levelsCount = Resources.LoadAll<Level>("Levels").Length;
            // luckySpin.gameObject.SetActive(GameManager.instance.GameSettings.enableLuckySpin);
            if(!GameManager.instance.GameSettings.enableTimedMode)
                timedMode.gameObject.SetActive(false);
        }
        
        /*
        private bool CanUseFreeSpinToday()
        {
            if (!PlayerPrefs.HasKey(LastFreeSpinTimeKey))
            {
                return true;
            }

            var lastFreeSpinTimeStr = PlayerPrefs.GetString(LastFreeSpinTimeKey);
            var lastFreeSpinTime = DateTime.Parse(lastFreeSpinTimeStr);
            return DateTime.Now.Date > lastFreeSpinTime.Date;
        }

        private void UpdateFreeSpinMarker()
        {
            var isFreeSpinAvailable = CanUseFreeSpinToday();
            freeSpinMarker.SetActive(isFreeSpinAvailable);
        }
        */

        private void PlayClassicMode()
        {
            GameManager.instance.SetGameMode(EGameMode.Classic);
            GameManager.instance.OpenMap();
        }

        private void PlayAdventureMode()
        {
            GameManager.instance.SetGameMode(EGameMode.Adventure);
            GameManager.instance.OpenMap();
        }

        private void PlayTimedMode()
        {
            GameManager.instance.SetGameMode(EGameMode.Timed);
            GameManager.instance.OpenMap();
        }

        private void SettingsButtonClicked()
        {
            MenuManager.instance.ShowPopup<Settings>();
        }

        /*
        private void LuckySpinButtonClicked()
        {
            MenuManager.instance.ShowPopup<LuckySpin>(null, _ => UpdateFreeSpinMarker());
        }
        */

        private void LeaderboardButtonClicked()
        {
            Debug.Log("=== [LEADERBOARD] 리더보드 버튼 클릭 ===");
            
            // 현재 저장된 최고 점수 확인
            if (ResourceManager.instance != null)
            {
                var scoreResource = ResourceManager.instance.GetResource("Score");
                if (scoreResource != null)
                {
                    int bestScore = scoreResource.GetValue();
                    Debug.Log($"[LEADERBOARD] 현재 저장된 최고 점수: {bestScore}");
                }
                else
                {
                    Debug.LogWarning("[LEADERBOARD] Score 리소스를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning("[LEADERBOARD] ResourceManager.instance가 null입니다.");
            }
            
            // GPGS 인증 상태 확인
            if (GPGSLoginManager.instance != null)
            {
                Debug.Log($"[LEADERBOARD] GPGS 인증 상태: {GPGSLoginManager.instance.IsAuthenticated}");
                Debug.Log($"[LEADERBOARD] 사용자 이름: {GPGSLoginManager.instance.GetUserName()}");
                Debug.Log($"[LEADERBOARD] 사용자 ID: {GPGSLoginManager.instance.GetUserId()}");
                
                // 현재 사용자의 리더보드 점수 로드
                Debug.Log("[LEADERBOARD] 리더보드 점수 로드 시작...");
                try
                {
                    GPGSLoginManager.instance.LoadLeaderboardScores((scores) => {
                        try
                        {
                            Debug.Log($"[LEADERBOARD] 리더보드 점수 로드 완료: {scores.Length}개 점수");
                            if (scores.Length > 0)
                            {
                                Debug.Log($"[LEADERBOARD] 최고 점수: {scores[0].value}");
                                Debug.Log($"[LEADERBOARD] 최고 점수 사용자: {scores[0].userID}");
                                Debug.Log($"[LEADERBOARD] 최고 점수 날짜: {scores[0].date}");
                                
                                // 현재 사용자의 점수 찾기
                                string currentUserId = GPGSLoginManager.instance.GetUserId();
                                Debug.Log($"[LEADERBOARD] 현재 사용자 ID: {currentUserId}");
                                
                                for (int i = 0; i < scores.Length; i++)
                                {
                                    if (scores[i].userID == currentUserId)
                                    {
                                        Debug.Log($"[LEADERBOARD] 현재 사용자 점수: {scores[i].value} (순위: {i + 1})");
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                Debug.Log("[LEADERBOARD] 리더보드에 점수가 없습니다.");
                            }
                        }
                        catch (global::System.Exception e)
                        {
                            Debug.LogError($"[LEADERBOARD] 리더보드 점수 처리 중 에러: {e.Message}");
                        }
                    });
                }
                catch (global::System.Exception e)
                {
                    Debug.LogError($"[LEADERBOARD] 리더보드 점수 로드 중 에러: {e.Message}");
                }
                
                // 전체 리더보드 목록 표시 (클래식 점수, 최고 스테이지 모두 확인 가능)
                Debug.Log("[LEADERBOARD] 전체 리더보드 표시 시작...");
                GPGSLoginManager.instance.ShowAllLeaderboards();
                Debug.Log("[LEADERBOARD] 전체 리더보드 표시 요청 완료");
            }
            else
            {
                Debug.LogWarning("[LEADERBOARD] GPGSLoginManager가 없습니다. 리더보드를 표시할 수 없습니다.");
            }
            
            Debug.Log("=== [LEADERBOARD] 리더보드 버튼 클릭 완료 ===");
        }

        public void OnAnimationEnd(){
            OnAnimationEnded?.Invoke();
        }
    }
}