using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NoranDev.ScrollVirtualizer
{
#if !UNITASK_SUPPORT
    internal static class ScrollVirtualizerExtensions
    {
        public static void Forget(this Task task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        return;
                    }
                    Debug.LogException(ex);
                }
            }
            else
            {
                awaiter.OnCompleted(() =>
                {
                    try
                    {
                        awaiter.GetResult();
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException)
                        {
                            return;
                        }
                        Debug.LogException(ex);
                    }
                });
            }
        }

        public static void Forget(this Task task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread = true)
        {
            if (exceptionHandler == null)
            {
                Forget(task);
                return;
            }

            ForgetCoreWithCatch(task, exceptionHandler, handleExceptionOnMainThread).Forget();
        }

        public static void Forget<T>(this Task<T> task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        return;
                    }
                    Debug.LogException(ex);
                }
            }
            else
            {
                awaiter.OnCompleted(() =>
                {
                    try
                    {
                        awaiter.GetResult();
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException)
                        {
                            return;
                        }
                        Debug.LogException(ex);
                    }
                });
            }
        }

        public static void Forget<T>(this Task<T> task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread = true)
        {
            if (exceptionHandler == null)
            {
                Forget(task);
                return;
            }

            ForgetCoreWithCatch(task, exceptionHandler, handleExceptionOnMainThread).Forget();
        }

        private static async Task ForgetCoreWithCatch(Task task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                try
                {
                    if (handleExceptionOnMainThread)
                    {
                        await SwitchToMainThreadAsync();
                    }
                    exceptionHandler(ex);
                }
                catch (Exception ex2)
                {
                    Debug.LogException(ex2);
                }
            }
        }

        private static async Task ForgetCoreWithCatch<T>(Task<T> task, Action<Exception> exceptionHandler, bool handleExceptionOnMainThread)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                try
                {
                    if (handleExceptionOnMainThread)
                    {
                        await SwitchToMainThreadAsync();
                    }
                    exceptionHandler(ex);
                }
                catch (Exception ex2)
                {
                    Debug.LogException(ex2);
                }
            }
        }

        private static System.Threading.SynchronizationContext unitySynchronizationContext;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            unitySynchronizationContext = System.Threading.SynchronizationContext.Current;
        }

        private static Task SwitchToMainThreadAsync()
        {
            if (unitySynchronizationContext == null)
            {
                unitySynchronizationContext = System.Threading.SynchronizationContext.Current;
            }

            if (unitySynchronizationContext != null &&
                System.Threading.SynchronizationContext.Current != unitySynchronizationContext)
            {
                var tcs = new TaskCompletionSource<bool>();
                unitySynchronizationContext.Post(_ => tcs.SetResult(true), null);
                return tcs.Task;
            }

            return Task.CompletedTask;
        }
    }
#endif
}
