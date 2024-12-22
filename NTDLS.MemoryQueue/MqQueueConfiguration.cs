using static NTDLS.MemoryQueue.MqClient;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Defines a queue configuration.
    /// </summary>
    public class MqQueueConfiguration(string queueName)
    {
        /// <summary>
        /// The name of the queue.
        /// </summary>
        public string QueueName { get; set; } = queueName;

        /// <summary>
        /// The maximum number of times the server will attempt to deliver any message to a subscriber before giving up. 0 = infinite.
        /// </summary>
        public int MaxDeliveryAttempts { get; set; } = 10;

        /// <summary>
        /// The maximum time that a message item can remain in the queue without being delivered before being removed. 0 = infinite.
        /// </summary>
        public TimeSpan MaxMessageAge { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Determines when to remove messages from the queue as they are distributed to subscribers.
        /// </summary>
        public ConsumptionScheme ConsumptionScheme { get; set; } = ConsumptionScheme.SuccessfulDeliveryToAllSubscribers;

        /// <summary>
        /// Determines how messages are distributed to subscribers.
        /// </summary>
        public DeliveryScheme DeliveryScheme { get; set; } = DeliveryScheme.Random;
    }
}
