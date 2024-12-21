namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Defines a queue configuration.
    /// </summary>
    public class MqQueueConfiguration
    {
        /// <summary>
        /// The name of the queue.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The maximum number of times the queue will attempt to distribute a message to a subscriber before giving up.
        /// </summary>
        public int MaxDistributionAttempts { get; set; } = 10;

        /// <summary>
        /// The maximum time in seconds that a item can remain in the queue without being delivered. 0 = infinite.
        /// </summary>
        public int MaxAgeInSeconds { get; set; } = 0;

        /// <summary>
        /// Constructs a default queue configuration.
        /// </summary>
        /// <param name="name"></param>
        public MqQueueConfiguration(string name)
        {
            Name = name;
        }
    }
}
