namespace NTDLS.MemoryQueue.Server
{
    /// <summary>
    /// A message that is in the queue and waiting to be delivered to all subscribers.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="messageJson"></param>
    internal class EnqueuedMessage(string objectType, string messageJson)
    {
        /// <summary>
        /// The unique ID of the message.
        /// </summary>
        public Guid MessageId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The UTC date and time when the message was enqueued.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The full assembly qualified name of the type of MessageJson.
        /// </summary>
        public string ObjectType { get; set; } = objectType;

        /// <summary>
        /// The message payload that needs to be sent to the subscriber.
        /// </summary>
        public string MessageJson { get; set; } = messageJson;

        /// <summary>
        /// The list of connection IDs that the message has been successfully delivered to.
        /// </summary>
        public HashSet<Guid> DeliveredSubscriberConnectionIDs { get; set; } = new();
    }
}
