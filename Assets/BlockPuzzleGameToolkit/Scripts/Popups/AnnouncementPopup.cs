using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace BlockPuzzleGameToolkit.Scripts.Popups
{
    public class AnnouncementPopup : Popup
    {
        [Header("Display Settings")]
        [SerializeField] private float displayDuration = 3f;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private Ease appearEase = Ease.OutBack;
        [SerializeField] private Ease disappearEase = Ease.InBack;
        
        private string messageKey;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        
        protected override void Awake()
        {
            base.Awake();
            
            // 필요한 컴포넌트들 가져오기
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            // CanvasGroup이 없다면 추가
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // 원본 스케일 저장
            originalScale = rectTransform.localScale;
            
            // 초기 상태 설정 (보이지 않게)
            SetInitialState();
        }
        
        /// <summary>
        /// 초기 상태 설정 (애니메이션 시작 전)
        /// </summary>
        private void SetInitialState()
        {
            canvasGroup.alpha = 0f;
            rectTransform.localScale = Vector3.zero;
        }
        
        /// <summary>
        /// 메시지 키를 설정하고 팝업을 표시합니다
        /// </summary>
        /// <param name="key">로컬라이제이션 키</param>
        public void ShowMessage(string key)
        {
            messageKey = key;
            
            Debug.Log($"AnnouncementPopup ShowMessage 호출: {key} - LocalizedTextMeshProUGUI 컴포넌트가 자동으로 텍스트를 설정합니다.");
            
            // LocalizedTextMeshProUGUI 컴포넌트가 자동으로 텍스트를 처리하므로
            // SetLocalizedText() 호출하지 않음
            
            // 등장 애니메이션 실행
            PlayAppearAnimation();
            
            // 지정 시간 후 자동 닫기 시작
            StartCoroutine(AutoCloseAfterDuration());
        }
        
        /// <summary>
        /// 등장 애니메이션 실행
        /// </summary>
        private void PlayAppearAnimation()
        {
            // 모든 기존 애니메이션 중단
            transform.DOKill();
            canvasGroup.DOKill();
            
            // 초기 상태로 설정
            SetInitialState();
            
            Debug.Log("AnnouncementPopup 등장 애니메이션 시작");
            
            // Scale 애니메이션 (작은 크기에서 원본 크기로)
            rectTransform.DOScale(originalScale, animationDuration)
                .SetEase(appearEase)
                .SetUpdate(true); // Time.timeScale에 영향받지 않음
            
            // Alpha 애니메이션 (투명에서 불투명으로)
            canvasGroup.DOFade(1f, animationDuration * 0.8f) // 스케일보다 빠르게
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        
        /// <summary>
        /// 지정된 시간 후 자동으로 팝업을 닫습니다
        /// </summary>
        private IEnumerator AutoCloseAfterDuration()
        {
            Debug.Log($"AnnouncementPopup {displayDuration}초 후 자동 닫기 예정");
            yield return new WaitForSeconds(displayDuration);
            
            Debug.Log("AnnouncementPopup 사라짐 애니메이션 시작");
            PlayDisappearAnimation();
        }
        
        /// <summary>
        /// 사라짐 애니메이션 실행 후 팝업 닫기
        /// </summary>
        private void PlayDisappearAnimation()
        {
            // 모든 기존 애니메이션 중단
            transform.DOKill();
            canvasGroup.DOKill();
            
            // Scale 애니메이션 (원본 크기에서 작은 크기로)
            rectTransform.DOScale(Vector3.zero, animationDuration)
                .SetEase(disappearEase)
                .SetUpdate(true)
                .OnComplete(() => {
                    Debug.Log("AnnouncementPopup 사라짐 애니메이션 완료 - Close() 호출");
                    Close();
                });
            
            // Alpha 애니메이션 (불투명에서 투명으로)
            canvasGroup.DOFade(0f, animationDuration * 0.6f) // 스케일보다 빠르게 사라짐
                .SetEase(Ease.InQuad)
                .SetUpdate(true);
        }
        
        /// <summary>
        /// 팝업이 강제로 닫힐 때 애니메이션 정리
        /// </summary>
        public override void Close()
        {
            // 실행 중인 모든 DOTween 애니메이션 중단
            transform.DOKill();
            canvasGroup.DOKill();
            
            base.Close();
        }
        
        /// <summary>
        /// 오브젝트 파괴 시 DOTween 애니메이션 정리
        /// </summary>
        private void OnDestroy()
        {
            // 메모리 누수 방지를 위해 모든 DOTween 애니메이션 중단
            transform.DOKill();
            canvasGroup?.DOKill();
        }
    }
} 