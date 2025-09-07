using Google.Play.AppUpdate;
using Google.Play.AppUpdate.Internal;
using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Play In-App Update를 관리하는 매니저 클래스
/// 즉시 업데이트와 유연한 업데이트를 모두 지원합니다.
/// </summary>
public class InAppUpdateManager : MonoBehaviour
{
    [Header("업데이트 설정")]
    [SerializeField] private bool enableImmediateUpdate = true;
    [SerializeField] private bool enableFlexibleUpdate = true;
    [SerializeField] private int maxRetryAttempts = 3;
    [SerializeField] private float retryDelaySeconds = 5f;
    
    private AppUpdateManager _appUpdateManager;
    private bool _isUpdateInProgress = false;
    private int _currentRetryAttempt = 0;
    
    // 이벤트
    public event Action<AppUpdateInfo> OnUpdateAvailable;
    public event Action<float> OnUpdateProgress;
    public event Action OnUpdateCompleted;
    public event Action<string> OnUpdateFailed;
    
    private void Awake()
    {
        // ANDROID가 아닌 환경(에디터/기타 플랫폼)에서는 초기화를 건너뜁니다.
#if UNITY_ANDROID && !UNITY_EDITOR
        _appUpdateManager = new AppUpdateManager();
#else
        Debug.Log("In-App Update는 Android 기기에서만 동작합니다. 이 컴포넌트를 비활성화합니다.");
        enabled = false;
#endif
    }
    
    /// <summary>
    /// 업데이트 확인을 시작합니다.
    /// </summary>
    public void StartUpdateCheck()
    {
        // Android가 아닌 경우 조기 반환
#if !(UNITY_ANDROID && !UNITY_EDITOR)
        Debug.Log("[AppUpdate] Android가 아니므로 업데이트 체크를 건너뜁니다.");
        return;
#else
        Debug.Log("[AppUpdate] 업데이트 체크 시작");
        
        if (!_isUpdateInProgress)
        {
            // 먼저 진행 중인 업데이트가 있는지 확인
            StartCoroutine(CheckForUpdateResume());
        }
        else
        {
            Debug.Log("[AppUpdate] 이미 업데이트가 진행 중입니다.");
        }
#endif
    }
    
    /// <summary>
    /// 지연 후 업데이트 체크를 재시도합니다.
    /// </summary>
    private IEnumerator RetryUpdateCheckAfterDelay()
    {
        Debug.Log($"[AppUpdate] {retryDelaySeconds}초 후 재시도");
        yield return new WaitForSeconds(retryDelaySeconds);
        
        // 재시도할 때는 CheckForUpdateResume을 다시 호출
        yield return StartCoroutine(CheckForUpdateResume());
    }
    
    /// <summary>
    /// 진행 중인 업데이트가 있는지 확인하고, 없으면 새로운 업데이트를 체크합니다.
    /// </summary>
    private IEnumerator CheckForUpdateResume()
    {
        _isUpdateInProgress = true;
        
        var appUpdateInfoOperation = _appUpdateManager.GetAppUpdateInfo();
        yield return appUpdateInfoOperation;
        
        if (appUpdateInfoOperation.Error == AppUpdateErrorCode.NoError)
        {
            var appUpdateInfoResult = appUpdateInfoOperation.GetResult();
            
            Debug.Log($"[AppUpdate] === 앱 업데이트 정보 조회 성공 ===");
            Debug.Log($"[AppUpdate] 현재 앱 설치 소스: {GetAppInstallSource()}");
            Debug.Log($"[AppUpdate] 패키지명: {Application.identifier}");
            Debug.Log($"[AppUpdate] 현재 버전: {Application.version}");
            
            // 진행 중인 유연한 업데이트가 있는지 확인
            if (appUpdateInfoResult.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress)
            {
                Debug.Log("[AppUpdate] 진행 중인 업데이트 재개");
                yield return StartCoroutine(ResumeFlexibleUpdate(appUpdateInfoResult));
            }
            else
            {
                // 일반적인 업데이트 체크 진행
                yield return StartCoroutine(ProcessUpdateInfo(appUpdateInfoResult));
            }
        }
        else
        {
            Debug.LogError($"[AppUpdate] 업데이트 정보 조회 실패: {appUpdateInfoOperation.Error}");
            Debug.LogError($"[AppUpdate] 에러 세부사항: {GetDetailedError(appUpdateInfoOperation.Error)}");
            HandleUpdateError($"업데이트 정보 조회 실패: {appUpdateInfoOperation.Error}");
        }
        
        _isUpdateInProgress = false;
    }
    
