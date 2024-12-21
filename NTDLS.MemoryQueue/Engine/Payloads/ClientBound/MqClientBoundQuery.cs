using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ClientBound
{
    /// <summary>
    /// This is a query that is sent from the server to a client when the queues are being processed.
    /// </summary>
    internal class MqClientBoundQuery(string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        : IFramePayloadNotification
    {
        public string QueueName { get; set; } = queueName;
        public Guid QueryId { get; set; } = queryId;
        public string PayloadJson { get; set; } = payloadJson;
        public string PayloadType { get; set; } = payloadType;
        public string ReplyType { get; set; } = replyType;
    }
}
