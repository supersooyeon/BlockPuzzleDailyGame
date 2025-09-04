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
using System.Collections.Generic;
using BlockPuzzleGameToolkit.Scripts.LevelsData;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;

namespace BlockPuzzleGameToolkit.Scripts.Gameplay
{
    public class Cell : MonoBehaviour
    {
        private static readonly Dictionary<string, ObjectPool<Item>> CustomItemPools = new();
        public Item item;
        private CanvasGroup group;
        public bool busy;
        private ItemTemplate saveTemplate;
        private BoxCollider2D _boxCollider2D;
        private bool isDestroying;
        private Item originalItem;
        private Item customItem;

        public Image image;

        private bool isEmpty => !busy;
        private bool IsEmptyPreview => group.alpha == 0;

        private void Awake()
        {
            _boxCollider2D = GetComponent<BoxCollider2D>();
            group = item.GetComponent<CanvasGroup>();
            CustomItemPools.Clear();
        }

        private ObjectPool<Item> GetOrCreatePool(Item prefab)
        {
            if (!CustomItemPools.TryGetValue(prefab.name, out var pool))
            {
                pool = new ObjectPool<Item>(
                    createFunc: () =>
                    {
                        var instantiate = Instantiate(prefab,transform);
                        instantiate.name = prefab.name;
                        return instantiate;
                    },
                    actionOnGet: item =>
                    {
                        if (GetValue(prefab, item, pool))
                        {
                            return;
                        }
                        item.transform.SetParent(transform);
                        item.gameObject.SetActive(true);
                    },
                    actionOnRelease: item =>
                    {
                        if (GetValue(prefab, item, pool))
                        {
                            return;
                        }
                        if (item?.gameObject != null)
                            item.gameObject.SetActive(false);
                    },
                    actionOnDestroy: item =>
                    {
                        if (GetValue(prefab, item, pool))
                        {
                            return;
                        }
                        if (item?.gameObject != null)
                            Destroy(item.gameObject);
                    }
                );
                CustomItemPools[prefab.name] = pool;
            }
            return pool;
        }

        private bool GetValue(Item prefab, Item item, ObjectPool<Item> pool)
        {
            if (item == null)
            {
                pool.Clear();
                CustomItemPools.Remove(prefab.name);
                ClearCell();
                return true;
            }

            return false;
        }

        private void ReplaceWithCustomItem(ItemTemplate itemTemplate)
        {
            if (originalItem == null)
            {
                originalItem = item;
                originalItem.gameObject.SetActive(false);
            }

            if (customItem != null)
            {
                GetOrCreatePool(itemTemplate.customItemPrefab).Release(customItem);
            }
            
            customItem = GetOrCreatePool(itemTemplate.customItemPrefab).Get();
            customItem.transform.SetParent(transform);
            customItem.transform.position = originalItem.transform.position;
            customItem.transform.localScale = originalItem.transform.localScale;
            var rectTransform = customItem.GetComponent<RectTransform>();
            var originalRect = originalItem.GetComponent<RectTransform>();
            if (rectTransform != null && originalRect != null)
            {
                rectTransform.anchorMin = originalRect.anchorMin;
                rectTransform.anchorMax = originalRect.anchorMax;
                rectTransform.pivot = originalRect.pivot;
                rectTransform.sizeDelta = originalRect.sizeDelta;
                rectTransform.anchoredPosition = originalRect.anchoredPosition;
            }
            item = customItem;
            group = item.GetComponent<CanvasGroup>();
        }

        public void FillCell(ItemTemplate itemTemplate)
        {
            if (itemTemplate.customItemPrefab != null)
            {
                ReplaceWithCustomItem(itemTemplate);
            }
            else 
            {
                if (originalItem != null)
                {
                    if (customItem != null)
                    {
                        Destroy(customItem.gameObject);
                        customItem = null;
                    }
                    item = originalItem;
                    item.gameObject.SetActive(true);
                    originalItem = null;
                    group = item.GetComponent<CanvasGroup>();
                }
            }

            item.FillIcon(itemTemplate);
            group.alpha = 1;
            busy = true;
        }

        public void FillCellFailed(ItemTemplate itemTemplate)
        {
            item.FillIcon(itemTemplate);
            group.alpha = 1;
            busy = true; // busy 상태를 true로 설정하여 블록이 표시되도록 함
        }

        public bool IsEmpty(bool preview = false)
        {
            return preview ? IsEmptyPreview || isDestroying: isEmpty;
        }

