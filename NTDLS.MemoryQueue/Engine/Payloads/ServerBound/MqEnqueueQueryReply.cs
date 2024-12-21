using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// This is a enqueue query reply request that is sent from the client to the server.
    internal class MqEnqueueQueryReply(string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        : IFramePayloadQuery
    {
        public string QueueName { get; set; } = queueName;
        public string PayloadJson { get; set; } = payloadJson;
        public Guid QueryId { get; set; } = queryId;
        public string PayloadType { get; set; } = payloadType;
        public string ReplyType { get; set; } = replyType;
    }
}
