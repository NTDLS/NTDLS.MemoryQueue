﻿using static NTDLS.MemoryQueue.MqClient;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Defines a queue configuration.
    /// </summary>
    public class MqReadonlyQueueConfiguration()
    {
        /// <summary>
        /// The name of the queue.
        /// </summary>
        public string QueueName { get; internal set; } = string.Empty;

        /// <summary>
        /// The interval in which the queue will deliver all of its contents to the subscribers. 0 = immediate.
        /// </summary>
        public TimeSpan BatchDeliveryInterval { get; internal set; } = TimeSpan.Zero;

        /// <summary>
        /// The amount of time to wait between sending individual messages to subscribers.
        /// </summary>
        public TimeSpan DeliveryThrottle { get; internal set; } = TimeSpan.Zero;

        /// <summary>
        /// The maximum number of times the server will attempt to deliver any message to a subscriber before giving up. 0 = infinite.
        /// </summary>
        public int MaxDeliveryAttempts { get; internal set; } = 10;

        /// <summary>
        /// The maximum time that a message item can remain in the queue without being delivered before being removed. 0 = infinite.
        /// </summary>
        public TimeSpan MaxMessageAge { get; internal set; } = TimeSpan.Zero;

        /// <summary>
        /// Determines when to remove messages from the queue as they are distributed to subscribers.
        /// </summary>
        public ConsumptionScheme ConsumptionScheme { get; internal set; } = ConsumptionScheme.SuccessfulDeliveryToAllSubscribers;

        /// <summary>
        /// Determines how messages are distributed to subscribers.
        /// </summary>
        public DeliveryScheme DeliveryScheme { get; internal set; } = DeliveryScheme.Random;
    }
}
