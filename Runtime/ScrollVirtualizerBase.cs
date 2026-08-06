using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
using TaskType = Cysharp.Threading.Tasks.UniTask;
#else
using System.Threading.Tasks;
using TaskType = System.Threading.Tasks.Task;
#endif

namespace NoranDev.ScrollVirtualizer
{
    /// <summary>
    /// Marker interface to identify ScrollVirtualizer
    /// </summary>
    public interface IScrollVirtualizer { }
    
    public enum ScrollDirection
    {
        Vertical,
        Horizontal
    }

    public enum PullDirection
    {
        Start,
        End
    }

    public enum CellVisibilityState
    {
        Visible,
        Invisible
    }

    public enum CellUpdateMode
    {
        SyncOnly,
        AsyncOnly,
        Both
    }

    /// <summary>
    /// Virtualized scroll base class
    /// </summary>
    /// <typeparam name="TCell">Cell component type</typeparam>
    /// <typeparam name="TData">Data type</typeparam>
    public abstract class ScrollVirtualizerBase<TCell, TData> : MonoBehaviour, IScrollVirtualizer, IBeginDragHandler, IEndDragHandler where TCell : ScrollVirtualizerCell<TData>
    {
        [Header("ScrollRect Settings")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private float paddingLeft;
        [SerializeField] private float paddingRight;
        [SerializeField] private float paddingTop;
        [SerializeField] private float paddingBottom;
        [SerializeField] private float spacing;

        [Header("Cell Settings")]
        [SerializeField] private TCell cellPrefab;
        [SerializeField] private int maxRecycleCount = 20;

        [Header("Elastic Pull Settings")]
        [SerializeField] private float pullThreshold = 150f;

        [Header("Common Settings")]
        [SerializeField] private CellUpdateMode cellUpdateMode = CellUpdateMode.AsyncOnly;

        private static readonly Vector2 AnchorTopLeft = new Vector2(0, 1);

        private readonly List<TCell> _cells = new();
        private readonly Dictionary<TCell, CancellationTokenSource> _cellCts = new();

        private List<TData> _items = new();
        private int _itemCount;
        private bool _isInitialized;
        private bool _isDragging;
        private bool? _elasticPullSide;
        private bool? _thresholdExceededSide;
        private Vector2 _previousContentPosition;
        private int _previousActualFirstVisible = -1;
        private int _previousActualLastVisible = -1;
        private int _previousFirstVisible = -1;
        private int _previousLastVisible = -1;
        private CancellationTokenSource _scrollCts;
        private TCell[] _tempCellArray;
        private ScrollRect.MovementType _originalMovementType;

        /// <summary>
        /// Whether fallback release processing has been completed
        /// </summary>
        protected bool _releaseFallbackProcessed;

        /// <summary>
        /// Space between cells
        /// </summary>
        protected virtual float Spacing => spacing;

        /// <summary>
        /// Scroll direction (Vertical/Horizontal)
        /// </summary>
        protected virtual ScrollDirection ScrollDirection => ScrollDirection.Vertical;

        /// <summary>
        /// Number of buffer cells before and after visible area (default: 2)
        /// </summary>
        protected virtual int VisibleCellBuffer => 2;

        /// <summary>
        /// Pull threshold for triggering scroll pull events
        /// </summary>
        protected float PullThreshold => pullThreshold;

        /// <summary>
        /// Read-only list of current data items
        /// </summary>
        protected IReadOnlyList<TData> Items => _items;

        /// <summary>
        /// Reference to the ScrollRect component
        /// </summary>
        protected ScrollRect ScrollRect => scrollRect;

        /// <summary>
        /// Viewport RectTransform of the scroll area
        /// </summary>
        protected RectTransform Viewport => viewport;

        /// <summary>
        /// Content RectTransform that holds cells
        /// </summary>
        protected RectTransform Content => content;

        /// <summary>
        /// Left padding of the content area
        /// </summary>
        protected float PaddingLeft => paddingLeft;

        /// <summary>
        /// Right padding of the content area
        /// </summary>
        protected float PaddingRight => paddingRight;

        /// <summary>
        /// Top padding of the content area
        /// </summary>
        protected float PaddingTop => paddingTop;

        /// <summary>
        /// Bottom padding of the content area
        /// </summary>
        protected float PaddingBottom => paddingBottom;

        /// <summary>
        /// Total number of items in the data list
        /// </summary>
        protected int ItemCount => _itemCount;

        /// <summary>
        /// Maximum number of cells to recycle
        /// </summary>
        protected int MaxRecycleCount => maxRecycleCount;

        /// <summary>
        /// Current scroll position
        /// </summary>
        protected float ScrollPosition => GetCurrentScrollPosition();

        /// <summary>
        /// Maximum scroll position
        /// </summary>
        protected float MaxScrollPosition => GetMaxScrollPosition();

        /// <summary>
        /// Set scroll position directly in pixels
        /// </summary>
        protected void SetScrollPosition(float position)
        {
            if (content == null) return;

            var clamped = Mathf.Clamp(position, 0f, GetMaxScrollPosition());

            var newPosition = ScrollDirection == ScrollDirection.Vertical
                ? new Vector2(content.anchoredPosition.x, clamped)
                : new Vector2(-clamped, content.anchoredPosition.y);

            content.anchoredPosition = newPosition;
            _previousContentPosition = newPosition;

            UpdateAssignedCells();
        }

        /// <summary>
        /// Event when a cell button is clicked
        /// </summary>
        public event Action<TData> CellButtonClicked;

        /// <summary>
        /// Event when a cell is touched
        /// </summary>
        public event Action<TData> CellTouched;

        /// <summary>
        /// Event when scroll animation completes
        /// </summary>
        public event Action ScrollCompleted;

        /// <summary>
        /// Event when pulled and released (threshold exceeded)
        /// </summary>
        public event Action<PullDirection> ScrollPullReleased;

        /// <summary>
        /// Event when elastic pull starts (drag begins at edge)
        /// </summary>
        public event Action<PullDirection> ElasticPullStarted;

        /// <summary>
        /// Event when elastic pull is released (content returns to edge)
        /// </summary>
        public event Action<PullDirection> ElasticPullReleased;

        /// <summary>
        /// Event when pull threshold is exceeded (during drag, before release)
        /// </summary>
        public event Action<PullDirection> PullThresholdExceeded;

        /// <summary>
        /// Event when a cell visibility changes
        /// </summary>
        public event Action<int, TCell, CellVisibilityState> CellVisibilityChanged;

        /// <summary>
        /// Set total data count
        /// </summary>
        public void SetItemCount(int count)
        {
            _itemCount = Mathf.Max(0, count);
            UpdateLayout();
        }

        /// <summary>
        /// Clear all contents and reset state
        /// </summary>
        public void ClearContents()
        {
            StopScrollAnimation();
            CancelAllCellUpdates();

            _items = new List<TData>();
            _itemCount = 0;

            for (var i = 0; i < _cells.Count; i++)
            {
                _cells[i].gameObject.SetActive(false);
            }

            _previousFirstVisible = -1;
            _previousLastVisible = -1;
            _previousActualFirstVisible = -1;
            _previousActualLastVisible = -1;

            _isDragging = false;
            _elasticPullSide = null;
            _thresholdExceededSide = null;
            _releaseFallbackProcessed = false;

            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.velocity = Vector2.zero;
            }

            if (content != null)
            {
                content.anchoredPosition = Vector2.zero;
                _previousContentPosition = Vector2.zero;
            }

            UpdateLayout();
        }

