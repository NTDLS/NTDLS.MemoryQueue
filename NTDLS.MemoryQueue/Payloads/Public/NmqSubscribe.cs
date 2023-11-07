using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.Public
{
    public class NmqSubscribe : IFrameNotification
    {
        public string QueueName { get; set; }

        public NmqSubscribe(string queueName)
        {
            QueueName = queueName;
        }
    }
}
