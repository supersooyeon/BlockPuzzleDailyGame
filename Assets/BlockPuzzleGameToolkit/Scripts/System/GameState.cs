using System;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay;
using BlockPuzzleGameToolkit.Scripts.LevelsData;
using UnityEngine;

namespace BlockPuzzleGameToolkit.Scripts.System
{
    [Serializable]
    public abstract class GameState
    {
        public EGameState gameStatus;
        public int currentLevel;
        public EGameMode gameMode;
        public int score;
        public LevelRow[] levelRows;
        public DateTime quitTime;
        public int bestScore;

        public static void Save(GameState state, FieldManager field)
        {
            if (state == null) 
            {
                Debug.LogWarning("[GameState] Cannot save null state");
                return;
            }
            
            Debug.Log($"[GameState] Save 시작 - GameMode: {state.gameMode}, levelRows: {(state.levelRows != null ? "not null" : "null")}, FieldManager: {(field != null ? "not null" : "null")}");
            
            // state.levelRows가 이미 null로 설정되어 있으면 필드 상태를 저장하지 않음 (리워드 광고 후 등)
            if (state.levelRows == null)
            {
                Debug.Log("[GameState] levelRows가 null로 설정됨 - 필드 상태 저장 건너뜀 (리워드 광고 후 등)");
            }
            else if (field != null)
            {
                var cells = field.GetAllCells();
                if (cells != null && cells.GetLength(0) > 0 && cells.GetLength(1) > 0)
                {
                    state.levelRows = new LevelRow[cells.GetLength(0)];
                    
                    for (var i = 0; i < cells.GetLength(0); i++)
                    {
                        state.levelRows[i] = new LevelRow(cells.GetLength(1));
                        for (var j = 0; j < cells.GetLength(1); j++) 
                        {
                            if (cells[i, j] != null && cells[i, j].item != null && !cells[i, j].IsEmpty())
                            {
                                state.levelRows[i].cells[j] = cells[i, j].item?.itemTemplate;
                                state.levelRows[i].bonusItems[j] = cells[i, j].HasBonusItem();
                                state.levelRows[i].disabled[j] = cells[i, j].IsDisabled();
                            }
                        }
                    }
                    Debug.Log($"[GameState] Field state saved - {cells.GetLength(0)}x{cells.GetLength(1)} cells");
                }
                else
                {
                    Debug.LogWarning("[GameState] Field cells are null or empty - not saving field state");
                    state.levelRows = null;
                }
            }
            else
            {
                Debug.LogWarning("[GameState] FieldManager is null - not saving field state");
                state.levelRows = null;
            }
            
            state.quitTime = DateTime.Now;
            
            // bestScore는 각 모드의 권위 소스(Resource)와 동기화된 값만 저장
            switch (state.gameMode)
            {
                case EGameMode.Classic:
                    state.bestScore = HighScoreService.GetBest(EGameMode.Classic);
                    break;
                case EGameMode.Timed:
                    state.bestScore = HighScoreService.GetBest(EGameMode.Timed);
                    break;
            }

            var json = JsonUtility.ToJson(state);
            string key = "GameState_" + state.gameMode;
            
            if (state is ClassicGameState classicState)
            {
                Debug.Log($"[GameState] ClassicState 저장 - Score: {state.score}, hasUsedReward: {classicState.hasUsedReward}, scoreBeforeReward: {classicState.scoreBeforeReward}, hasUsedHighScoreBonus: {classicState.hasUsedHighScoreBonus}");
            }
            
            PlayerPrefs.SetString(key, json);
            
            // Also save the current game mode
            PlayerPrefs.SetString("LastPlayedMode", state.gameMode.ToString());
            PlayerPrefs.Save();
            
            Debug.Log($"[GameState] Game state saved successfully for {state.gameMode}");
        }

