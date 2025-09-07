using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Popups;
using BlockPuzzleGameToolkit.Scripts.Localization;

public class PrivacyTermsPopup : Popup
{
    [Header("UI References")]
    public Button termsOfServiceButton;
    public Button privacyPolicyButton;
    public Toggle consentToggle;
    public Button agreeButton;

    [Header("URLs")]
    [SerializeField] private string termsOfServiceUrlEnglish = "https://www.notion.so/Terms-of-Service-for-Block-Puzzle-Daily-Game-231b0ae0679880178482c2941cf41dab?source=copy_link";
    [SerializeField] private string termsOfServiceUrlKorean = "https://www.notion.so/BlockPuzzle-Daily-Game-231b0ae067988037b886f6aace19bcff?source=copy_link";
    [SerializeField] private string privacyPolicyUrlEnglish = "https://www.notion.so/Privacy-Policy-for-Block-Puzzle-Daily-Game-1dfb0ae0679880509102d5842bec4f14?source=copy_link";
    [SerializeField] private string privacyPolicyUrlKorean = "https://www.notion.so/BlockPuzzle-Daily-Game-231b0ae067988037a715c5a92a800d20?source=copy_link";

    [Header("Events")]
    public UnityEvent onClose = new UnityEvent();

    private const string PRIVACY_TERMS_AGREED_KEY = "privacy_terms_agreed";

    void Start()
    {
        Debug.Log("=== PrivacyTermsPopup.Start() 시작 ===");
        Debug.Log($"PrivacyTermsPopup.Start: IsPrivacyTermsAgreed = {IsPrivacyTermsAgreed()}");
        Debug.Log($"PrivacyTermsPopup.Start: PlayerPrefs 값 = {PlayerPrefs.GetInt(PRIVACY_TERMS_AGREED_KEY, -1)}");
        
        // Privacy Terms 동의 여부 확인 - 이미 동의한 경우 팝업 바로 닫기
        if (IsPrivacyTermsAgreed())
        {
            Debug.Log("PrivacyTermsPopup.Start: 이미 동의함 - ClosePopupImmediately() 호출");
            // 기존 사용자: GPGS는 시스템에서 자동 처리되므로 팝업만 닫기
            StartCoroutine(ClosePopupImmediately());
            return;
        }
        
        Debug.Log("PrivacyTermsPopup.Start: 신규 사용자 - 팝업 표시 준비");
        // 신규 사용자: 팝업 표시를 위한 초기 설정
        SetupUI();
        
        // 버튼 이벤트 설정
        SetupButtonEvents();
        
        // 체크박스 초기 상태 설정 (체크 해제)
        if (consentToggle != null)
        {
            consentToggle.isOn = false;
        }
        
        Debug.Log("=== PrivacyTermsPopup.Start() 완료 ===");
    }

    private void SetupUI()
    {
        // prefab에서 UI 요소들을 자동으로 찾기
        if (termsOfServiceButton == null)
            termsOfServiceButton = transform.Find("menus-label/Buttons/Vertical_Buttons/Trems_of_service_button")?.GetComponent<Button>();
        
        if (privacyPolicyButton == null)
            privacyPolicyButton = transform.Find("menus-label/Buttons/Vertical_Buttons/Privacy_policy_button")?.GetComponent<Button>();
        
        if (consentToggle == null)
            consentToggle = transform.Find("menus-label/Toggle")?.GetComponent<Toggle>();
        
        if (agreeButton == null)
            agreeButton = transform.Find("menus-label/Buttons/Agree_button")?.GetComponent<Button>();
        
        // 부모 클래스의 closeButton 사용 (이미 Popup 클래스에서 설정됨)
    }

    private void SetupButtonEvents()
    {
        // Terms of Service 버튼 이벤트
        if (termsOfServiceButton != null)
        {
            termsOfServiceButton.onClick.RemoveAllListeners();
            termsOfServiceButton.onClick.AddListener(OnTermsOfServiceClicked);
        }

        // Privacy Policy 버튼 이벤트
        if (privacyPolicyButton != null)
        {
            privacyPolicyButton.onClick.RemoveAllListeners();
            privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyClicked);
        }

        // 동의 버튼 이벤트
        if (agreeButton != null)
        {
            agreeButton.onClick.RemoveAllListeners();
            agreeButton.onClick.AddListener(OnAgreeClicked);
        }

