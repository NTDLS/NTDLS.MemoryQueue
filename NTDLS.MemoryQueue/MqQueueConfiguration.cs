namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Defines a queue configuration.
    /// </summary>
    public class MqQueueConfiguration(string name)
    {
        /// <summary>
        /// The name of the queue.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// The maximum number of times the server will attempt to deliver any message to a subscriber before giving up.
        /// </summary>
        public int MaxDeliveryAttempts { get; set; } = 10;

        /// <summary>
        /// The maximum time that a message item can remain in the queue without being delivered before being removed. 0 = infinite.
        /// </summary>
        public TimeSpan MaxMessageAge { get; set; } = TimeSpan.Zero;
    }
}
