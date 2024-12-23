using NTDLS.MemoryQueue.Payloads.Queries.ClientToServer;
using NTDLS.ReliableMessaging;

namespace NTDLS.MemoryQueue.Server.QueryHandlers
{
    internal class InternalServerQueryHandlers(MqServer mqServer)
        : IRmMessageHandler
    {
        private readonly MqServer _mqServer = mqServer;

        public CreateQueueQueryReply CreateQueueQuery(RmContext context, CreateQueueQuery param)
        {
            try
            {
                _mqServer.CreateQueue(param.QueueConfiguration);
                return new CreateQueueQueryReply(true);
            }
            catch (Exception ex)
            {
                return new CreateQueueQueryReply(ex.GetBaseException());
            }
        }

        public DeleteQueueQueryReply DeleteQueueQuery(RmContext context, DeleteQueueQuery param)
        {
            try
            {
                _mqServer.DeleteQueue(param.QueueName);
                return new DeleteQueueQueryReply(true);
            }
            catch (Exception ex)
            {
                return new DeleteQueueQueryReply(ex.GetBaseException());
            }
        }

        public SubscribeToQueueQueryReply SubscribeToQueueQuery(RmContext context, SubscribeToQueueQuery param)
        {
            try
            {
                _mqServer.SubscribeToQueue(context.ConnectionId, param.QueueName);
                return new SubscribeToQueueQueryReply(true);
            }
            catch (Exception ex)
            {
                return new SubscribeToQueueQueryReply(ex.GetBaseException());
            }
        }

        public UnsubscribeFromQueueQueryReply UnsubscribeFromQueueQuery(RmContext context, UnsubscribeFromQueueQuery param)
        {
            try
            {
                _mqServer.UnsubscribeFromQueue(context.ConnectionId, param.QueueName);
                return new UnsubscribeFromQueueQueryReply(true);
            }
            catch (Exception ex)
            {
                return new UnsubscribeFromQueueQueryReply(ex.GetBaseException());
            }
        }

        public EnqueueMessageToQueueReply EnqueueMessageToQueue(RmContext context, EnqueueMessageToQueue param)
        {
            try
            {
                _mqServer.EnqueueMessage(param.QueueName, param.ObjectType, param.MessageJson);
                return new EnqueueMessageToQueueReply(true);
            }
            catch (Exception ex)
            {
                return new EnqueueMessageToQueueReply(ex.GetBaseException());
            }
        }
    }
}
