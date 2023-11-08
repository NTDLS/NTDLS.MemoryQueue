﻿using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ServerBound
{
    /// <summary>
    /// This is a enque query request that is sent from the client to the server.
    /// </summary>
    internal class NmqEnqueueQuery : IFrameNotification
    {
        public Guid QueryId { get; set; }
        public string QueueName { get; set; }
        public string PayloadJson { get; set; }
        public string PayloadType { get; set; }
        public string ReplyType { get; set; }

        public NmqEnqueueQuery(string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        {
            QueueName = queueName;
            QueryId = queryId;
            PayloadJson = payloadJson;
            PayloadType = payloadType;
            ReplyType = replyType;
        }
    }
}
