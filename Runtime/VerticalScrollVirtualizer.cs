using System;
using UnityEngine;

namespace NoranDev.ScrollVirtualizer
{
    /// <summary>
    /// ScrollVirtualizer for vertical virtualized scrolling
    /// </summary>
    /// <typeparam name="TCell">Cell component type</typeparam>
    /// <typeparam name="TData">Data type</typeparam>
    public abstract class VerticalScrollVirtualizer<TCell, TData> : ScrollVirtualizerBase<TCell, TData> where TCell : ScrollVirtualizerCell<TData>
    {
        [Header("Vertical Settings")]
        [SerializeField] private bool useDynamicCellWidth = false;
        [SerializeField] private float cellWidth = 100f;
        [SerializeField] private float cellHeight = 100f;
        [SerializeField] private VerticalContentAlignment verticalContentAlignment = VerticalContentAlignment.Left;

        protected override ScrollDirection ScrollDirection => ScrollDirection.Vertical;

        /// <summary>
        /// Initialization
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (ScrollRect != null && !ScrollRect.vertical)
            {
                Debug.LogWarning("[VerticalScrollVirtualizer] ScrollRect.vertical is disabled. Please enable it in the Inspector.", this);
            }
        }

        /// <summary>
        /// Get current scroll position
        /// </summary>
        protected override float GetCurrentScrollPosition()
        {
            return Content != null ? Content.anchoredPosition.y : 0f;
        }

        /// <summary>
        /// Get maximum scroll position
        /// </summary>
        protected override float GetMaxScrollPosition()
        {
            if (Content == null || Viewport == null)
            {
                return 0f;
            }

            var contentHeight = Content.sizeDelta.y;
            var viewportHeight = Viewport.rect.height;
            return Mathf.Max(0, contentHeight - viewportHeight);
        }

        /// <summary>
        /// Get cell size
        /// </summary>
        protected override Vector2 GetCellSize()
        {
            var width = useDynamicCellWidth && Viewport != null
                ? Viewport.rect.width - PaddingLeft - PaddingRight
                : cellWidth;

            return new Vector2(width, cellHeight);
        }

        /// <summary>
        /// Calculate total content size
        /// </summary>
        protected override Vector2 CalculateContentSize()
        {
            var viewportHeight = Viewport != null ? Viewport.rect.height : 0f;

            if (ItemCount == 0)
            {
                return new Vector2(0f, viewportHeight);
            }

            var totalHeight = PaddingTop + (cellHeight + Spacing) * ItemCount - Spacing + PaddingBottom;

            return new Vector2(0f, totalHeight);
        }

        /// <summary>
        /// Calculate cell position for specified index
        /// </summary>
        protected override Vector2 CalculateCellPosition(int index)
        {
            var y = -(PaddingTop + index * (cellHeight + Spacing));

            var x = PaddingLeft;
            if (!useDynamicCellWidth && Viewport != null)
            {
                var viewportWidth = Viewport.rect.width;
                switch (verticalContentAlignment)
                {
                    case VerticalContentAlignment.Left:
                        x = PaddingLeft;
                        break;
                    case VerticalContentAlignment.Center:
                        x = (viewportWidth - cellWidth) * 0.5f;
                        break;
                    case VerticalContentAlignment.Right:
                        x = viewportWidth - cellWidth - PaddingRight;
                        break;
                }
            }

            return new Vector2(x, y);
        }

        /// <summary>
        /// Calculate index range to display
        /// </summary>
        protected override void CalculateVisibleRange(out int firstIndex, out int lastIndex)
        {
            if (ItemCount == 0 || Viewport == null || Content == null)
            {
                firstIndex = 0;
                lastIndex = -1;
                return;
            }

            var scrollPosition = Content.anchoredPosition.y - PaddingTop;
            var viewportHeight = Viewport.rect.height;
            var cellSize = cellHeight + Spacing;

            firstIndex = Mathf.FloorToInt(scrollPosition / cellSize);
            firstIndex = Mathf.Max(0, firstIndex - VisibleCellBuffer);

            lastIndex = Mathf.CeilToInt((scrollPosition + viewportHeight) / cellSize);
            lastIndex = Mathf.Min(ItemCount - 1, lastIndex + VisibleCellBuffer);

            if (lastIndex - firstIndex + 1 > MaxRecycleCount)
            {
                lastIndex = firstIndex + MaxRecycleCount - 1;
            }

            firstIndex = Mathf.Clamp(firstIndex, 0, ItemCount - 1);
            lastIndex = Mathf.Clamp(lastIndex, 0, ItemCount - 1);
        }

