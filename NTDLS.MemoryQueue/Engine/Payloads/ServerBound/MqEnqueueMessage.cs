using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// <summary>
    /// This is a enqueue message request that is sent from the client to the server.
    /// </summary>
    internal class MqEnqueueMessage : IFrameNotification
    {
        public string QueueName { get; set; }
        public string PayloadJson { get; set; }
        public string PayloadType { get; set; }

        public MqEnqueueMessage(string queueName, string payloadJson, string payloadType)
        {
            QueueName = queueName;
            PayloadJson = payloadJson;
            PayloadType = payloadType;
        }
    }
}