        /// <summary>
        /// Relayout all
        /// </summary>
        private void Refresh()
        {
            UpdateLayout();
        }

        /// <summary>
        /// Update cell at specified index
        /// </summary>
        private void RefreshItem(int index)
        {
            if (index < 0 || index >= _itemCount)
            {
                return;
            }

            if (Items == null) return;

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell != null && cell.gameObject.activeSelf && cell.Index == index)
                {
                    if (index < Items.Count)
                    {
                        cell.Data = Items[index];
                        cell.UpdateCellInternal(Items[index]);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Scroll immediately to specified index
        /// </summary>
        public void ScrollToIndex(int index)
        {
            JumpToIndex(index);
        }

        /// <summary>
        /// Clamp index to valid range
        /// </summary>
        /// <param name="index">Index to validate</param>
        /// <param name="validatedIndex">Validated index (out)</param>
        /// <returns>True if valid data exists, false otherwise</returns>
        private bool ValidateIndex(int index, out int validatedIndex)
        {
            if (_itemCount == 0)
            {
                validatedIndex = 0;
                return false;
            }

            validatedIndex = Mathf.Clamp(index, 0, _itemCount - 1);
            return true;
        }

        /// <summary>
        /// Stop running scroll animation
        /// </summary>
        private void StopScrollAnimation()
        {
            var cts = _scrollCts;
            if (cts == null)
            {
                return;
            }

            _scrollCts = null;
            cts.Cancel();
            cts.Dispose();
        }

        /// <summary>
        /// Jump immediately to specified index
        /// </summary>
        /// <param name="index">Target index for jump</param>
        protected void JumpToIndex(int index = 0)
        {
            if (!ValidateIndex(index, out var validatedIndex))
            {
                return;
            }

            StopScrollAnimation();

            var targetPosition = CalculateScrollPosition(validatedIndex);
            content.anchoredPosition = targetPosition;
            _previousContentPosition = targetPosition;

            UpdateAssignedCells();
        }

        /// <summary>
        /// Jump immediately to specified index (inverted coordinates version)
        /// </summary>
        /// <param name="index">Target index for jump</param>
        /// <param name="reverseX">Whether to reverse X coordinate</param>
        /// <param name="reverseY">Whether to reverse Y coordinate</param>
        protected void ReverseJumpToIndex(int index, bool reverseX, bool reverseY)
        {
            if (!ValidateIndex(index, out var validatedIndex))
            {
                return;
            }

            StopScrollAnimation();

            var targetPosition = CalculateScrollPosition(validatedIndex);
            var x = reverseX ? -targetPosition.x : targetPosition.x;
            var y = reverseY ? -targetPosition.y : targetPosition.y;
            targetPosition = new Vector2(x, y);

            content.anchoredPosition = targetPosition;
            _previousContentPosition = targetPosition;

            UpdateAssignedCells();
        }

        /// <summary>
        /// Scroll to specified index with animation
        /// </summary>
        /// <param name="index">Target index for scroll</param>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="ease">Easing type</param>
        /// <param name="onComplete">Callback on completion</param>
        protected void ScrollToIndexAnimated(int index, float duration, Ease ease, Action onComplete = null)
        {
            if (!ValidateIndex(index, out var validatedIndex))
            {
                return;
            }

            StopScrollAnimation();

            _scrollCts = new CancellationTokenSource();
            ScrollToAsync(validatedIndex, duration, ease, onComplete, _scrollCts).Forget();
        }

        /// <summary>
        /// Run scroll animation asynchronously
        /// </summary>
        private async TaskType ScrollToAsync(int index, float duration, Ease ease, Action onComplete, CancellationTokenSource cts)
        {
            var cancellationToken = cts.Token;
            var startPosition = content?.anchoredPosition ?? Vector2.zero;
            var targetPosition = CalculateScrollPosition(index);
            var elapsedTime = 0f;

            try
            {
                while (elapsedTime < duration)
                {
                    if (this == null || gameObject == null || content == null)
                    {
                        return;
                    }

                    elapsedTime += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsedTime / duration);
                    var easedT = EasingFunction.Interpolate(t, ease);

                    content.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedT);
                    UpdateAssignedCells();

                    await TaskType.Yield(cancellationToken);
                }

                if (this == null || gameObject == null || content == null)
                {
                    return;
                }

                content.anchoredPosition = targetPosition;
                _previousContentPosition = targetPosition;

                UpdateAssignedCells();

                ScrollCompleted?.Invoke();
                onComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (_scrollCts == cts)
                {
                    _scrollCts = null;
                    cts.Dispose();
                }
            }
        }

