using System;
using UnityEngine;

namespace NoranDev.ScrollVirtualizer
{
    /// <summary>
    /// ScrollVirtualizer for horizontal virtualized scrolling
    /// </summary>
    /// <typeparam name="TCell">Cell component type</typeparam>
    /// <typeparam name="TData">Data type</typeparam>
    public abstract class HorizontalScrollVirtualizer<TCell, TData> : ScrollVirtualizerBase<TCell, TData> where TCell : ScrollVirtualizerCell<TData>
    {
        [Header("Horizontal Settings")]
        [SerializeField] private bool useDynamicCellHeight = false;
        [SerializeField] private float cellWidth = 100f;
        [SerializeField] private float cellHeight = 100f;
        [SerializeField] private HorizontalContentAlignment horizontalContentAlignment = HorizontalContentAlignment.Top;

        protected override ScrollDirection ScrollDirection => ScrollDirection.Horizontal;

        /// <summary>
        /// Initialization
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (ScrollRect != null && !ScrollRect.horizontal)
            {
                Debug.LogWarning("[HorizontalScrollVirtualizer] ScrollRect.horizontal is disabled. Please enable it in the Inspector.", this);
            }
        }

        /// <summary>
        /// Get current scroll position
        /// </summary>
        protected override float GetCurrentScrollPosition()
        {
            return Content != null ? -Content.anchoredPosition.x : 0f;
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

            var contentWidth = Content.sizeDelta.x;
            var viewportWidth = Viewport.rect.width;
            return Mathf.Max(0, contentWidth - viewportWidth);
        }

        /// <summary>
        /// Get cell size
        /// </summary>
        protected override Vector2 GetCellSize()
        {
            var height = useDynamicCellHeight && Viewport != null
                ? Viewport.rect.height - PaddingTop - PaddingBottom
                : cellHeight;

            return new Vector2(cellWidth, height);
        }

        /// <summary>
        /// Calculate total content size
        /// </summary>
        protected override Vector2 CalculateContentSize()
        {
            var viewportWidth = Viewport != null ? Viewport.rect.width : 0f;

            if (ItemCount == 0)
            {
                return new Vector2(viewportWidth, 0f);
            }

            var totalWidth = PaddingLeft + (cellWidth + Spacing) * ItemCount - Spacing + PaddingRight;

            return new Vector2(totalWidth, 0f);
        }

