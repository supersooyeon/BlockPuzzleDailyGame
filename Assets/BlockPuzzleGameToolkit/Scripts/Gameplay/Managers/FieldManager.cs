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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlockPuzzleGameToolkit.Scripts.Audio;
using BlockPuzzleGameToolkit.Scripts.LevelsData;
using BlockPuzzleGameToolkit.Scripts.System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BlockPuzzleGameToolkit.Scripts.Gameplay.Managers;

namespace BlockPuzzleGameToolkit.Scripts.Gameplay
{
    public class FieldManager : MonoBehaviour
    {
        public RectTransform field;
        public Cell prefab;

        public Cell[,] cells;

        public RectTransform outline;

        [SerializeField]
        private ItemFactory itemFactory;

        [Header("Animation Sounds")]
        [SerializeField]
        private AudioClip gameStartSound;
        
        [SerializeField]
        private AudioClip lineClearSound;

        private float _cellSize;
        
        // 애니메이션 중복 실행 방지를 위한 플래그
        private bool isAnimationPlaying = false;
        public bool IsAnimationPlaying => isAnimationPlaying;

        public void Generate(Level level, bool isTutorialLevel = false, bool skipStartAnimation = false)
        {
            var oneColorMode = level.levelType.singleColorMode;

            if (level == null)
            {
                Debug.LogError("Attempted to generate field with null level");
                return;
            }

            GenerateField(level.rows, level.columns);

            for (var i = 0; i < level.rows; i++)
            {
                for (var j = 0; j < level.columns; j++)
                {
                    var item = level.GetItem(i, j);
                    if (item != null)
                    {
                        cells[i, j].FillCell(item);
                    }

                    var bonus = false;
                    if (level.levelRows[i].bonusItems[j])
                    {
                        // 1차: 런타임 타겟 인스턴스(수량>0)에서 보너스 템플릿 수집
                        var bonusItemTemplates = level.targetInstance
                            .Where(t => t.targetScriptable != null && t.targetScriptable.bonusItem != null && t.amount > 0)
                            .Select(t => t.targetScriptable.bonusItem)
                            .ToArray();

                        // 2차: 없으면 LevelType에 정의된 모든 보너스 템플릿으로 폴백
                        if (bonusItemTemplates.Length == 0 && level.levelType != null && level.levelType.targets != null)
                        {
                            bonusItemTemplates = level.levelType.targets
                                .Where(t => t != null && t.bonusItem != null)
                                .Select(t => t.bonusItem)
                                .ToArray();
                        }

                        if (bonusItemTemplates.Length > 0)
                        {
                            cells[i, j].SetBonus(bonusItemTemplates[UnityEngine.Random.Range(0, bonusItemTemplates.Length)]);
                            bonus = true;
                        }
                        else
                        {
                            Debug.LogWarning("[FieldManager] Bonus cell flagged but no bonusItemTemplate found in targets.");
                        }
                    }

                    if (item != null && oneColorMode && !bonus)
                    {
                        cells[i, j].FillCell(itemFactory.GetColor());
                    }

                    // Disable cell if it is marked as disabled in the level data
                    if (level.IsDisabled(i, j))
                    {
                        cells[i, j].DisableCell();
                    }

                    // Highlight cell if it is marked as highlighted in the level data
                    if (level.IsCellHighlighted(i, j))
                    {
                        cells[i, j].HighlightCellTutorial();
                    }
                }
            }

            // 게임 시작 시 애니메이션 효과 재생 (튜토리얼이 아닌 경우에만)
            // 튜토리얼에서 일반 레벨로 전환될 때는 애니메이션을 완전히 건너뛰기
            if (!isTutorialLevel && !IsTutorialLevel(level) && !skipStartAnimation)
            {
                StartCoroutine(PlayGameStartEffect());
            }
        }

        // 이벤트 시스템이 없으므로 주석 처리
        // private void OnEnable()
        // {
        //     // 라인 클리어 이벤트 구독
        //     EventManager.GetEvent(EGameEvent.ShapePlaced).Subscribe(OnShapePlaced);
        // }

        // private void OnDisable()
        // {
        //     // 이벤트 구독 해제
        //     EventManager.GetEvent(EGameEvent.ShapePlaced).Unsubscribe(OnShapePlaced);
        // }

