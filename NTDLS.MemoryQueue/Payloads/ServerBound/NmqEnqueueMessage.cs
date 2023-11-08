using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ServerBound
{
    /// <summary>
    /// This is a enque message request that is sent from the client to the server.
    /// </summary>
    internal class NmqEnqueueMessage : IFrameNotification
    {
        public string QueueName { get; set; }
        public string Payload { get; set; }
        public string PayloadType { get; set; }

        public NmqEnqueueMessage(string queueName, string payload, string payloadType)
        {
            QueueName = queueName;
            Payload = payload;
            PayloadType = payloadType;
        }
    }
}
