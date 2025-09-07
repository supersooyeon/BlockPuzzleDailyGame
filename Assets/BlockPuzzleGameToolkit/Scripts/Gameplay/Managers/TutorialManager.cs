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

using System.Collections;
using System.Linq;
using System.Reflection;
using BlockPuzzleGameToolkit.Scripts.Enums;
using BlockPuzzleGameToolkit.Scripts.Gameplay.FX;
using BlockPuzzleGameToolkit.Scripts.GUI;
using BlockPuzzleGameToolkit.Scripts.LevelsData;
using BlockPuzzleGameToolkit.Scripts.Settings;
using BlockPuzzleGameToolkit.Scripts.System;
using BlockPuzzleGameToolkit.Scripts.Utils;
using UnityEngine;
using BlockPuzzleGameToolkit.Scripts.Gameplay;

namespace BlockPuzzleGameToolkit.Scripts.Gameplay.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        private const float offsethand = .5f;

        [SerializeField]
        private TutorialSettings tutorialSettings;

        [SerializeField]
        private CellDeckManager cellDeckManager;

        [SerializeField]
        private ItemFactory itemFactory;

        [SerializeField]
        private LevelManager levelManager;

        [SerializeField]
        private Transform handSprite;

        [SerializeField]
        private GameObject uiObject; // 튜토리얼 중에 비활성화할 UI 오브젝트

        private ShapeTemplate[] tutorialShapesQueue;
        public int currentPhase;

        // public Outline outline;

        private Vector3 deckPosition;
        private Vector3 centerPosition;

        public bool IsTutorialActive { get; private set; }

        private Coroutine handAnimationCoroutine;
        private bool subscribed;

        // 현재 Level이 튜토리얼 레벨인지 확인하는 메서드
        private bool IsCurrentLevelTutorial()
        {
            if (levelManager == null || tutorialSettings == null) return false;
            var currentLevel = levelManager.GetCurrentLevel();
            return currentLevel != null && tutorialSettings.tutorialLevels.Contains(currentLevel);
        }

        private void OnEnable()
        {
            if (handSprite != null)
                handSprite.gameObject.SetActive(false); // 항상 먼저 꺼줌

            if (IsCurrentLevelTutorial())
            {
                subscribed = true;
                EventManager.GetEvent<Shape>(EGameEvent.ShapePlaced).Subscribe(OnShapePlaced);
                EventManager.GetEvent<Shape>(EGameEvent.LineDestroyed).Subscribe(OnLineDestroyed);
                
                // 튜토리얼 레벨일 때 UI 비활성화
                SetUIObjectActive(false);
            }
            // Shape 드래그 이벤트 구독
            ShapeDraggable.OnAnyShapeDragStart += HandleShapeDragStart;
            ShapeDraggable.OnAnyShapeDragEnd += HandleShapeDragEnd;
        }

        private void OnDisable()
        {
            if (!subscribed)
            {
                return;
            }

            EventManager.GetEvent<Shape>(EGameEvent.ShapePlaced).Unsubscribe(OnShapePlaced);
            EventManager.GetEvent<Shape>(EGameEvent.LineDestroyed).Unsubscribe(OnLineDestroyed);
            // Shape 드래그 이벤트 구독 해제
            ShapeDraggable.OnAnyShapeDragStart -= HandleShapeDragStart;
            ShapeDraggable.OnAnyShapeDragEnd -= HandleShapeDragEnd;
        }

        public void StartTutorial()
        {
            IsTutorialActive = true;
            FillCellDecks();
            StartCoroutine(DelayedBoundsCalculation());
            
            // 튜토리얼 시작 시 UI 비활성화
            SetUIObjectActive(false);
        }

        private void SetUIObjectActive(bool active)
        {
            if (uiObject != null)
            {
                uiObject.SetActive(active);
                Debug.Log($"[TutorialManager] UI Object {(active ? "활성화" : "비활성화")}");
            }
        }

        private void FillCellDecks()
        {
            if (cellDeckManager == null || tutorialSettings == null)
            {
                Debug.LogError("Tutorial: cellDeckManager or tutorialSettings is null");
                return;
            }

            // 튜토리얼 모드일 때는 한 개의 Shape만 사용
            tutorialShapesQueue = tutorialSettings.tutorialShapes
                .Skip(currentPhase).Take(1).ToArray();
            
            Debug.Log($"Tutorial: FillCellDecks - currentPhase: {currentPhase}, shapes loaded: {tutorialShapesQueue.Length}");
            
            cellDeckManager.ClearCellDecks();
            cellDeckManager.FillCellDecksWithShapes(tutorialShapesQueue);
        }

        public void EndTutorial()
        {
            StartCoroutine(EndTutorialCoroutine());
        }

        private IEnumerator EndTutorialCoroutine()
        {
            IsTutorialActive = false;
            GameManager.instance.SetTutorialCompleted();
            StopHandAnimation();
            // outline.gameObject.SetActive(false);
            GameManager.instance.SetTutorialMode(false);
            
            // Classic 모드로 명시적으로 설정
            GameDataManager.SetGameMode(EGameMode.Classic);
            // PostTutorialGameMode도 설정하여 기존 유저로 표시
            GameDataManager.SetPostTutorialGameMode(EGameMode.Classic);
            Debug.Log("Tutorial: Game mode set to Classic, PostTutorialGameMode also set");
            
            // 튜토리얼 종료 시 UI 다시 활성화
            SetUIObjectActive(true);
            
            // LevelManager에 튜토리얼에서 전환되는 상황을 알림 (중복 호출 제거)
            // var levelManager = FindObjectOfType<LevelManager>();
            // if (levelManager != null)
            // {
            //     levelManager.SetTransitioningFromTutorial();
            // }
            
            // Trigger tutorial completed event before restarting level
            EventManager.GetEvent(EGameEvent.TutorialCompleted).Invoke();
            
            // 모든 라인 제거 애니메이션 실행 및 대기
            var fieldManager = FindObjectOfType<FieldManager>();
            if (fieldManager != null)
            {
                yield return fieldManager.PlayAllLinesClearEffectWithSound();
            }
            
            // ClassicLevel로 전환하기 전에 TargetPanel 활성화
            var targetManager = FindObjectOfType<TargetManager>();
            if (targetManager != null && targetManager.targetPanel != null && targetManager.targetParent != null)
            {
                // 기존 TargetPanel이 있다면 제거
                var existingTargetPanel = FindObjectOfType<TargetsUIHandler>();
                if (existingTargetPanel != null)
                {
                    Destroy(existingTargetPanel.gameObject);
                }
                
                // ClassicLevel 데이터 로드
                var classicLevel = Resources.Load<Level>("Misc/ClassicLevel");
                if (classicLevel != null)
                {
                    // 새로운 TargetPanel 생성
                    var newTargetPanel = Instantiate(targetManager.targetPanel, targetManager.targetParent);
                    
                    // 게임 모드 확인
                    Debug.Log($"Tutorial: Current game mode: {GameDataManager.GetGameMode()}, Level type: {classicLevel.levelType.elevelType}");
                    
                    newTargetPanel.OnLevelLoaded(classicLevel.levelType.elevelType);
                    
                    // OnLevelLoaded가 자동으로 ClassicModeLabel을 활성화함
                    Debug.Log("Tutorial: TargetPanel OnLevelLoaded called - ClassicModeLabel should be activated automatically");
                    
                    Debug.Log("Tutorial: TargetPanel activated for Classic mode");
                }
                else
                {
                    Debug.LogError("Tutorial: ClassicLevel.asset not found in Resources/Misc");
                }
            }
            
            // 애니메이션 완료 후 ClassicLevel로 전환
            GameManager.instance.RestartLevel();
            
            // RestartLevel 후 ClassicModeHandler 초기화 (UI가 다시 생성될 수 있으므로)
            StartCoroutine(DelayedClassicModeHandlerInit());
            
            gameObject.SetActive(false);

            // 튜토리얼 모드가 아니면 모든 CellDeck 활성화
            if (cellDeckManager != null && !GameManager.instance.IsTutorialMode())
            {
                foreach (var deck in cellDeckManager.cellDecks)
                {
                    deck.gameObject.SetActive(true);
                }
            }
        }

        private void OnShapePlaced(Shape obj)
        {
            StopHandAnimation();
            // 튜토리얼 모드에서는 Shape를 추가하지 않음 - 하나의 Shape만 유지
            // if (tutorialShapesQueue.Length > 0)
            // {
            //     cellDeckManager.AddShapeToFreeCell(tutorialShapesQueue[0]);
            // }
            StopHandAnimation();
        }

        private void OnLineDestroyed(Shape obj)
        {
            Debug.Log($"Tutorial: Line destroyed, currentPhase is {currentPhase}");
            StartCoroutine(DelayedNextPhase());
        }

        private IEnumerator DelayedNextPhase()
        {
            yield return new WaitForSeconds(0.5f);
            
            // currentPhase 증가
            currentPhase++;
            Debug.Log($"Tutorial: DelayedNextPhase - currentPhase increased to {currentPhase}");
            
            // 마지막 튜토리얼 레벨에서만 EndTutorial 실행
            if (currentPhase < tutorialSettings.tutorialLevels.Length)
            {
                CheckPhase(currentPhase);
            }
            else
            {
                // 튜토리얼 마지막 페이즈: DestroyLines 전에 플래그를 true로 설정
                var levelManager = FindObjectOfType<LevelManager>();
                if (levelManager != null)
                {
                    levelManager.SetTransitioningFromTutorial();
                }
                EndTutorial();
            }
        }

        public void CheckPhase(int phase)
        {
            Debug.Log($"Tutorial: CheckPhase called with phase {phase}, currentPhase is {currentPhase}");
            if (phase > 0)
            {
                // 튜토리얼 모드에서는 게임 오버 체크 없이 다음 페이즈로 진행
                // currentPhase는 이미 OnLineDestroyed에서 증가했으므로 다시 설정하지 않음
                Debug.Log($"Tutorial: Proceeding to next phase, loading shapes for phase {currentPhase}");
                
                // 새로운 튜토리얼 레벨 로드
                var levelManager = FindObjectOfType<LevelManager>();
                if (levelManager != null)
                {
                    Debug.Log($"Tutorial: Loading new level for phase {currentPhase}");
                    levelManager.Load();
                }
                else
                {
                    Debug.LogError("Tutorial: LevelManager not found");
                }
            }
        }

        private IEnumerator DelayedBoundsCalculation()
        {
            yield return new WaitForSeconds(0.1f);
            
            // 튜토리얼 모드에서는 첫 번째 CellDeck만 활성화되어 있으므로 [0] 인덱스 사용
            if (cellDeckManager.cellDecks.Length > 0 && cellDeckManager.cellDecks[0].shape != null)
            {
                deckPosition = cellDeckManager.cellDecks[0].shape.GetActiveItems()[0].transform.position + Vector3.right * offsethand;
                var fieldManager = FindObjectOfType<FieldManager>();
                if (fieldManager != null)
                {
                    centerPosition = fieldManager.GetCenterCell().item.transform.position + Vector3.right * offsethand + Vector3.down * offsethand;
                    StartHandAnimation();
                }
            }
            // outline.gameObject.SetActive(true);
            // var value = RectTransformUtils.GetMinMaxAndSizeForCanvas(FindObjectOfType<FieldManager>().GetTutorialLine(), transform.parent.GetComponent<Canvas>());
            // value.size += new Vector2(50, 50);
            // Color hexColor;
            // if (ColorUtility.TryParseHtmlString("#609FFF", out hexColor))
            // {
            //     outline.Play(value.center, value.size, hexColor);
            // }
        }

        public Level GetLevelForPhase()
        {
            return tutorialSettings.tutorialLevels[currentPhase];
        }

        private void StartHandAnimation()
        {
            if (!IsCurrentLevelTutorial() || handSprite == null) return;
            handSprite.gameObject.SetActive(true);
            if (handAnimationCoroutine != null)
            {
                StopCoroutine(handAnimationCoroutine);
            }

            handAnimationCoroutine = StartCoroutine(HandAnimationCoroutine());
        }

        private IEnumerator HandAnimationCoroutine()
        {
            while (true)
            {
                if (handSprite == null) yield break;
                handSprite.position = deckPosition;
                var elapsedTime = 0f;
                var duration = 1f;

                while (elapsedTime < duration)
                {
                    if (handSprite == null) yield break;
                    handSprite.position = Vector3.Lerp(deckPosition, centerPosition, elapsedTime / duration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                if (handSprite == null) yield break;
                handSprite.position = centerPosition;
                yield return new WaitForSeconds(0.5f); // Pause before restarting the animation
            }
        }

        private void StopHandAnimation()
        {
            if (handAnimationCoroutine != null)
            {
                StopCoroutine(handAnimationCoroutine);
                handAnimationCoroutine = null;
            }

            if (handSprite == null) return;
            handSprite.gameObject.SetActive(false);
        }

        // Shape 드래그 시작 시 hand 애니메이션 멈춤
        private void HandleShapeDragStart()
        {
            StopHandAnimation();
        }

        // Shape 드래그 종료(복귀) 시 hand 애니메이션 재시작
        private void HandleShapeDragEnd()
        {
            if (!IsCurrentLevelTutorial()) return;
            StartHandAnimation();
        }

        private IEnumerator DelayedClassicModeHandlerInit()
        {
            // RestartLevel 후 UI가 다시 생성될 시간을 기다림
            yield return new WaitForSeconds(0.5f); // StartGame이 호출되는 시간과 맞춤
            
            // ClassicModeHandler를 다시 찾아서 초기화
            var classicModeHandler = FindObjectOfType<ClassicModeHandler>();
            if (classicModeHandler != null)
            {
                classicModeHandler.InitializeForTutorialTransition();
                Debug.Log("Tutorial: ClassicModeHandler re-initialized after RestartLevel");
                
                // LevelManager에서 classicModeHandler 참조 업데이트
                var levelManager = FindObjectOfType<LevelManager>();
                if (levelManager != null)
                {
                    // LevelManager의 classicModeHandler 필드를 업데이트
                    var classicModeHandlerField = typeof(LevelManager).GetField("classicModeHandler", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (classicModeHandlerField != null)
                    {
                        classicModeHandlerField.SetValue(levelManager, classicModeHandler);
                        Debug.Log("Tutorial: LevelManager classicModeHandler reference updated");
                    }
                }
            }
            else
            {
                Debug.LogWarning("Tutorial: ClassicModeHandler not found after RestartLevel");
            }
        }

        private void SwitchToClassicLevel()
        {
            Debug.Log("Tutorial: Switching to Classic mode after tutorial completion");
            
            // LevelManager를 찾아서 Classic 모드로 전환
            var levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
            {
                // LevelManager.Load()에서 자동으로 Classic 모드와 ClassicLevel로 설정됨
                levelManager.Load();
                Debug.Log("Tutorial: Successfully switched to Classic mode");
            }
            else
            {
                Debug.LogError("Tutorial: LevelManager not found when switching to Classic mode");
            }
        }
    }
}