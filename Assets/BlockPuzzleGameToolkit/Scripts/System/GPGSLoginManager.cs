using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using System;
using BlockPuzzleGameToolkit.Scripts.Data;
using BlockPuzzleGameToolkit.Scripts.Enums;

namespace BlockPuzzleGameToolkit.Scripts.System
{
    
    /// <summary>
    /// Google Play Games Services 로그인 관리자
    /// GPGS 11.01 버전을 사용하여 실제 Google Play Games 로그인 제공
    /// </summary>
    public class GPGSLoginManager : SingletonBehaviour<GPGSLoginManager>
    {
        [Header("GPGS 설정")]
        [Tooltip("⚠️ 이 설정은 무시됩니다. Privacy Terms 동의 여부만으로 로그인이 제어됩니다.")]
        public bool autoLogin = false; // Privacy Terms 동의 여부만으로 제어됨 (이 필드는 무시됨)
        
        [Header("리더보드 설정")]
        public string leaderboardId = "CgkIlqWo1e0GEAIQAw";
        [Tooltip("최고 스테이지(진행도) 리더보드 ID")]
        public string leaderboardIdHighestStage = "";
        
        [Header("디버그")]
        public bool enableDebugLog = true;
        
        // 이벤트 델리게이트
        public static event Action<bool> OnLoginStatusChanged;
        public static event Action<string> OnLoginError;
        
        // 로그인 상태
        public bool IsAuthenticated 
        { 
            get 
            {
                // PlayGamesPlatform 인증 상태 우선 확인
                bool playGamesAuth = false;
                try
                {
                    if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.localUser != null)
                    {
                        playGamesAuth = PlayGamesPlatform.Instance.localUser.authenticated;
                    }
                }
                catch (global::System.Exception e)
                {
                    DebugLog($"[LEADERBOARD] PlayGamesPlatform 인증 상태 확인 중 예외: {e.Message}");
                }
                
                // Social API 인증 상태 확인
                bool socialAuth = Social.localUser.authenticated;
                
                // 둘 중 하나라도 true면 인증된 것으로 간주
                bool actualAuth = playGamesAuth || socialAuth;
                
                if (actualAuth != _isAuthenticated)
                {
                    _isAuthenticated = actualAuth;
                    DebugLog($"[LEADERBOARD] 인증 상태 변경: {_isAuthenticated} (PlayGames: {playGamesAuth}, Social: {socialAuth})");
                }
                return _isAuthenticated;
            }
            private set => _isAuthenticated = value;
        }
        
        private bool _isAuthenticated = false;
        
        public override void Awake()
        {
            // autoLogin 설정 강제 비활성화
            autoLogin = false;
            
            base.Awake();
            
            // 싱글톤 인스턴스가 성공적으로 생성되었을 때만 초기화
            if (instance == this)
            {
                DontDestroyOnLoad(gameObject);
                InitializeGPGS();
            }
        }
        
        void Start()
        {
            // Privacy Terms 동의 여부 확인
            //bool privacyTermsAgreed = IsPrivacyTermsAgreed(); // 삭제
            
            // 현재 GPGS 인증 상태 확인
            bool currentlyAuthenticated = Social.localUser.authenticated;
            
            // GPGS 세션이 이미 유지 중이라면 SignOut 호출
            if (currentlyAuthenticated)
            {
                ForceSignOutOnAppStart();
            }
            
            // 게임 실행 시 자동 로그인 시도
            StartCoroutine(AutoLoginCoroutine());
        }
        
        /// <summary>
        /// Google Play Games Services 초기화 (GPGS 11.01 버전)
        /// </summary>
        void InitializeGPGS()
        {
            try
            {
                PlayGamesPlatform.DebugLogEnabled = enableDebugLog;
            }
            catch (global::System.Exception e)
            {
                DebugLog($"GPGS 초기화 실패: {e.Message}");
            }
        }
        
        /// <summary>
        /// 자동 로그인 시도 (GPGS 11.01 버전) - 현재 비활성화됨
        /// </summary>
        IEnumerator AutoLoginCoroutine()
        {
            DebugLog("=== 자동 로그인 코루틴 시작 ===");
            //DebugLog("🚫 자동 로그인이 비활성화되어 있습니다.");
            //DebugLog("모든 로그인은 Privacy Terms 동의 후 PrivacyTermsPopup에서만 처리됩니다.");
            
            // 자동 로그인 활성화 - Privacy Terms 동의 여부와 상관없이 로그인 시도
            if (!IsAuthenticated)
            {
                Login();
            }
            yield break;
        }
        
