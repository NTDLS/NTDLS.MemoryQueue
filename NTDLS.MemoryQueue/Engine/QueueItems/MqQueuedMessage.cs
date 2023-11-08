namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// Represents a "physical" message that is waiting in the queue.
    /// </summary>
    internal class MqQueuedMessage : MqQueuedItemBase, IMqQueuedItem
    {
        public MqQueuedMessage(MqQueue queue, string payloadJson, string payloadType)
            : base(queue, payloadJson, payloadType)
        {
        }
    }
}