        public void ClearCell()
        {
            if (customItem != null)
            {
                GetOrCreatePool(customItem.GetComponent<Item>()).Release(customItem);
                customItem = null;
            }
            if (originalItem != null)
            {
                item = originalItem;
                item.gameObject.SetActive(true);
                originalItem = null;
                group = item.GetComponent<CanvasGroup>();
            }
            
            item.transform.localScale = Vector3.one;
            if (saveTemplate == null && !busy)
            {
                group.alpha = 0;
                busy = false;
            }
            else if (saveTemplate != null && busy)
            {
                FillCell(saveTemplate);
                saveTemplate = null;
            }
        }

        /// <summary>
        /// 게임 오버 애니메이션 블록을 강제로 제거하는 메서드
        /// </summary>
        public void ForceClearGameOverAnimation()
        {
            if (customItem != null)
            {
                GetOrCreatePool(customItem.GetComponent<Item>()).Release(customItem);
                customItem = null;
            }
            if (originalItem != null)
            {
                item = originalItem;
                item.gameObject.SetActive(true);
                originalItem = null;
                group = item.GetComponent<CanvasGroup>();
            }
            
            // 강제로 모든 상태 초기화
            item.transform.localScale = Vector3.one;
            group.alpha = 0;
            busy = false;
            saveTemplate = null;
            
            Debug.Log($"[Cell] {name} 게임 오버 애니메이션 블록 강제 제거 완료");
        }

        public void HighlightCell(ItemTemplate itemTemplate)
        {
            if (itemTemplate.customItemPrefab != null)
            {
                ReplaceWithCustomItem(itemTemplate);
            }
            else 
            {
                if (originalItem != null)
                {
                    if (customItem != null)
                    {
                        Destroy(customItem.gameObject);
                        customItem = null;
                    }
                    item = originalItem;
                    item.gameObject.SetActive(true);
                    originalItem = null;
                    group = item.GetComponent<CanvasGroup>();
                }
            }

            item.FillIcon(itemTemplate);
            group.alpha = 0.05f; // Make it semi-transparent to indicate it's a highlight
        }

        public void HighlightCellTutorial()
        {
            image.color = new Color(43f / 255f, 59f / 255f, 120f / 255f, 1f);
        }

        public void HighlightCellFill(ItemTemplate itemTemplate)
        {
            saveTemplate = item.itemTemplate;
            if (!item.HasBonusItem())
            {
                item.FillIcon(itemTemplate);
            }

            group.alpha = 1f;
        }

        public void DestroyCell()
        {
            saveTemplate = null;
            busy = false;
            isDestroying = true; // isDestroying 플래그를 true로 설정
            item.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() =>
            {
                isDestroying = false;
                ClearCell();
                item.ClearBonus();
            });
        }

        public Bounds GetBounds()
        {
            return _boxCollider2D.bounds;
        }

        public void InitItem()
        {
            item.name = "Item " + name;
            StartCoroutine(UpdateItem());
        }

        private IEnumerator UpdateItem()
        {
            yield return new WaitForSeconds(0.1f);
            _boxCollider2D.size = transform.GetComponent<RectTransform>().sizeDelta;
            // item.transform.SetParent(GameObject.Find("ItemsCanvas/Items").transform);
            item.transform.position = transform.position;
        }

        public void SetBonus(BonusItemTemplate bonusItemTemplate)
        {
            item.SetBonus(bonusItemTemplate);
        }

        public bool HasBonusItem()
        {
            return item.HasBonusItem();
        }

        public BonusItemTemplate GetBonusItem()
        {
            return item.bonusItemTemplate;
        }

        public void AnimateFill()
        {
            item.transform.DOScale(Vector3.one * 0.5f, 0.1f).OnComplete(() => { item.transform.DOScale(Vector3.one, 0.1f); });
        }

        public void DisableCell()
        {
            _boxCollider2D.enabled = false;
        }

        public bool IsDisabled()
        {
            return !_boxCollider2D.enabled;
        }

        public bool IsHighlighted()
        {
            return !IsDisabled();
        }

        public void SetDestroying(bool destroying)
        {
            isDestroying = destroying;
        }

        public bool IsDestroying()
        {
            return isDestroying;
        }

        private void OnDestroy()
        {
            if (customItem != null)
            {
                GetOrCreatePool(customItem.GetComponent<Item>()).Release(customItem);
            }
        }
    }
}