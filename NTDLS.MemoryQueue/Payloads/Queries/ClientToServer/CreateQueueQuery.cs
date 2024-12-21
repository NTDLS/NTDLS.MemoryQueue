using NTDLS.ReliableMessaging;

namespace NTDLS.MemoryQueue.Payloads.Queries.ClientToServer
{
    internal class CreateQueueQuery(string queueName)
        : IRmQuery<CreateQueueQueryReply>
    {
        public string QueueName { get; set; } = queueName;
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
