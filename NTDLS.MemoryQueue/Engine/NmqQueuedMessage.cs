namespace NTDLS.MemoryQueue.Engine
{
    internal class NmqQueuedMessage
    {
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string Payload { get; private set; } = string.Empty;

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        public HashSet<Guid> SatisfiedSubscribers { get; set; } = new();

        public NmqQueuedMessage(string payload)
        {
            Payload = payload;
        }

        public NmqQueuedMessage()
        {
        }
    }
}
