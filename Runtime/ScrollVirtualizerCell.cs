using System;
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
    /// Cell base class
    /// </summary>
    public abstract class ScrollVirtualizerCell : MonoBehaviour
    {
        private RectTransform _rectTransform;

        internal RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = (RectTransform)transform;
                }
                return _rectTransform;
            }
        }

        /// <summary>
        /// Index of the data displayed by the cell
        /// </summary>
        public int Index { get; internal set; }
    }

    /// <summary>
    /// Cell base class (generic version)
    /// </summary>
    /// <typeparam name="TData">Data type</typeparam>
    public abstract class ScrollVirtualizerCell<TData> : ScrollVirtualizerCell, IPointerClickHandler
    {
        [Header("Click Settings")]
        [SerializeField] private Button button;

        /// <summary>
        /// Update cell content
        /// </summary>
        public abstract void UpdateCell(TData data);

        /// <summary>
        /// Update cell content asynchronously
        /// </summary>
        public virtual TaskType UpdateCellAsync(TData data, CancellationToken ct)
        {
#if UNITASK_SUPPORT
            if (ct.IsCancellationRequested) return UniTask.FromCanceled(ct);

            try
            {
                UpdateCell(data);
                return UniTask.CompletedTask;
            }
            catch (Exception ex)
            {
                return UniTask.FromException(ex);
            }
#else
            if (ct.IsCancellationRequested) return Task.FromCanceled(ct);

            try
            {
                UpdateCell(data);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
#endif
        }

        /// <summary>
        /// Internal method for updating cell (called from ScrollVirtualizerBase)
        /// </summary>
        internal virtual void UpdateCellInternal(TData data)
        {
            UpdateCell(data);
        }

        /// <summary>
        /// Internal method for updating cell asynchronously (called from ScrollVirtualizerBase)
        /// </summary>
        internal virtual TaskType UpdateCellAsyncInternal(TData data, CancellationToken ct)
        {
            return UpdateCellAsync(data, ct);
        }

        /// <summary>
        /// Current data displayed by the cell
        /// </summary>
        internal TData Data { get; set; }

        /// <summary>
        /// Button assigned to the cell
        /// </summary>
        internal Button Button => button;

        /// <summary>
        /// Callback when the cell is touched
        /// </summary>
        internal Action<TData> OnTouchedCallback { get; set; }

        /// <summary>
        /// Callback when the cell button is clicked
        /// </summary>
        internal Action<TData> OnButtonClickedCallback { get; set; }

        /// <summary>
        /// Called when button is clicked (for zero allocation)
        /// </summary>
        internal void OnButtonClick()
        {
            OnButtonClickedCallback?.Invoke(Data);
        }

        /// <summary>
        /// Called when pointer clicks
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (button == null)
            {
                OnTouchedCallback?.Invoke(Data);
            }
        }
    }

    /// <summary>
    /// Cell base class (context-enabled version)
    /// </summary>
    /// <typeparam name="TData">Data type</typeparam>
    /// <typeparam name="TContext">Context type</typeparam>
    public abstract class ScrollVirtualizerCellWithContext<TData, TContext> : ScrollVirtualizerCell<TData>
    {
        /// <summary>
        /// Context referenced by the cell
        /// </summary>
        public TContext Context { get; internal set; }

        private bool _isInitialized;

#if !UNITASK_SUPPORT
        private CancellationTokenSource _cellCts;
#endif

        /// <summary>
        /// Initialize cell
        /// </summary>
        public abstract void Initialize(TContext context);

        /// <summary>
        /// Update cell content (users override this in derived classes)
        /// </summary>
        public abstract override void UpdateCell(TData data);

        /// <summary>
        /// Update cell content asynchronously (users can override this in derived classes)
        /// </summary>
        public override TaskType UpdateCellAsync(TData data, CancellationToken ct)
        {
            return base.UpdateCellAsync(data, ct);
        }

        /// <summary>
        /// Internal method for updating cell (override to add Initialize call)
        /// </summary>
        internal sealed override void UpdateCellInternal(TData data)
        {
            if (!_isInitialized)
            {
                Initialize(Context);
                _isInitialized = true;
            }

            UpdateCell(data);
        }

        /// <summary>
        /// Internal method for updating cell asynchronously (override to add Initialize call)
        /// </summary>
        internal sealed override async TaskType UpdateCellAsyncInternal(TData data, CancellationToken ct)
        {
            if (!_isInitialized)
            {
                Initialize(Context);
                _isInitialized = true;
            }

#if UNITASK_SUPPORT
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct,
                this.GetCancellationTokenOnDestroy()
            );

            try
            {
                await UpdateCellAsync(data, linkedCts.Token);
            }
            finally
            {
                linkedCts.Dispose();
            }
#else
            CancelCellTasks();
            _cellCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            try
            {
                await UpdateCellAsync(data, _cellCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
#endif
        }

#if !UNITASK_SUPPORT
        /// <summary>
        /// Cancel running task in the cell
        /// </summary>
        private void CancelCellTasks()
        {
            if (_cellCts != null)
            {
                try
                {
                    _cellCts.Cancel();
                    _cellCts.Dispose();
                }
                catch
                {
                }
                _cellCts = null;
            }
        }

        /// <summary>
        /// Called when destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            CancelCellTasks();
        }

        /// <summary>
        /// Called when disabled
        /// </summary>
        protected virtual void OnDisable()
        {
            CancelCellTasks();
        }
#endif
    }
}
