using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using BlockPuzzleGameToolkit.Scripts.GUI;
using BlockPuzzleGameToolkit.Scripts.Localization;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Data;
using GooglePlayGames;
using UnityEngine.SocialPlatforms;

namespace BlockPuzzleGameToolkit.Scripts.Popups
{
    public class DeleteAccountPopup : Popup
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField deleteInputField;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button cancelButton;
        
        private const string REQUIRED_INPUT = "DELETE";
        private const string LOCALIZED_KEY = "ACCOUNT_DELETION_FAILED";
        
        void Start()
        {
            // InputField 이벤트 등록
            if (deleteInputField != null)
            {
                deleteInputField.onValueChanged.AddListener(OnInputFieldChanged);
            }
            
            // Delete 버튼 이벤트 등록
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            }
            
            // Cancel 버튼 이벤트 등록
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
            
            // announcement_Popup은 Resources에서 동적으로 로드됨
        }
        
        /// <summary>
        /// InputField 값이 변경될 때 소문자를 대문자로 변환
        /// </summary>
        /// <param name="value">입력된 값</param>
        private void OnInputFieldChanged(string value)
        {
            if (deleteInputField != null)
            {
                string upperValue = value.ToUpper();
                if (value != upperValue)
                {
                    deleteInputField.text = upperValue;
                }
            }
        }
        
        /// <summary>
        /// Delete 버튼 클릭 시 호출되는 메서드
        /// </summary>
        private void OnDeleteButtonClicked()
        {
            if (deleteInputField == null) return;
            
            string inputText = deleteInputField.text.Trim();
            
            if (inputText == REQUIRED_INPUT)
            {
                // 정확하게 DELETE를 입력한 경우
                DeleteAllUserData();
            }
            else
            {
                // 잘못 입력한 경우
                ShowAnnouncementPopup();
            }
        }
        
        /// <summary>
        /// Cancel 버튼 클릭 시 호출되는 메서드 - 팝업을 닫음
        /// </summary>
        private void OnCancelButtonClicked()
        {
            // 팝업을 닫음
            Close();
        }
        
        /// <summary>
        /// 사용자의 모든 저장 데이터를 삭제하고 게임을 종료
        /// </summary>
        private void DeleteAllUserData()
        {
            Debug.Log("=== 계정 삭제 시작 ===");
            
            // GPGS 로그아웃 처리
            PerformGPGSLogout();
            
            // 게임 상태 데이터 삭제
            GameState.Delete(); // 모든 게임 모드의 상태 삭제
            
            // 사용자 점수 및 리소스 데이터 삭제
            var resourceManager = ResourceManager.instance;
            if (resourceManager != null)
            {
                // 모든 리소스를 기본값으로 초기화 (점수, 코인 등)
                foreach (var resource in resourceManager.Resources)
                {
                    if (resource != null)
                    {
                        resource.Set(resource.DefaultValue);
                    }
                }
            }
            
            // 튜토리얼 데이터 삭제
            PlayerPrefs.DeleteKey("tutorial");
            
            // Privacy Policy/Terms of Service 동의 내역 삭제
            PlayerPrefs.DeleteKey("privacy_terms_agreed");
            
            // 언어 설정 초기화 (기기 언어로 재설정)
            PlayerPrefs.DeleteKey("SelectedLanguage");
            
            // 게임 진행 데이터 삭제
            PlayerPrefs.DeleteKey("Level"); // 현재 레벨
            PlayerPrefs.DeleteKey("LastPlayedMode"); // 마지막 플레이 모드
            PlayerPrefs.DeleteKey("DailyBonusDay"); // 일일 보너스 날짜
            
            // 게임 설정 데이터 삭제 (완전한 신규 유저 상태)
            PlayerPrefs.DeleteKey("VibrationLevel"); // 진동 설정
            
            // HighScoreService 백업 키들 삭제 (최고 점수 완전 초기화)
            PlayerPrefs.DeleteKey("ScoreBackup"); // Classic 모드 최고 점수 백업
            PlayerPrefs.DeleteKey("TimedBestScoreBackup"); // Timed 모드 최고 점수 백업
            
            // 게임 모드 관련 데이터 삭제
            PlayerPrefs.DeleteKey("GameMode"); // 현재 게임 모드
            PlayerPrefs.DeleteKey("PostTutorialGameMode"); // 튜토리얼 후 게임 모드
            
            // 추가적인 사용자 데이터 키들 삭제
            // (향후 추가되는 사용자 데이터가 있다면 여기에 추가)
            
            // 변경사항 저장
            PlayerPrefs.Save();
            
            // 추가적인 데이터 삭제 로직이 필요한 경우 여기에 추가
            // 예: 로컬 파일 삭제, 서버 데이터 삭제 요청 등
            
            Debug.Log("=== 계정 삭제 완료 ===");
            Debug.Log("모든 사용자 데이터가 삭제되었습니다. 완전한 신규 유저 상태로 초기화되었습니다.");
            Debug.Log("다음 실행 시 신규 유저로 시작됩니다.");
            
            // 게임 종료
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        /// <summary>
        /// GPGS 로그아웃 처리 (GPGS 11.01 호환 버전)
        /// </summary>
        private void PerformGPGSLogout()
        {
            Debug.Log("=== GPGS 로그아웃 처리 시작 ===");
            
            // 계정 삭제를 위한 로그아웃 처리
            try
            {
                Debug.Log("GPGS 11.01 - 계정 삭제를 위한 로컬 상태 초기화");
                Debug.Log("Privacy Terms 삭제와 함께 완전한 신규 유저 상태로 전환");
                
                // GPGSLoginManager를 통한 로그아웃 처리
                var gpgsManager = GPGSLoginManager.instance;
                if (gpgsManager != null)
                {
                    Debug.Log("GPGSLoginManager 인스턴스 발견 - 로그아웃 처리");
                    gpgsManager.Logout();
                }
                else
                {
                    // FindObjectOfType으로 다시 시도
                    var foundManager = FindObjectOfType<GPGSLoginManager>();
                    if (foundManager != null)
                    {
                        Debug.Log("FindObjectOfType으로 GPGSLoginManager 발견 - 로그아웃");
                        foundManager.Logout();
                    }
                    else
                    {
                        Debug.LogWarning("GPGSLoginManager를 찾을 수 없습니다");
                    }
                }
                
                // Social.localUser 상태 확인
                Debug.Log($"로그아웃 후 Social.localUser.authenticated: {Social.localUser.authenticated}");
                
            }
            catch (global::System.Exception e)
            {
                Debug.LogError($"GPGS 로그아웃 중 예외 발생: {e.Message}");
            }
            
            Debug.Log("=== GPGS 로그아웃 처리 완료 ===");
            Debug.Log("Privacy Terms 초기화와 함께 다음 앱 실행 시 신규 유저로 시작됩니다.");
        }
        
        /// <summary>
        /// 실패 시 announcement_Popup을 3초간 표시
        /// </summary>
        private void ShowAnnouncementPopup()
        {
            Debug.Log("ShowAnnouncementPopup 호출됨");
            
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
                announcementPopup.ShowMessage(LOCALIZED_KEY);
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
                    localizedText.instanceID = LOCALIZED_KEY;
                    
                    Debug.Log($"설정 후 instanceID: '{localizedText.instanceID}'");
                    
                    // 텍스트 업데이트 호출
                    localizedText.UpdateText();
                    
                    Debug.Log($"UpdateText() 호출 후 text: '{localizedText.text}'");
                    Debug.Log($"✅ LocalizedTextMeshProUGUI의 instanceID를 '{LOCALIZED_KEY}'로 설정 완료");
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
        

        
        void OnDestroy()
        {
            // 이벤트 리스너 해제
            if (deleteInputField != null)
            {
                deleteInputField.onValueChanged.RemoveListener(OnInputFieldChanged);
            }
            
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveListener(OnDeleteButtonClicked);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            }
        }
    }
}

