using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// <summary>
    /// This is a enqueue query request that is sent from the client to the server.
    /// </summary>
    internal class MqEnqueueQuery(string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        : IFramePayloadQuery
    {
        public Guid QueryId { get; set; } = queryId;
        public string QueueName { get; set; } = queueName;
        public string PayloadJson { get; set; } = payloadJson;
        public string PayloadType { get; set; } = payloadType;
        public string ReplyType { get; set; } = replyType;
    }
}