        /// <summary>
        /// Calculate cell position for specified index
        /// </summary>
        protected override Vector2 CalculateCellPosition(int index)
        {
            var x = PaddingLeft + index * (cellWidth + Spacing);

            var y = -PaddingTop;
            if (!useDynamicCellHeight && Viewport != null)
            {
                var viewportHeight = Viewport.rect.height;
                switch (horizontalContentAlignment)
                {
                    case HorizontalContentAlignment.Top:
                        y = -PaddingTop;
                        break;
                    case HorizontalContentAlignment.Center:
                        y = -(viewportHeight - cellHeight) * 0.5f;
                        break;
                    case HorizontalContentAlignment.Bottom:
                        y = -(viewportHeight - cellHeight - PaddingBottom);
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

            var scrollPosition = -Content.anchoredPosition.x - PaddingLeft;
            var viewportWidth = Viewport.rect.width;
            var cellSize = cellWidth + Spacing;

            firstIndex = Mathf.FloorToInt(scrollPosition / cellSize);
            firstIndex = Mathf.Max(0, firstIndex - VisibleCellBuffer);

            lastIndex = Mathf.CeilToInt((scrollPosition + viewportWidth) / cellSize);
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

            var scrollPosition = -Content.anchoredPosition.x - PaddingLeft;
            var viewportWidth = Viewport.rect.width;
            var cellSize = cellWidth + Spacing;

            firstIndex = Mathf.FloorToInt((scrollPosition - cellWidth) / cellSize) + 1;
            firstIndex = Mathf.Max(0, firstIndex);

            lastIndex = Mathf.FloorToInt((scrollPosition + viewportWidth - 1) / cellSize);
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

            var targetX = -(index * (cellWidth + Spacing));

            if (Viewport != null && Content != null)
            {
                var viewportWidth = Viewport.rect.width;
                var contentWidth = CalculateContentSize().x;
                var maxScrollX = -(contentWidth - viewportWidth);

                if (targetX < maxScrollX)
                {
                    targetX = maxScrollX;
                }
            }

            return new Vector2(targetX, Content.anchoredPosition.y);
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
            var offsetX = -insertedCount * (cellWidth + Spacing);
            return new Vector2(offsetX, 0);
        }

        /// <summary>
        /// Check if content needs scrolling
        /// </summary>
        protected override bool IsContentScrollable(Vector2 contentSize)
        {
            if (ScrollRect == null) return false;

            var scrollRectTransform = ScrollRect.transform as RectTransform;
            if (scrollRectTransform == null) return false;

            var viewportWidth = scrollRectTransform.rect.width;
            return contentSize.x > viewportWidth;
        }
    }

    /// <summary>
    /// ScrollVirtualizer for horizontal virtualized scrolling (context-enabled version)
    /// </summary>
    /// <typeparam name="TCell">Cell component type</typeparam>
    /// <typeparam name="TData">Data type</typeparam>
    /// <typeparam name="TContext">Context type</typeparam>
    public abstract class HorizontalScrollVirtualizerWithContext<TCell, TData, TContext> : ScrollVirtualizerBaseWithContext<TCell, TData, TContext>
        where TCell : ScrollVirtualizerCellWithContext<TData, TContext>
    {
        [Header("Horizontal Settings")]
        [SerializeField] private bool useDynamicCellHeight = false;
        [SerializeField] private float cellWidth = 100f;
        [SerializeField] private float cellHeight = 100f;
        [SerializeField] private HorizontalContentAlignment horizontalContentAlignment = HorizontalContentAlignment.Top;

        protected override ScrollDirection ScrollDirection => ScrollDirection.Horizontal;

        protected override void Awake()
        {
            base.Awake();

            if (ScrollRect != null && !ScrollRect.horizontal)
            {
                Debug.LogWarning("[HorizontalScrollVirtualizer] ScrollRect.horizontal is disabled. Please enable it in the Inspector.", this);
            }
        }

        /// <summary>
        /// Get current scroll position
        /// </summary>
        protected override float GetCurrentScrollPosition()
        {
            return Content != null ? -Content.anchoredPosition.x : 0f;
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

            var contentWidth = Content.sizeDelta.x;
            var viewportWidth = Viewport.rect.width;
            return Mathf.Max(0, contentWidth - viewportWidth);
        }

        /// <summary>
        /// Get cell size
        /// </summary>
        protected override Vector2 GetCellSize()
        {
            var height = useDynamicCellHeight && Viewport != null
                ? Viewport.rect.height - PaddingTop - PaddingBottom
                : cellHeight;

            return new Vector2(cellWidth, height);
        }

        /// <summary>
        /// Calculate total content size
        /// </summary>
        protected override Vector2 CalculateContentSize()
        {
            var viewportWidth = Viewport != null ? Viewport.rect.width : 0f;

            if (ItemCount == 0)
            {
                return new Vector2(viewportWidth, 0f);
            }

            var totalWidth = PaddingLeft + (cellWidth + Spacing) * ItemCount - Spacing + PaddingRight;

            return new Vector2(totalWidth, 0f);
        }

        /// <summary>
        /// Calculate cell position for specified index
        /// </summary>
        protected override Vector2 CalculateCellPosition(int index)
        {
            var x = PaddingLeft + index * (cellWidth + Spacing);

            var y = -PaddingTop;
            if (!useDynamicCellHeight && Viewport != null)
            {
                var viewportHeight = Viewport.rect.height;
                switch (horizontalContentAlignment)
                {
                    case HorizontalContentAlignment.Top:
                        y = -PaddingTop;
                        break;
                    case HorizontalContentAlignment.Center:
                        y = -(viewportHeight - cellHeight) * 0.5f;
                        break;
                    case HorizontalContentAlignment.Bottom:
                        y = -(viewportHeight - cellHeight - PaddingBottom);
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

            var scrollPosition = -Content.anchoredPosition.x - PaddingLeft;
            var viewportWidth = Viewport.rect.width;
            var cellSize = cellWidth + Spacing;

            firstIndex = Mathf.FloorToInt(scrollPosition / cellSize);
            firstIndex = Mathf.Max(0, firstIndex - VisibleCellBuffer);

            lastIndex = Mathf.CeilToInt((scrollPosition + viewportWidth) / cellSize);
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

            var scrollPosition = -Content.anchoredPosition.x - PaddingLeft;
            var viewportWidth = Viewport.rect.width;
            var cellSize = cellWidth + Spacing;

            firstIndex = Mathf.FloorToInt((scrollPosition - cellWidth) / cellSize) + 1;
            firstIndex = Mathf.Max(0, firstIndex);

            lastIndex = Mathf.FloorToInt((scrollPosition + viewportWidth - 1) / cellSize);
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

            var targetX = -(index * (cellWidth + Spacing));

            if (Viewport != null && Content != null)
            {
                var viewportWidth = Viewport.rect.width;
                var contentWidth = CalculateContentSize().x;
                var maxScrollX = -(contentWidth - viewportWidth);

                if (targetX < maxScrollX)
                {
                    targetX = maxScrollX;
                }
            }

            return new Vector2(targetX, Content.anchoredPosition.y);
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
            var offsetX = -insertedCount * (cellWidth + Spacing);
            return new Vector2(offsetX, 0);
        }

        /// <summary>
        /// Check if content needs scrolling
        /// </summary>
        protected override bool IsContentScrollable(Vector2 contentSize)
        {
            if (ScrollRect == null) return false;

            var scrollRectTransform = ScrollRect.transform as RectTransform;
            if (scrollRectTransform == null) return false;

            var viewportWidth = scrollRectTransform.rect.width;
            return contentSize.x > viewportWidth;
        }
    }
}
