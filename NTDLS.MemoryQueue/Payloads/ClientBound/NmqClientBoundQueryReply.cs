using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ClientBound
{
    /// <summary>
    /// This is a query reply that is sent from the server to a client when the queues are being processed.
    /// </summary>
    internal class NmqClientBoundQueryReply : IFrameNotification
    {
        public string QueueName { get; set; }
        public Guid QueryId { get; set; }
        public Guid OriginationId { get; set; }
        public string? Payload { get; set; }

        public NmqClientBoundQueryReply(string queueName, Guid originationId, Guid queryId, string payload)
        {
            QueueName = queueName;
            QueryId = queryId;
            Payload = payload;
            OriginationId = originationId;
        }
    }
}
