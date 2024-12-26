using NTDLS.MemoryQueue.Server;

namespace NTDLS.MemoryQueue.Management
{
    /// <summary>
    /// Queue client configuration parameters.
    /// </summary>
    public class MqServerInformation
    {
        /// <summary>
        /// When true, query replies are queued in a thread pool. Otherwise, queries block other activities.
        /// </summary>
        public bool AsynchronousQueryWaiting { get; internal set; } = true;

        /// <summary>
        /// The default amount of time to wait for a query to reply before throwing a timeout exception.
        /// </summary>
        public TimeSpan QueryTimeout { get; internal set; } = TimeSpan.FromSeconds(30);
        /// <summary>
        /// The initial size in bytes of the receive buffer.
        /// If the buffer ever gets full while receiving data it will be automatically resized up to MaxReceiveBufferSize.
        /// </summary>
        public int InitialReceiveBufferSize { get; internal set; } = ServerDefaults.INITIAL_BUFFER_SIZE;

        /// <summary>
        ///The maximum size in bytes of the receive buffer.
        ///If the buffer ever gets full while receiving data it will be automatically resized up to MaxReceiveBufferSize.
        /// </summary>
        public int MaxReceiveBufferSize { get; internal set; } = ServerDefaults.MAX_BUFFER_SIZE;

        /// <summary>
        ///The growth rate of the auto-resizing for the receive buffer.
        /// </summary>
        public double ReceiveBufferGrowthRate { get; internal set; } = ServerDefaults.BUFFER_GROWTH_RATE;

        /// <summary>
        /// The TCP/IP port that the message queue server is listening on.
        /// </summary>
        public int ListenPort { get; internal set; }
    }
}