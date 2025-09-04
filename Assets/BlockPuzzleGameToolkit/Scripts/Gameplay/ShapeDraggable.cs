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

using System.Collections.Generic;
using System.Linq;
using BlockPuzzleGameToolkit.Scripts.Audio;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.System.Haptic;
using BlockPuzzleGameToolkit.Scripts.Gameplay.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System;
using DG.Tweening;

namespace BlockPuzzleGameToolkit.Scripts.Gameplay
{
    public class ShapeDraggable : MonoBehaviour
    {
        public static event Action OnAnyShapeDragStart;
        public static event Action OnAnyShapeDragEnd;
        private static ShapeDraggable currentlyDragging = null; // 현재 드래그 중인 shape
        private RectTransform rectTransform;
        private Vector2 originalPosition;
        private Vector2 touchOffset;
        private readonly float verticalOffset = 300;
        private Vector3 originalScale;
        private Transform originalParent;
        private Vector3 originalWorldPosition;
        private CanvasGroup canvasGroup;
        private bool isDragging;
        private int activeTouchId = -1;
        private Canvas canvas;
        private Camera eventCamera;
        [Header("Audio")]
        [SerializeField] private AudioClip dragStartSound;
        private AudioSource audioSource;

        private Shape shape;
        private List<Item> _items = new();
        private HighlightManager highlightManager;
        private FieldManager field;
        private ItemFactory itemFactory;
        private VirtualMouseInput virtualMouseInput;
        private bool wasVirtualMousePressed = false;
        private TimerManager timerManager;
        private Tween moveTween;

        private void OnEnable()
        {
            itemFactory ??= FindObjectOfType<ItemFactory>();
            rectTransform = GetComponent<RectTransform>();
            shape = GetComponent<Shape>();
            shape.OnShapeUpdated += UpdateItems;
            UpdateItems();
            highlightManager ??= FindObjectOfType<HighlightManager>();
            field ??= FindObjectOfType<FieldManager>();
            timerManager ??= FindObjectOfType<TimerManager>();

            // Get canvas and camera reference
            canvas = GetComponentInParent<Canvas>();
            eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            
            // Initialize CanvasGroup for drag handling
            InitializeCanvasGroup();
            
            // Initialize AudioSource
            InitializeAudioSource();
            
            // Find virtual mouse if available
            virtualMouseInput ??= FindObjectOfType<VirtualMouseInput>();

            // Subscribe to events that can cancel dragging
            EventManager.GetEvent(EGameEvent.TimerExpired).Subscribe(CancelDragIfActive);
            EventManager.GetEvent(EGameEvent.LevelAboutToComplete).Subscribe(CancelDragIfActive);
            EventManager.GetEvent(EGameEvent.TutorialCompleted).Subscribe(CancelDragIfActive);
            EventManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            shape.OnShapeUpdated -= UpdateItems;
            EventManager.GetEvent(EGameEvent.TimerExpired).Unsubscribe(CancelDragIfActive);
            EventManager.GetEvent(EGameEvent.LevelAboutToComplete).Unsubscribe(CancelDragIfActive);
            EventManager.GetEvent(EGameEvent.TutorialCompleted).Unsubscribe(CancelDragIfActive);
            EventManager.OnGameStateChanged -= OnGameStateChanged;
            EndDrag();
        }

        private void OnGameStateChanged(EGameState newState)
        {
            // 게임 오버 연출/전환 중에는 드래그 금지 및 즉시 취소
            if (newState == EGameState.PreFailed || newState == EGameState.Failed ||
                newState == EGameState.PreWin || newState == EGameState.Win)
            {
                CancelDragIfActive();
            }
        }

        private void CancelDragIfActive()
        {
            if (isDragging)
            {
                CancelDragWithReturn();
            }
        }