    /// <summary>
    /// 유연한 업데이트를 재개합니다.
    /// </summary>
    private IEnumerator ResumeFlexibleUpdate(AppUpdateInfo updateInfo)
    {
        var appUpdateOptions = AppUpdateOptions.FlexibleAppUpdateOptions();
        var resumeUpdateRequest = _appUpdateManager.StartUpdate(updateInfo, appUpdateOptions);
        
        while (!resumeUpdateRequest.IsDone)
        {
            OnUpdateProgress?.Invoke(resumeUpdateRequest.DownloadProgress);
            yield return null;
        }
        
        if (resumeUpdateRequest.Error == AppUpdateErrorCode.NoError)
        {
            Debug.Log("[AppUpdate] 업데이트 재개 완료");
            OnUpdateCompleted?.Invoke();
            StartCoroutine(CheckInstallationCompleted());
        }
        else
        {
            Debug.LogError($"[AppUpdate] 업데이트 재개 실패: {resumeUpdateRequest.Error}");
            OnUpdateFailed?.Invoke($"업데이트 재개 실패: {resumeUpdateRequest.Error}");
        }
    }
    
    /// <summary>
    /// 업데이트 정보를 처리하고 적절한 업데이트 방식을 선택합니다.
    /// </summary>
    private IEnumerator ProcessUpdateInfo(AppUpdateInfo updateInfo)
    {
        // 상세한 업데이트 정보 로그
        Debug.Log($"[AppUpdate] === 업데이트 정보 상세 ===");
        Debug.Log($"[AppUpdate] UpdateAvailability: {updateInfo.UpdateAvailability}");
        Debug.Log($"[AppUpdate] AppUpdateStatus: {updateInfo.AppUpdateStatus}");
        Debug.Log($"[AppUpdate] UpdatePriority: {updateInfo.UpdatePriority}");
        
        if (updateInfo.ClientVersionStalenessDays.HasValue)
        {
            Debug.Log($"[AppUpdate] ClientVersionStalenessDays: {updateInfo.ClientVersionStalenessDays.Value}");
        }
        else
        {
            Debug.Log("[AppUpdate] ClientVersionStalenessDays: null");
        }
        
        Debug.Log($"[AppUpdate] AvailableVersionCode: {updateInfo.AvailableVersionCode}");
        Debug.Log($"[AppUpdate] 패키지명: {Application.identifier}");
        Debug.Log("[AppUpdate] ========================");
        
        if (updateInfo.UpdateAvailability == UpdateAvailability.UpdateAvailable)
        {
            Debug.Log("[AppUpdate] ✅ 업데이트 사용 가능!");
            OnUpdateAvailable?.Invoke(updateInfo);
            
            // 업데이트 우선도에 따라 업데이트 방식 결정
            bool shouldUseImmediateUpdate = ShouldUseImmediateUpdate(updateInfo);
            Debug.Log($"[AppUpdate] 즉시 업데이트 필요 여부: {shouldUseImmediateUpdate}");
            Debug.Log($"[AppUpdate] enableImmediateUpdate: {enableImmediateUpdate}");
            Debug.Log($"[AppUpdate] enableFlexibleUpdate: {enableFlexibleUpdate}");
            
            if (enableImmediateUpdate && shouldUseImmediateUpdate)
            {
                Debug.Log("[AppUpdate] 🚀 즉시 업데이트 시작");
                yield return StartCoroutine(StartImmediateUpdateCoroutine());
            }
            else if (enableFlexibleUpdate)
            {
                Debug.Log("[AppUpdate] 📥 유연한 업데이트 시작");
                yield return StartCoroutine(StartFlexibleUpdateCoroutine());
            }
            else if (enableImmediateUpdate)
            {
                Debug.Log("[AppUpdate] 🚀 즉시 업데이트 시작 (대체)");
                yield return StartCoroutine(StartImmediateUpdateCoroutine());
            }
            else
            {
                Debug.Log("[AppUpdate] ❌ 업데이트 설정이 비활성화되어 있습니다.");
            }
        }
        else if (updateInfo.UpdateAvailability == UpdateAvailability.UpdateNotAvailable)
        {
            Debug.Log("[AppUpdate] ℹ️ 업데이트가 필요하지 않음 (최신 버전)");
        }
        else if (updateInfo.UpdateAvailability == UpdateAvailability.Unknown)
        {
            Debug.Log("[AppUpdate] ❓ 업데이트 상태 알 수 없음");
        }
        else
        {
            Debug.Log($"[AppUpdate] ⚠️ 기타 상태: {updateInfo.UpdateAvailability}");
        }
    }
    