        // private void OnShapePlaced(Shape shape)
        // {
        //     // 셰이프가 배치된 후 라인이 클리어되었는지 확인
        //     var filledLines = GetFilledLines();
        //     if (filledLines.Count > 0)
        //     {
        //         // 라인이 클리어되면 애니메이션 실행
        //         StartLineClearEffect();
        //     }
        // }

        private void GenerateField(int rows, int columns)
        {
            foreach (Transform child in field)
            {
                Destroy(child.gameObject);
            }

            cells = new Cell[rows, columns];

            // Configure grid layout
            var gridLayout = field.GetComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columns;

            // Calculate cell size to fit the field
            // var totalMargin = gridLayout.padding.left + gridLayout.padding.right;
            // var spacing = gridLayout.spacing;
            // var availableWidth = field.rect.width - totalMargin;
            // var availableHeight = field.rect.height - (gridLayout.padding.top + gridLayout.padding.bottom);
            
            // Calculate cell size to fit both dimensions
            // var cellSizeFromWidth = (availableWidth - (spacing.x * (columns - 1))) / columns;
            // var cellSizeFromHeight = (availableHeight - (spacing.y * (rows - 1))) / rows;
            // var cellSize = Mathf.Min(cellSizeFromWidth, cellSizeFromHeight);
            
            // gridLayout.cellSize = new Vector2(cellSize, cellSize);

            // Create cells
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    var cell = Instantiate(prefab, field);
                    cells[i, j] = cell;
                    cell.name = $"Cell {i}, {j}";
                    cell.InitItem();
                }
            }

            // _cellSize = cellSize;
            
