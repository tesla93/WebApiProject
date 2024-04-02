using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BackgroundProcessing
{
    public sealed class BackgroundTaskQueue : IBackgroundTaskQueue, IDisposable
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task<object>>> _workItems = new ConcurrentQueue<Func<CancellationToken, Task<object>>>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private object _lastResult;
        private CancellationTokenSource _tokenSource;


        public void QueueBackgroundWorkItem(Func<CancellationToken, Task<object>> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task Dequeue(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);

            _workItems.TryDequeue(out var workItem);
            if (workItem != null)
            {
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _lastResult = await workItem(_tokenSource.Token);
            }
        }

        public void CancelWorkItem()
        {
            if (_tokenSource != null)
            {
                _tokenSource.Cancel();
                _tokenSource = null;
            }
        }

        public T GetLastResult<T>() where T : class => _lastResult as T;


        public void Dispose() => _signal.Dispose();
    }
}