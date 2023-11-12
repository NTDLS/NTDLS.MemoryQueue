using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ClientBound
{
    /// <summary>
    /// This is a query that is sent from the server to a client when the queues are being processed.
    /// </summary>
    internal class MqClientBoundQuery : IFramePayloadNotification
    {
        public string QueueName { get; set; }
        public Guid QueryId { get; set; }
        public string PayloadJson { get; set; }
        public string PayloadType { get; set; }
        public string ReplyType { get; set; }

        public MqClientBoundQuery(string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        {
            QueueName = queueName;
            QueryId = queryId;
            PayloadJson = payloadJson;
            PayloadType = payloadType;
            ReplyType = replyType;
        }
    }
}
