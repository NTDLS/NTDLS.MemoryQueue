using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue
{
    public class MqQueueConfiguration : IFrameNotification
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
        /// The maximum time in seconds that a item can remain in the queue without being deleivered. 0 = infinite.
        /// </summary>
        public int MaxAgeInSeconds { get; set; } = 0;

        public MqQueueConfiguration(string name)
        {
            Name = name;
        }
    }
}