    /// <summary>
    /// 즉시 업데이트를 시작합니다.
    /// </summary>
    public void StartImmediateUpdate()
    {
        // Android가 아닌 경우 조기 반환
#if !(UNITY_ANDROID && !UNITY_EDITOR)
        Debug.Log("StartImmediateUpdate: Android가 아니므로 즉시 업데이트를 수행하지 않습니다.");
        return;
#else
        if (!_isUpdateInProgress)
        {
            StartCoroutine(StartImmediateUpdateCoroutine());
        }
#endif
    }
    
    /// <summary>
    /// 유연한 업데이트를 시작합니다.
    /// </summary>
    public void StartFlexibleUpdate()
    {
        // Android가 아닌 경우 조기 반환
#if !(UNITY_ANDROID && !UNITY_EDITOR)
        Debug.Log("StartFlexibleUpdate: Android가 아니므로 유연한 업데이트를 수행하지 않습니다.");
        return;
#else
        if (!_isUpdateInProgress)
        {
            StartCoroutine(StartFlexibleUpdateCoroutine());
        }
#endif
    }
    
    /// <summary>
    /// 업데이트 에러를 처리합니다.
    /// </summary>
    private void HandleUpdateError(string errorMessage)
    {
        // 재시도 로직
        if (_currentRetryAttempt < maxRetryAttempts)
        {
            _currentRetryAttempt++;
            Debug.Log($"[AppUpdate] 재시도 {_currentRetryAttempt}/{maxRetryAttempts}");
            StartCoroutine(RetryUpdateCheckAfterDelay());
        }
        else
        {
            Debug.LogError("[AppUpdate] 최대 재시도 횟수 도달");
            OnUpdateFailed?.Invoke(errorMessage);
            _currentRetryAttempt = 0; // 재시도 카운터 리셋
        }
    }
    