        private void Update()
        {
            if (EventManager.GameStatus != EGameState.Playing && EventManager.GameStatus != EGameState.Tutorial)
            {
                return;
            }
            
            // Handle touch input with new Input System
            if (Touchscreen.current != null)
            {
                // Handle existing active touch
                if (isDragging && activeTouchId != -1)
                {
                    bool foundActiveTouch = false;
                    for (int i = 0; i < Touchscreen.current.touches.Count; i++)
                    {
                        var touch = Touchscreen.current.touches[i];
                        if (touch.touchId.ReadValue() == activeTouchId)
                        {
                            HandleDrag(touch.position.ReadValue());
                            foundActiveTouch = true;
                    
                            // Check if touch has ended
                            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended || 
                                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
                            {
                                EndDrag();
                            }
                            break;
                        }
                    }
            
                    if (!foundActiveTouch)
                    {
                        EndDrag();
                    }
                }
                // Check for new touches if not already dragging
                else if (!isDragging)
                {
                    for (int i = 0; i < Touchscreen.current.touches.Count; i++)
                    {
                        var touch = Touchscreen.current.touches[i];
                        if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                        {
                            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, touch.position.ReadValue(), eventCamera))
                            {
                                activeTouchId = touch.touchId.ReadValue();
                                BeginDrag(touch.position.ReadValue());
                                break;
                            }
                        }
                    }
                }
            }
            
            // Track virtual mouse state if available - works on ALL platforms
            bool virtualMouseHandled = false;
            bool isVirtualMousePressed = false;
            Vector2 virtualMousePosition = Vector2.zero;
            