        /// <summary>
        /// Calculate actually displayed index range
        /// </summary>
        protected override void CalculateActualVisibleRange(out int firstIndex, out int lastIndex)
        {
            if (ItemCount == 0 || Viewport == null || Content == null)
            {
                firstIndex = 0;
                lastIndex = -1;
                return;
            }

            var scrollPosition = Content.anchoredPosition.y - PaddingTop;
            var viewportHeight = Viewport.rect.height;
            var cellSize = cellHeight + Spacing;

            firstIndex = Mathf.FloorToInt((scrollPosition - cellHeight) / cellSize) + 1;
            firstIndex = Mathf.Max(0, firstIndex);

            lastIndex = Mathf.FloorToInt((scrollPosition + viewportHeight - 1) / cellSize);
            lastIndex = Mathf.Min(ItemCount - 1, lastIndex);

            firstIndex = Mathf.Clamp(firstIndex, 0, ItemCount - 1);
            lastIndex = Mathf.Clamp(lastIndex, 0, ItemCount - 1);
        }

        /// <summary>
        /// Calculate scroll position for specified index
        /// </summary>
        protected override Vector2 CalculateScrollPosition(int index)
        {
            if (index < 0 || index >= ItemCount)
            {
                return Content.anchoredPosition;
            }

            var targetY = index * (cellHeight + Spacing);

            if (Viewport != null && Content != null)
            {
                var viewportHeight = Viewport.rect.height;
                var contentHeight = CalculateContentSize().y;
                var maxScrollY = contentHeight - viewportHeight;

                if (targetY > maxScrollY)
                {
                    targetY = maxScrollY;
                }
            }

            return new Vector2(Content.anchoredPosition.x, targetY);
        }

        /// <summary>
        /// Jump immediately to specified index
        /// </summary>
        /// <param name="index">Target index for jump (values -1 or less are treated as 0, default: 0)</param>
        public void JumpTo(int index = 0)
        {
            JumpToIndex(index);
        }

        /// <summary>
        /// Jump immediately to start (index 0)
        /// </summary>
        public void JumpToStart()
        {
            JumpToIndex(0);
        }

        /// <summary>
        /// Jump immediately to end (last index)
        /// </summary>
        public void JumpToEnd()
        {
            JumpToIndex(ItemCount - 1);
        }

        /// <summary>
        /// Scroll to specified index with animation
        /// </summary>
        /// <param name="index">Target index for scroll (values -1 or less are treated as 0)</param>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="ease">Easing type</param>
        /// <param name="onComplete">Callback on completion</param>
        public void ScrollTo(int index, float duration, Ease ease, Action onComplete = null)
        {
            ScrollToIndexAnimated(index, duration, ease, onComplete);
        }

        /// <summary>
        /// Scroll to start (index 0) with animation
        /// </summary>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="ease">Easing type</param>
        /// <param name="onComplete">Callback on completion</param>
        public void ScrollToStart(float duration, Ease ease, Action onComplete = null)
        {
            ScrollToIndexAnimated(0, duration, ease, onComplete);
        }

        /// <summary>
        /// Scroll to end (last index) with animation
        /// </summary>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="ease">Easing type</param>
        /// <param name="onComplete">Callback on completion</param>
        public void ScrollToEnd(float duration, Ease ease, Action onComplete = null)
        {
            ScrollToIndexAnimated(ItemCount - 1, duration, ease, onComplete);
        }

        /// <summary>
        /// Calculate scroll position offset for insertion at start
        /// </summary>
        protected override Vector2 CalculateOffsetForInsertedItems(int insertedCount)
        {
            var offsetY = insertedCount * (cellHeight + Spacing);
            return new Vector2(0, offsetY);
        }

