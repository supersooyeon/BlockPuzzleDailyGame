// ©2015 - 2025 Candy Smith
// All rights reserved
// Redistribution of this software is strictly not allowed.
// Copy of this software can be obtained from unity asset store only.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using BlockPuzzleGameToolkit.Scripts.Popups;
using UnityEngine;
using System.Collections;

namespace BlockPuzzleGameToolkit.Scripts.Services.GDPR
{
    /// <summary>
    /// PrivacyTermsPopup 관리자 - 신규 유저 최초 실행 시 개인정보 처리방침 팝업 표시
    /// </summary>
    public class PrivacyTermsPopupManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float showDelay = 0.1f; // 즉시 표시를 위해 매우 짧은 지연
        
        // 전역에서 접근 가능한 Privacy Terms 상태
        public static bool IsPrivacyTermsActive { get; private set; } = false;
        
        private static PrivacyTermsPopupManager instance;
        
        private void Awake()
        {
            Debug.Log("=== PrivacyTermsPopupManager.Awake() 시작 ===");
            
            // 싱글톤 설정
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("PrivacyTermsPopupManager: 싱글톤 인스턴스 설정 완료");
            }
            else
            {
                Debug.Log("PrivacyTermsPopupManager: 중복 인스턴스 발견 - 파괴");
                Destroy(gameObject);
                return;
            }
            
            Debug.Log($"PrivacyTermsPopupManager: IsPrivacyTermsAgreed = {PrivacyTermsPopup.IsPrivacyTermsAgreed()}");
            
            // 이미 Privacy Terms에 동의한 경우 팝업 표시 없이 바로 게임 모드 진입
            if (PrivacyTermsPopup.IsPrivacyTermsAgreed())
            {
                Debug.Log("PrivacyTermsPopupManager: 이미 Privacy Terms에 동의함 - 팝업 표시 없이 바로 게임 모드 진입");
                StartCoroutine(StartClassicModeAfterPrivacyTerms());
                Debug.Log("PrivacyTermsPopupManager: StartClassicModeAfterPrivacyTerms() 호출 완료");
                // 이미 동의한 유저는 PrivacyTermsPopupManager를 완전히 비활성화
                gameObject.SetActive(false);
                Debug.Log("PrivacyTermsPopupManager: 매니저 비활성화 완료");
                return;
            }
            
