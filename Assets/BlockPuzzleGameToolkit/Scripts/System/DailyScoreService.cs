using System;
using UnityEngine;
using BlockPuzzleGameToolkit.Scripts.Enums;

namespace BlockPuzzleGameToolkit.Scripts.System
{
    /// <summary>
    /// 일간/주간 최고 점수를 관리하는 서비스
    /// </summary>
    public static class DailyScoreService
    {
        private const string DAILY_SCORE_KEY_PREFIX = "DailyScore_";
        private const string DAILY_DATE_KEY_PREFIX = "DailyDate_";
        private const string WEEKLY_SCORE_KEY_PREFIX = "WeeklyScore_";
        private const string WEEKLY_WEEK_KEY_PREFIX = "WeeklyWeek_";
        
        /// <summary>
        /// 오늘의 최고 점수를 가져옵니다
        /// </summary>
        /// <param name="gameMode">게임 모드</param>
        /// <returns>오늘의 최고 점수</returns>
        public static int GetTodayBest(EGameMode gameMode)
        {
            string dateKey = GetDateKey(gameMode);
            string scoreKey = GetScoreKey(gameMode);
            
            // 오늘 날짜 확인
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string storedDate = PlayerPrefs.GetString(dateKey, "");
            
            Debug.Log($"[DailyScoreService] ===== 오늘의 최고 점수 조회 =====");
            Debug.Log($"[DailyScoreService] 게임 모드: {gameMode}");
            Debug.Log($"[DailyScoreService] 오늘 날짜: {today}");
            Debug.Log($"[DailyScoreService] 저장된 날짜: {storedDate}");
            
            // 날짜가 다르면 오늘의 점수를 0으로 리셋
            if (storedDate != today)
            {
                PlayerPrefs.SetString(dateKey, today);
                PlayerPrefs.SetInt(scoreKey, 0);
                PlayerPrefs.Save();
                Debug.Log($"[DailyScoreService] 날짜 변경으로 오늘의 점수 리셋: 0");
                return 0;
            }
            
            int todayBest = PlayerPrefs.GetInt(scoreKey, 0);
            Debug.Log($"[DailyScoreService] 오늘의 최고 점수: {todayBest}");
            return todayBest;
        }
        