            if (virtualMouseInput != null && virtualMouseInput.virtualMouse != null)
            {
                isVirtualMousePressed = virtualMouseInput.virtualMouse.leftButton.isPressed;
                virtualMousePosition = virtualMouseInput.virtualMouse.position.value;
                
                // Handle virtual mouse input
                if (activeTouchId == -1)
                {
                    // Virtual mouse button down this frame
                    if (isVirtualMousePressed && !wasVirtualMousePressed && !isDragging)
                    {
                        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, virtualMousePosition, eventCamera))
                        {
                            BeginDrag(virtualMousePosition);
                            virtualMouseHandled = true;
                        }
                    }
                    // Continue dragging with virtual mouse
                    else if (isVirtualMousePressed && isDragging)
                    {
                        HandleDrag(virtualMousePosition);
                        virtualMouseHandled = true;
                    }
                    // Release with virtual mouse
                    else if (!isVirtualMousePressed && wasVirtualMousePressed && isDragging)
                    {
                        EndDrag();
                        virtualMouseHandled = true;
                    }
                }
                
                wasVirtualMousePressed = isVirtualMousePressed;
            }
            
            // Handle regular mouse input using the new Input System if not already handled
            if (!virtualMouseHandled && activeTouchId == -1)
            {
                if (Mouse.current != null)
                {
                    if (Mouse.current.leftButton.wasPressedThisFrame && !isDragging)
                    {
                        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Mouse.current.position.ReadValue(), eventCamera))
                        {
                            BeginDrag(Mouse.current.position.ReadValue());
                        }
                    }
                    else if (Mouse.current.leftButton.isPressed && isDragging)
                    {
                        HandleDrag(Mouse.current.position.ReadValue());
                    }
                    else if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
                    {
                        EndDrag();
                    }
                }
            }

            // Additional safety check to ensure EndDrag is called if dragging unexpectedly stops
            if (isDragging && activeTouchId == -1 && 
                (Mouse.current == null || !Mouse.current.leftButton.isPressed) &&
                !(virtualMouseInput != null && virtualMouseInput.virtualMouse != null && 
                  virtualMouseInput.virtualMouse.leftButton.isPressed))
            {
                EndDrag();
            }
        }

        private void UpdateItems()
        {
            _items = shape.GetActiveItems();
        }

        private void BeginDrag(Vector2 position)
        {
            // 다른 shape가 이미 드래그 중이면 드래그를 시작하지 않음
            if (currentlyDragging != null && currentlyDragging != this)
            {
                return;
            }

            // 셀덱/필드 애니메이션 중이면 드래그 불가
            var fieldManager = FindObjectOfType<BlockPuzzleGameToolkit.Scripts.Gameplay.FieldManager>();
            if (fieldManager != null && fieldManager.IsAnimationPlaying)
                return;

            // 이전 복귀 트윈이 진행 중이면 먼저 완료하여 상태를 원복
            if (moveTween != null && moveTween.IsActive())
            {
                moveTween.Complete();
            }

            isDragging = true;
            currentlyDragging = this; // 현재 드래그 중인 shape로 설정
            originalScale = transform.localScale;
            originalParent = transform.parent;
            originalWorldPosition = transform.position;

            // SafeArea로 이동하여 자유롭게 드래그할 수 있도록 함
            var safeArea = FindSafeArea();
            if (safeArea != null)
            {
                transform.SetParent(safeArea);
                transform.SetAsLastSibling();
            }
            // SafeArea로 부모가 바뀐 후의 anchoredPosition을 복귀 기준으로 저장
            originalPosition = rectTransform.anchoredPosition;

            transform.localScale = Vector3.one;
            canvasGroup.blocksRaycasts = false; // 드래그 중에는 raycast 차단

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, position, eventCamera, out touchOffset);

            // 드래그 시작 효과음 재생
            PlayDragStartSound();

            // 튜토리얼용 드래그 시작 이벤트 호출
            OnAnyShapeDragStart?.Invoke();
        }

        private void CancelDragWithReturn()
        {
            // 기존 트윈이 있으면 중지
            moveTween?.Kill();
            // 더 빠른 부드러운 원래 위치로 복귀
            moveTween = rectTransform.DOAnchorPos(originalPosition, 0.1f).SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    transform.localScale = originalScale;
                    // 원래 부모로 복원
                    if (originalParent != null)
                    {
                        transform.SetParent(originalParent);
                        transform.position = originalWorldPosition;
                        originalParent = null;
                    }
                    canvasGroup.blocksRaycasts = true; // raycast 다시 활성화
                    highlightManager.ClearAllHighlights();
                    highlightManager.OnDragEndedWithoutPlacement();
                    isDragging = false;
                    // 현재 드래그 중인 shape 초기화
                    if (currentlyDragging == this)
                    {
                        currentlyDragging = null;
                    }
                });
        }

        private void HandleDrag(Vector2 position)
        {
            if (!isDragging)
            {
                return;
            }

            var cellSize = field.GetCellSize();
            var shapeOriginalWidth = 126f;
            var scaleFactor = cellSize / shapeOriginalWidth;

            transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform.parent as RectTransform, position, eventCamera, out var localPoint))
            {
                var targetPosition = localPoint - touchOffset;
                targetPosition.y += verticalOffset; // 손가락보다 위로 보정

                // 기존 트윈이 있으면 중지
                moveTween?.Kill();
                // 더 빠른 부드러운 이동
                moveTween = rectTransform.DOAnchorPos(targetPosition, 0.05f).SetEase(Ease.OutQuad);
            }

            if (AnyBusyCellsOrNoneCells())
            {
                if (IsDistancesToHighlightedCellsTooHigh())
                {
                    highlightManager.ClearAllHighlights();
                }

                return;
            }

            UpdateCellHighlights();
        }

        private void EndDrag()
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            activeTouchId = -1;

            // 튜토리얼용 드래그 종료 이벤트 호출
            OnAnyShapeDragEnd?.Invoke();

            if (highlightManager.GetHighlightedCells().Count == 0)
            {
                // 기존 트윈이 있으면 중지
                moveTween?.Kill();
                // 더 빠른 부드러운 원래 위치로 복귀
                moveTween = rectTransform.DOAnchorPos(originalPosition, 0.1f).SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        transform.localScale = originalScale;
                        // 원래 부모로 복원
                        if (originalParent != null)
                        {
                            transform.SetParent(originalParent);
                            transform.position = originalWorldPosition;
                            originalParent = null;
                        }
                        canvasGroup.blocksRaycasts = true; // raycast 다시 활성화
                        highlightManager.ClearAllHighlights();
                        highlightManager.OnDragEndedWithoutPlacement();
                        // 현재 드래그 중인 shape 초기화
                        if (currentlyDragging == this)
                        {
                            currentlyDragging = null;
                        }
                    });
                return;
            }

            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticForce.Light);
            SoundBase.instance.PlaySound(SoundBase.instance.placeShape);

            foreach (var kvp in highlightManager.GetHighlightedCells())
            {
                kvp.Key.FillCell(kvp.Value.itemTemplate);
                kvp.Key.AnimateFill();
                if (kvp.Value.bonusItemTemplate != null)
                {
                    kvp.Key.SetBonus(kvp.Value.bonusItemTemplate);
                }
            }

            EventManager.GetEvent<Shape>(EGameEvent.ShapePlaced).Invoke(shape);
            
            // 현재 드래그 중인 shape 초기화
            if (currentlyDragging == this)
            {
                currentlyDragging = null;
            }
        }

        private bool IsDistancesToHighlightedCellsTooHigh()
        {
            var firstOrDefault = highlightManager.GetHighlightedCells().FirstOrDefault();
            return firstOrDefault.Key != null &&
                   Vector3.Distance(_items[0].transform.position, firstOrDefault.Key.transform.position) > 1f;
        }

        private bool AnyBusyCellsOrNoneCells()
        {
            return _items.Any(item =>
            {
                var cell = GetCellUnderShape(item);
                var cellComponent = cell?.GetComponent<Cell>();
                return cell == null || !cellComponent.IsEmpty() || cellComponent.IsDestroying();
            });
        }

        private void UpdateCellHighlights()
        {
            highlightManager.ClearAllHighlights();

            foreach (var item in _items)
            {
                var cell = GetCellUnderShape(item);
                if (cell != null)
                {
                    highlightManager.HighlightCell(cell, item);
                }
            }

            if (itemFactory._oneColorMode)
            {
                highlightManager.HighlightFill(field.GetFilledLines(true), itemFactory.GetColor());
            }
            else
            {
                highlightManager.HighlightFill(field.GetFilledLines(true), _items[0].itemTemplate);
            }
        }

        private Transform GetCellUnderShape(Item item)
        {
            var hit = Physics2D.Raycast(item.transform.position, Vector2.zero, 1);
            return hit.collider != null && hit.collider.CompareTag("Cell") ? hit.collider.transform : null;
        }

        private void InitializeCanvasGroup()
        {
            // CanvasGroup 컴포넌트가 없으면 추가
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private Transform FindSafeArea()
        {
            // ItemsCanvas/SafeArea를 찾기 위해 씬에서 검색
            var itemsCanvas = GameObject.Find("ItemsCanvas");
            if (itemsCanvas != null)
            {
                var safeArea = itemsCanvas.transform.Find("SafeArea");
                if (safeArea != null)
                {
                    return safeArea;
                }
            }
            
            // SafeArea가 없으면 현재 Canvas를 사용
            return canvas.transform;
        }

        private void InitializeAudioSource()
        {
            // 로컬 AudioSource 초기화는 유지하되, 전역 SoundBase를 우선 사용
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.volume = 0.0f; // 로컬 소스는 직접 재생하지 않음
            audioSource.pitch = 1.0f;
        }

        private void PlayDragStartSound()
        {
            if (dragStartSound == null)
            {
                return;
            }
            // 전역 사운드 설정을 따르도록 중앙 사운드 매니저 사용
            SoundBase.instance.PlaySound(dragStartSound);
        }
    }
}