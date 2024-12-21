namespace NTDLS.MemoryQueue.Engine.QueueItems
{
    /// <summary>
    /// Represents a "physical" query-reply message that is waiting in the queue.
    /// </summary>
    internal class MqQueuedQueryReply(MqQueue queue, Guid originationId, Guid queryId, string payloadJson, string payloadType, string replyType)
        : MqQueuedItemBase(queue, payloadJson, payloadType), IMqQueuedItem
    {
        /// <summary>
        /// The ID of the client connection that expects a response to this query.
        /// </summary>
        public Guid OriginationId { get; private set; } = originationId;
        /// <summary>
        /// The id of the query that this reply is in response to.
        /// </summary>
        public Guid QueryId { get; private set; } = queryId;
        public string ReplyType { get; set; } = replyType;

        /// <summary>
        /// A list of the subscribers that the message has been sent to or that have had too many retries.
        /// </summary>
        public HashSet<Guid> SatisfiedSubscribers { get; set; } = new();
    }
}
