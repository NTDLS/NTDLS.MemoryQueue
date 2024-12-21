using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// <summary>
    /// This is a enqueue message request that is sent from the client to the server.
    /// </summary>
    internal class MqEnqueueMessage(string queueName, string payloadJson, string payloadType)
        : IFramePayloadQuery
    {
        public string QueueName { get; set; } = queueName;
        public string PayloadJson { get; set; } = payloadJson;
        public string PayloadType { get; set; } = payloadType;
    }
}