        public static GameState Load(EGameMode gameMode)
        {
            string key = "GameState_" + gameMode;
            
            if (PlayerPrefs.HasKey(key))
            {
                var json = PlayerPrefs.GetString(key);
                GameState state = null;
                
                try
                {
                    switch (gameMode)
                    {
                        case EGameMode.Classic:
                            state = JsonUtility.FromJson<ClassicGameState>(json);
                            var classicState = state as ClassicGameState;
                            if (classicState != null)
                            {
                                Debug.Log($"[GameState] ClassicState 로드 - hasUsedReward: {classicState.hasUsedReward}, scoreBeforeReward: {classicState.scoreBeforeReward}");
                            }
                            break;
                        case EGameMode.Timed:
                            state = JsonUtility.FromJson<TimedGameState>(json);
                            break;
                    }
                    
                    // Validate loaded state
                    if (state != null && state.gameMode != gameMode)
                    {
                        Debug.LogWarning($"[GameState] 게임 모드 불일치 - 저장된: {state.gameMode}, 요청된: {gameMode}");
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading game state: {e.Message}");
                    return null;
                }
                
                return state;
            }
            return null;
        }

        public static GameState Load()
        {
            // Legacy loading for backward compatibility
            if (PlayerPrefs.HasKey("GameState"))
            {
                var json = PlayerPrefs.GetString("GameState");
                var tempState = JsonUtility.FromJson<LegacyGameState>(json);
                
                // Convert to appropriate state based on gameMode
                switch (tempState.gameMode)
                {
                    case EGameMode.Classic:
                        var classicState = new ClassicGameState();
                        CopyBaseProperties(tempState, classicState);
                        return classicState;
                    case EGameMode.Timed:
                        var timedState = new TimedGameState();
                        CopyBaseProperties(tempState, timedState);
                        timedState.remainingTime = tempState.remainingTime;
                        return timedState;
                    default:
                        return null;
                }
            }
            return null;
        }

        private static void CopyBaseProperties(LegacyGameState source, GameState target)
        {
            target.gameStatus = source.gameStatus;
            target.currentLevel = source.currentLevel;
            target.gameMode = source.gameMode;
            target.score = source.score;
            target.levelRows = source.levelRows;
            target.quitTime = source.quitTime;
            target.bestScore = source.bestScore;
            
            // ClassicGameState의 특별한 필드들도 복사
            if (target is ClassicGameState classicTarget)
            {
                // LegacyGameState에는 이 필드들이 없으므로 기본값 유지
                // hasUsedReward = false, hasUsedHighScoreBonus = false로 유지
                // highScoreAtStart는 현재 최고 점수로 설정
                classicTarget.highScoreAtStart = HighScoreService.GetBest(EGameMode.Classic);
                Debug.Log($"[GameState] LegacyGameState → ClassicGameState 변환 - highScoreAtStart: {classicTarget.highScoreAtStart}");
            }
        }

        public static void Delete(EGameMode gameMode)
        {
            PlayerPrefs.DeleteKey("GameState_" + gameMode);
            PlayerPrefs.Save();
        }

        public static void Delete()
        {
            // Delete legacy key
            PlayerPrefs.DeleteKey("GameState");
            
            // Delete all game mode specific keys
            foreach (EGameMode mode in Enum.GetValues(typeof(EGameMode)))
            {
                PlayerPrefs.DeleteKey("GameState_" + mode);
            }
            
            PlayerPrefs.Save();
        }
    }

    [Serializable]
    public class ClassicGameState : GameState
    {
        public int level;
        public bool hasUsedReward = false; // 현재 게임에서 리워드를 사용했는지 추적
        public int scoreBeforeReward = 0; // 리워드 사용 직전의 점수
        public bool hasUsedHighScoreBonus = false; // 새로운 최고 점수로 한 번 더 PreFailed를 사용했는지 추적
        public int highScoreAtStart = 0; // 게임 시작 시의 최고 점수

        public ClassicGameState()
        {
            gameMode = EGameMode.Classic;
        }
    }

    [Serializable]
    public class TimedGameState : GameState
    {
        public float remainingTime;

        public TimedGameState()
        {
            gameMode = EGameMode.Timed;
            remainingTime = 180f; // Default duration if not set
            score = 0;
            bestScore = 0;
        }

        public void SetBestScore(int newScore)
        {
            if (remainingTime <= 0 && newScore > bestScore)
            {
                bestScore = newScore;
            }
        }
    }

    [Serializable]
    public class LegacyGameState
    {
        // For backwards compatibility when loading old saved states
        public EGameState gameStatus;
        public int currentLevel;
        public EGameMode gameMode;
        public int score;
        public int remainingTime;
        public LevelRow[] levelRows;
        public DateTime quitTime;
        public int bestScore;
    }
}
