using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// <summary>
    /// This is a enqueue query request that is sent from the client to the server.
    /// </summary>
    internal class MqEnqueueQuery : IFrameQuery
    {
        public Guid QueryId { get; set; }
        public string QueueName { get; set; }
        public string PayloadJson { get; set; }
        public string PayloadType { get; set; }
        public string ReplyType { get; set; }

        public MqEnqueueQuery(string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        {
            QueueName = queueName;
            QueryId = queryId;
            PayloadJson = payloadJson;
            PayloadType = payloadType;
            ReplyType = replyType;
        }
    }
}
