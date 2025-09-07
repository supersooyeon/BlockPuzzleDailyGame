using System;
using System.Collections;
using GooglePlayGames;
using UnityEngine;
using Firebase;
using Firebase.Auth;

namespace BlockPuzzleGameToolkit.Scripts.System
{
	/// <summary>
	/// Firebase Auth 관리자
	/// - GPGS 로그인 성공 후 Firebase Auth 연동 (PlayGamesAuthProvider)
	/// - 다른 로그인 방식은 사용하지 않음
	/// </summary>
	public class FirebaseAuthManager : SingletonBehaviour<FirebaseAuthManager>
	{
		public bool enableDebugLog = true;

		private FirebaseApp _app;
		private FirebaseAuth _auth;
		private bool _initialized;

		public override void Awake()
		{
			base.Awake();
			if (instance == this)
			{
				DontDestroyOnLoad(gameObject);
				StartCoroutine(InitializeCoroutine());
			}
		}

		private IEnumerator InitializeCoroutine()
		{
			var checkTask = FirebaseApp.CheckAndFixDependenciesAsync();
			yield return new WaitUntil(() => checkTask.IsCompleted);
			if (checkTask.Exception != null)
			{
				DebugLog($"Firebase 의존성 확인 실패: {checkTask.Exception.Message}\n{checkTask.Exception}");
				yield break;
			}

			if (checkTask.Result == DependencyStatus.Available)
			{
				_app = FirebaseApp.DefaultInstance;
				_auth = FirebaseAuth.DefaultInstance;
				_initialized = true;
				DebugLog("Firebase 초기화 완료");
				PrintFirebaseOptions();
				DebugLog($"현재 Firebase 사용자: {_auth.CurrentUser?.UserId ?? "null"}");
			}
			else
			{
				DebugLog($"Firebase 의존성 상태 비정상: {checkTask.Result}");
			}
		}

		public void SignInWithPlayGames()
		{
			if (!_initialized)
			{
				DebugLog("Firebase 미초기화 상태 - 초기화 대기 후 시도합니다.");
				StartCoroutine(SignInAfterInitialized());
				return;
			}

			if (_auth.CurrentUser != null)
			{
				DebugLog($"이미 Firebase 로그인됨: {_auth.CurrentUser.UserId}");
				return;
			}

			if (PlayGamesPlatform.Instance == null)
			{
				DebugLog("PlayGamesPlatform.Instance 가 null 입니다.");
				return;
			}

			// GPGS v11: 서버사이드 접근 코드 요청 -> Firebase PlayGamesAuthProvider로 교환
			DebugLog("GPGS 서버사이드 접근 코드 요청...");
			PlayGamesPlatform.Instance.RequestServerSideAccess(true, serverAuthCode =>
			{
				if (string.IsNullOrEmpty(serverAuthCode))
				{
					DebugLog("serverAuthCode 획득 실패 (빈 값) - SHA-1/Google Play 설정, 구글 플레이 서비스 상태 확인 필요");
					return;
				}

				DebugLog($"serverAuthCode 획득 성공 - 길이: {serverAuthCode.Length}, 프리픽스: {serverAuthCode.Substring(0, Math.Min(8, serverAuthCode.Length))}***");
				DebugLog("Firebase 자격증명 생성 및 로그인 시도");
				var credential = PlayGamesAuthProvider.GetCredential(serverAuthCode);
				var signInTask = _auth.SignInWithCredentialAsync(credential);
				signInTask.ContinueWith(t =>
				{
					if (t.IsFaulted || t.IsCanceled)
					{
						var ex = t.Exception?.Flatten();
						DebugLog($"Firebase 로그인 실패 - Faulted:{t.IsFaulted}, Canceled:{t.IsCanceled}");
						DebugLog($"예외 메시지: {ex?.InnerException?.Message}");
						DebugLog($"예외 전체: {ex}");
						return;
					}
					DebugLog($"Firebase 로그인 성공: {t.Result.UserId}");
				});
			});
		}

		private IEnumerator SignInAfterInitialized()
		{
			while (!_initialized)
			{
				yield return null;
			}
			SignInWithPlayGames();
		}

		public void SignOut()
		{
			if (_auth == null)
			{
				return;
			}
			try
			{
				_auth.SignOut();
				DebugLog("Firebase 로그아웃 완료");
			}
			catch (Exception e)
			{
				DebugLog($"Firebase 로그아웃 중 오류: {e.Message}");
			}
		}

		private void DebugLog(string message)
		{
			if (enableDebugLog)
			{
				Debug.Log($"[FirebaseAuth] {message}");
			}
		}

		private void PrintFirebaseOptions()
		{
			try
			{
				var opts = _app?.Options;
				if (opts == null)
				{
					DebugLog("Firebase Options가 null 입니다. google-services.json 적용 여부 확인 필요");
					return;
				}
				DebugLog($"Firebase Options - ProjectId:{opts.ProjectId}, AppId:{opts.AppId}, ApiKey:{opts.ApiKey?.Substring(0, Math.Min(6, opts.ApiKey.Length))}***, StorageBucket:{opts.StorageBucket}");
			}
			catch (Exception e)
			{
				DebugLog($"Firebase Options 출력 중 예외: {e.Message}");
			}
		}
	}
}