            // Start coroutine to resize field with delay
            // StartCoroutine(ResizeFieldWithDelay(rows, columns, cellSize, gridLayout));
        }
        
        // private IEnumerator ResizeFieldWithDelay(int rows, int columns, float cellSize, GridLayoutGroup gridLayout)
        // {
            
        //     // Wait for additional frame to ensure grid layout is calculated
        //     yield return new WaitForFixedUpdate();
            
        //     // Calculate exact size based on grid layout
        //     var spacing = gridLayout.spacing;
        //     var padding = gridLayout.padding;
        //     float width = (cellSize * columns) + (spacing.x * (columns - 1)) + padding.left + padding.right;
        //     float height = (cellSize * rows) + (spacing.y * (rows - 1)) + padding.top + padding.bottom;
            
        //     // Resize field to exactly match the grid layout size
        //     GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        // }

        public void RestoreFromState(LevelRow[] levelRows)
        {
            //restore score
            if (levelRows == null || levelRows.Length == 0) 
            {
                Debug.LogWarning("[FieldManager] RestoreFromState: levelRows가 null이거나 비어있음");
                return;
            }
            
            if (levelRows[0] == null || levelRows[0].cells == null)
            {
                Debug.LogWarning("[FieldManager] RestoreFromState: levelRows[0] 또는 cells가 null임");
                return;
            }
            
            Debug.Log($"[FieldManager] RestoreFromState: {levelRows.Length}x{levelRows[0].cells.Length} 필드 복원 시작");
            GenerateField(levelRows.Length, levelRows[0].cells.Length);
            
            for (var i = 0; i < levelRows.Length; i++)
            {
                if (levelRows[i] == null || levelRows[i].cells == null) continue;
                
                for (var j = 0; j < levelRows[i].cells.Length; j++)
                {
                    var item = levelRows[i].cells[j];
                    if (item != null)
                    {
                        cells[i,j].FillCell(item);
                    }

                    if (levelRows[i].disabled[j])
                    {
                        cells[i,j].DisableCell();
                    }
                }
            }
        }

        public List<List<Cell>> GetFilledLines(bool preview = false, bool merge = true)
        {
            var horizontalLines = GetFilledLinesHorizontal(preview);
            var verticalLines = GetFilledLinesVertical(preview);

            var lines = new List<List<Cell>>();
            lines.AddRange(horizontalLines);
            lines.AddRange(verticalLines);
            return lines;
        }

        public List<List<Cell>> GetFilledLinesHorizontal(bool preview)
        {
            var lines = new List<List<Cell>>();
            for (var i = 0; i < cells.GetLength(0); i++)
            {
                var isLineFilled = true;
                var line = new List<Cell>();
                for (var j = 0; j < cells.GetLength(1); j++)
                {
                    if (cells[i, j].IsEmpty(preview))
                    {
                        isLineFilled = false;
                        break;
                    }

                    line.Add(cells[i, j]);
                }

                if (isLineFilled)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        public List<List<Cell>> GetFilledLinesVertical(bool preview)
        {
            var lines = new List<List<Cell>>();
            for (var i = 0; i < cells.GetLength(1); i++)
            {
                var isLineFilled = true;
                var line = new List<Cell>();
                for (var j = 0; j < cells.GetLength(0); j++)
                {
                    if (cells[j, i].IsEmpty(preview))
                    {
                        isLineFilled = false;
                        break;
                    }

                    line.Add(cells[j, i]);
                }

                if (isLineFilled)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        public bool CanPlaceShape(Shape shape)
        {
            if (cells == null)
            {
                return false;
            }

            var activeItems = shape.GetActiveItems();
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            // Find the bounding box of the shape
            foreach (var item in activeItems)
            {
                var pos = item.GetPosition();
                minX = Mathf.Min(minX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxX = Mathf.Max(maxX, pos.x);
                maxY = Mathf.Max(maxY, pos.y);
            }

            var shapeWidth = maxX - minX + 1;
            var shapeHeight = maxY - minY + 1;

            // Try to place the shape at every possible position on the field
            for (var fieldY = 0; fieldY <= cells.GetLength(0) - shapeHeight; fieldY++)
            {
                for (var fieldX = 0; fieldX <= cells.GetLength(1) - shapeWidth; fieldX++)
                {
                    if (CanPlaceShapeAt(activeItems, fieldX - minX, fieldY - minY))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Cell GetCenterCell()
        {
            var x = cells.GetLength(1) / 2;
            var y = cells.GetLength(0) / 2;
            return cells[y, x];
        }

        private bool CanPlaceShapeAt(List<Item> items, int offsetX, int offsetY)
        {
            foreach (var item in items)
            {
                var pos = item.GetPosition();
                var x = offsetX + pos.x;
                var y = offsetY + pos.y;

                if (x < 0 || x >= cells.GetLength(1) || y < 0 || y >= cells.GetLength(0))
                {
                    return false; // Out of bounds
                }

                if (!cells[y, x].IsEmpty() && cells[y, x].busy)
                {
                    return false; // Cell is already occupied
                }
            }

            return true;
        }

        public void ShowOutline(bool show)
        {
            var paddingX = 0.033f;
            var paddingY = 0.033f;

            outline.anchoredPosition = field.anchoredPosition;
            outline.sizeDelta = field.sizeDelta;
            outline.anchorMin = new Vector2(field.anchorMin.x - paddingX, field.anchorMin.y - paddingY);
            outline.anchorMax = new Vector2(field.anchorMax.x + paddingX, field.anchorMax.y + paddingY);
            outline.pivot = field.pivot;
            outline.gameObject.SetActive(show);
        }

        public Cell[,] GetAllCells()
        {
            return cells;
        }

        public List<List<Cell>> GetRow(int i)
        {
            var row = new List<List<Cell>>();
            for (var j = 0; j < cells.GetLength(1); j++)
            {
                row.Add(new List<Cell> { cells[i, j] });
            }

            return row;
        }

        public Cell[] GetEmptyCells()
        {
            return cells.Cast<Cell>().Where(cell => !cell.busy).ToArray();
        }

        public float GetCellSize()
        {
            // Grid Layout Group에서 직접 셀 크기를 가져옴
            var gridLayout = field.GetComponent<GridLayoutGroup>();
            return gridLayout != null ? gridLayout.cellSize.x : 126f; // 기본값 100f
        }

        public List<Cell> GetTutorialLine()
        {
            var line = new List<Cell>();
            for (var i = 0; i < cells.GetLength(0); i++)
            {
                for (var j = 0; j < cells.GetLength(1); j++)
                {
                    if (cells[i, j].IsHighlighted())
                    {
                        line.Add(cells[i, j]);
                    }
                }
            }

            return line;
        }



        /// <summary>
        /// 튜토리얼 레벨인지 확인
        /// </summary>
        private bool IsTutorialLevel(Level level)
        {
            // 튜토리얼 레벨 판단 기준: 하이라이트된 셀이 있거나 특별한 튜토리얼 플래그가 있는 경우
            if (level == null) return false;
            
            // 하이라이트된 셀이 있는지 확인
            for (var i = 0; i < level.rows; i++)
            {
                for (var j = 0; j < level.columns; j++)
                {
                    if (level.IsCellHighlighted(i, j))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }



        public IEnumerator PlayGameStartEffect()
        {
            // 이미 애니메이션이 실행 중이면 건너뛰기
            if (isAnimationPlaying)
            {
                yield break;
            }
            
            isAnimationPlaying = true;
            
            // ShapeDraggable.SetDraggable(false); // SetDraggable 메서드가 없음

            var fieldManager = FindObjectOfType<FieldManager>();
            yield return new WaitForSeconds(0.1f);

            var emptyCells = GetEmptyCells();

            // 게임 시작 사운드 재생 (전역 사운드 설정 준수)
            if (gameStartSound != null)
            {
                SoundBase.instance.PlaySound(gameStartSound);
            }

            // 단색 블록을 위한 랜덤 아이템 템플릿
            var allTemplates = Resources.LoadAll<ItemTemplate>("Items")
                                .Where(t => t.name != "ItemTemplate 0")
                                .ToArray();
            
            if (allTemplates.Length == 0)
            {
                yield break;
            }

            // 랜덤한 템플릿 선택
            var randomTemplate = allTemplates[UnityEngine.Random.Range(0, allTemplates.Length)];
            yield return null;

            float fillSpeed = 0.003f;
            int batchSize = 3;

            // 모든 셀에 순차적으로 단색 블록 생성
            for (int i = 0; i < emptyCells.Length; i += batchSize)
            {
                for (int j = 0; j < batchSize && (i + j) < emptyCells.Length; j++)
                {
                    var cell = emptyCells[i + j];
                    if (cell == null || cell.gameObject == null) continue;

                    if (randomTemplate != null)
                    {
                        // 랜덤한 단색 블록으로 셀 채우기
                        cell.FillCellFailed(randomTemplate);
                    }
                }
                yield return new WaitForSeconds(fillSpeed);
            }
            yield return new WaitForSeconds(0.1f);

            // 모든 셀에서 블록 제거
            for (int i = 0; i < emptyCells.Length; i += batchSize)
            {
                for (int j = 0; j < batchSize && (i + j) < emptyCells.Length; j++)
                {
                    var cell = emptyCells[i + j];
                    if (cell == null || cell.gameObject == null) continue;
                    cell.DestroyCell(); // DestroyCell로 변경하여 애니메이션 효과 적용
                }
                yield return new WaitForSeconds(fillSpeed);
            }

            // ShapeDraggable.SetDraggable(true); // SetDraggable 메서드가 없음
            
            // 애니메이션 완료 후 플래그 리셋
            isAnimationPlaying = false;
            var levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null) levelManager.isLineClearEffectPlaying = false;
        }

        /// <summary>
        /// 모든 라인 제거 애니메이션 (사운드 포함, 튜토리얼 완료 후 호출용)
        /// </summary>
        public IEnumerator PlayAllLinesClearEffectWithSound()
        {
            if (isAnimationPlaying)
                yield break;

            isAnimationPlaying = true;

            // 사운드 재생 (모든 라인 제거용, 전역 사운드 설정 준수)
            if (lineClearSound != null)
                SoundBase.instance.PlaySound(lineClearSound);

            var allCells = cells.Cast<Cell>().ToArray();
            var allTemplates = Resources.LoadAll<ItemTemplate>("Items")
                                .Where(t => t.name != "ItemTemplate 0")
                                .ToArray();
            if (allTemplates.Length == 0)
                yield break;

            float fillSpeed = 0.003f;
            int batchSize = 3;

            // 모든 셀에 순차적으로 블록 생성
            for (int i = 0; i < allCells.Length; i += batchSize)
            {
                for (int j = 0; j < batchSize && (i + j) < allCells.Length; j++)
                {
                    var cell = allCells[i + j];
                    if (cell == null || cell.gameObject == null) continue;
                    var randomTemplate = allTemplates[UnityEngine.Random.Range(0, allTemplates.Length)];
                    cell.FillCellFailed(randomTemplate);
                    cell.busy = true; // 애니메이션용 busy 상태 설정
                }
                yield return new WaitForSeconds(fillSpeed);
            }
            yield return new WaitForSeconds(0.1f);

            // 모든 셀에서 블록 제거 (애니메이션 효과와 함께)
            for (int i = 0; i < allCells.Length; i += batchSize)
            {
                for (int j = 0; j < batchSize && (i + j) < allCells.Length; j++)
                {
                    var cell = allCells[i + j];
                    if (cell == null || cell.gameObject == null) continue;
                    
                    // 애니메이션 효과와 함께 블록 제거
                    cell.DestroyCell();
                }
                yield return new WaitForSeconds(fillSpeed);
            }

            // 모든 블록이 제거될 때까지 대기
            yield return new WaitForSeconds(0.3f);

            isAnimationPlaying = false;
            var levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null) levelManager.isLineClearEffectPlaying = false;
        }

        public IEnumerator PlayLineClearEffect()
        {
            // 이미 애니메이션이 실행 중이면 건너뛰기
            if (isAnimationPlaying)
            {
                yield break;
            }
            
            isAnimationPlaying = true;

            // 모든 셀을 올바르게 가져오기 (2차원 배열을 1차원으로 변환)
            var rows = cells.GetLength(0);
            var columns = cells.GetLength(1);
            var allCells = new List<Cell>();
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    var cell = cells[i, j];
                    if (cell != null && cell.gameObject != null)
                    {
                        allCells.Add(cell);
                    }
                }
            }

            // 라인 클리어 사운드 재생 (전역 사운드 설정 준수)
            if (lineClearSound != null)
            {
                SoundBase.instance.PlaySound(lineClearSound);
            }

            // 알록달록한 블록들을 위한 모든 템플릿
            var allTemplates = Resources.LoadAll<ItemTemplate>("Items")
                                .Where(t => t.name != "ItemTemplate 0")
                                .ToArray();
            
            if (allTemplates.Length == 0)
            {
                Debug.LogWarning("ItemTemplate을 찾을 수 없습니다.");
                isAnimationPlaying = false;
                yield break;
            }

            yield return new WaitForSeconds(0.1f);

            float fillSpeed = 0.003f;
            int batchSize = 3;

            Debug.Log($"라인 클리어 애니메이션 시작: {allCells.Count}개 셀에 블록 생성");

            // 모든 셀에 순차적으로 알록달록한 블록 생성
            for (int i = 0; i < allCells.Count; i += batchSize)
            {
                for (int j = 0; j < batchSize && (i + j) < allCells.Count; j++)
                {
                    var cell = allCells[i + j];
                    if (cell == null || cell.gameObject == null) continue;

                    var randomTemplate = allTemplates[UnityEngine.Random.Range(0, allTemplates.Length)];
                    
                    // 셀을 초기화하고 블록 생성 (ClearCell 호출하지 않음)
                    cell.FillCellFailed(randomTemplate);
                    cell.busy = true; // 애니메이션용 busy 상태 설정
                }
                yield return new WaitForSeconds(fillSpeed);
            }

            yield return new WaitForSeconds(0.1f);

            Debug.Log($"라인 클리어 애니메이션: {allCells.Count}개 셀에서 블록 제거");

            // 모든 셀에서 블록 제거 (애니메이션 효과와 함께)
            for (int i = 0; i < allCells.Count; i += batchSize)
            {
                for (int j = 0; j < batchSize && (i + j) < allCells.Count; j++)
                {
                    var cell = allCells[i + j];
                    if (cell == null || cell.gameObject == null) continue;
                    
                    // 애니메이션 효과와 함께 블록 제거
                    cell.DestroyCell();
                }

                yield return new WaitForSeconds(fillSpeed);
            }
            
            // 모든 블록이 제거될 때까지 대기
            yield return new WaitForSeconds(0.3f);
            
            // 애니메이션 완료 후 플래그 리셋
            isAnimationPlaying = false;
            var levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null) levelManager.isLineClearEffectPlaying = false;
            Debug.Log("라인 클리어 애니메이션 완료");
        }

        /// <summary>
        /// 라인 클리어 애니메이션을 시작하는 public 메서드
        /// </summary>
        public IEnumerator StartLineClearEffect()
        {
            yield return PlayLineClearEffect();
        }

        /// <summary>
        /// 모든 블록이 제거되었는지 확인하는 메서드
        /// </summary>
        public bool IsAllBlocksCleared()
        {
            if (cells == null) return false;
            
            var rows = cells.GetLength(0);
            var columns = cells.GetLength(1);
            
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < columns; j++)
                {
                    var cell = cells[i, j];
                    if (cell != null && !cell.IsEmpty())
                    {
                        return false; // 아직 블록이 남아있음
                    }
                }
            }
            
            return true; // 모든 블록이 제거됨
        }
    }
}