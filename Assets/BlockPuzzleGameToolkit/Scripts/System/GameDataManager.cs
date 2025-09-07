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
using BlockPuzzleGameToolkit.Scripts.Data;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.LevelsData;
using UnityEditor;
using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.System
{
    public static class GameDataManager
    {
        public static int LevelNum;

        private static Level _level;

        public static bool isTestPlay = false;

        public static void ClearPlayerProgress()
        {
            PlayerPrefs.DeleteKey("Level");
            PlayerPrefs.Save();
        }

        public static void ClearALlData()
        {
            #if UNITY_EDITOR
            // clear variables ResourceObject from Resources/Variables
            var resourceObjects = Resources.LoadAll<ResourceObject>("Variables");
            foreach (var resourceObject in resourceObjects)
            {
                resourceObject.Set(0);
            }

            AssetDatabase.SaveAssets();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            #endif
        }

        public static void UnlockLevel(int currentLevel)
        {
            int savedLevel = PlayerPrefs.GetInt("Level", 1);
            if (savedLevel < currentLevel)
            {
                LevelNum = currentLevel;
                PlayerPrefs.SetInt("Level", currentLevel);
                PlayerPrefs.Save();
            }
        }

        public static int GetLevelNum()
        {
            return PlayerPrefs.GetInt("Level", 1);
        }

        public static Level GetLevel()
        {
            if (_level != null && isTestPlay)
            {
                return _level;
            }

            if (_level != null)
            {
                return _level;
            }

            _level = GetGameMode() == EGameMode.Classic ? Resources.Load<Level>("Misc/ClassicLevel") : Resources.Load<Level>("Levels/Level_" + GetLevelNum());
            return _level;
        }

        public static void SetLevel(Level level)
        {
            _level = level;
        }

                public static EGameMode GetGameMode()
        {
            if (PlayerPrefs.HasKey("GameMode"))
            {
                var mode = (EGameMode)PlayerPrefs.GetInt("GameMode");
                Debug.Log($"[GameDataManager] GetGameMode: {mode} (from PlayerPrefs)");
                return mode;
            }
            else
            {
                Debug.Log("[GameDataManager] GetGameMode: Classic (default - no PlayerPrefs)");
                return EGameMode.Classic;
            }
        }

        public static void SetGameMode(EGameMode gameMode)
        {
            PlayerPrefs.SetInt("GameMode", (int)gameMode);
            PlayerPrefs.Save();
            // 모드 전환 시 레벨 캐시를 무효화하여 잘못된 레벨 데이터가 남지 않도록 함
            _level = null;
            Debug.Log($"[GameDataManager] SetGameMode: {gameMode} (saved to PlayerPrefs, level cache cleared)");
        }

        public static void SetPostTutorialGameMode(EGameMode gameMode)
        {
            PlayerPrefs.SetInt("PostTutorialGameMode", (int)gameMode);
            PlayerPrefs.Save();
        }

        public static EGameMode GetPostTutorialGameMode()
        {
            if (PlayerPrefs.HasKey("PostTutorialGameMode"))
            {
                return (EGameMode)PlayerPrefs.GetInt("PostTutorialGameMode");
            }
            else
            {
                // 신규 유저의 경우 기본값으로 Classic 모드 반환
                return EGameMode.Classic;
            }
        }

        public static void SetAllLevelsCompleted()
        {
            var levels = Resources.LoadAll<Level>("Levels").Length;
            PlayerPrefs.SetInt("Level", levels);
            PlayerPrefs.Save();
        }

        internal static bool HasMoreLevels()
        {
            int currentLevel = GetLevelNum();
            int totalLevels = Resources.LoadAll<Level>("Levels").Length;
            return currentLevel < totalLevels;
        }

        public static void SetLevelNum(int stateCurrentLevel)
        {
            LevelNum = stateCurrentLevel;
        }
    }
}