        /// <summary>
        /// Initialize data
        /// </summary>
        /// <param name="items">Data list</param>
        public void InitializeContents(IReadOnlyList<TData> items)
        {
            _items = new List<TData>(items);
            _isInitialized = true;
            SetItemCount(_items.Count);
        }

        /// <summary>
        /// Update data and reset scroll position
        /// </summary>
        /// <param name="items">Data list</param>
        /// <param name="resetScrollPosition">Reset scroll position to zero</param>
        /// <param name="refreshVisibleCells">Refresh currently visible cells with new data</param>
        public void UpdateContents(IReadOnlyList<TData> items, bool resetScrollPosition = true, bool refreshVisibleCells = true)
        {
            _items = new List<TData>(items);
            SetItemCount(_items.Count);
            Refresh();

            if (resetScrollPosition && content != null)
            {
                content.anchoredPosition = Vector2.zero;
                _previousContentPosition = content.anchoredPosition;
            }

            UpdateAssignedCells();
            Canvas.ForceUpdateCanvases();

            if (resetScrollPosition)
            {
                ScrollRect.StopMovement();
                ScrollRect.velocity = Vector2.zero;
            }

            if (refreshVisibleCells)
            {
                RefreshVisibleCells();
            }
        }

        /// <summary>
        /// Refresh all currently visible cells with current data
        /// </summary>
        public void RefreshVisibleCells()
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell.gameObject.activeSelf && cell.Index >= 0 && cell.Index < _items.Count)
                {
                    UpdateCell(cell.Index, _items[cell.Index]);
                }
            }
        }

        /// <summary>
        /// Add item
        /// </summary>
        /// <param name="items">Items to add</param>
        /// <param name="insertAtStart">True to insert at start</param>
        /// <param name="onComplete">Callback on completion</param>
        public void AddContents(IReadOnlyList<TData> items, bool insertAtStart = false, Action onComplete = null)
        {
            if (items == null) return;

            var itemList = items is List<TData> list ? list : new List<TData>(items);
            if (itemList.Count == 0) return;

            var previousContentPosition = content != null ? content.anchoredPosition : Vector2.zero;

            if (insertAtStart)
            {
                _items.InsertRange(0, itemList);
            }
            else
            {
                _items.AddRange(itemList);
            }

            SetItemCount(_items.Count);

            if (insertAtStart && content != null)
            {
                var offset = CalculateOffsetForInsertedItems(itemList.Count);
                content.anchoredPosition = previousContentPosition + offset;
                _previousContentPosition = content.anchoredPosition;
            }

            Refresh();
            onComplete?.Invoke();
        }

        /// <summary>
        /// Calculate scroll position offset for insertion at start
        /// </summary>
        /// <param name="insertedCount">Number of inserted items</param>
        /// <returns>Scroll position offset</returns>
        protected abstract Vector2 CalculateOffsetForInsertedItems(int insertedCount);

        /// <summary>
        /// Update specific cell
        /// </summary>
        /// <param name="index">Data index</param>
        /// <param name="data">Data to update</param>
        protected void UpdateCell(int index, TData data)
        {
            if (index < 0 || index >= _itemCount)
            {
                return;
            }

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell.gameObject.activeSelf && cell.Index == index)
                {
                    cell.Data = data;
                    cell.UpdateCellInternal(data);
                    return;
                }
            }
        }

        /// <summary>
        /// Cell update process
        /// </summary>
        protected virtual void OnUpdateCell(TCell cell, TData data)
        {
        }

        /// <summary>
        /// Get cell size
        /// </summary>
        protected abstract Vector2 GetCellSize();

        /// <summary>
        /// Calculate total content size
        /// </summary>
        protected abstract Vector2 CalculateContentSize();

        /// <summary>
        /// Calculate cell position for specified index
        /// </summary>
        protected abstract Vector2 CalculateCellPosition(int index);

        /// <summary>
        /// Calculate index range to display (with buffer)
        /// </summary>
        protected abstract void CalculateVisibleRange(out int firstIndex, out int lastIndex);

        /// <summary>
        /// Calculate actually displayed index range (without buffer)
        /// </summary>
        protected abstract void CalculateActualVisibleRange(out int firstIndex, out int lastIndex);

        /// <summary>
        /// Calculate scroll position for specified index
        /// </summary>
        protected abstract Vector2 CalculateScrollPosition(int index);

        /// <summary>
        /// Get current scroll position
        /// </summary>
        protected abstract float GetCurrentScrollPosition();

        /// <summary>
        /// Get maximum scroll position
        /// </summary>
        protected abstract float GetMaxScrollPosition();

        /// <summary>
        /// Check if content needs scrolling
        /// </summary>
        protected abstract bool IsContentScrollable(Vector2 contentSize);

        /// <summary>
        /// Initialization
        /// </summary>
        protected virtual void Awake()
        {
            InitializeComponents();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor validation
        /// </summary>
        protected virtual void OnValidate()
        {
            var scrollVirtualizers = GetComponents<IScrollVirtualizer>();
            if (scrollVirtualizers is { Length: > 1 })
            {
                Debug.LogError($"[ScrollVirtualizer] Only one ScrollVirtualizer (Vertical/Horizontal/Grid) is allowed per GameObject. Removing {GetType().Name}.", this);
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        DestroyImmediate(this);
                    }
                };
            }
        }
