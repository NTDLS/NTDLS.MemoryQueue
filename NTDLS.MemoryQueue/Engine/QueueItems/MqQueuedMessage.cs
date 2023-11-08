using NTDLS.Semaphore;

namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// Represents a "physical" message that is waiting in the queue.
    /// </summary>
    internal class MqQueuedMessage : MqQueuedItemBase, IMqQueuedItem
    {
        public MqQueuedMessage(string payloadJson, string payloadType)
            : base(payloadJson, payloadType)
        {
        }
    }
}
