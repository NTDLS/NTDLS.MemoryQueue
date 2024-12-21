﻿using NTDLS.ReliableMessaging;

namespace NTDLS.MemoryQueue.Payloads.Queries.ClientToServer
{
    internal class EnqueueMessageToQueue(string queueName, string objectType, string messageJson)
        : IRmQuery<EnqueueMessageToQueueReply>
    {
        public string QueueName { get; set; } = queueName;
        /// <summary>
        /// The full assembly qualified name of the type of MessageJson.
        /// </summary>
        public string ObjectType { get; set; } = objectType;
        public string MessageJson { get; set; } = messageJson;
    }

    internal class EnqueueMessageToQueueReply
        : IRmQueryReply
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public EnqueueMessageToQueueReply(Exception exception)
        {
            IsSuccess = false;
            ErrorMessage = exception.Message;
        }

        public EnqueueMessageToQueueReply(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public EnqueueMessageToQueueReply()
        {
        }
    }
}