        // 부모 클래스의 닫기 버튼 이벤트 (Popup 클래스에서 자동 처리됨)
        // 필요시 부모의 closeButton에 추가 이벤트 등록 가능
    }

    private void OnTermsOfServiceClicked()
    {
        // 한국어 사용자인 경우에만 한국어 URL, 아니면 영어 URL
        string url = IsKoreanLanguage() ? termsOfServiceUrlKorean : termsOfServiceUrlEnglish;
        Application.OpenURL(url);
    }

    private void OnPrivacyPolicyClicked()
    {
        // 한국어 사용자인 경우에만 한국어 URL, 아니면 영어 URL
        string url = IsKoreanLanguage() ? privacyPolicyUrlKorean : privacyPolicyUrlEnglish;
        Application.OpenURL(url);
    }

    private void OnAgreeClicked()
    {
        if (consentToggle != null && !consentToggle.isOn)
        {
            // 체크박스가 체크되지 않았을 때 경고 팝업 표시
            ShowCheckboxWarning();
            return;
        }

        Debug.Log("OnAgreeClicked() - Privacy Terms 동의 처리 시작");

        // 동의 처리
        PlayerPrefs.SetInt(PRIVACY_TERMS_AGREED_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log($"OnAgreeClicked() - PlayerPrefs 설정 완료: {PlayerPrefs.GetInt(PRIVACY_TERMS_AGREED_KEY, -1)}");

        // onClose 이벤트 호출
        onClose?.Invoke();

        // 팝업 닫기
        Close();
        
        // PrivacyTermsPopupManager에서 클래식 모드 진입을 처리함
    }

    /// <summary>
    /// 부모 클래스의 Close() 메서드 오버라이드
    /// </summary>
    public override void Close()
    {
        Debug.Log("=== PrivacyTermsPopup.Close() 호출됨 ===");
        Debug.Log($"PrivacyTermsPopup.Close: IsPrivacyTermsAgreed = {PrivacyTermsPopup.IsPrivacyTermsAgreed()}");
        Debug.Log($"PrivacyTermsPopup.Close: PlayerPrefs 값 = {PlayerPrefs.GetInt(PRIVACY_TERMS_AGREED_KEY, -1)}");
        
        // 동의하지 않고 팝업을 닫으려는 경우 게임 종료
        if (!PrivacyTermsPopup.IsPrivacyTermsAgreed())
        {
            Debug.Log("PrivacyTermsPopup.Close: Privacy Terms 미동의 - 게임 종료");
            
            // 게임 종료
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
            return;
        }
        
        // 동의한 경우에만 정상적으로 팝업 닫기
        Debug.Log("PrivacyTermsPopup.Close: Privacy Terms 동의함 - base.Close() 호출");
        base.Close();
        Debug.Log("=== PrivacyTermsPopup.Close() 완료 ===");
    }

    private void ShowCheckboxWarning()
    {
        Debug.Log("ShowCheckboxWarning 호출됨");
        
        // MenuManager를 통해 AnnouncementPopup 표시
        var menuManager = FindObjectOfType<MenuManager>();
        if (menuManager != null)
        {
            Debug.Log("MenuManager 발견, AnnouncementPopup 표시 중...");
            
            menuManager.ShowPopup<AnnouncementPopup>(
                onShow: () => {
                    Debug.Log("AnnouncementPopup onShow 콜백 호출됨");
                    
                    // 팝업이 완전히 로드될 때까지 약간 대기
                    StartCoroutine(SetupAnnouncementPopup());
                }
            );
        }
        else
        {
            Debug.LogError("MenuManager를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// AnnouncementPopup 설정을 위한 코루틴
    /// </summary>
    private IEnumerator SetupAnnouncementPopup()
    {
        // 팝업이 완전히 초기화될 때까지 대기
        yield return new WaitForEndOfFrame();
        
        Debug.Log("AnnouncementPopup 설정 시작...");
        
        var announcementPopup = FindObjectOfType<AnnouncementPopup>();
        if (announcementPopup != null)
        {
            Debug.Log("AnnouncementPopup 인스턴스 발견");
            
            // LocalizedTextMeshProUGUI 컴포넌트의 instanceID 설정
            SetLocalizedTextInstanceID(announcementPopup);
            
            // 애니메이션 및 자동 닫기 시작
            announcementPopup.ShowMessage("CHECKBOX");
        }
        else
        {
            Debug.LogError("AnnouncementPopup 인스턴스를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// AnnouncementPopup의 LocalizedTextMeshProUGUI 컴포넌트의 instanceID 설정
    /// </summary>
    /// <param name="announcementPopup">AnnouncementPopup 인스턴스</param>
    private void SetLocalizedTextInstanceID(AnnouncementPopup announcementPopup)
    {
        Debug.Log("SetLocalizedTextInstanceID 시작...");
        
        // LocalizedTextMeshProUGUI 컴포넌트 찾기 (모든 하위 오브젝트 포함)
        var localizedTextComponents = announcementPopup.GetComponentsInChildren<LocalizedTextMeshProUGUI>(true);
        
        Debug.Log($"발견된 LocalizedTextMeshProUGUI 컴포넌트 수: {localizedTextComponents.Length}");
        
        if (localizedTextComponents.Length > 0)
        {
            foreach (var localizedText in localizedTextComponents)
            {
                Debug.Log($"LocalizedTextMeshProUGUI 컴포넌트 발견: {localizedText.gameObject.name}");
                Debug.Log($"설정 전 instanceID: '{localizedText.instanceID}'");
                Debug.Log($"설정 전 text: '{localizedText.text}'");
                
                // instanceID 필드에 로컬라이제이션 키 설정
                localizedText.instanceID = "CHECKBOX";
                
                Debug.Log($"설정 후 instanceID: '{localizedText.instanceID}'");
                
                // 텍스트 업데이트 호출
                localizedText.UpdateText();
                
                Debug.Log($"UpdateText() 호출 후 text: '{localizedText.text}'");
                Debug.Log($"✅ LocalizedTextMeshProUGUI의 instanceID를 'CHECKBOX'로 설정 완료");
            }
        }
        else
        {
            Debug.LogError("❌ AnnouncementPopup에서 LocalizedTextMeshProUGUI 컴포넌트를 찾을 수 없습니다!");
            
            // 디버깅을 위해 모든 하위 컴포넌트 로그 출력
            var allComponents = announcementPopup.GetComponentsInChildren<MonoBehaviour>(true);
            Debug.Log($"발견된 모든 컴포넌트 ({allComponents.Length}개):");
            foreach (var comp in allComponents)
            {
                Debug.Log($"- {comp.GetType().Name} on {comp.gameObject.name}");
            }
        }
    }



    private bool IsKoreanLanguage()
    {
#if UNITY_EDITOR
        // 에디터에서는 DebugSettings의 TestLanguage를 우선 사용
        var debugSettings = GameObject.FindObjectOfType<BlockPuzzleGameToolkit.Scripts.Settings.DebugSettings>();
        if (debugSettings != null)
        {
            return debugSettings.TestLanguage == SystemLanguage.Korean;
        }
#endif
        // 시스템 언어가 한국어인지 확인
        return Application.systemLanguage == SystemLanguage.Korean;
    }

    public static bool IsPrivacyTermsAgreed()
    {
        int value = PlayerPrefs.GetInt(PRIVACY_TERMS_AGREED_KEY, 0);
        bool result = value == 1;
        Debug.Log($"IsPrivacyTermsAgreed() - PlayerPrefs 값: {value}, 결과: {result}");
        return result;
    }

    public static void ResetPrivacyTermsAgreement()
    {
        PlayerPrefs.DeleteKey(PRIVACY_TERMS_AGREED_KEY);
        PlayerPrefs.Save();
    }
    

    
    /// <summary>
    /// 기존 사용자용 - 팝업 즉시 닫기
    /// </summary>
    private IEnumerator ClosePopupImmediately()
    {
        Debug.Log("=== ClosePopupImmediately() 시작 ===");
        // 프레임 한 번 대기 (UI 안정성을 위해)
        yield return null;
        
        Debug.Log("ClosePopupImmediately: onClose 이벤트 호출");
        // onClose 이벤트 호출 (필요한 경우)
        onClose?.Invoke();
        
        Debug.Log("ClosePopupImmediately: 팝업 직접 닫기 (base.Close() 호출하지 않음)");
        // 팝업 닫기 (게임 종료 방지를 위해 직접 처리)
        gameObject.SetActive(false);
        if (transform.parent != null)
        {
            Destroy(gameObject);
        }
        
        Debug.Log("ClosePopupImmediately: GameManager 찾기");
        // 기존 유저의 경우에도 PrivacyTermsPopupManager와 동일한 로직 적용
        var gameManager = FindObjectOfType<BlockPuzzleGameToolkit.Scripts.System.GameManager>();
        if (gameManager != null)
        {
            Debug.Log($"ClosePopupImmediately: PostTutorialGameMode 키 존재 = {PlayerPrefs.HasKey("PostTutorialGameMode")}");
            Debug.Log($"ClosePopupImmediately: IsTutorialShown = {gameManager.IsTutorialShown()}");
            
            // 신규 유저 vs 기존 유저 튜토리얼 미완료 상황 구분
            if (!PlayerPrefs.HasKey("PostTutorialGameMode"))
            {
                // 신규 유저: 튜토리얼 모드로 진입
                Debug.Log("ClosePopupImmediately: 신규 유저, 튜토리얼 모드로 진입");
                gameManager.SetTutorialMode(true);
                gameManager.OpenGame();
            }
            else if (!gameManager.IsTutorialShown())
            {
                // 기존 유저 튜토리얼 미완료: 튜토리얼 모드로 진입
                Debug.Log("ClosePopupImmediately: 기존 유저 튜토리얼 미완료, 튜토리얼 모드로 진입");
                gameManager.SetTutorialMode(true);
                gameManager.OpenGame();
            }
            else
            {
                // 기존 유저 튜토리얼 완료: 메인 화면으로 이동
                Debug.Log("ClosePopupImmediately: 기존 유저 튜토리얼 완료, 메인 화면으로 이동");
                gameManager.SetGameMode(BlockPuzzleGameToolkit.Scripts.Enums.EGameMode.Classic);
                // 메인 화면으로 이동 (게임 씬이 아닌 메인 메뉴 씬)
                gameManager.MainMenu();
            }
        }
        else
        {
            Debug.LogError("ClosePopupImmediately: GameManager를 찾을 수 없습니다.");
        }
        Debug.Log("=== ClosePopupImmediately() 완료 ===");
    }
    

}
