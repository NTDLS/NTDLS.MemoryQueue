using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// <summary>
    /// /// This is a queue remove subscription request that is sent from the client to the server.
    /// </summary>
    internal class MqUnsubscribe : IFrameQuery
    {
        public string QueueName { get; set; }

        public MqUnsubscribe(string queueName)
        {
            QueueName = queueName;
        }
    }
}