            // 즉시 Privacy Terms 팝업 표시 체크
            Debug.Log($"PrivacyTermsPopupManager: ShowPrivacyTermsPopup() {showDelay}초 후 호출 예약");
            Invoke(nameof(ShowPrivacyTermsPopup), showDelay);
            Debug.Log("=== PrivacyTermsPopupManager.Awake() 완료 ===");
        }

        private void ShowPrivacyTermsPopup()
        {
            Debug.Log("=== ShowPrivacyTermsPopup() 호출됨 ===");
            Debug.Log($"ShowPrivacyTermsPopup: IsPrivacyTermsAgreed = {PrivacyTermsPopup.IsPrivacyTermsAgreed()}");
            
            // 사용자가 이미 Privacy Terms에 동의했는지 확인
            if (!PrivacyTermsPopup.IsPrivacyTermsAgreed())
            {
                Debug.Log("ShowPrivacyTermsPopup: Privacy Terms 미동의 - 팝업 표시");
                // 신규 유저: Privacy Terms 활성화 상태로 설정
                SetPrivacyTermsActive(true);
                
                // 아직 동의하지 않았다면 PrivacyTermsPopup 표시
                if (MenuManager.instance != null)
                {
                    Debug.Log("ShowPrivacyTermsPopup: MenuManager.instance.ShowPopup<PrivacyTermsPopup>() 호출");
                    var popup = MenuManager.instance.ShowPopup<PrivacyTermsPopup>();
                    if (popup != null)
                    {
                        Debug.Log("ShowPrivacyTermsPopup: PrivacyTermsPopup 생성 성공, onClose 콜백 등록");
                        // 팝업이 닫힐 때 콜백 등록
                        popup.onClose.AddListener(OnPrivacyTermsPopupClosed);
                    }
                    else
                    {
                        Debug.LogError("ShowPrivacyTermsPopup: PrivacyTermsPopup 생성 실패");
                    }
                }
                else
                {
                    Debug.LogError("ShowPrivacyTermsPopup: MenuManager.instance가 null");
                }
            }
            else
            {
                Debug.Log("ShowPrivacyTermsPopup: Privacy Terms 이미 동의함 - StartGPGSLogin() 호출");
                // 이미 Privacy Terms에 동의했다면 바로 GPGS 로그인 시작
                StartGPGSLogin();
            }
            Debug.Log("=== ShowPrivacyTermsPopup() 완료 ===");
        }

        /// <summary>
        /// Privacy Terms 활성화 상태 설정
        /// </summary>
        private void SetPrivacyTermsActive(bool active)
        {
            IsPrivacyTermsActive = active;
            Debug.Log($"Privacy Terms Active: {active}");
        }

        /// <summary>
        /// PrivacyTermsPopup이 닫힐 때 호출되는 콜백
        /// </summary>
        private void OnPrivacyTermsPopupClosed()
        {
            // Privacy Terms 비활성화
            SetPrivacyTermsActive(false);
            
            // 동의가 완료되었다면 GPGS 로그인 시작
            if (PrivacyTermsPopup.IsPrivacyTermsAgreed())
            {
                StartGPGSLogin();
                
                // Privacy Terms 동의 후 클래식 모드로 진입
                StartCoroutine(StartClassicModeAfterPrivacyTerms());
            }
        }

        private void StartGPGSLogin()
        {
            Debug.Log("=== StartGPGSLogin() 호출됨 ===");
            // Privacy Terms 동의 후 바로 GPGS 로그인 시작
            // TODO: 실제 GPGS 로그인 로직을 여기에 추가하세요
            // 예: GameManager.instance.StartGPGSLogin();
            // 또는 GooglePlayGamesManager.instance.Login();
            Debug.Log("Starting GPGS Login from PrivacyTermsPopupManager");
            
            // 기존 유저가 이미 Privacy Terms에 동의한 경우에도 적절한 게임 모드로 진입
            Debug.Log("StartGPGSLogin: StartClassicModeAfterPrivacyTerms() 호출");
            StartCoroutine(StartClassicModeAfterPrivacyTerms());
            Debug.Log("=== StartGPGSLogin() 완료 ===");
        }

        // 개발자용 메서드 - Privacy Terms 동의 상태 리셋
        [ContextMenu("Reset Privacy Terms Agreement")]
        public void ResetPrivacyTermsAgreement()
        {
            PrivacyTermsPopup.ResetPrivacyTermsAgreement();
            Debug.Log("Privacy Terms agreement has been reset");
        }

        // 개발자용 메서드 - Privacy Terms 동의 상태 확인
        [ContextMenu("Check Privacy Terms Agreement Status")]
        public void CheckPrivacyTermsAgreementStatus()
        {
            bool isAgreed = PrivacyTermsPopup.IsPrivacyTermsAgreed();
            Debug.Log($"Privacy Terms Agreement Status: {(isAgreed ? "Agreed" : "Not Agreed")}");
        }

        // 개발자용 메서드 - Privacy Terms 상태 강제 해제 (긴급용)
        [ContextMenu("Force Disable Privacy Terms")]
        public void ForceDisablePrivacyTerms()
        {
            SetPrivacyTermsActive(false);
            Debug.Log("Privacy Terms forcefully disabled");
        }

        // 개발자용 메서드 - Privacy Terms 상태 체크
        [ContextMenu("Check Privacy Terms Status")]
        public void CheckPrivacyTermsStatus()
        {
            Debug.Log($"IsPrivacyTermsActive: {IsPrivacyTermsActive}");
            Debug.Log($"IsPrivacyTermsAgreed: {PrivacyTermsPopup.IsPrivacyTermsAgreed()}");
        }

        private void OnDestroy()
        {
            // 매니저가 파괴될 때 상태 리셋
            if (IsPrivacyTermsActive)
            {
                SetPrivacyTermsActive(false);
                Debug.Log("PrivacyTermsPopupManager destroyed - Privacy Terms state reset");
            }
        }

        private void OnDisable()
        {
            // 매니저가 비활성화될 때 모든 Invoke 취소
            CancelInvoke();
            Debug.Log("PrivacyTermsPopupManager disabled - All Invoke calls cancelled");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // 앱이 백그라운드로 갈 때/돌아올 때 안전장치
            if (!pauseStatus && IsPrivacyTermsActive)
            {
                // 앱이 다시 포그라운드로 왔는데 여전히 활성화 상태라면
                Debug.Log("App returned to foreground - Checking Privacy Terms popup state");
                
                // 팝업이 실제로 활성화되어 있는지 확인
                var popup = FindObjectOfType<PrivacyTermsPopup>();
                if (popup == null)
                {
                    // 팝업이 없다면 상태 리셋
                    SetPrivacyTermsActive(false);
                    Debug.Log("Privacy Terms popup not found - Resetting state");
                }
            }
        }
        
        /// <summary>
        /// Privacy Terms 동의 후 게임 모드로 진입하는 코루틴
        /// </summary>
        private IEnumerator StartClassicModeAfterPrivacyTerms()
        {
            Debug.Log("=== StartClassicModeAfterPrivacyTerms() 시작 ===");
            // 프레임 한 번 대기 (UI 안정성을 위해)
            yield return null;
            
            var gameManager = FindObjectOfType<BlockPuzzleGameToolkit.Scripts.System.GameManager>();
            if (gameManager != null)
            {
                Debug.Log($"StartClassicModeAfterPrivacyTerms: PostTutorialGameMode 키 존재 = {PlayerPrefs.HasKey("PostTutorialGameMode")}");
                Debug.Log($"StartClassicModeAfterPrivacyTerms: IsTutorialShown = {gameManager.IsTutorialShown()}");
                
                // 신규 유저 vs 기존 유저 튜토리얼 미완료 상황 구분
                if (!PlayerPrefs.HasKey("PostTutorialGameMode"))
                {
                    // 신규 유저: 튜토리얼 모드로 진입
                    Debug.Log("StartClassicModeAfterPrivacyTerms: 신규 유저, 튜토리얼 모드로 진입");
                    gameManager.SetTutorialMode(true);
                    gameManager.OpenGame();
                }
                else if (!gameManager.IsTutorialShown())
                {
                    // 기존 유저 튜토리얼 미완료: 튜토리얼 모드로 진입
                    Debug.Log("StartClassicModeAfterPrivacyTerms: 기존 유저 튜토리얼 미완료, 튜토리얼 모드로 진입");
                    gameManager.SetTutorialMode(true);
                    gameManager.OpenGame();
                }
                else
                {
                    // 기존 유저 튜토리얼 완료: 메인 화면으로 이동
                    Debug.Log("StartClassicModeAfterPrivacyTerms: 기존 유저 튜토리얼 완료, 메인 화면으로 이동");
                    gameManager.SetGameMode(BlockPuzzleGameToolkit.Scripts.Enums.EGameMode.Classic);
                    // 메인 화면으로 이동 (게임 씬이 아닌 메인 메뉴 씬)
                    gameManager.MainMenu();
                }
            }
            else
            {
                Debug.LogError("StartClassicModeAfterPrivacyTerms: GameManager를 찾을 수 없습니다.");
            }
            Debug.Log("=== StartClassicModeAfterPrivacyTerms() 완료 ===");
        }
    }
} 