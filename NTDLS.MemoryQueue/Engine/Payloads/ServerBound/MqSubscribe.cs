using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// <summary>
    /// This is a queue subscription request that is sent from the client to the server.
    /// </summary>
    internal class MqSubscribe : IFramePayloadQuery
    {
        public string QueueName { get; set; }

        public MqSubscribe(string queueName)
        {
            QueueName = queueName;
        }
    }
}
