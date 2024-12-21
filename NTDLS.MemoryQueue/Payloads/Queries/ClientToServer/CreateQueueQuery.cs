using NTDLS.ReliableMessaging;

namespace NTDLS.MemoryQueue.Payloads.Queries.ClientToServer
{
    internal class CreateQueueQuery(MqQueueConfiguration queueConfiguration)
        : IRmQuery<CreateQueueQueryReply>
    {
        public MqQueueConfiguration QueueConfiguration { get; set; } = queueConfiguration;
    }

    internal class CreateQueueQueryReply
        : IRmQueryReply
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public CreateQueueQueryReply(Exception exception)
        {
            IsSuccess = false;
            ErrorMessage = exception.Message;
        }

        public CreateQueueQueryReply(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public CreateQueueQueryReply()
        {
        }
    }
}