        /// <summary>
        /// Check if content needs scrolling
        /// </summary>
        protected override bool IsContentScrollable(Vector2 contentSize)
        {
            if (ScrollRect == null) return false;

            var scrollRectTransform = ScrollRect.transform as RectTransform;
            if (scrollRectTransform == null) return false;

            var viewportHeight = scrollRectTransform.rect.height;
            return contentSize.y > viewportHeight;
        }
    }

    /// <summary>
    /// ScrollVirtualizer for vertical virtualized scrolling (context-enabled version)
    /// </summary>
    /// <typeparam name="TCell">Cell component type</typeparam>
    /// <typeparam name="TData">Data type</typeparam>
    /// <typeparam name="TContext">Context type</typeparam>
    public abstract class VerticalScrollVirtualizerWithContext<TCell, TData, TContext> : ScrollVirtualizerBaseWithContext<TCell, TData, TContext>
        where TCell : ScrollVirtualizerCellWithContext<TData, TContext>
    {
        [Header("Vertical Settings")]
        [SerializeField] private bool useDynamicCellWidth = false;
        [SerializeField] private float cellWidth = 100f;
        [SerializeField] private float cellHeight = 100f;
        [SerializeField] private VerticalContentAlignment verticalContentAlignment = VerticalContentAlignment.Left;

        protected override ScrollDirection ScrollDirection => ScrollDirection.Vertical;

        /// <summary>
        /// Initialization
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (ScrollRect != null && !ScrollRect.vertical)
            {
                Debug.LogWarning("[VerticalScrollVirtualizer] ScrollRect.vertical is disabled. Please enable it in the Inspector.", this);
            }
        }

        /// <summary>
        /// Get current scroll position
        /// </summary>
        protected override float GetCurrentScrollPosition()
        {
            return Content != null ? Content.anchoredPosition.y : 0f;
        }

        /// <summary>
        /// Get maximum scroll position
        /// </summary>
        protected override float GetMaxScrollPosition()
        {
            if (Content == null || Viewport == null)
            {
                return 0f;
            }

            var contentHeight = Content.sizeDelta.y;
            var viewportHeight = Viewport.rect.height;
            return Mathf.Max(0, contentHeight - viewportHeight);
        }

        /// <summary>
        /// Get cell size
        /// </summary>
        protected override Vector2 GetCellSize()
        {
            var width = useDynamicCellWidth && Viewport != null
                ? Viewport.rect.width - PaddingLeft - PaddingRight
                : cellWidth;

            return new Vector2(width, cellHeight);
        }

        /// <summary>
        /// Calculate total content size
        /// </summary>
        protected override Vector2 CalculateContentSize()
        {
            var viewportHeight = Viewport != null ? Viewport.rect.height : 0f;

            if (ItemCount == 0)
            {
                return new Vector2(0f, viewportHeight);
            }

            var totalHeight = PaddingTop + (cellHeight + Spacing) * ItemCount - Spacing + PaddingBottom;

            return new Vector2(0f, totalHeight);
        }

        /// <summary>
        /// Calculate cell position for specified index
        /// </summary>
        protected override Vector2 CalculateCellPosition(int index)
        {
            var y = -(PaddingTop + index * (cellHeight + Spacing));

            var x = PaddingLeft;
            if (!useDynamicCellWidth && Viewport != null)
            {
                var viewportWidth = Viewport.rect.width;
                switch (verticalContentAlignment)
                {
                    case VerticalContentAlignment.Left:
                        x = PaddingLeft;
                        break;
                    case VerticalContentAlignment.Center:
                        x = (viewportWidth - cellWidth) * 0.5f;
                        break;
                    case VerticalContentAlignment.Right:
                        x = viewportWidth - cellWidth - PaddingRight;
                        break;
                }
            }

            return new Vector2(x, y);
        }

        /// <summary>
        /// Calculate index range to display
        /// </summary>
        protected override void CalculateVisibleRange(out int firstIndex, out int lastIndex)
        {
            if (ItemCount == 0 || Viewport == null || Content == null)
            {
                firstIndex = 0;
                lastIndex = -1;
                return;
            }

            var scrollPosition = Content.anchoredPosition.y - PaddingTop;
            var viewportHeight = Viewport.rect.height;
            var cellSize = cellHeight + Spacing;

            firstIndex = Mathf.FloorToInt(scrollPosition / cellSize);
            firstIndex = Mathf.Max(0, firstIndex - VisibleCellBuffer);

            lastIndex = Mathf.CeilToInt((scrollPosition + viewportHeight) / cellSize);
            lastIndex = Mathf.Min(ItemCount - 1, lastIndex + VisibleCellBuffer);

            if (lastIndex - firstIndex + 1 > MaxRecycleCount)
            {
                lastIndex = firstIndex + MaxRecycleCount - 1;
            }

            firstIndex = Mathf.Clamp(firstIndex, 0, ItemCount - 1);
            lastIndex = Mathf.Clamp(lastIndex, 0, ItemCount - 1);
        }

        /// <summary>
        /// Calculate actually displayed index range
        /// </summary>
        protected override void CalculateActualVisibleRange(out int firstIndex, out int lastIndex)
        {
            if (ItemCount == 0 || Viewport == null || Content == null)
            {
                firstIndex = 0;
                lastIndex = -1;
                return;
            }

            var scrollPosition = Content.anchoredPosition.y - PaddingTop;
            var viewportHeight = Viewport.rect.height;
            var cellSize = cellHeight + Spacing;

            firstIndex = Mathf.FloorToInt((scrollPosition - cellHeight) / cellSize) + 1;
            firstIndex = Mathf.Max(0, firstIndex);

            lastIndex = Mathf.FloorToInt((scrollPosition + viewportHeight - 1) / cellSize);
            lastIndex = Mathf.Min(ItemCount - 1, lastIndex);

            firstIndex = Mathf.Clamp(firstIndex, 0, ItemCount - 1);
            lastIndex = Mathf.Clamp(lastIndex, 0, ItemCount - 1);
        }

        /// <summary>
        /// Calculate scroll position for specified index
        /// </summary>
        protected override Vector2 CalculateScrollPosition(int index)
        {
            if (index < 0 || index >= ItemCount)
            {
                return Content.anchoredPosition;
            }

            var targetY = index * (cellHeight + Spacing);

            if (Viewport != null && Content != null)
            {
                var viewportHeight = Viewport.rect.height;
                var contentHeight = CalculateContentSize().y;
                var maxScrollY = contentHeight - viewportHeight;

                if (targetY > maxScrollY)
                {
                    targetY = maxScrollY;
                }
            }

            return new Vector2(Content.anchoredPosition.x, targetY);
        }

        /// <summary>
        /// Jump immediately to specified index
        /// </summary>
        /// <param name="index">Target index for jump (values -1 or less are treated as 0, default: 0)</param>
        public void JumpTo(int index = 0)
        {
            JumpToIndex(index);
        }

        /// <summary>
        /// Jump immediately to start (index 0)
        /// </summary>
        public void JumpToStart()
        {
            JumpToIndex(0);
        }

        /// <summary>
        /// Jump immediately to end (last index)
        /// </summary>
        public void JumpToEnd()
        {
            JumpToIndex(ItemCount - 1);
        }

        /// <summary>
        /// Scroll to specified index with animation
        /// </summary>
        /// <param name="index">Target index for scroll (values -1 or less are treated as 0)</param>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="ease">Easing type</param>
        /// <param name="onComplete">Callback on completion</param>
        public void ScrollTo(int index, float duration, Ease ease, Action onComplete = null)
        {
            ScrollToIndexAnimated(index, duration, ease, onComplete);
        }

        /// <summary>
        /// Scroll to start (index 0) with animation
        /// </summary>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="ease">Easing type</param>
        /// <param name="onComplete">Callback on completion</param>
        public void ScrollToStart(float duration, Ease ease, Action onComplete = null)
        {
            ScrollToIndexAnimated(0, duration, ease, onComplete);
        }

        /// <summary>
        /// Scroll to end (last index) with animation
        /// </summary>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="ease">Easing type</param>
        /// <param name="onComplete">Callback on completion</param>
        public void ScrollToEnd(float duration, Ease ease, Action onComplete = null)
        {
            ScrollToIndexAnimated(ItemCount - 1, duration, ease, onComplete);
        }

        /// <summary>
        /// Calculate scroll position offset for insertion at start
        /// </summary>
        protected override Vector2 CalculateOffsetForInsertedItems(int insertedCount)
        {
            var offsetY = insertedCount * (cellHeight + Spacing);
            return new Vector2(0, offsetY);
        }

        /// <summary>
        /// Check if content needs scrolling
        /// </summary>
        protected override bool IsContentScrollable(Vector2 contentSize)
        {
            if (ScrollRect == null) return false;

            var scrollRectTransform = ScrollRect.transform as RectTransform;
            if (scrollRectTransform == null) return false;

            var viewportHeight = scrollRectTransform.rect.height;
            return contentSize.y > viewportHeight;
        }
    }
}
