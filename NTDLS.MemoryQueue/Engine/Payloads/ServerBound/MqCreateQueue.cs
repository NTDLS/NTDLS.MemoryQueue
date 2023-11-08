using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Engine.Payloads.ServerBound
{
    /// <summary>
    /// This is a create queue request that is sent from the client to the server.
    /// </summary>
    internal class MqCreateQueue : IFrameNotification
    {
        public MqQueueConfiguration Configuration { get; set; }

        public MqCreateQueue(MqQueueConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
