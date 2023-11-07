using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
    internal class NmqUnsubscribe : IFrameNotification
    {
        public string QueueName { get; set; }

        public NmqUnsubscribe(string queueName)
        {
            QueueName = queueName;
        }
    }
}
