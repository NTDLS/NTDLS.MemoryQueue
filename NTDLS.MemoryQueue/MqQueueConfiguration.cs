using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue
{
    public class MqQueueConfiguration : IFrameNotification
    {
        public string Name { get; set; }

        public MqQueueConfiguration(string name)
        {
            Name = name;
        }
    }
}
