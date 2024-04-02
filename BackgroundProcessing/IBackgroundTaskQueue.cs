using System;
using System.Threading;
using System.Threading.Tasks;

namespace BackgroundProcessing
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(Func<CancellationToken, Task<object>> workItem);

        Task Dequeue(CancellationToken cancellationToken);

        void CancelWorkItem();

        T GetLastResult<T>() where T : class;
    }
}