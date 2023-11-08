﻿using NTDLS.StreamFraming.Payloads;

namespace NTDLS.MemoryQueue.Payloads.ServerBound
{
    /// This is a enque query reply request that is sent from the client to the server.
    internal class NmqEnqueueQueryReply : IFrameNotification
    {
        public string QueueName { get; set; }
        public string PayloadJson { get; set; }
        public Guid QueryId { get; set; }
        public string PayloadType { get; set; }
        public string ReplyType { get; set; }

        public NmqEnqueueQueryReply(string queueName, Guid queryId, string payloadJson, string payloadType, string replyType)
        {
            QueueName = queueName;
            PayloadJson = payloadJson;
            QueryId = queryId;
            PayloadType = payloadType;
            ReplyType = replyType;
        }
    }
}
