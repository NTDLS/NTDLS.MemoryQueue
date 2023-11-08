using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// <summary>
    /// This is a create queue request that is sent from the client to the server.
    /// </summary>
    internal class NmqCreateQueue : IFrameNotification
    {
        public NmqQueueConfiguration Configuration { get; set; }

        public NmqCreateQueue(NmqQueueConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