#endif

        /// <summary>
        /// Called when enabled
        /// </summary>
        protected virtual void OnEnable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }
        }

        /// <summary>
        /// Called when disabled
        /// </summary>
        protected virtual void OnDisable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }

            CancelAllCellUpdates();

            _isDragging = false;
            _elasticPullSide = null;
            _releaseFallbackProcessed = false;
        }

        /// <summary>
        /// Called every frame after Update
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (_isDragging && !IsPointerPressed())
            {
                _isDragging = false;

                if (_elasticPullSide.HasValue && GetMaxScrollPosition() > 0)
                {
                    ElasticPullReleased?.Invoke(_elasticPullSide.Value ? PullDirection.Start : PullDirection.End);
                    _elasticPullSide = null;
                }

                EndDragProcessing();
            }

            if (_isDragging && scrollRect != null && scrollRect.movementType == ScrollRect.MovementType.Elastic)
            {
                UpdateElasticPullState();
            }
        }

        /// <summary>
        /// Update elastic pull state
        /// </summary>
        private void UpdateElasticPullState()
        {
            var topPull = GetTopPullAmount();
            var bottomPull = GetBottomPullAmount();
            const float epsilon = 0.5f;

            var isAtStartEdge = topPull > epsilon;
            var isAtEndEdge = bottomPull > epsilon;

            if (_elasticPullSide == null)
            {
                if (isAtStartEdge)
                {
                    _elasticPullSide = true;
                    _thresholdExceededSide = null;
                    ElasticPullStarted?.Invoke(PullDirection.Start);
                }
                else if (isAtEndEdge)
                {
                    _elasticPullSide = false;
                    _thresholdExceededSide = null;
                    ElasticPullStarted?.Invoke(PullDirection.End);
                }
            }
            else
            {
                var wasStartSide = _elasticPullSide.Value;
                var isStillAtEdge = wasStartSide ? isAtStartEdge : isAtEndEdge;

                if (!isStillAtEdge && GetMaxScrollPosition() > 0)
                {
                    ElasticPullReleased?.Invoke(wasStartSide ? PullDirection.Start : PullDirection.End);
                    _elasticPullSide = null;
                    _thresholdExceededSide = null;
                }
            }

            if (_elasticPullSide != null && GetMaxScrollPosition() > 0)
            {
                var currentSide = _elasticPullSide.Value;
                var pullAmount = currentSide ? topPull : bottomPull;

                if (_thresholdExceededSide == null && pullAmount > pullThreshold)
                {
                    _thresholdExceededSide = currentSide;
                    PullThresholdExceeded?.Invoke(currentSide ? PullDirection.Start : PullDirection.End);
                }
            }
        }

        /// <summary>
        /// Called when destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            StopScrollAnimation();

            CancelAllCellUpdates();

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }
        }

        /// <summary>
        /// Cancel updates for all cells
        /// </summary>
        private void CancelAllCellUpdates()
        {
            if (_cellCts.Count == 0) return;

            if (_tempCellArray == null || _tempCellArray.Length < _cellCts.Count)
            {
                _tempCellArray = new TCell[_cellCts.Count];
            }

            _cellCts.Keys.CopyTo(_tempCellArray, 0);

            for (var i = 0; i < _cellCts.Count; i++)
            {
                if (_cellCts.TryGetValue(_tempCellArray[i], out var cts))
                {
                    try
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            _cellCts.Clear();
        }

        /// <summary>
        /// Called when drag begins
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _releaseFallbackProcessed = false;
        }

        /// <summary>
        /// Called when drag ends
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;

            if (_elasticPullSide.HasValue && GetMaxScrollPosition() > 0)
            {
                ElasticPullReleased?.Invoke(_elasticPullSide.Value ? PullDirection.Start : PullDirection.End);
                _elasticPullSide = null;
            }

            EndDragProcessing();
        }

        /// <summary>
        /// Called when drag ends
        /// </summary>
        protected virtual void EndDragProcessing()
        {
            if (content == null || viewport == null)
            {
                return;
            }

            if (_releaseFallbackProcessed)
            {
                return;
            }

            if (GetMaxScrollPosition() > 0)
            {
                var topPull = GetTopPullAmount();
                var bottomPull = GetBottomPullAmount();

                if (topPull > pullThreshold)
                {
                    InvokeScrollPullEventAsync(PullDirection.Start, ScrollPullReleased, destroyCancellationToken).Forget();
                }

                if (bottomPull > pullThreshold)
                {
                    InvokeScrollPullEventAsync(PullDirection.End, ScrollPullReleased, destroyCancellationToken).Forget();
                }
            }

            _releaseFallbackProcessed = true;
        }

        /// <summary>
        /// Check whether pointer is pressed
        /// </summary>
        private bool IsPointerPressed()
        {
            if (Input.GetMouseButton(0)) return true;

            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0).phase;
                return t is TouchPhase.Began or TouchPhase.Moved or TouchPhase.Stationary;
            }

            return false;
        }

        /// <summary>
        /// Get pull amount at start side
        /// </summary>
        protected float GetTopPullAmount()
        {
            if (Content == null || Viewport == null) return 0f;

            var isVertical = ScrollDirection == ScrollDirection.Vertical;

            var contentCorners = new Vector3[4];
            var viewCorners = new Vector3[4];
            Content.GetWorldCorners(contentCorners);
            Viewport.GetWorldCorners(viewCorners);

            if (isVertical)
            {
                var contentTop = contentCorners[1].y;
                var viewTop = viewCorners[1].y;
                var amount = viewTop - contentTop;
                return Mathf.Max(0f, amount);
            }
            else
            {
                var contentLeft = contentCorners[0].x;
                var viewLeft = viewCorners[0].x;
                var amount = contentLeft - viewLeft;
                return Mathf.Max(0f, amount);
            }
        }

        /// <summary>
        /// Get pull amount at end side
        /// </summary>
        protected float GetBottomPullAmount()
        {
            if (Content == null || Viewport == null) return 0f;

            var isVertical = ScrollDirection == ScrollDirection.Vertical;

            var contentCorners = new Vector3[4];
            var viewCorners = new Vector3[4];
            Content.GetWorldCorners(contentCorners);
            Viewport.GetWorldCorners(viewCorners);

            if (isVertical)
            {
                var contentBottom = contentCorners[0].y;
                var viewBottom = viewCorners[0].y;
                var amount = contentBottom - viewBottom;
                return Mathf.Max(0f, amount);
            }
            else
            {
                var contentRight = contentCorners[2].x;
                var viewRight = viewCorners[2].x;
                var amount = viewRight - contentRight;
                return Mathf.Max(0f, amount);
            }
        }

        /// <summary>
        /// Initialize component
        /// </summary>
        private void InitializeComponents()
        {
            if (scrollRect == null)
            {
                scrollRect = GetComponent<ScrollRect>();
                if (scrollRect == null)
                {
                    Debug.LogError("[ScrollVirtualizer] ScrollRect not found. Please assign ScrollRect component.");
                    return;
                }
            }

            if (viewport == null)
            {
                viewport = scrollRect.viewport;
                if (viewport == null)
                {
                    Debug.LogError("[ScrollVirtualizer] Viewport not found. Please assign Viewport RectTransform.");
                    return;
                }
            }

            if (content == null)
            {
                content = scrollRect.content;
                if (content == null)
                {
                    Debug.LogError("[ScrollVirtualizer] Content not found. Please assign Content RectTransform.");
                    return;
                }
            }

            content.anchorMin = AnchorTopLeft;
            content.anchorMax = AnchorTopLeft;
            content.pivot = AnchorTopLeft;

            _originalMovementType = scrollRect.movementType;
        }

        /// <summary>
        /// Update layout
        /// </summary>
        private void UpdateLayout()
        {
            if (!_isInitialized || content == null)
            {
                return;
            }

            var contentSize = CalculateContentSize();
            content.sizeDelta = contentSize;

            var poolSize = Mathf.Min(maxRecycleCount, _itemCount);
            if (_cells.Count < poolSize)
            {
                CreateCellPool(poolSize - _cells.Count);
            }

            _previousFirstVisible = -1;
            _previousLastVisible = -1;

            Canvas.ForceUpdateCanvases();

            if (scrollRect != null)
            {
                scrollRect.movementType = IsContentScrollable(contentSize)
                    ? _originalMovementType
                    : ScrollRect.MovementType.Clamped;
            }

            UpdateAssignedCells();
        }

        /// <summary>
        /// Create cell pool
        /// </summary>
        private void CreateCellPool(int addCount)
        {
            if (cellPrefab == null)
            {
                Debug.LogError("[ScrollVirtualizer] Cell prefab is not assigned.");
                return;
            }

            for (var i = 0; i < addCount; i++)
            {
                var cell = Instantiate(cellPrefab, content);
                var cellTransform = cell.RectTransform;

                cellTransform.anchorMin = AnchorTopLeft;
                cellTransform.anchorMax = AnchorTopLeft;
                cellTransform.pivot = AnchorTopLeft;

                if (cell.Button != null)
                {
                    cell.Button.onClick.AddListener(cell.OnButtonClick);
                }

                cell.OnTouchedCallback = OnCellTouched;
                cell.OnButtonClickedCallback = OnCellButtonClicked;

                cell.gameObject.SetActive(false);
                _cells.Add(cell);
            }
        }

        /// <summary>
        /// Called when cell is touched
        /// </summary>
        private void OnCellTouched(TData data)
        {
            CellTouched?.Invoke(data);
        }

        /// <summary>
        /// Called when cell button is clicked
        /// </summary>
        private void OnCellButtonClicked(TData data)
        {
            CellButtonClicked?.Invoke(data);
        }

        /// <summary>
        /// Assign cells to display based on visible range
        /// </summary>
        private void UpdateAssignedCells()
        {
            if (_itemCount == 0 || _cells.Count == 0)
            {
                for (var i = 0; i < _cells.Count; i++)
                {
                    _cells[i].gameObject.SetActive(false);
                }
                return;
            }

            CalculateVisibleRange(out var firstIndex, out var lastIndex);

            firstIndex = Mathf.Clamp(firstIndex, 0, _itemCount - 1);
            lastIndex = Mathf.Clamp(lastIndex, 0, _itemCount - 1);

            if (lastIndex < firstIndex)
            {
                return;
            }

            if (firstIndex == _previousFirstVisible && lastIndex == _previousLastVisible)
            {
                return;
            }

            var neededIndices = new HashSet<int>();
            for (var i = firstIndex; i <= lastIndex; i++)
            {
                neededIndices.Add(i);
            }

            var availableCells = new List<TCell>();
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell.gameObject.activeSelf && neededIndices.Contains(cell.Index))
                {
                    neededIndices.Remove(cell.Index);
                }
                else
                {
                    availableCells.Add(cell);
                }
            }

            var availableIndex = 0;
            foreach (var index in neededIndices)
            {
                if (availableIndex >= availableCells.Count) break;
                SetupCell(availableCells[availableIndex], index);
                availableIndex++;
            }

            for (var i = availableIndex; i < availableCells.Count; i++)
            {
                availableCells[i].gameObject.SetActive(false);
            }

            _previousFirstVisible = firstIndex;
            _previousLastVisible = lastIndex;

            DetectAndFireVisibilityEvents();
        }

        /// <summary>
        /// Setup cell
        /// </summary>
        protected virtual void SetupCell(TCell cell, int index)
        {
            CancelCellUpdate(cell);

            cell.gameObject.SetActive(true);
            cell.Index = index;

            var cellTransform = cell.RectTransform;
            cellTransform.anchoredPosition = CalculateCellPosition(index);
            cellTransform.sizeDelta = GetCellSize();

            if (Items != null && index >= 0 && index < Items.Count)
            {
                cell.Data = Items[index];

                switch (cellUpdateMode)
                {
                    case CellUpdateMode.SyncOnly:
                        cell.UpdateCellInternal(Items[index]);
                        break;

                    case CellUpdateMode.AsyncOnly:
                        {
                            var cts = new CancellationTokenSource();
                            _cellCts[cell] = cts;
                            _ = UpdateCellAsync(cell, Items[index], cts.Token);
                        }
                        break;

                    case CellUpdateMode.Both:
                        cell.UpdateCellInternal(Items[index]);
                        {
                            var cts = new CancellationTokenSource();
                            _cellCts[cell] = cts;
                            _ = UpdateCellAsync(cell, Items[index], cts.Token);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Update cell asynchronously
        /// </summary>
        private async TaskType UpdateCellAsync(TCell cell, TData data, CancellationToken ct)
        {
            try
            {
                await cell.UpdateCellAsyncInternal(data, ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                RemoveCellToken(cell);
            }
        }

        /// <summary>
        /// Cancel cell update
        /// </summary>
        private void CancelCellUpdate(TCell cell)
        {
            if (_cellCts.TryGetValue(cell, out var cts))
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch
                {
                }
                _cellCts.Remove(cell);
            }
        }

        /// <summary>
        /// Remove cell token
        /// </summary>
        private void RemoveCellToken(TCell cell)
        {
            if (_cellCts.TryGetValue(cell, out var cts))
            {
                try
                {
                    cts.Dispose();
                }
                catch
                {
                }
                _cellCts.Remove(cell);
            }
        }

        /// <summary>
        /// Detect cell visibility changes and fire events
        /// </summary>
        private void DetectAndFireVisibilityEvents()
        {
            if (_itemCount == 0)
            {
                _previousActualFirstVisible = -1;
                _previousActualLastVisible = -1;
                return;
            }

            CalculateActualVisibleRange(out var currentFirstVisible, out var currentLastVisible);

            currentFirstVisible = Mathf.Clamp(currentFirstVisible, 0, _itemCount - 1);
            currentLastVisible = Mathf.Clamp(currentLastVisible, 0, _itemCount - 1);

            if (_previousActualFirstVisible < 0 || _previousActualLastVisible < 0)
            {
                for (var i = currentFirstVisible; i <= currentLastVisible; i++)
                {
                    var cell = FindCellByIndex(i);
                    if (cell != null && cell.gameObject.activeSelf)
                    {
                        CellVisibilityChanged?.Invoke(i, cell, CellVisibilityState.Visible);
                    }
                }
                _previousActualFirstVisible = currentFirstVisible;
                _previousActualLastVisible = currentLastVisible;
                return;
            }

            for (var i = currentFirstVisible; i <= currentLastVisible; i++)
            {
                if (i < _previousActualFirstVisible || i > _previousActualLastVisible)
                {
                    var cell = FindCellByIndex(i);
                    if (cell != null && cell.gameObject.activeSelf)
                    {
                        CellVisibilityChanged?.Invoke(i, cell, CellVisibilityState.Visible);
                    }
                }
            }

            for (var i = _previousActualFirstVisible; i <= _previousActualLastVisible; i++)
            {
                if (i < currentFirstVisible || i > currentLastVisible)
                {
                    var cell = FindCellByIndex(i);
                    if (cell != null)
                    {
                        CellVisibilityChanged?.Invoke(i, cell, CellVisibilityState.Invisible);
                    }
                }
            }

            _previousActualFirstVisible = currentFirstVisible;
            _previousActualLastVisible = currentLastVisible;
        }

        /// <summary>
        /// Find cell for specified index
        /// </summary>
        private TCell FindCellByIndex(int index)
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell.gameObject.activeSelf && cell.Index == index)
                {
                    return cell;
                }
            }
            return null;
        }

        /// <summary>
        /// Called when scroll position changes
        /// </summary>
        private void OnScrollValueChanged(Vector2 value)
        {
            var currentPosition = content.anchoredPosition;

            if (currentPosition != _previousContentPosition)
            {
                UpdateAssignedCells();
                _previousContentPosition = currentPosition;
            }
        }


        /// <summary>
        /// Get pull released event callback
        /// </summary>
        protected Action<PullDirection> GetScrollPullReleasedCallback() => ScrollPullReleased;

        /// <summary>
        /// Invoke scroll pull event
        /// </summary>
        protected async TaskType InvokeScrollPullEventAsync(PullDirection direction, Action<PullDirection> callback, CancellationToken cancellationToken)
        {
            if (scrollRect == null)
            {
                callback?.Invoke(direction);
                return;
            }

            const float timeout = 0.7f;
            const float velocityThreshold = 0.02f;
            const float positionEpsilon = 0.5f;
            var elapsed = 0f;

            try
            {
                while (elapsed < timeout)
                {
                    var velocityOk = scrollRect.velocity.sqrMagnitude <= velocityThreshold * velocityThreshold;
                    var positionOk = IsContentRestedFor(direction);
                    if (!IsPointerPressed() && velocityOk && positionOk)
                        break;

                    elapsed += Time.unscaledDeltaTime;
                    await TaskType.Yield(cancellationToken);
                }

                scrollRect.StopMovement();
                scrollRect.velocity = Vector2.zero;

                callback?.Invoke(direction);
            }
            catch (OperationCanceledException)
            {
            }

            bool IsContentRestedFor(PullDirection pullDirection)
            {
                if (Content == null || Viewport == null) return true;

                var isStart = pullDirection == PullDirection.Start;

                if (ScrollDirection == ScrollDirection.Vertical)
                {
                    var y = Content.anchoredPosition.y;
                    var max = GetMaxScrollPosition();
                    if (isStart)
                    {
                        return y >= -positionEpsilon;
                    }

                    return y >= -positionEpsilon && y <= max + positionEpsilon;
                }
                else
                {
                    var x = Content.anchoredPosition.x;
                    var max = GetMaxScrollPosition();
                    if (isStart)
                    {
                        return x >= -positionEpsilon;
                    }

                    return x <= positionEpsilon && x >= -max - positionEpsilon;
                }
            }
        }
    }

    /// <summary>
    /// Virtualized scroll base class (context-enabled version)
    /// </summary>
    /// <typeparam name="TCell">Cell component type</typeparam>
    /// <typeparam name="TData">Data type</typeparam>
    /// <typeparam name="TContext">Context type</typeparam>
    public abstract class ScrollVirtualizerBaseWithContext<TCell, TData, TContext> : ScrollVirtualizerBase<TCell, TData>
        where TCell : ScrollVirtualizerCellWithContext<TData, TContext>
    {
        private bool _isContextCreated;

        /// <summary>
        /// Context referenced by the cell
        /// </summary>
        protected TContext Context { get; private set; }

        /// <summary>
        /// Create context
        /// </summary>
        protected abstract TContext CreateContext();

        /// <summary>
        /// Initialization
        /// </summary>
        protected virtual void Start()
        {
            EnsureContextCreated();
        }

        /// <summary>
        /// Create context if not created yet (InitializeContents may be called before Start)
        /// </summary>
        private void EnsureContextCreated()
        {
            if (_isContextCreated)
            {
                return;
            }

            Context = CreateContext();
            _isContextCreated = true;
        }

        /// <summary>
        /// Setup cell
        /// </summary>
        protected override void SetupCell(TCell cell, int index)
        {
            EnsureContextCreated();

            if (cell != null && Context != null)
            {
                cell.Context = Context;
            }

            base.SetupCell(cell, index);
        }
    }
}
