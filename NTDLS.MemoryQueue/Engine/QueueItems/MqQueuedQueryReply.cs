namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// Represents a "physical" query-reply message that is waiting in the queue.
    /// </summary>
    internal class MqQueuedQueryReply : IMqQueuedItem
    {
        /// <summary>
        /// The ID of the client connection that expects a response to this query.
        /// </summary>
        public Guid OriginationId { get; private set; }
        /// <summary>
        /// The id of the query that this reply is in response to.
        /// </summary>
        public Guid QueryId { get; private set; }
        public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
        public string PayloadJson { get; private set; }
        public string PayloadType { get; set; }
        public string ReplyType { get; set; }

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        public HashSet<Guid> SatisfiedSubscribers { get; set; } = new();

        public MqQueuedQueryReply(Guid originationId, Guid queryId, string payloadJson, string payloadType, string replyType)
        {
            OriginationId = originationId;
            PayloadJson = payloadJson;
            QueryId = queryId;
            PayloadType = payloadType;
            ReplyType = replyType;
        }
    }
}
