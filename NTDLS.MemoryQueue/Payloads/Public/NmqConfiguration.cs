using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.Public
{
    public class NmqConfiguration : IFrameNotification
    {
        public string Name { get; set; }

        public NmqConfiguration(string name)
        {
            Name = name;
        }
    }
}
