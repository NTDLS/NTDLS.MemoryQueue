namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// Represents a "physical" message that is waiting in the queue.
    /// </summary>
    internal class MqQueuedMessage : IMqQueuedItem
    {
        public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
        public string PayloadJson { get; private set; }
        public string PayloadType { get; private set; }

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        public HashSet<Guid> SatisfiedSubscribers { get; set; } = new();

        public MqQueuedMessage(string payloadJson, string payloadType)
        {
            PayloadJson = payloadJson;
            PayloadType = payloadType;
        }
    }
}