        /// <summary>
        /// 수동 로그인 시도 (GPGS 11.01 버전)
        /// </summary>
        public void Login()
        {
            DebugLog("=== 수동 로그인 시도 시작 ===");
            DebugLog($"현재 로그인 상태: {IsAuthenticated}");
            
            // 🔒 Privacy Terms 동의 여부 확인 (보안 검증)
            //bool privacyTermsAgreed = IsPrivacyTermsAgreed(); // 삭제
            //DebugLog($"Privacy Terms 동의 상태 확인: {privacyTermsAgreed}"); // 삭제
            
            //if (!privacyTermsAgreed)
            //{
            //    DebugLog("🚫 Privacy Terms 미동의 - 로그인 시도 차단");
            //    DebugLog("사용자는 먼저 Privacy Terms에 동의해야 합니다.");
            //    return;
            //}
            
            if (IsAuthenticated)
            {
                DebugLog("이미 로그인되어 있습니다.");
                return;
            }
            
            // PlayGamesPlatform 인스턴스 상태 확인
            if (PlayGamesPlatform.Instance == null)
            {
                DebugLog("에러: PlayGamesPlatform.Instance가 null입니다!");
                DebugLog("GPGS 초기화가 완료되지 않았을 수 있습니다.");
                return;
            }
            
            DebugLog("PlayGamesPlatform 인스턴스 확인 완료");
            DebugLog("Social.localUser 상태 확인...");
            DebugLog($"Social.localUser.authenticated: {Social.localUser.authenticated}");
            DebugLog($"Social.localUser.userName: {Social.localUser.userName}");
            DebugLog($"Social.localUser.id: {Social.localUser.id}");
            
            try
            {
                DebugLog("PlayGamesPlatform.Instance.Authenticate 호출...");
                
                // GPGS 11.01 버전 새로운 API 사용
                PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
                
                DebugLog("Authenticate 메서드 호출 완료 (콜백 대기 중)");
            }
            catch (global::System.Exception e)
            {
                DebugLog($"수동 로그인 중 예외 발생:");
                DebugLog($"에러 메시지: {e.Message}");
                DebugLog($"에러 타입: {e.GetType()}");
                DebugLog($"스택 트레이스: {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// GPGS 11.01 인증 결과 처리 콜백
        /// </summary>
        internal void ProcessAuthentication(SignInStatus status)
        {
            DebugLog($"=== 인증 결과 처리 시작 ===");
            DebugLog($"SignInStatus: {status}");
            
            bool success = (status == SignInStatus.Success);
            DebugLog($"인증 성공 여부: {success}");
            DebugLog($"이전 로그인 상태: {IsAuthenticated}");
            
            IsAuthenticated = success;
            DebugLog($"새로운 로그인 상태: {IsAuthenticated}");
            
            if (success)
            {
                DebugLog("=== 로그인 성공 처리 ===");
                
                // Social.localUser 정보 상세 확인
                DebugLog("Social.localUser 정보 확인 중...");
                DebugLog($"Social.localUser.authenticated: {Social.localUser.authenticated}");
                DebugLog($"Social.localUser.underage: {Social.localUser.underage}");
                DebugLog($"Social.localUser.state: {Social.localUser.state}");
                
                string userName = Social.localUser.userName;
                string userId = Social.localUser.id;
                
                DebugLog($"로그인 성공!");
                DebugLog($"사용자 이름: {(string.IsNullOrEmpty(userName) ? "없음" : userName)}");
                DebugLog($"사용자 ID: {(string.IsNullOrEmpty(userId) ? "없음" : userId)}");
                
                // PlayGamesPlatform 특화 정보 확인
                try
                {
                    if (PlayGamesPlatform.Instance != null)
                    {
                        DebugLog("PlayGamesPlatform 추가 정보 확인 중...");
                        var localUser = PlayGamesPlatform.Instance.localUser;
                        if (localUser != null)
                        {
                            DebugLog($"PlayGames 사용자 이름: {localUser.userName}");
                            DebugLog($"PlayGames 사용자 ID: {localUser.id}");
                        }
                    }
                }
                catch (global::System.Exception e)
                {
                    DebugLog($"PlayGamesPlatform 추가 정보 확인 중 예외: {e.Message}");
                }
                
                // 로그인 성공 시 저장된 최고 점수/최고 스테이지 제출
                SubmitBestScoreIfAvailable();
                SubmitHighestStageIfAvailable();
                
                DebugLog("OnLoginStatusChanged 이벤트 호출...");
                OnLoginStatusChanged?.Invoke(true);
                
                // Firebase Auth 연동 (GPGS만 사용)
                try
                {
                    DebugLog("FirebaseAuthManager를 통한 Firebase 로그인 시도...");
                    FirebaseAuthManager.instance?.SignInWithPlayGames();
                }
                catch (global::System.Exception e)
                {
                    DebugLog($"Firebase Auth 연동 중 예외: {e.Message}");
                }
                DebugLog("=== 로그인 성공 처리 완료 ===");
            }
            else
            {
                DebugLog("=== 로그인 실패 처리 ===");
                
                // 실패 원인 상세 분석
                DebugLog($"실패 상태 코드: {status}");
                DebugLog("실패 원인 분석 중...");
                DebugLog($"Social.localUser.authenticated: {Social.localUser.authenticated}");
                DebugLog($"PlayGamesPlatform.Instance null 여부: {PlayGamesPlatform.Instance == null}");
                
                // 네트워크 상태 확인
                DebugLog($"인터넷 연결 상태: {Application.internetReachability}");
                
                string errorMsg = $"로그인 실패 (상태: {status})";
                DebugLog(errorMsg);
                
                DebugLog("OnLoginError 이벤트 호출...");
                OnLoginError?.Invoke(errorMsg);
                
                DebugLog("OnLoginStatusChanged 이벤트 호출...");
                OnLoginStatusChanged?.Invoke(false);
                
                DebugLog("=== 로그인 실패 처리 완료 ===");
            }
        }
        
        /// <summary>
        /// 로그아웃 (GPGS 11.01 버전)
        /// </summary>
        public void Logout()
        {
            if (!IsAuthenticated)
            {
                DebugLog("이미 로그아웃되어 있습니다.");
                return;
            }
            
            DebugLog("=== GPGS 로그아웃 시작 ===");
            
            try
            {
                // GPGS 11.01에서는 직접적인 SignOut이 제한적임
                // 로컬 상태만 변경하고 앱 재시작 시 인증 체크로 제어
                DebugLog("GPGS 11.01 - 로컬 로그아웃 상태로 변경");
                DebugLog("실제 시스템 로그아웃은 앱 재시작 시 Privacy Terms 체크로 제어됨");
            }
            catch (global::System.Exception e)
            {
                DebugLog($"GPGS 로그아웃 처리 중 예외 발생: {e.Message}");
            }
            
            // 로컬 상태 변경
            IsAuthenticated = false;
            
            // Firebase 로그아웃 병행
            try
            {
                DebugLog("Firebase 로그아웃 시도...");
                FirebaseAuthManager.instance?.SignOut();
            }
            catch (global::System.Exception e)
            {
                DebugLog($"Firebase 로그아웃 중 예외: {e.Message}");
            }

            DebugLog("=== GPGS 로그아웃 완료 ===");
            OnLoginStatusChanged?.Invoke(false);
        }
        
        /// <summary>
        /// 리더보드 UI 표시 (일간)
        /// </summary>
        public void ShowLeaderboard()
        {
            ShowLeaderboardInternal(LeaderboardTimeSpan.Daily);
        }
        
        /// <summary>
        /// 주간 리더보드 UI 표시
        /// </summary>
        public void ShowWeeklyLeaderboard()
        {
            ShowLeaderboardInternal(LeaderboardTimeSpan.Weekly);
        }
        
        /// <summary>
        /// 전체 기간 리더보드 UI 표시
        /// </summary>
        public void ShowAllTimeLeaderboard()
        {
            ShowLeaderboardInternal(LeaderboardTimeSpan.AllTime);
        }
        
        /// <summary>
        /// 리더보드 UI 표시 (내부 메서드)
        /// </summary>
        private void ShowLeaderboardInternal(LeaderboardTimeSpan timeSpan)
        {
            string periodType = timeSpan == LeaderboardTimeSpan.Daily ? "일간" : 
                               timeSpan == LeaderboardTimeSpan.Weekly ? "주간" : "전체 기간";
            DebugLog($"=== [LEADERBOARD] {periodType} 리더보드 표시 시작 ===");
            DebugLog($"[LEADERBOARD] ===== {periodType} 리더보드 표시 상세 =====");
            DebugLog($"[LEADERBOARD] 요청된 기간: {timeSpan}");
            DebugLog($"[LEADERBOARD] 기간 유형: {periodType}");
            DebugLog($"[LEADERBOARD] 현재 인증 상태: {IsAuthenticated}");
            DebugLog($"[LEADERBOARD] Social.localUser.authenticated: {Social.localUser.authenticated}");
            DebugLog($"[LEADERBOARD] 리더보드 ID: {leaderboardId}");
            
            // PlayGamesPlatform 인증 상태 확인
            bool playGamesAuth = false;
            try
            {
                if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.localUser != null)
                {
                    playGamesAuth = PlayGamesPlatform.Instance.localUser.authenticated;
                    DebugLog($"[LEADERBOARD] PlayGamesPlatform.localUser.authenticated: {playGamesAuth}");
                }
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] PlayGamesPlatform 인증 상태 확인 중 예외: {e.Message}");
            }
            
            // 실제 인증 상태 확인 (둘 중 하나라도 true면 인증된 것으로 간주)
            bool isActuallyAuthenticated = playGamesAuth || Social.localUser.authenticated;
            DebugLog($"[LEADERBOARD] 실제 인증 상태: {isActuallyAuthenticated}");
            
            if (!isActuallyAuthenticated)
            {
                DebugLog("[LEADERBOARD] 인증되지 않음 - 리더보드 표시 중단");
                DebugLog("=== [LEADERBOARD] 리더보드 표시 실패 (인증 안됨) ===");
                return;
            }
            
            DebugLog($"[LEADERBOARD] {periodType} 리더보드 표시 시도: {leaderboardId}");
            
            try
            {
                // GPGS 11.01 리더보드 표시
                DebugLog($"[LEADERBOARD] PlayGamesPlatform.Instance.ShowLeaderboardUI 호출 중... ({periodType} 기간)");
                PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId, timeSpan, null);
                DebugLog($"[LEADERBOARD] {periodType} 리더보드 표시 성공");
                DebugLog($"=== [LEADERBOARD] {periodType} 리더보드 표시 완료 (성공) ===");
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] {periodType} 리더보드 표시 실패: {e.Message}");
                DebugLog($"[LEADERBOARD] 예외 타입: {e.GetType()}");
                DebugLog($"[LEADERBOARD] 스택 트레이스: {e.StackTrace}");
                DebugLog($"=== [LEADERBOARD] {periodType} 리더보드 표시 완료 (실패) ===");
            }
        }
        
        /// <summary>
        /// 모든 리더보드 목록 UI 표시
        /// </summary>
        public void ShowAllLeaderboards()
        {
            DebugLog("=== [LEADERBOARD] 전체 리더보드 UI 표시 시작 ===");
            
            bool playGamesAuth = false;
            try
            {
                if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.localUser != null)
                {
                    playGamesAuth = PlayGamesPlatform.Instance.localUser.authenticated;
                }
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] 인증 확인 중 예외: {e.Message}");
            }
            bool isActuallyAuthenticated = playGamesAuth || Social.localUser.authenticated;
            if (!isActuallyAuthenticated)
            {
                DebugLog("[LEADERBOARD] 인증되지 않음 - 전체 리더보드 표시 중단");
                return;
            }
            try
            {
                PlayGamesPlatform.Instance.ShowLeaderboardUI();
                DebugLog("[LEADERBOARD] 전체 리더보드 UI 표시 성공");
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] 전체 리더보드 표시 실패: {e.Message}");
            }
        }
        

        
        /// <summary>
        /// 점수 제출 (리더보드 새로고침 옵션 포함)
        /// </summary>
        public void SubmitScore(long score, bool suppressRefresh)
        {
            SubmitScoreInternal(score, suppressRefresh, false);
        }

        /// <summary>
        /// 점수 제출 (기본: 새로고침 수행)
        /// </summary>
        public void SubmitScore(long score)
        {
            SubmitScoreInternal(score, false, false);
        }
        
        /// <summary>
        /// 주간 점수 제출 (리더보드 새로고침 옵션 포함)
        /// </summary>
        public void SubmitWeeklyScore(long score, bool suppressRefresh)
        {
            SubmitScoreInternal(score, suppressRefresh, true);
        }

        /// <summary>
        /// 주간 점수 제출 (기본: 새로고침 수행)
        /// </summary>
        public void SubmitWeeklyScore(long score)
        {
            SubmitScoreInternal(score, false, true);
        }
        
        /// <summary>
        /// 전체 기간 점수 제출 (리더보드 새로고침 옵션 포함)
        /// </summary>
        public void SubmitAllTimeScore(long score, bool suppressRefresh)
        {
            SubmitAllTimeScoreInternal(score, suppressRefresh);
        }

        /// <summary>
        /// 전체 기간 점수 제출 (기본: 새로고침 수행)
        /// </summary>
        public void SubmitAllTimeScore(long score)
        {
            SubmitAllTimeScoreInternal(score, false);
        }

        private void SubmitAllTimeScoreInternal(long score, bool suppressRefresh)
        {
            DebugLog($"=== [LEADERBOARD] 전체 기간 점수 제출 시작 ===");
            DebugLog($"[LEADERBOARD] ===== 전체 기간 리더보드 점수 제출 상세 =====");
            DebugLog($"[LEADERBOARD] 입력된 점수: {score}");
            DebugLog($"[LEADERBOARD] 새로고침 억제: {suppressRefresh}");
            DebugLog($"[LEADERBOARD] 현재 인증 상태: {IsAuthenticated}");
            DebugLog($"[LEADERBOARD] Social.localUser.authenticated: {Social.localUser.authenticated}");
            DebugLog($"[LEADERBOARD] Social.Active null: {Social.Active == null}");
            
            // PlayGamesPlatform 인증 상태 확인
            bool playGamesAuth = false;
            try
            {
                if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.localUser != null)
                {
                    playGamesAuth = PlayGamesPlatform.Instance.localUser.authenticated;
                    DebugLog($"[LEADERBOARD] PlayGamesPlatform.localUser.authenticated: {playGamesAuth}");
                }
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] PlayGamesPlatform 인증 상태 확인 중 예외: {e.Message}");
            }
            
            // 실제 인증 상태 확인 (둘 중 하나라도 true면 인증된 것으로 간주)
            bool isActuallyAuthenticated = playGamesAuth || Social.localUser.authenticated;
            DebugLog($"[LEADERBOARD] 실제 인증 상태: {isActuallyAuthenticated}");
            
            if (!isActuallyAuthenticated)
            {
                DebugLog("[LEADERBOARD] 인증되지 않음 - 점수 제출 중단");
                DebugLog("=== [LEADERBOARD] 점수 제출 완료 (인증 안됨) ===");
                return;
            }
            
            // 전체 기간 최고 점수 확인 및 제출
            int allTimeBest = HighScoreService.GetBest(EGameMode.Classic);
            bool isNewAllTimeBest = HighScoreService.TryUpdateBest(EGameMode.Classic, (int)score);
            
            DebugLog($"[LEADERBOARD] ===== 전체 기간 점수 비교 =====");
            DebugLog($"[LEADERBOARD] 기존 전체 기간 최고 점수: {allTimeBest}");
            DebugLog($"[LEADERBOARD] 입력된 점수: {score}");
            DebugLog($"[LEADERBOARD] 새로운 최고 점수 여부: {isNewAllTimeBest}");
            DebugLog($"[LEADERBOARD] 최종 전체 기간 최고 점수: {(isNewAllTimeBest ? score : allTimeBest)}");
            
            // 전체 기간 최고 점수만 제출
            long scoreToSubmit = isNewAllTimeBest ? score : allTimeBest;
            DebugLog($"[LEADERBOARD] ===== 제출할 점수 결정 =====");
            DebugLog($"[LEADERBOARD] 제출할 점수 (전체 기간 최고): {scoreToSubmit}");
            DebugLog($"[LEADERBOARD] 리더보드 ID: {leaderboardId}");
            
            // PlayGamesPlatform을 직접 사용하여 점수 제출
            DebugLog($"[LEADERBOARD] 점수 제출 시도: {scoreToSubmit} (리더보드: {leaderboardId})");
            DebugLog($"[LEADERBOARD] PlayGamesPlatform.Instance.ReportScore 호출 전...");
            
            try
            {
                // PlayGamesPlatform 직접 사용
                PlayGamesPlatform.Instance.ReportScore(scoreToSubmit, leaderboardId, (bool success) => {
                    DebugLog($"[LEADERBOARD] 점수 제출 콜백 호출됨 - 성공: {success}");
                    if (success)
                    {
                        DebugLog($"[LEADERBOARD] 점수 제출 성공: {scoreToSubmit} (전체 기간 최고)");
                        DebugLog("=== [LEADERBOARD] 점수 제출 완료 (성공) ===");
                        
                        // 성공 후 선택적으로 리더보드 새로고침
                        if (!suppressRefresh)
                        {
                            StartCoroutine(RefreshLeaderboardAfterSubmit());
                        }
                    }
                    else
                    {
                        DebugLog($"[LEADERBOARD] 점수 제출 실패: {scoreToSubmit}");
                        DebugLog("=== [LEADERBOARD] 점수 제출 완료 (실패) ===");
                    }
                });
                DebugLog("[LEADERBOARD] PlayGamesPlatform.Instance.ReportScore 호출 완료 (콜백 대기 중)");
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] 점수 제출 중 예외 발생: {e.Message}");
                DebugLog($"[LEADERBOARD] 예외 타입: {e.GetType()}");
                DebugLog($"[LEADERBOARD] 스택 트레이스: {e.StackTrace}");
                DebugLog("=== [LEADERBOARD] 점수 제출 완료 (예외) ===");
            }
            
            DebugLog("=== [LEADERBOARD] 전체 기간 점수 제출 요청 완료 ===");
        }

        private void SubmitScoreInternal(long score, bool suppressRefresh, bool isWeekly)
        {
            string periodType = isWeekly ? "주간" : "일간";
            DebugLog($"=== [LEADERBOARD] {periodType} 점수 제출 시작 ===");
            DebugLog($"[LEADERBOARD] ===== {periodType} 리더보드 점수 제출 상세 =====");
            DebugLog($"[LEADERBOARD] 입력된 점수: {score}");
            DebugLog($"[LEADERBOARD] 새로고침 억제: {suppressRefresh}");
            DebugLog($"[LEADERBOARD] 기간 유형: {periodType}");
            DebugLog($"[LEADERBOARD] 현재 인증 상태: {IsAuthenticated}");
            DebugLog($"[LEADERBOARD] Social.localUser.authenticated: {Social.localUser.authenticated}");
            DebugLog($"[LEADERBOARD] Social.Active null: {Social.Active == null}");
            
            // PlayGamesPlatform 인증 상태 확인
            bool playGamesAuth = false;
            try
            {
                if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.localUser != null)
                {
                    playGamesAuth = PlayGamesPlatform.Instance.localUser.authenticated;
                    DebugLog($"[LEADERBOARD] PlayGamesPlatform.localUser.authenticated: {playGamesAuth}");
                }
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] PlayGamesPlatform 인증 상태 확인 중 예외: {e.Message}");
            }
            
            // 실제 인증 상태 확인 (둘 중 하나라도 true면 인증된 것으로 간주)
            bool isActuallyAuthenticated = playGamesAuth || Social.localUser.authenticated;
            DebugLog($"[LEADERBOARD] 실제 인증 상태: {isActuallyAuthenticated}");
            
            if (!isActuallyAuthenticated)
            {
                DebugLog("[LEADERBOARD] 인증되지 않음 - 점수 제출 중단");
                DebugLog("=== [LEADERBOARD] 점수 제출 완료 (인증 안됨) ===");
                return;
            }
            
            // 기간별 최고 점수 업데이트 및 제출
            int periodBest;
            bool isNewPeriodBest;
            
            if (isWeekly)
            {
                periodBest = DailyScoreService.GetThisWeekBest(EGameMode.Classic);
                isNewPeriodBest = DailyScoreService.TryUpdateThisWeekBest(EGameMode.Classic, (int)score);
                DebugLog($"[LEADERBOARD] ===== 주간 점수 비교 =====");
                DebugLog($"[LEADERBOARD] 기존 이번주 최고 점수: {periodBest}");
                DebugLog($"[LEADERBOARD] 입력된 점수: {score}");
                DebugLog($"[LEADERBOARD] 새로운 최고 점수 여부: {isNewPeriodBest}");
                DebugLog($"[LEADERBOARD] 최종 이번주 최고 점수: {(isNewPeriodBest ? score : periodBest)}");
            }
            else
            {
                periodBest = DailyScoreService.GetTodayBest(EGameMode.Classic);
                isNewPeriodBest = DailyScoreService.TryUpdateTodayBest(EGameMode.Classic, (int)score);
                DebugLog($"[LEADERBOARD] ===== 일간 점수 비교 =====");
                DebugLog($"[LEADERBOARD] 기존 오늘의 최고 점수: {periodBest}");
                DebugLog($"[LEADERBOARD] 입력된 점수: {score}");
                DebugLog($"[LEADERBOARD] 새로운 최고 점수 여부: {isNewPeriodBest}");
                DebugLog($"[LEADERBOARD] 최종 오늘의 최고 점수: {(isNewPeriodBest ? score : periodBest)}");
            }
            
            // 기간별 최고 점수만 제출
            long scoreToSubmit = isNewPeriodBest ? score : periodBest;
            DebugLog($"[LEADERBOARD] ===== 제출할 점수 결정 =====");
            DebugLog($"[LEADERBOARD] 제출할 점수 ({periodType} 최고): {scoreToSubmit}");
            DebugLog($"[LEADERBOARD] 리더보드 ID: {leaderboardId}");
            
            // PlayGamesPlatform을 직접 사용하여 점수 제출
            DebugLog($"[LEADERBOARD] 점수 제출 시도: {scoreToSubmit} (리더보드: {leaderboardId})");
            DebugLog($"[LEADERBOARD] PlayGamesPlatform.Instance.ReportScore 호출 전...");
            
            try
            {
                // PlayGamesPlatform 직접 사용
                PlayGamesPlatform.Instance.ReportScore(scoreToSubmit, leaderboardId, (bool success) => {
                    DebugLog($"[LEADERBOARD] 점수 제출 콜백 호출됨 - 성공: {success}");
                    if (success)
                    {
                        DebugLog($"[LEADERBOARD] 점수 제출 성공: {scoreToSubmit} ({periodType} 최고)");
                        DebugLog("=== [LEADERBOARD] 점수 제출 완료 (성공) ===");
                        
                        // 성공 후 선택적으로 리더보드 새로고침
                        if (!suppressRefresh)
                        {
                            StartCoroutine(RefreshLeaderboardAfterSubmit());
                        }
                    }
                    else
                    {
                        DebugLog($"[LEADERBOARD] 점수 제출 실패: {score}");
                        DebugLog("=== [LEADERBOARD] 점수 제출 완료 (실패) ===");
                        
                        // 실패 원인 분석
                        if (!isActuallyAuthenticated)
                        {
                            DebugLog("[LEADERBOARD] 실패 원인: 인증되지 않음");
                        }
                        else if (!Social.localUser.authenticated && !playGamesAuth)
                        {
                            DebugLog("[LEADERBOARD] 실패 원인: Social.localUser.authenticated와 PlayGamesPlatform 모두 false");
                        }
                        else
                        {
                            DebugLog("[LEADERBOARD] 실패 원인: 기타 (네트워크, 서버, 설정 등)");
                        }
                    }
                });
                DebugLog($"[LEADERBOARD] PlayGamesPlatform.Instance.ReportScore 호출 완료 (콜백 대기 중)");
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] 점수 제출 중 예외 발생: {e.Message}");
                DebugLog($"[LEADERBOARD] 예외 타입: {e.GetType()}");
                DebugLog($"[LEADERBOARD] 스택 트레이스: {e.StackTrace}");
                DebugLog("=== [LEADERBOARD] 점수 제출 완료 (예외 발생) ===");
            }
        }
        
        /// <summary>
        /// 점수 제출 후 리더보드 새로고침
        /// </summary>
        private IEnumerator RefreshLeaderboardAfterSubmit()
        {
            DebugLog("[LEADERBOARD] 점수 제출 후 리더보드 새로고침 대기 중...");
            yield return new WaitForSeconds(2f); // 2초 대기
            
            DebugLog("[LEADERBOARD] 리더보드 새로고침 시작...");
            LoadLeaderboardScores((scores) => {
                DebugLog($"[LEADERBOARD] 새로고침된 점수 개수: {scores.Length}");
                if (scores.Length > 0)
                {
                    DebugLog($"[LEADERBOARD] 새로고침된 최고 점수: {scores[0].value}");
                }
            });
        }
        
        /// <summary>
        /// 리더보드 점수 로드
        /// </summary>
        public void LoadLeaderboardScores(global::System.Action<IScore[]> onScoresLoaded = null)
        {
            DebugLog("=== [LEADERBOARD] 리더보드 점수 로드 시작 ===");
            DebugLog($"[LEADERBOARD] 인증 상태: {IsAuthenticated}");
            DebugLog($"[LEADERBOARD] Social.localUser.authenticated: {Social.localUser.authenticated}");
            
            // PlayGamesPlatform 인증 상태 확인
            bool playGamesAuth = false;
            try
            {
                if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.localUser != null)
                {
                    playGamesAuth = PlayGamesPlatform.Instance.localUser.authenticated;
                    DebugLog($"[LEADERBOARD] PlayGamesPlatform.localUser.authenticated: {playGamesAuth}");
                }
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] PlayGamesPlatform 인증 상태 확인 중 예외: {e.Message}");
            }
            
            // 실제 인증 상태 확인 (둘 중 하나라도 true면 인증된 것으로 간주)
            bool isActuallyAuthenticated = playGamesAuth || Social.localUser.authenticated;
            DebugLog($"[LEADERBOARD] 실제 인증 상태: {isActuallyAuthenticated}");
            
            if (!isActuallyAuthenticated)
            {
                DebugLog("[LEADERBOARD] 인증되지 않음 - 점수 로드 중단");
                onScoresLoaded?.Invoke(new IScore[0]);
                DebugLog("=== [LEADERBOARD] 리더보드 점수 로드 완료 (인증 안됨) ===");
                return;
            }
            
            DebugLog($"[LEADERBOARD] 리더보드 점수 로드 시도: {leaderboardId}");
            
            try
            {
                Social.LoadScores(leaderboardId, (IScore[] scores) => {
                    try
                    {
                        if (scores != null)
                        {
                            DebugLog($"[LEADERBOARD] 리더보드 점수 로드 성공: {scores.Length}개");
                            onScoresLoaded?.Invoke(scores);
                        }
                        else
                        {
                            DebugLog("[LEADERBOARD] 리더보드 점수 로드 실패: scores가 null");
                            onScoresLoaded?.Invoke(new IScore[0]);
                        }
                    }
                    catch (global::System.Exception e)
                    {
                        DebugLog($"[LEADERBOARD] 리더보드 점수 처리 중 예외: {e.Message}");
                        onScoresLoaded?.Invoke(new IScore[0]);
                    }
                });
                DebugLog("[LEADERBOARD] Social.LoadScores 호출 완료 (콜백 대기 중)");
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] 리더보드 점수 로드 중 예외 발생: {e.Message}");
                DebugLog($"[LEADERBOARD] 예외 타입: {e.GetType()}");
                onScoresLoaded?.Invoke(new IScore[0]);
            }
            
            DebugLog("=== [LEADERBOARD] 리더보드 점수 로드 요청 완료 ===");
        }
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        void DebugLog(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[GPGS 11.01] {message}");
            }
        }
        
        /// <summary>
        /// 사용자 정보 가져오기
        /// </summary>
        public string GetUserName()
        {
            if (!IsAuthenticated)
            {
                DebugLog("GetUserName: 인증되지 않음");
                return "Guest";
            }
            
            if (Social.localUser == null)
            {
                DebugLog("GetUserName: Social.localUser가 null");
                return "Guest";
            }
            
            string userName = Social.localUser.userName;
            DebugLog($"GetUserName: {userName}");
            return string.IsNullOrEmpty(userName) ? "Guest" : userName;
        }
        
        /// <summary>
        /// 사용자 ID 가져오기
        /// </summary>
        public string GetUserId()
        {
            if (!IsAuthenticated)
            {
                DebugLog("GetUserId: 인증되지 않음");
                return "";
            }
            
            if (Social.localUser == null)
            {
                DebugLog("GetUserId: Social.localUser가 null");
                return "";
            }
            
            string userId = Social.localUser.id;
            DebugLog($"GetUserId: {userId}");
            return string.IsNullOrEmpty(userId) ? "" : userId;
        }
        
        /// <summary>
        /// 로그인 상태 확인 (Unity Social API 사용)
        /// </summary>
        public bool CheckAuthenticationStatus()
        {
            bool currentStatus = Social.localUser.authenticated;
            if (currentStatus != IsAuthenticated)
            {
                IsAuthenticated = currentStatus;
                OnLoginStatusChanged?.Invoke(IsAuthenticated);
            }
            return IsAuthenticated;
        }
        
        /// <summary>
        /// 저장된 최고 점수가 있으면 리더보드에 제출
        /// </summary>
        public void SubmitBestScoreIfAvailable()
        {
            if (!IsAuthenticated)
            {
                DebugLog("로그인이 필요합니다.");
                return;
            }
            
            // ResourceManager를 통해 저장된 최고 점수 확인
            try
            {
                var scoreResource = ResourceManager.instance?.GetResource("Score");
                if (scoreResource != null)
                {
                    int bestScore = scoreResource.GetValue();
                    if (bestScore > 0)
                    {
                        DebugLog($"저장된 최고 점수 발견: {bestScore}");
                        SubmitScore(bestScore);
                    }
                    else
                    {
                        DebugLog("제출할 최고 점수가 없습니다.");
                    }
                }
                else
                {
                    DebugLog("ResourceManager에서 점수 정보를 찾을 수 없습니다.");
                }
            }
            catch (global::System.Exception e)
            {
                DebugLog($"최고 점수 확인 중 에러: {e.Message}");
            }
        }

        /// <summary>
        /// 저장된 최고 스테이지(진행 레벨)를 리더보드에 제출
        /// </summary>
        public void SubmitHighestStageIfAvailable()
        {
            if (!IsAuthenticated)
            {
                DebugLog("로그인이 필요합니다.(최고 스테이지)");
                return;
            }
            if (string.IsNullOrEmpty(leaderboardIdHighestStage))
            {
                DebugLog("최고 스테이지 리더보드 ID가 설정되지 않았습니다.");
                return;
            }
            try
            {
                int highestStage = GameDataManager.GetLevelNum();
                if (highestStage > 0)
                {
                    DebugLog($"저장된 최고 스테이지 발견: {highestStage}");
                    SubmitScoreToBoard(highestStage, leaderboardIdHighestStage);
                }
                else
                {
                    DebugLog("제출할 최고 스테이지가 없습니다.");
                }
            }
            catch (global::System.Exception e)
            {
                DebugLog($"최고 스테이지 확인 중 에러: {e.Message}");
            }
        }

        /// <summary>
        /// 임의의 리더보드 ID로 점수 제출
        /// </summary>
        public void SubmitScoreToBoard(long score, string boardId)
        {
            DebugLog($"=== [LEADERBOARD] 리더보드 제출 시작 ===");
            DebugLog($"[LEADERBOARD] 제출할 점수/레벨: {score}");
            DebugLog($"[LEADERBOARD] 리더보드 ID: {boardId}");
            DebugLog($"[LEADERBOARD] 현재 인증 상태: {IsAuthenticated}");
            
            if (!IsAuthenticated)
            {
                DebugLog("로그인이 필요합니다.(보드 지정 제출)");
                DebugLog("=== [LEADERBOARD] 리더보드 제출 실패 (인증 안됨) ===");
                return;
            }
            bool playGamesAuth = false;
            try
            {
                if (PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.localUser != null)
                {
                    playGamesAuth = PlayGamesPlatform.Instance.localUser.authenticated;
                }
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] 인증 상태 확인 중 예외: {e.Message}");
            }
            bool isActuallyAuthenticated = playGamesAuth || Social.localUser.authenticated;
            DebugLog($"[LEADERBOARD] 실제 인증 상태: {isActuallyAuthenticated}");
            
            if (!isActuallyAuthenticated)
            {
                DebugLog("[LEADERBOARD] 인증되지 않음 - 점수 제출 중단(보드 지정)");
                DebugLog("=== [LEADERBOARD] 리더보드 제출 실패 (인증 안됨) ===");
                return;
            }
            
            try
            {
                DebugLog($"[LEADERBOARD] PlayGamesPlatform.Instance.ReportScore 호출 중...");
                PlayGamesPlatform.Instance.ReportScore(score, boardId, (bool success) =>
                {
                    DebugLog($"[LEADERBOARD] 보드({boardId}) 제출 콜백 호출됨");
                    DebugLog($"[LEADERBOARD] 제출 성공 여부: {success}");
                    DebugLog($"[LEADERBOARD] 제출된 값: {score}");
                    if (success)
                    {
                        DebugLog("=== [LEADERBOARD] 리더보드 제출 성공 ===");
                    }
                    else
                    {
                        DebugLog("=== [LEADERBOARD] 리더보드 제출 실패 ===");
                    }
                });
                DebugLog($"[LEADERBOARD] ReportScore 호출 완료 (콜백 대기 중)");
            }
            catch (global::System.Exception e)
            {
                DebugLog($"[LEADERBOARD] 점수 제출 예외(보드 지정): {e.Message}");
                DebugLog($"[LEADERBOARD] 예외 타입: {e.GetType()}");
                DebugLog("=== [LEADERBOARD] 리더보드 제출 실패 (예외 발생) ===");
            }
        }
        
        /// <summary>
        /// GPGS 버전 정보 출력
        /// </summary>
        public void ShowVersionInfo()
        {
            DebugLog("Google Play Games Services 버전: 11.01");
            DebugLog($"로그인 상태: {(IsAuthenticated ? "로그인됨" : "로그아웃됨")}");
            if (IsAuthenticated)
            {
                DebugLog($"사용자: {GetUserName()} (ID: {GetUserId()})");
            }
        }
        
        /// <summary>
        /// Privacy Terms 동의 여부 확인
        /// </summary>
        private bool IsPrivacyTermsAgreed()
        {
            const string PRIVACY_TERMS_AGREED_KEY = "privacy_terms_agreed";
            return PlayerPrefs.GetInt(PRIVACY_TERMS_AGREED_KEY, 0) == 1;
        }
        
        /// <summary>
        /// 앱 시작 시 GPGS 세션 SignOut
        /// </summary>
        private void ForceSignOutOnAppStart()
        {
            try
            {
                // 로컬 상태 강제 초기화
                IsAuthenticated = false;
                
                // 이벤트 발생
                OnLoginStatusChanged?.Invoke(false);
            }
            catch (global::System.Exception e)
            {
                DebugLog($"SignOut 중 예외 발생: {e.Message}");
                
                // 예외가 발생해도 로컬 상태는 초기화
                IsAuthenticated = false;
                OnLoginStatusChanged?.Invoke(false);
            }
        }
        
        /// <summary>
        /// Privacy 준수를 위한 강제 로그아웃
        /// Privacy Terms에 동의하지 않은 상태에서 GPGS가 자동 로그인된 경우 강제 로그아웃
        /// </summary>
        private void ForceLogoutForPrivacyCompliance()
        {
            DebugLog("=== Privacy 준수를 위한 강제 로그아웃 시작 ===");
            
            try
            {
                // 현재 인증 상태 로그
                DebugLog($"강제 로그아웃 전 상태:");
                DebugLog($"  - Social.localUser.authenticated: {Social.localUser.authenticated}");
                DebugLog($"  - IsAuthenticated: {IsAuthenticated}");
                
                // GPGS 11.01에서는 직접적인 SignOut 대신 로컬 상태 제어
                DebugLog("GPGS 11.01 - Privacy 준수를 위한 로컬 상태 강제 초기화");
                DebugLog("시스템 레벨 인증은 유지되지만 앱 레벨에서 접근 차단");
                
                // 로컬 상태 강제 초기화
                IsAuthenticated = false;
                
                // 추가적인 Social 상태 확인
                DebugLog($"강제 로그아웃 후 상태:");
                DebugLog($"  - Social.localUser.authenticated: {Social.localUser.authenticated}");
                DebugLog($"  - IsAuthenticated: {IsAuthenticated}");
                
                // 이벤트 발생
                OnLoginStatusChanged?.Invoke(false);
                
                DebugLog("Privacy 준수를 위한 강제 로그아웃 완료");
                DebugLog("사용자는 Privacy Terms 동의 후 다시 로그인할 수 있습니다.");
                DebugLog("앱 재시작 시에도 Privacy Terms 체크로 접근이 제어됩니다.");
            }
            catch (global::System.Exception e)
            {
                DebugLog($"강제 로그아웃 중 예외 발생: {e.Message}");
                DebugLog($"예외 타입: {e.GetType()}");
                
                // 예외가 발생해도 로컬 상태는 초기화
                IsAuthenticated = false;
                OnLoginStatusChanged?.Invoke(false);
            }
            
            DebugLog("=== Privacy 준수를 위한 강제 로그아웃 완료 ===");
        }
    }
}
