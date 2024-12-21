namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// Represents a "physical" message that is waiting in the queue.
    /// </summary>
    internal class MqQueuedMessage(MqQueue queue, string payloadJson, string payloadType)
        : MqQueuedItemBase(queue, payloadJson, payloadType), IMqQueuedItem
    {
    }
}
