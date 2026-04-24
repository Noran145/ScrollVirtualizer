using System;
using UnityEngine;

#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace NoranDev.ScrollVirtualizer
{
    /// <summary>
    /// ScrollVirtualizer for grid-based virtualized scrolling
    /// </summary>
    /// <typeparam name="TCell">Cell component type</typeparam>
    /// <typeparam name="TData">Data type</typeparam>
    public abstract class GridScrollVirtualizer<TCell, TData> : ScrollVirtualizerBase<TCell, TData> where TCell : ScrollVirtualizerCell<TData>
    {
        [Header("Cell Settings")]
        [SerializeField] private float cellWidth = 100f;
        [SerializeField] private float cellHeight = 100f;

        [Header("Grid Settings")]
        [SerializeField] private float spacingX = 0f;
        [SerializeField] private float spacingY = 0f;
        [SerializeField] private GridStartCorner startCorner = GridStartCorner.UpperLeft;
        [SerializeField] private GridAxis startAxis = GridAxis.Horizontal;
        [SerializeField] private GridChildAlignment childAlignment = GridChildAlignment.UpperLeft;
        [SerializeField] private GridConstraint constraint = GridConstraint.FixedColumnCount;
        [SerializeField] private int constraintCount = 3;

        [Header("Common Settings")]
        [SerializeField] private ScrollDirection scrollDirection;

        protected override ScrollDirection ScrollDirection => scrollDirection;

        protected float SpacingX => spacingX;
        protected float SpacingY => spacingY;
        protected GridConstraint Constraint => constraint;
        protected int ConstraintCount => constraintCount;

        private bool _hasSetInitialPosition;

        /// <summary>
        /// Update (set initial scroll position)
        /// </summary>
        protected virtual void Update()
        {
            if (!_hasSetInitialPosition && ItemCount > 0)
            {
                SetInitialScrollPosition();
            }
        }

        /// <summary>
        /// Set initial scroll position
        /// </summary>
        private void SetInitialScrollPosition()
        {
            _hasSetInitialPosition = true;

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            if (isVerticalScroll)
            {
                if (startCorner is GridStartCorner.LowerLeft or GridStartCorner.LowerRight)
                {
                    var contentHeight = Content.sizeDelta.y;
                    var viewportHeight = Viewport.rect.height;
                    var maxY = Mathf.Max(0, contentHeight - viewportHeight);
                    Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, maxY);
                }
            }
            else
            {
                if (startCorner is GridStartCorner.UpperRight or GridStartCorner.LowerRight)
                {
                    var contentWidth = Content.sizeDelta.x;
                    var viewportWidth = Viewport.rect.width;
                    var minX = -Mathf.Max(0, contentWidth - viewportWidth);
                    Content.anchoredPosition = new Vector2(minX, Content.anchoredPosition.y);
                }
            }
        }

        /// <summary>
        /// Get current scroll position
        /// </summary>
        protected override float GetCurrentScrollPosition()
        {
            if (Content == null)
            {
                return 0f;
            }

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            return isVerticalScroll ? Content.anchoredPosition.y : -Content.anchoredPosition.x;
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

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            if (isVerticalScroll)
            {
                var contentHeight = Content.sizeDelta.y;
                var viewportHeight = Viewport.rect.height;
                return Mathf.Max(0, contentHeight - viewportHeight);
            }
            else
            {
                var contentWidth = Content.sizeDelta.x;
                var viewportWidth = Viewport.rect.width;
                return Mathf.Max(0, contentWidth - viewportWidth);
            }
        }

        /// <summary>
        /// Get cell size
        /// </summary>
        protected override Vector2 GetCellSize()
        {
            return new Vector2(cellWidth, cellHeight);
        }

        /// <summary>
        /// Calculate total content size
        /// </summary>
        protected override Vector2 CalculateContentSize()
        {
            if (constraint == GridConstraint.Flexible)
            {
                Canvas.ForceUpdateCanvases();
            }

            var viewportWidth = Viewport != null ? Viewport.rect.width : 0f;
            var viewportHeight = Viewport != null ? Viewport.rect.height : 0f;

            if (ItemCount == 0)
            {
                return new Vector2(viewportWidth, viewportHeight);
            }

            GetGridDimensions(out var columnCount, out var rowCount);

            var gridWidth = PaddingLeft + (cellWidth + spacingX) * columnCount - spacingX + PaddingRight;
            var gridHeight = PaddingTop + (cellHeight + spacingY) * rowCount - spacingY + PaddingBottom;

            var totalWidth = Mathf.Max(viewportWidth, gridWidth);
            var totalHeight = Mathf.Max(viewportHeight, gridHeight);

            return new Vector2(totalWidth, totalHeight);
        }

        /// <summary>
        /// Get grid column and row counts
        /// </summary>
        private void GetGridDimensions(out int columnCount, out int rowCount)
        {
            var viewportWidth = Viewport != null ? Viewport.rect.width : 0f;
            var viewportHeight = Viewport != null ? Viewport.rect.height : 0f;

            switch (constraint)
            {
                case GridConstraint.Flexible:
                    var availableWidth = viewportWidth - PaddingLeft - PaddingRight + spacingX;
                    columnCount = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (cellWidth + spacingX)));
                    rowCount = Mathf.CeilToInt((float)ItemCount / columnCount);
                    break;

                case GridConstraint.FixedColumnCount:
                    columnCount = Mathf.Max(1, constraintCount);
                    rowCount = Mathf.CeilToInt((float)ItemCount / columnCount);
                    break;

                case GridConstraint.FixedRowCount:
                    rowCount = Mathf.Max(1, constraintCount);
                    columnCount = Mathf.CeilToInt((float)ItemCount / rowCount);
                    break;

                default:
                    columnCount = 1;
                    rowCount = ItemCount;
                    break;
            }
        }

        /// <summary>
        /// Calculate cell position for specified index
        /// </summary>
        protected override Vector2 CalculateCellPosition(int index)
        {
            GetGridDimensions(out var columnCount, out var rowCount);

            int row, column;

            if (constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible)
            {
                row = index / columnCount;
                column = index % columnCount;
            }
            else
            {
                column = index / rowCount;
                row = index % rowCount;
            }

            switch (startCorner)
            {
                case GridStartCorner.UpperRight:
                    column = columnCount - 1 - column;
                    break;
                case GridStartCorner.LowerLeft:
                    row = rowCount - 1 - row;
                    break;
                case GridStartCorner.LowerRight:
                    column = columnCount - 1 - column;
                    row = rowCount - 1 - row;
                    break;
            }

            var x = PaddingLeft + column * (cellWidth + spacingX);
            var y = -(PaddingTop + row * (cellHeight + spacingY));

            var alignmentOffset = GetChildAlignmentOffset(columnCount, rowCount);
            x += alignmentOffset.x;
            y += alignmentOffset.y;

            return new Vector2(x, y);
        }

        /// <summary>
        /// Get child layout offset
        /// </summary>
        private Vector2 GetChildAlignmentOffset(int columnCount, int rowCount)
        {
            var viewportWidth = Viewport != null ? Viewport.rect.width : 0f;
            var viewportHeight = Viewport != null ? Viewport.rect.height : 0f;

            var gridWidth = columnCount * cellWidth + (columnCount - 1) * spacingX;
            var gridHeight = rowCount * cellHeight + (rowCount - 1) * spacingY;

            var availableWidth = viewportWidth - PaddingLeft - PaddingRight;
            var availableHeight = viewportHeight - PaddingTop - PaddingBottom;

            var offsetX = 0f;
            var offsetY = 0f;

            if (gridWidth < availableWidth)
            {
                switch (childAlignment)
                {
                    case GridChildAlignment.UpperCenter:
                    case GridChildAlignment.MiddleCenter:
                    case GridChildAlignment.LowerCenter:
                        offsetX = (availableWidth - gridWidth) / 2f;
                        break;
                    case GridChildAlignment.UpperRight:
                    case GridChildAlignment.MiddleRight:
                    case GridChildAlignment.LowerRight:
                        offsetX = availableWidth - gridWidth;
                        break;
                }
            }

            if (gridHeight < availableHeight)
            {
                switch (childAlignment)
                {
                    case GridChildAlignment.MiddleLeft:
                    case GridChildAlignment.MiddleCenter:
                    case GridChildAlignment.MiddleRight:
                        offsetY = -(availableHeight - gridHeight) / 2f;
                        break;
                    case GridChildAlignment.LowerLeft:
                    case GridChildAlignment.LowerCenter:
                    case GridChildAlignment.LowerRight:
                        offsetY = -(availableHeight - gridHeight);
                        break;
                }
            }

            return new Vector2(offsetX, offsetY);
        }

        /// <summary>
        /// Calculate index range to display (with buffer)
        /// </summary>
        protected override void CalculateVisibleRange(out int firstIndex, out int lastIndex)
        {
            if (ItemCount == 0 || Viewport == null || Content == null)
            {
                firstIndex = 0;
                lastIndex = -1;
                return;
            }

            GetGridDimensions(out var columnCount, out var rowCount);

            var viewportWidth = Viewport.rect.width;
            var viewportHeight = Viewport.rect.height;

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            var alignmentOffset = GetChildAlignmentOffset(columnCount, rowCount);

            if (isVerticalScroll)
            {
                var scrollPosition = Content.anchoredPosition.y - PaddingTop - alignmentOffset.y;
                var cellHeightWithSpacing = cellHeight + spacingY;

                var firstRow = Mathf.FloorToInt(scrollPosition / cellHeightWithSpacing);
                firstRow = Mathf.Max(0, firstRow);

                var lastRow = Mathf.CeilToInt((scrollPosition + viewportHeight) / cellHeightWithSpacing) - 1;
                lastRow = Mathf.Min(rowCount - 1, lastRow);

                if (startCorner is GridStartCorner.LowerLeft or GridStartCorner.LowerRight)
                {
                    var logicalFirst = rowCount - 1 - lastRow;
                    var logicalLast = rowCount - 1 - firstRow;
                    firstRow = logicalFirst;
                    lastRow = logicalLast;
                }

                if (constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible)
                {
                    firstIndex = firstRow * columnCount;
                    lastIndex = Mathf.Min(ItemCount - 1, (lastRow + 1) * columnCount - 1);
                }
                else
                {
                    var minIndex = firstRow;
                    var maxIndex = (columnCount - 1) * rowCount + lastRow;

                    firstIndex = Mathf.Min(minIndex, maxIndex);
                    lastIndex = Mathf.Min(ItemCount - 1, Mathf.Max(minIndex, maxIndex));
                }
            }
            else
            {
                var scrollPosition = -Content.anchoredPosition.x - PaddingLeft - alignmentOffset.x;
                var cellWidthWithSpacing = cellWidth + spacingX;

                var firstColumn = Mathf.FloorToInt(scrollPosition / cellWidthWithSpacing);
                firstColumn = Mathf.Max(0, firstColumn);

                var lastColumn = Mathf.CeilToInt((scrollPosition + viewportWidth) / cellWidthWithSpacing) - 1;
                lastColumn = Mathf.Min(columnCount - 1, lastColumn);

                if (startCorner is GridStartCorner.UpperRight or GridStartCorner.LowerRight)
                {
                    var logicalFirst = columnCount - 1 - lastColumn;
                    var logicalLast = columnCount - 1 - firstColumn;
                    firstColumn = logicalFirst;
                    lastColumn = logicalLast;
                }

                firstIndex = firstColumn * rowCount;
                lastIndex = Mathf.Min(ItemCount - 1, (lastColumn + 1) * rowCount - 1);
            }

            if (lastIndex - firstIndex + 1 > MaxRecycleCount)
            {
                lastIndex = firstIndex + MaxRecycleCount - 1;
            }

            firstIndex = Mathf.Clamp(firstIndex, 0, ItemCount - 1);
            lastIndex = Mathf.Clamp(lastIndex, 0, ItemCount - 1);
        }

        /// <summary>
        /// Calculate actually displayed index range (without buffer)
        /// </summary>
        protected override void CalculateActualVisibleRange(out int firstIndex, out int lastIndex)
        {
            if (ItemCount == 0 || Viewport == null || Content == null)
            {
                firstIndex = 0;
                lastIndex = -1;
                return;
            }

            GetGridDimensions(out var columnCount, out var rowCount);

            var viewportWidth = Viewport.rect.width;
            var viewportHeight = Viewport.rect.height;
            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            var alignmentOffset = GetChildAlignmentOffset(columnCount, rowCount);

            if (isVerticalScroll)
            {
                var scrollPosition = Content.anchoredPosition.y - PaddingTop - alignmentOffset.y;
                var cellHeightWithSpacing = cellHeight + spacingY;

                var firstRow = Mathf.FloorToInt((scrollPosition - cellHeight) / cellHeightWithSpacing) + 1;
                firstRow = Mathf.Max(0, firstRow);

                var lastRow = Mathf.FloorToInt((scrollPosition + viewportHeight - 1) / cellHeightWithSpacing);
                lastRow = Mathf.Min(rowCount - 1, lastRow);

                if (startCorner is GridStartCorner.LowerLeft or GridStartCorner.LowerRight)
                {
                    var logicalFirst = rowCount - 1 - lastRow;
                    var logicalLast = rowCount - 1 - firstRow;
                    firstRow = logicalFirst;
                    lastRow = logicalLast;
                }

                if (constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible)
                {
                    firstIndex = firstRow * columnCount;
                    lastIndex = Mathf.Min(ItemCount - 1, (lastRow + 1) * columnCount - 1);
                }
                else
                {
                    var minIndex = firstRow;
                    var maxIndex = (columnCount - 1) * rowCount + lastRow;

                    firstIndex = Mathf.Min(minIndex, maxIndex);
                    lastIndex = Mathf.Min(ItemCount - 1, Mathf.Max(minIndex, maxIndex));
                }
            }
            else
            {
                var scrollPosition = -Content.anchoredPosition.x - PaddingLeft - alignmentOffset.x;
                var cellWidthWithSpacing = cellWidth + spacingX;

                var firstColumn = Mathf.FloorToInt((scrollPosition - cellWidth) / cellWidthWithSpacing) + 1;
                firstColumn = Mathf.Max(0, firstColumn);

                var lastColumn = Mathf.FloorToInt((scrollPosition + viewportWidth - 1) / cellWidthWithSpacing);
                lastColumn = Mathf.Min(columnCount - 1, lastColumn);

                if (startCorner is GridStartCorner.UpperRight or GridStartCorner.LowerRight)
                {
                    var logicalFirst = columnCount - 1 - lastColumn;
                    var logicalLast = columnCount - 1 - firstColumn;
                    firstColumn = logicalFirst;
                    lastColumn = logicalLast;
                }

                firstIndex = firstColumn * rowCount;
                lastIndex = Mathf.Min(ItemCount - 1, (lastColumn + 1) * rowCount - 1);
            }

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

            GetGridDimensions(out var columnCount, out var rowCount);

            int row, column;

            if (constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible)
            {
                row = index / columnCount;
                column = index % columnCount;
            }
            else
            {
                column = index / rowCount;
                row = index % rowCount;
            }

            switch (startCorner)
            {
                case GridStartCorner.UpperRight:
                    column = columnCount - 1 - column;
                    break;
                case GridStartCorner.LowerLeft:
                    row = rowCount - 1 - row;
                    break;
                case GridStartCorner.LowerRight:
                    column = columnCount - 1 - column;
                    row = rowCount - 1 - row;
                    break;
            }

            var alignmentOffset = GetChildAlignmentOffset(columnCount, rowCount);

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            if (isVerticalScroll)
            {
                var targetY = row * (cellHeight + spacingY) - alignmentOffset.y;

                if (startCorner is GridStartCorner.LowerLeft or GridStartCorner.LowerRight)
                {
                    if (Viewport != null)
                    {
                        var viewportHeight = Viewport.rect.height;
                        targetY = targetY - viewportHeight + cellHeight;
                    }
                }

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
            else
            {
                var targetX = -(column * (cellWidth + spacingX) - alignmentOffset.x);

                if (startCorner is GridStartCorner.UpperRight or GridStartCorner.LowerRight)
                {
                    if (Viewport != null)
                    {
                        var viewportWidth = Viewport.rect.width;
                        targetX = targetX + viewportWidth - cellWidth;
                    }
                }

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
        }

        /// <summary>
        /// Jump immediately to specified index
        /// </summary>
        /// <param name="index">Target index for jump (values -1 or less are treated as 0, default: 0)</param>
        public void JumpTo(int index = 0)
        {
            _hasSetInitialPosition = true;

            JumpToIndex(index);
        }

        /// <summary>
        /// Jump immediately to start (index 0)
        /// </summary>
        public void JumpToStart()
        {
            JumpTo(0);
        }

        /// <summary>
        /// Jump immediately to end (last index)
        /// </summary>
        public void JumpToEnd()
        {
            JumpTo(ItemCount - 1);
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
            if (startAxis == GridAxis.Horizontal)
            {
                var insertedRows = Mathf.CeilToInt((float)insertedCount / constraintCount);
                var offsetY = insertedRows * (cellHeight + spacingY);
                return new Vector2(0, offsetY);
            }
            else
            {
                var insertedColumns = Mathf.CeilToInt((float)insertedCount / constraintCount);
                var offsetX = -insertedColumns * (cellWidth + spacingX);
                return new Vector2(offsetX, 0);
            }
        }

        /// <summary>
        /// Check if content needs scrolling
        /// </summary>
        protected override bool IsContentScrollable(Vector2 contentSize)
        {
            if (ScrollRect == null) return false;

            var scrollRectTransform = ScrollRect.transform as RectTransform;
            if (scrollRectTransform == null) return false;

            if (ScrollDirection == ScrollDirection.Vertical)
            {
                var viewportHeight = scrollRectTransform.rect.height;
                return contentSize.y > viewportHeight;
            }
            else
            {
                var viewportWidth = scrollRectTransform.rect.width;
                return contentSize.x > viewportWidth;
            }
        }

        /// <summary>
        /// Called when drag ends
        /// </summary>
        protected override void EndDragProcessing()
        {
            var shouldReverse = ShouldReversePullEvents();

            if (shouldReverse)
            {
                if (Content == null || Viewport == null) return;

                var topPull = GetTopPullAmount();
                var bottomPull = GetBottomPullAmount();

                if (topPull > PullThreshold)
                {
                    InvokeScrollPullEventAsync(PullDirection.End, GetScrollPullReleasedCallback(), destroyCancellationToken).Forget();
                }

                if (bottomPull > PullThreshold)
                {
                    InvokeScrollPullEventAsync(PullDirection.Start, GetScrollPullReleasedCallback(), destroyCancellationToken).Forget();
                }
            }
            else
            {
                base.EndDragProcessing();
            }
        }

        /// <summary>
        /// Determine whether to invert pull events based on StartCorner
        /// </summary>
        private bool ShouldReversePullEvents()
        {
            if (scrollDirection == ScrollDirection.Vertical &&
                (startCorner == GridStartCorner.LowerLeft || startCorner == GridStartCorner.LowerRight))
            {
                return true;
            }

            if (scrollDirection == ScrollDirection.Horizontal &&
                (startCorner == GridStartCorner.UpperRight || startCorner == GridStartCorner.LowerRight))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// ScrollVirtualizer for grid-based virtualized scrolling (context-enabled version)
    /// </summary>
    /// <typeparam name="TCell">Cell component type</typeparam>
    /// <typeparam name="TData">Data type</typeparam>
    /// <typeparam name="TContext">Context type</typeparam>
    public abstract class GridScrollVirtualizerWithContext<TCell, TData, TContext> : ScrollVirtualizerBaseWithContext<TCell, TData, TContext>
        where TCell : ScrollVirtualizerCellWithContext<TData, TContext>
    {
        [Header("Cell Settings")]
        [SerializeField] private float cellWidth = 100f;
        [SerializeField] private float cellHeight = 100f;

        [Header("Grid Settings")]
        [SerializeField] private float spacingX = 0f;
        [SerializeField] private float spacingY = 0f;
        [SerializeField] private GridStartCorner startCorner = GridStartCorner.UpperLeft;
        [SerializeField] private GridAxis startAxis = GridAxis.Horizontal;
        [SerializeField] private GridChildAlignment childAlignment = GridChildAlignment.UpperLeft;
        [SerializeField] private GridConstraint constraint = GridConstraint.FixedColumnCount;
        [SerializeField] private int constraintCount = 3;

        [Header("Common Settings")]
        [SerializeField] private ScrollDirection scrollDirection;

        protected override ScrollDirection ScrollDirection => scrollDirection;

        protected float SpacingX => spacingX;
        protected float SpacingY => spacingY;
        protected GridConstraint Constraint => constraint;
        protected int ConstraintCount => constraintCount;

        private bool _hasSetInitialPosition;

        /// <summary>
        /// Update (set initial scroll position)
        /// </summary>
        protected virtual void Update()
        {
            if (!_hasSetInitialPosition && ItemCount > 0)
            {
                SetInitialScrollPosition();
            }
        }

        /// <summary>
        /// Set initial scroll position
        /// </summary>
        private void SetInitialScrollPosition()
        {
            _hasSetInitialPosition = true;

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            if (isVerticalScroll)
            {
                if (startCorner is GridStartCorner.LowerLeft or GridStartCorner.LowerRight)
                {
                    var contentHeight = Content.sizeDelta.y;
                    var viewportHeight = Viewport.rect.height;
                    var maxY = Mathf.Max(0, contentHeight - viewportHeight);
                    Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, maxY);
                }
            }
            else
            {
                if (startCorner is GridStartCorner.UpperRight or GridStartCorner.LowerRight)
                {
                    var contentWidth = Content.sizeDelta.x;
                    var viewportWidth = Viewport.rect.width;
                    var minX = -Mathf.Max(0, contentWidth - viewportWidth);
                    Content.anchoredPosition = new Vector2(minX, Content.anchoredPosition.y);
                }
            }
        }

        /// <summary>
        /// Get current scroll position
        /// </summary>
        protected override float GetCurrentScrollPosition()
        {
            if (Content == null)
            {
                return 0f;
            }

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            return isVerticalScroll ? Content.anchoredPosition.y : -Content.anchoredPosition.x;
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

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            if (isVerticalScroll)
            {
                var contentHeight = Content.sizeDelta.y;
                var viewportHeight = Viewport.rect.height;
                return Mathf.Max(0, contentHeight - viewportHeight);
            }
            else
            {
                var contentWidth = Content.sizeDelta.x;
                var viewportWidth = Viewport.rect.width;
                return Mathf.Max(0, contentWidth - viewportWidth);
            }
        }

        /// <summary>
        /// Get cell size
        /// </summary>
        protected override Vector2 GetCellSize()
        {
            return new Vector2(cellWidth, cellHeight);
        }

        /// <summary>
        /// Calculate total content size
        /// </summary>
        protected override Vector2 CalculateContentSize()
        {
            if (constraint == GridConstraint.Flexible)
            {
                Canvas.ForceUpdateCanvases();
            }

            var viewportWidth = Viewport != null ? Viewport.rect.width : 0f;
            var viewportHeight = Viewport != null ? Viewport.rect.height : 0f;

            if (ItemCount == 0)
            {
                return new Vector2(viewportWidth, viewportHeight);
            }

            GetGridDimensions(out var columnCount, out var rowCount);

            var gridWidth = PaddingLeft + (cellWidth + spacingX) * columnCount - spacingX + PaddingRight;
            var gridHeight = PaddingTop + (cellHeight + spacingY) * rowCount - spacingY + PaddingBottom;

            var totalWidth = Mathf.Max(viewportWidth, gridWidth);
            var totalHeight = Mathf.Max(viewportHeight, gridHeight);

            return new Vector2(totalWidth, totalHeight);
        }

        /// <summary>
        /// Get grid column and row counts
        /// </summary>
        private void GetGridDimensions(out int columnCount, out int rowCount)
        {
            var viewportWidth = Viewport != null ? Viewport.rect.width : 0f;
            var viewportHeight = Viewport != null ? Viewport.rect.height : 0f;

            switch (constraint)
            {
                case GridConstraint.Flexible:
                    var availableWidth = viewportWidth - PaddingLeft - PaddingRight + spacingX;
                    columnCount = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (cellWidth + spacingX)));
                    rowCount = Mathf.CeilToInt((float)ItemCount / columnCount);
                    break;

                case GridConstraint.FixedColumnCount:
                    columnCount = Mathf.Max(1, constraintCount);
                    rowCount = Mathf.CeilToInt((float)ItemCount / columnCount);
                    break;

                case GridConstraint.FixedRowCount:
                    rowCount = Mathf.Max(1, constraintCount);
                    columnCount = Mathf.CeilToInt((float)ItemCount / rowCount);
                    break;

                default:
                    columnCount = 1;
                    rowCount = ItemCount;
                    break;
            }
        }

        /// <summary>
        /// Calculate cell position for specified index
        /// </summary>
        protected override Vector2 CalculateCellPosition(int index)
        {
            GetGridDimensions(out var columnCount, out var rowCount);

            int row, column;

            if (constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible)
            {
                row = index / columnCount;
                column = index % columnCount;
            }
            else
            {
                column = index / rowCount;
                row = index % rowCount;
            }

            switch (startCorner)
            {
                case GridStartCorner.UpperRight:
                    column = columnCount - 1 - column;
                    break;
                case GridStartCorner.LowerLeft:
                    row = rowCount - 1 - row;
                    break;
                case GridStartCorner.LowerRight:
                    column = columnCount - 1 - column;
                    row = rowCount - 1 - row;
                    break;
            }

            var x = PaddingLeft + column * (cellWidth + spacingX);
            var y = -(PaddingTop + row * (cellHeight + spacingY));

            var alignmentOffset = GetChildAlignmentOffset(columnCount, rowCount);
            x += alignmentOffset.x;
            y += alignmentOffset.y;

            return new Vector2(x, y);
        }

        /// <summary>
        /// Get child layout offset
        /// </summary>
        private Vector2 GetChildAlignmentOffset(int columnCount, int rowCount)
        {
            var viewportWidth = Viewport != null ? Viewport.rect.width : 0f;
            var viewportHeight = Viewport != null ? Viewport.rect.height : 0f;

            var gridWidth = columnCount * cellWidth + (columnCount - 1) * spacingX;
            var gridHeight = rowCount * cellHeight + (rowCount - 1) * spacingY;

            var availableWidth = viewportWidth - PaddingLeft - PaddingRight;
            var availableHeight = viewportHeight - PaddingTop - PaddingBottom;

            var offsetX = 0f;
            var offsetY = 0f;

            if (gridWidth < availableWidth)
            {
                switch (childAlignment)
                {
                    case GridChildAlignment.UpperCenter:
                    case GridChildAlignment.MiddleCenter:
                    case GridChildAlignment.LowerCenter:
                        offsetX = (availableWidth - gridWidth) / 2f;
                        break;
                    case GridChildAlignment.UpperRight:
                    case GridChildAlignment.MiddleRight:
                    case GridChildAlignment.LowerRight:
                        offsetX = availableWidth - gridWidth;
                        break;
                }
            }

            if (gridHeight < availableHeight)
            {
                switch (childAlignment)
                {
                    case GridChildAlignment.MiddleLeft:
                    case GridChildAlignment.MiddleCenter:
                    case GridChildAlignment.MiddleRight:
                        offsetY = -(availableHeight - gridHeight) / 2f;
                        break;
                    case GridChildAlignment.LowerLeft:
                    case GridChildAlignment.LowerCenter:
                    case GridChildAlignment.LowerRight:
                        offsetY = -(availableHeight - gridHeight);
                        break;
                }
            }

            return new Vector2(offsetX, offsetY);
        }

        /// <summary>
        /// Calculate index range to display (with buffer)
        /// </summary>
        protected override void CalculateVisibleRange(out int firstIndex, out int lastIndex)
        {
            if (ItemCount == 0 || Viewport == null || Content == null)
            {
                firstIndex = 0;
                lastIndex = -1;
                return;
            }

            GetGridDimensions(out var columnCount, out var rowCount);

            var viewportWidth = Viewport.rect.width;
            var viewportHeight = Viewport.rect.height;

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            var alignmentOffset = GetChildAlignmentOffset(columnCount, rowCount);

            if (isVerticalScroll)
            {
                var scrollPosition = Content.anchoredPosition.y - PaddingTop - alignmentOffset.y;
                var cellHeightWithSpacing = cellHeight + spacingY;

                var firstRow = Mathf.FloorToInt(scrollPosition / cellHeightWithSpacing);
                firstRow = Mathf.Max(0, firstRow);

                var lastRow = Mathf.CeilToInt((scrollPosition + viewportHeight) / cellHeightWithSpacing) - 1;
                lastRow = Mathf.Min(rowCount - 1, lastRow);

                if (startCorner is GridStartCorner.LowerLeft or GridStartCorner.LowerRight)
                {
                    var logicalFirst = rowCount - 1 - lastRow;
                    var logicalLast = rowCount - 1 - firstRow;
                    firstRow = logicalFirst;
                    lastRow = logicalLast;
                }

                if (constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible)
                {
                    firstIndex = firstRow * columnCount;
                    lastIndex = Mathf.Min(ItemCount - 1, (lastRow + 1) * columnCount - 1);
                }
                else
                {
                    var minIndex = firstRow;
                    var maxIndex = (columnCount - 1) * rowCount + lastRow;

                    firstIndex = Mathf.Min(minIndex, maxIndex);
                    lastIndex = Mathf.Min(ItemCount - 1, Mathf.Max(minIndex, maxIndex));
                }
            }
            else
            {
                var scrollPosition = -Content.anchoredPosition.x - PaddingLeft - alignmentOffset.x;
                var cellWidthWithSpacing = cellWidth + spacingX;

                var firstColumn = Mathf.FloorToInt(scrollPosition / cellWidthWithSpacing);
                firstColumn = Mathf.Max(0, firstColumn);

                var lastColumn = Mathf.CeilToInt((scrollPosition + viewportWidth) / cellWidthWithSpacing) - 1;
                lastColumn = Mathf.Min(columnCount - 1, lastColumn);

                if (startCorner is GridStartCorner.UpperRight or GridStartCorner.LowerRight)
                {
                    var logicalFirst = columnCount - 1 - lastColumn;
                    var logicalLast = columnCount - 1 - firstColumn;
                    firstColumn = logicalFirst;
                    lastColumn = logicalLast;
                }

                firstIndex = firstColumn * rowCount;
                lastIndex = Mathf.Min(ItemCount - 1, (lastColumn + 1) * rowCount - 1);
            }

            if (lastIndex - firstIndex + 1 > MaxRecycleCount)
            {
                lastIndex = firstIndex + MaxRecycleCount - 1;
            }

            firstIndex = Mathf.Clamp(firstIndex, 0, ItemCount - 1);
            lastIndex = Mathf.Clamp(lastIndex, 0, ItemCount - 1);
        }

        /// <summary>
        /// Calculate actually displayed index range (without buffer)
        /// </summary>
        protected override void CalculateActualVisibleRange(out int firstIndex, out int lastIndex)
        {
            if (ItemCount == 0 || Viewport == null || Content == null)
            {
                firstIndex = 0;
                lastIndex = -1;
                return;
            }

            GetGridDimensions(out var columnCount, out var rowCount);

            var viewportWidth = Viewport.rect.width;
            var viewportHeight = Viewport.rect.height;
            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            var alignmentOffset = GetChildAlignmentOffset(columnCount, rowCount);

            if (isVerticalScroll)
            {
                var scrollPosition = Content.anchoredPosition.y - PaddingTop - alignmentOffset.y;
                var cellHeightWithSpacing = cellHeight + spacingY;

                var firstRow = Mathf.FloorToInt((scrollPosition - cellHeight) / cellHeightWithSpacing) + 1;
                firstRow = Mathf.Max(0, firstRow);

                var lastRow = Mathf.FloorToInt((scrollPosition + viewportHeight - 1) / cellHeightWithSpacing);
                lastRow = Mathf.Min(rowCount - 1, lastRow);

                if (startCorner is GridStartCorner.LowerLeft or GridStartCorner.LowerRight)
                {
                    var logicalFirst = rowCount - 1 - lastRow;
                    var logicalLast = rowCount - 1 - firstRow;
                    firstRow = logicalFirst;
                    lastRow = logicalLast;
                }

                if (constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible)
                {
                    firstIndex = firstRow * columnCount;
                    lastIndex = Mathf.Min(ItemCount - 1, (lastRow + 1) * columnCount - 1);
                }
                else
                {
                    var minIndex = firstRow;
                    var maxIndex = (columnCount - 1) * rowCount + lastRow;

                    firstIndex = Mathf.Min(minIndex, maxIndex);
                    lastIndex = Mathf.Min(ItemCount - 1, Mathf.Max(minIndex, maxIndex));
                }
            }
            else
            {
                var scrollPosition = -Content.anchoredPosition.x - PaddingLeft - alignmentOffset.x;
                var cellWidthWithSpacing = cellWidth + spacingX;

                var firstColumn = Mathf.FloorToInt((scrollPosition - cellWidth) / cellWidthWithSpacing) + 1;
                firstColumn = Mathf.Max(0, firstColumn);

                var lastColumn = Mathf.FloorToInt((scrollPosition + viewportWidth - 1) / cellWidthWithSpacing);
                lastColumn = Mathf.Min(columnCount - 1, lastColumn);

                if (startCorner is GridStartCorner.UpperRight or GridStartCorner.LowerRight)
                {
                    var logicalFirst = columnCount - 1 - lastColumn;
                    var logicalLast = columnCount - 1 - firstColumn;
                    firstColumn = logicalFirst;
                    lastColumn = logicalLast;
                }

                firstIndex = firstColumn * rowCount;
                lastIndex = Mathf.Min(ItemCount - 1, (lastColumn + 1) * rowCount - 1);
            }

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

            GetGridDimensions(out var columnCount, out var rowCount);

            int row, column;

            if (constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible)
            {
                row = index / columnCount;
                column = index % columnCount;
            }
            else
            {
                column = index / rowCount;
                row = index % rowCount;
            }

            switch (startCorner)
            {
                case GridStartCorner.UpperRight:
                    column = columnCount - 1 - column;
                    break;
                case GridStartCorner.LowerLeft:
                    row = rowCount - 1 - row;
                    break;
                case GridStartCorner.LowerRight:
                    column = columnCount - 1 - column;
                    row = rowCount - 1 - row;
                    break;
            }

            var alignmentOffset = GetChildAlignmentOffset(columnCount, rowCount);

            var isVerticalScroll = constraint is GridConstraint.FixedColumnCount or GridConstraint.Flexible;

            if (isVerticalScroll)
            {
                var targetY = row * (cellHeight + spacingY) - alignmentOffset.y;

                if (startCorner is GridStartCorner.LowerLeft or GridStartCorner.LowerRight)
                {
                    if (Viewport != null)
                    {
                        var viewportHeight = Viewport.rect.height;
                        targetY = targetY - viewportHeight + cellHeight;
                    }
                }

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
            else
            {
                var targetX = -(column * (cellWidth + spacingX) - alignmentOffset.x);

                if (startCorner is GridStartCorner.UpperRight or GridStartCorner.LowerRight)
                {
                    if (Viewport != null)
                    {
                        var viewportWidth = Viewport.rect.width;
                        targetX = targetX + viewportWidth - cellWidth;
                    }
                }

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
        }

        /// <summary>
        /// Jump immediately to specified index
        /// </summary>
        /// <param name="index">Target index for jump (values -1 or less are treated as 0, default: 0)</param>
        public void JumpTo(int index = 0)
        {
            _hasSetInitialPosition = true;

            JumpToIndex(index);
        }

        /// <summary>
        /// Jump immediately to start (index 0)
        /// </summary>
        public void JumpToStart()
        {
            JumpTo(0);
        }

        /// <summary>
        /// Jump immediately to end (last index)
        /// </summary>
        public void JumpToEnd()
        {
            JumpTo(ItemCount - 1);
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
            if (startAxis == GridAxis.Horizontal)
            {
                var insertedRows = Mathf.CeilToInt((float)insertedCount / constraintCount);
                var offsetY = insertedRows * (cellHeight + spacingY);
                return new Vector2(0, offsetY);
            }
            else
            {
                var insertedColumns = Mathf.CeilToInt((float)insertedCount / constraintCount);
                var offsetX = -insertedColumns * (cellWidth + spacingX);
                return new Vector2(offsetX, 0);
            }
        }

        /// <summary>
        /// Check if content needs scrolling
        /// </summary>
        protected override bool IsContentScrollable(Vector2 contentSize)
        {
            if (ScrollRect == null) return false;

            var scrollRectTransform = ScrollRect.transform as RectTransform;
            if (scrollRectTransform == null) return false;

            if (ScrollDirection == ScrollDirection.Vertical)
            {
                var viewportHeight = scrollRectTransform.rect.height;
                return contentSize.y > viewportHeight;
            }
            else
            {
                var viewportWidth = scrollRectTransform.rect.width;
                return contentSize.x > viewportWidth;
            }
        }

        /// <summary>
        /// Called when drag ends
        /// </summary>
        protected override void EndDragProcessing()
        {
            var shouldReverse = ShouldReversePullEvents();

            if (shouldReverse)
            {
                if (Content == null || Viewport == null) return;

                var topPull = GetTopPullAmount();
                var bottomPull = GetBottomPullAmount();

                if (topPull > PullThreshold)
                {
                    InvokeScrollPullEventAsync(PullDirection.End, GetScrollPullReleasedCallback(), destroyCancellationToken).Forget();
                }

                if (bottomPull > PullThreshold)
                {
                    InvokeScrollPullEventAsync(PullDirection.Start, GetScrollPullReleasedCallback(), destroyCancellationToken).Forget();
                }
            }
            else
            {
                base.EndDragProcessing();
            }
        }

        /// <summary>
        /// Determine whether to invert pull events based on StartCorner
        /// </summary>
        private bool ShouldReversePullEvents()
        {
            if (scrollDirection == ScrollDirection.Vertical &&
                (startCorner == GridStartCorner.LowerLeft || startCorner == GridStartCorner.LowerRight))
            {
                return true;
            }

            if (scrollDirection == ScrollDirection.Horizontal &&
                (startCorner == GridStartCorner.UpperRight || startCorner == GridStartCorner.LowerRight))
            {
                return true;
            }

            return false;
        }
    }
}