    /// <summary>
    /// 즉시 업데이트를 실행합니다.
    /// </summary>
    private IEnumerator StartImmediateUpdateCoroutine()
    {
        Debug.Log("즉시 업데이트를 시작합니다...");
        
        var appUpdateInfoOperation = _appUpdateManager.GetAppUpdateInfo();
        
        yield return appUpdateInfoOperation;
        
        if (appUpdateInfoOperation.Error == AppUpdateErrorCode.NoError)
        {
            var appUpdateInfoResult = appUpdateInfoOperation.GetResult();
            
            if (appUpdateInfoResult.UpdateAvailability == UpdateAvailability.UpdateAvailable)
            {
                var appUpdateOptions = AppUpdateOptions.ImmediateAppUpdateOptions();
                var startUpdateRequest = _appUpdateManager.StartUpdate(appUpdateInfoResult, appUpdateOptions);
                
                while (!startUpdateRequest.IsDone)
                {
                    yield return null;
                }
                
                // 즉시 업데이트는 완료되면 자동으로 앱이 재시작되므로 성공으로 간주
                Debug.Log("즉시 업데이트가 성공적으로 시작되었습니다.");
                OnUpdateCompleted?.Invoke();
            }
            else
            {
                Debug.LogWarning("즉시 업데이트를 사용할 수 없습니다.");
                OnUpdateFailed?.Invoke("즉시 업데이트를 사용할 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError($"업데이트 정보를 가져오는데 실패했습니다: {appUpdateInfoOperation.Error}");
            OnUpdateFailed?.Invoke($"업데이트 정보 조회 실패: {appUpdateInfoOperation.Error}");
        }
        
        _isUpdateInProgress = false;
    }
    
    /// <summary>
    /// 유연한 업데이트를 실행합니다.
    /// </summary>
    private IEnumerator StartFlexibleUpdateCoroutine()
    {
        Debug.Log("유연한 업데이트를 시작합니다...");
        
        var appUpdateInfoOperation = _appUpdateManager.GetAppUpdateInfo();
        
        yield return appUpdateInfoOperation;
        
        if (appUpdateInfoOperation.Error == AppUpdateErrorCode.NoError)
        {
            var appUpdateInfoResult = appUpdateInfoOperation.GetResult();
            
            if (appUpdateInfoResult.UpdateAvailability == UpdateAvailability.UpdateAvailable)
            {
                var appUpdateOptions = AppUpdateOptions.FlexibleAppUpdateOptions();
                var startUpdateRequest = _appUpdateManager.StartUpdate(appUpdateInfoResult, appUpdateOptions);
                
                // 업데이트 진행률 모니터링
                while (!startUpdateRequest.IsDone)
                {
                    // 진행률 업데이트
                    float progress = startUpdateRequest.DownloadProgress;
                    OnUpdateProgress?.Invoke(progress);
                    Debug.Log($"업데이트 진행 중... {progress:P0}");
                    
                    yield return null;
                }
                
                // 유연한 업데이트가 완료되면 성공으로 간주
                Debug.Log("유연한 업데이트가 성공적으로 완료되었습니다.");
                OnUpdateCompleted?.Invoke();
                
                // 유연한 업데이트 완료 후 앱 재시작을 위한 설치 완료 체크
                StartCoroutine(CheckInstallationCompleted());
            }
            else
            {
                Debug.LogWarning("유연한 업데이트를 사용할 수 없습니다.");
                OnUpdateFailed?.Invoke("유연한 업데이트를 사용할 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError($"업데이트 정보를 가져오는데 실패했습니다: {appUpdateInfoOperation.Error}");
            OnUpdateFailed?.Invoke($"업데이트 정보 조회 실패: {appUpdateInfoOperation.Error}");
        }
        
        _isUpdateInProgress = false;
    }
    
    /// <summary>
    /// 업데이트 진행 중인지 확인합니다.
    /// </summary>
    public bool IsUpdateInProgress()
    {
        return _isUpdateInProgress;
    }
    
    /// <summary>
    /// 즉시 업데이트를 사용해야 하는지 판단합니다.
    /// </summary>
    /// <param name="updateInfo">업데이트 정보</param>
    /// <returns>즉시 업데이트 필요 여부</returns>
    private bool ShouldUseImmediateUpdate(AppUpdateInfo updateInfo)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 업데이트 우선도를 확인합니다.
        // UpdatePriority가 높은 경우 (4-5) 즉시 업데이트 사용
        // 또는 스토킹된 일 수가 많은 경우 즉시 업데이트 사용
        if (updateInfo.UpdatePriority >= 4)
        {
            Debug.Log($"높은 우선도 업데이트입니다. Priority: {updateInfo.UpdatePriority}");
            return true;
        }
        
        // 업데이트가 사용 가능한 지 며칠이 지났는지 확인
        if (updateInfo.ClientVersionStalenessDays.HasValue && updateInfo.ClientVersionStalenessDays.Value >= 3)
        {
            Debug.Log($"업데이트가 {updateInfo.ClientVersionStalenessDays.Value}일 전부터 사용 가능했습니다. 즉시 업데이트를 권장합니다.");
            return true;
        }
        
        return false;
#else
        // Android가 아닌 경우 기본값 반환
        return false;
#endif
    }
    
    /// <summary>
    /// 유연한 업데이트의 설치 완료를 체크하고 사용자에게 재시작을 요청합니다.
    /// </summary>
    private IEnumerator CheckInstallationCompleted()
    {
        Debug.Log("업데이트 설치 완료를 확인하는 중...");
        
        while (true)
        {
            var appUpdateInfoOperation = _appUpdateManager.GetAppUpdateInfo();
            yield return appUpdateInfoOperation;
            
            if (appUpdateInfoOperation.Error == AppUpdateErrorCode.NoError)
            {
                var appUpdateInfoResult = appUpdateInfoOperation.GetResult();
                
                if (appUpdateInfoResult.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress)
                {
                    // 아직 다운로드 중
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                else if (appUpdateInfoResult.AppUpdateStatus == AppUpdateStatus.Downloaded)
                {
                    // 다운로드 완료, 설치 준비됨
                    Debug.Log("업데이트가 다운로드되었습니다. 앱 재시작을 요청합니다.");
                    
                    // 사용자에게 재시작 요청 (선택사항)
                    if (ShowInstallationDialog())
                    {
                        _appUpdateManager.CompleteUpdate();
                    }
                    
                    break;
                }
                else
                {
                    // 업데이트가 더 이상 필요하지 않거나 완료됨
                    Debug.Log("업데이트 설치가 완료되었거나 더 이상 필요하지 않습니다.");
                    break;
                }
            }
            else
            {
                Debug.LogError($"업데이트 상태 확인 실패: {appUpdateInfoOperation.Error}");
                break;
            }
        }
    }
    
    /// <summary>
    /// 사용자에게 설치 완료 대화상자를 표시합니다.
    /// </summary>
    /// <returns>사용자가 지금 설치할지 여부</returns>
    private bool ShowInstallationDialog()
    {
        // TODO: 실제 프로젝트에서는 UI 대화상자를 표시하고 사용자 선택을 받아야 합니다.
        // 현재는 자동으로 true를 반환하여 즉시 설치를 진행합니다.
        Debug.Log("[AppUpdate] 업데이트 설치가 준비되었습니다. 자동으로 설치를 진행합니다.");
        return true;
    }
    
    /// <summary>
    /// 앱 설치 소스를 확인합니다.
    /// </summary>
    private string GetAppInstallSource()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var packageManager = context.Call<AndroidJavaObject>("getPackageManager"))
            {
                string packageName = context.Call<string>("getPackageName");
                string installerPackageName = packageManager.Call<string>("getInstallerPackageName", packageName);
                
                if (string.IsNullOrEmpty(installerPackageName))
                {
                    return "Unknown (직접 설치)";
                }
                else if (installerPackageName.Contains("com.android.vending"))
                {
                    return "Google Play Store";
                }
                else
                {
                    return $"기타: {installerPackageName}";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AppUpdate] 설치 소스 확인 실패: {e.Message}");
            return "확인 실패";
        }
#else
        return "에디터/PC";
#endif
    }
    
    /// <summary>
    /// 에러 코드에 대한 상세 설명을 제공합니다.
    /// </summary>
    private string GetDetailedError(AppUpdateErrorCode errorCode)
    {
        switch (errorCode)
        {
            case AppUpdateErrorCode.NoError:
                return "에러 없음";
            case AppUpdateErrorCode.NoErrorPartiallyAllowed:
                return "일부 업데이트 타입만 허용됨";
            case AppUpdateErrorCode.ErrorUnknown:
                return "알 수 없는 에러";
            case AppUpdateErrorCode.ErrorApiNotAvailable:
                return "API 사용 불가 (Play Store 앱이 설치되지 않았거나 버전이 낮음)";
            case AppUpdateErrorCode.ErrorInvalidRequest:
                return "잘못된 요청";
            case AppUpdateErrorCode.ErrorUpdateUnavailable:
                return "업데이트 사용 불가";
            case AppUpdateErrorCode.ErrorUpdateNotAllowed:
                return "업데이트 허용되지 않음 (배터리 부족, 저장공간 부족 등)";
            case AppUpdateErrorCode.ErrorDownloadNotPresent:
                return "업데이트 다운로드가 완료되지 않음";
            case AppUpdateErrorCode.ErrorUpdateInProgress:
                return "이미 업데이트가 진행 중";
            case AppUpdateErrorCode.ErrorInternalError:
                return "Play Store 내부 에러";
            case AppUpdateErrorCode.ErrorUserCanceled:
                return "사용자가 업데이트 취소";
            case AppUpdateErrorCode.ErrorUpdateFailed:
                return "업데이트 실패 (네트워크 연결 중단 등)";
            case AppUpdateErrorCode.ErrorPlayStoreNotFound:
                return "Play Store 앱이 설치되지 않았거나 공식 버전이 아님";
            case AppUpdateErrorCode.ErrorAppNotOwned:
                return "이 앱이 Play Store에서 설치되지 않음";
            default:
                return $"기타 에러: {errorCode}";
        }
    }
    
    /// <summary>
    /// 업데이트 매니저를 정리합니다.
    /// </summary>
    private void OnDestroy()
    {
        // AppUpdateManager는 Dispose 메서드가 없으므로 null로 설정
        _appUpdateManager = null;
    }
}