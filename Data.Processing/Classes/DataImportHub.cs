using BackgroundProcessing;
using Microsoft.AspNetCore.SignalR;

namespace DataProcessing.Classes
{
    public class DataImportHub : Hub
    {
        private readonly IBackgroundTaskQueue _queue;
        public DataImportHub(IBackgroundTaskQueue queue)
        {
            _queue = queue;
        }

        public void Stop()
        {
            _queue.CancelWorkItem();
        }
    }
}
