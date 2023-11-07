using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads
{
    public class NmqQueueConfiguration : IFrameNotification
    {
        public string Name { get; set; }

        public NmqQueueConfiguration(string name)
        {
            Name = name;
        }
    }
}
