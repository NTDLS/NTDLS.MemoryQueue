using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.Public
{
    public class NmqUnsubscribe : IFrameNotification
    {
        public string QueueName { get; set; }

        public NmqUnsubscribe(string queueName)
        {
            QueueName = queueName;
        }
    }
}
