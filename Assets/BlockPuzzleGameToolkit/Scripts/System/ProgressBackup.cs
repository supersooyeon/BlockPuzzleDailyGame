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
using System.IO;
using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.System
{
    [Serializable]
    internal class ProgressSnapshot
    {
        public string classicStateJson;
        public string timedStateJson;
        public int levelNum;
        public int gameMode;
        public string lastPlayedMode;
        public int postTutorialGameMode;
    }

    internal static class ProgressBackup
    {
        private static string SnapshotPath => Path.Combine(Application.persistentDataPath, "progress_backup.json");

        public static void SaveSnapshot()
        {
            try
            {
                var snapshot = new ProgressSnapshot
                {
                    classicStateJson = PlayerPrefs.GetString("GameState_Classic", string.Empty),
                    timedStateJson = PlayerPrefs.GetString("GameState_Timed", string.Empty),
                    levelNum = PlayerPrefs.GetInt("Level", 1),
                    gameMode = PlayerPrefs.GetInt("GameMode", 0),
                    lastPlayedMode = PlayerPrefs.GetString("LastPlayedMode", string.Empty),
                    postTutorialGameMode = PlayerPrefs.GetInt("PostTutorialGameMode", 0)
                };

                var json = JsonUtility.ToJson(snapshot);
                File.WriteAllText(SnapshotPath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProgressBackup] SaveSnapshot 실패: {e.Message}");
            }
        }

        public static void LoadSnapshotIfMissing()
        {
            try
            {
                if (!File.Exists(SnapshotPath)) return;

                var json = File.ReadAllText(SnapshotPath);
                var snapshot = JsonUtility.FromJson<ProgressSnapshot>(json);
                if (snapshot == null) return;

                // 필요한 키가 없을 때만 복원
                if (!PlayerPrefs.HasKey("GameState_Classic") && !string.IsNullOrEmpty(snapshot.classicStateJson))
                {
                    PlayerPrefs.SetString("GameState_Classic", snapshot.classicStateJson);
                }
                if (!PlayerPrefs.HasKey("GameState_Timed") && !string.IsNullOrEmpty(snapshot.timedStateJson))
                {
                    PlayerPrefs.SetString("GameState_Timed", snapshot.timedStateJson);
                }
                if (!PlayerPrefs.HasKey("Level"))
                {
                    PlayerPrefs.SetInt("Level", snapshot.levelNum);
                }
                if (!PlayerPrefs.HasKey("GameMode"))
                {
                    PlayerPrefs.SetInt("GameMode", snapshot.gameMode);
                }
                if (!PlayerPrefs.HasKey("LastPlayedMode") && !string.IsNullOrEmpty(snapshot.lastPlayedMode))
                {
                    PlayerPrefs.SetString("LastPlayedMode", snapshot.lastPlayedMode);
                }
                if (!PlayerPrefs.HasKey("PostTutorialGameMode"))
                {
                    PlayerPrefs.SetInt("PostTutorialGameMode", snapshot.postTutorialGameMode);
                }

                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProgressBackup] LoadSnapshotIfMissing 실패: {e.Message}");
            }
        }
    }
}


