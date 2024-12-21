﻿using NTDLS.ReliableMessaging;

namespace NTDLS.MemoryQueue.Payloads.Queries.ClientToServer
{
    internal class SubscribeToQueueQuery(string queueName)
        : IRmQuery<SubscribeToQueueQueryReply>
    {
        public string QueueName { get; set; } = queueName;
    }

    internal class SubscribeToQueueQueryReply
        : IRmQueryReply
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public SubscribeToQueueQueryReply(Exception exception)
        {
            IsSuccess = false;
            ErrorMessage = exception.Message;
        }

        public SubscribeToQueueQueryReply(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public SubscribeToQueueQueryReply()
        {
        }
    }
}