        /// <summary>
        /// 오늘의 최고 점수를 업데이트합니다
        /// </summary>
        /// <param name="gameMode">게임 모드</param>
        /// <param name="score">새로운 점수</param>
        /// <returns>업데이트 성공 여부</returns>
        public static bool TryUpdateTodayBest(EGameMode gameMode, int score)
        {
            int currentBest = GetTodayBest(gameMode);
            
            if (score > currentBest)
            {
                string scoreKey = GetScoreKey(gameMode);
                PlayerPrefs.SetInt(scoreKey, score);
                PlayerPrefs.Save();
                
                Debug.Log($"[DailyScoreService] 오늘의 최고 점수 업데이트: {currentBest} -> {score} (모드: {gameMode})");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 오늘의 최고 점수를 리셋합니다
        /// </summary>
        /// <param name="gameMode">게임 모드</param>
        public static void ResetTodayBest(EGameMode gameMode)
        {
            string dateKey = GetDateKey(gameMode);
            string scoreKey = GetScoreKey(gameMode);
            
            PlayerPrefs.SetString(dateKey, "");
            PlayerPrefs.SetInt(scoreKey, 0);
            PlayerPrefs.Save();
            
            Debug.Log($"[DailyScoreService] 오늘의 최고 점수 리셋 (모드: {gameMode})");
        }
        
        /// <summary>
        /// 오늘의 최고 점수가 있는지 확인합니다
        /// </summary>
        /// <param name="gameMode">게임 모드</param>
        /// <returns>오늘의 최고 점수 존재 여부</returns>
        public static bool HasTodayBest(EGameMode gameMode)
        {
            return GetTodayBest(gameMode) > 0;
        }
        
        /// <summary>
        /// 이번주의 최고 점수를 가져옵니다
        /// </summary>
        /// <param name="gameMode">게임 모드</param>
        /// <returns>이번주의 최고 점수</returns>
        public static int GetThisWeekBest(EGameMode gameMode)
        {
            string weekKey = GetWeekKey(gameMode);
            string scoreKey = GetWeeklyScoreKey(gameMode);
            
            // 이번주 확인 (월요일 시작)
            string thisWeek = GetWeekString(DateTime.Now);
            string storedWeek = PlayerPrefs.GetString(weekKey, "");
            
            Debug.Log($"[DailyScoreService] ===== 이번주의 최고 점수 조회 =====");
            Debug.Log($"[DailyScoreService] 게임 모드: {gameMode}");
            Debug.Log($"[DailyScoreService] 이번주: {thisWeek}");
            Debug.Log($"[DailyScoreService] 저장된 주: {storedWeek}");
            
            // 주가 다르면 이번주의 점수를 0으로 리셋
            if (storedWeek != thisWeek)
            {
                PlayerPrefs.SetString(weekKey, thisWeek);
                PlayerPrefs.SetInt(scoreKey, 0);
                PlayerPrefs.Save();
                Debug.Log($"[DailyScoreService] 주 변경으로 이번주의 점수 리셋: 0");
                return 0;
            }
            
            int weekBest = PlayerPrefs.GetInt(scoreKey, 0);
            Debug.Log($"[DailyScoreService] 이번주의 최고 점수: {weekBest}");
            return weekBest;
        }
        
        /// <summary>
        /// 이번주의 최고 점수를 업데이트합니다
        /// </summary>
        /// <param name="gameMode">게임 모드</param>
        /// <param name="score">새로운 점수</param>
        /// <returns>업데이트 성공 여부</returns>
        public static bool TryUpdateThisWeekBest(EGameMode gameMode, int score)
        {
            int currentBest = GetThisWeekBest(gameMode);
            
            if (score > currentBest)
            {
                string scoreKey = GetWeeklyScoreKey(gameMode);
                PlayerPrefs.SetInt(scoreKey, score);
                PlayerPrefs.Save();
                
                Debug.Log($"[DailyScoreService] 이번주의 최고 점수 업데이트: {currentBest} -> {score} (모드: {gameMode})");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 이번주의 최고 점수를 리셋합니다
        /// </summary>
        /// <param name="gameMode">게임 모드</param>
        public static void ResetThisWeekBest(EGameMode gameMode)
        {
            string weekKey = GetWeekKey(gameMode);
            string scoreKey = GetWeeklyScoreKey(gameMode);
            
            PlayerPrefs.SetString(weekKey, "");
            PlayerPrefs.SetInt(scoreKey, 0);
            PlayerPrefs.Save();
            
            Debug.Log($"[DailyScoreService] 이번주의 최고 점수 리셋 (모드: {gameMode})");
        }
        
        /// <summary>
        /// 이번주의 최고 점수가 있는지 확인합니다
        /// </summary>
        /// <param name="gameMode">게임 모드</param>
        /// <returns>이번주의 최고 점수 존재 여부</returns>
        public static bool HasThisWeekBest(EGameMode gameMode)
        {
            return GetThisWeekBest(gameMode) > 0;
        }
        
        private static string GetDateKey(EGameMode gameMode)
        {
            return DAILY_DATE_KEY_PREFIX + gameMode.ToString();
        }
        
        private static string GetScoreKey(EGameMode gameMode)
        {
            return DAILY_SCORE_KEY_PREFIX + gameMode.ToString();
        }
        
        private static string GetWeekKey(EGameMode gameMode)
        {
            return WEEKLY_WEEK_KEY_PREFIX + gameMode.ToString();
        }
        
        private static string GetWeeklyScoreKey(EGameMode gameMode)
        {
            return WEEKLY_SCORE_KEY_PREFIX + gameMode.ToString();
        }
        
        /// <summary>
        /// 현재 날짜의 주차 문자열을 반환합니다 (월요일 시작)
        /// </summary>
        /// <param name="date">날짜</param>
        /// <returns>주차 문자열 (예: "2024-W01")</returns>
        private static string GetWeekString(DateTime date)
        {
            // 간단한 주차 계산 (월요일 시작)
            // 1월 1일을 기준으로 한 주차 계산
            var jan1 = new DateTime(date.Year, 1, 1);
            var daysOffset = (int)jan1.DayOfWeek - 1; // 월요일을 0으로 만들기
            if (daysOffset < 0) daysOffset += 7;
            
            var firstMonday = jan1.AddDays(-daysOffset);
            var weekNumber = ((date - firstMonday).Days / 7) + 1;
            
            return $"{date.Year}-W{weekNumber:D2}";
        }
    }
}
