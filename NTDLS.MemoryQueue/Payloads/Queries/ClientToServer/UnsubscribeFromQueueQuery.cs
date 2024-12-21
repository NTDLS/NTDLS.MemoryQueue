using NTDLS.ReliableMessaging;

namespace NTDLS.MemoryQueue.Payloads.Queries.ClientToServer
{
    internal class UnsubscribeFromQueueQuery(string queueName)
        : IRmQuery<UnsubscribeFromQueueQueryReply>
    {
        public string QueueName { get; set; } = queueName;
    }

    internal class UnsubscribeFromQueueQueryReply
        : IRmQueryReply
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public UnsubscribeFromQueueQueryReply(Exception exception)
        {
            IsSuccess = false;
            ErrorMessage = exception.Message;
        }

        public UnsubscribeFromQueueQueryReply(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public UnsubscribeFromQueueQueryReply()
        {
        }
    }
}
