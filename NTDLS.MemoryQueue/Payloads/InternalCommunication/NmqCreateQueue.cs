using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.InternalCommunication
{
    internal class NmqCreateQueue : IFrameNotification
    {
        public NmqQueueConfiguration Configuration { get; set; }

        public NmqCreateQueue(NmqQueueConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
