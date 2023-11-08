using Newtonsoft.Json;
using NTDLS.MemoryQueue.Engine;
using NTDLS.MemoryQueue.Engine.Payloads.ClientBound;
using NTDLS.MemoryQueue.Engine.Payloads.ServerBound;
using NTDLS.Semaphore;
using NTDLS.StreamFraming.Payloads;
using System.Net;
using System.Net.Sockets;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Connects to a MessageServer then sends/received and processes notifications/queries.
    /// </summary>
    public class NmqClient : INmqMemoryQueue
    {
        private readonly TcpClient _tcpClient = new();
        private PeerConnection? _activeConnection;
        private bool _keepRunning;
        private readonly CriticalResource<Dictionary<Guid, QueryWaitingForReply>> _queriesWaitingForReply = new();

        #region Events.

        /// <summary>
        /// Event fired when a client connects to the server.
        /// </summary>
        public event ConnectedEvent? OnConnected;
        /// <summary>
        /// Event fired when a client connects to the server.
        /// </summary>
        /// <param name="client">The instance of the client that is calling the event.</param>
        /// <param name="connectionId">The id of the client which was connected.</param>
        public delegate void ConnectedEvent(NmqClient client, Guid connectionId);

        /// <summary>
        /// Event fired when a client is disconnected from the server.
        /// </summary>
        public event DisconnectedEvent? OnDisconnected;
        /// <summary>
        /// Event fired when a client is disconnected from the server.
        /// </summary>
        /// <param name="client">The instance of the client that is calling the event.</param>
        /// <param name="connectionId">The id of the client which was disconnected.</param>
        public delegate void DisconnectedEvent(NmqClient client, Guid connectionId);

        /// <summary>
        /// Event fired when a message is received from a client.
        /// </summary>
        public event MessageReceivedEvent? OnMessageReceived;
        /// <summary>
        /// Event fired when a message is received from a client.
        /// </summary>
        /// <param name="client">The instance of the client that is calling the event.</param>
        /// <param name="connectionId">The id of the client which send the message.</param>
        /// <param name="payload"></param>
        public delegate void MessageReceivedEvent(NmqClient client, INmqMessage message);

        /// <summary>
        /// Event fired when a query is received from a client.
        /// </summary>
        public event QueryReceivedEvent? OnQueryReceived;
        /// <summary>
        /// Event fired when a query is received from a client.
        /// </summary>
        /// <param name="client">The instance of the client that is calling the event.</param>
        /// <param name="connectionId">The id of the client which send the query.</param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public delegate INmqQueryReply QueryReceivedEvent(NmqClient client, INmqQuery query);

        #endregion

        /// <summary>
        /// Creates a new queue with the given configuration.
        /// </summary>
        /// <param name="configuration">The configuration for the new queue.</param>
        public void CreateQueue(NmqQueueConfiguration configuration)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqCreateQueue(configuration));
        }

        /// <summary>
        /// Shuts down and deletes an existing queue.
        /// </summary>
        /// <param name="queueName"></param>
        public void DeleteQueue(string queueName)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqDeleteQueue(queueName));
        }

        /// <summary>
        /// Enqueues a one-way message for distirbution to all subscribers.
        /// </summary>
        /// <param name="queueName">The name of the queue to add the message to.</param>
        /// <param name="message">The message object.</param>
        /// <exception cref="Exception"></exception>
        public void EnqueueMessage(string queueName, INmqMessage message)
        {
            Utility.EnsureNotNull(_activeConnection);

            var payloadJson = JsonConvert.SerializeObject(message);
            var payloadType = message.GetType().AssemblyQualifiedName ?? throw new Exception("The message type could not be determined.");
            _activeConnection.SendNotification(new NmqEnqueueMessage(queueName, payloadJson, payloadType));
        }

        /// <summary>
        /// Enqueues a reply to a query, this will be directed only towards the connection that originally sent the query.
        /// </summary>
        /// <param name="query">The original query.</param>
        /// <param name="payloadJson">The payload of the reply</param>
        /// <param name="payloadType">The type of the original query.</param>
        /// <param name="replyType">The type that the reply payload can be deserialized to.</param>
        private void EnqueueQueryReply(NmqClientBoundQuery query, string payloadJson, string payloadType, string replyType)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqEnqueueQueryReply(query.QueueName, query.QueryId, payloadJson, payloadType, replyType));
        }

        /// <summary>
        /// Enqueues a query. This is distributed to all subscribers of the queue.
        /// </summary>
        /// <typeparam name="T">The expected reply type.</typeparam>
        /// <param name="queueName">The name of the queue to send the query to.</param>
        /// <param name="query">The query payload object.</param>
        /// <param name="secondsTimeout">The number of seconds to wait for a reply before egiving up.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<T?> EnqueueQuery<T>(string queueName, INmqQuery query, int secondsTimeout = 30) where T : INmqQueryReply
        {
            Utility.EnsureNotNull(_activeConnection);

            var waitingQuery = new QueryWaitingForReply();

            _queriesWaitingForReply.Use((o)
                => o.Add(waitingQuery.QueryId, waitingQuery));

            var payloadJson = JsonConvert.SerializeObject(query);
            var payloadType = query.GetType().AssemblyQualifiedName ?? throw new Exception("The query type could not be determined.");
            var replyType = typeof(T).AssemblyQualifiedName ?? throw new Exception("The reply type could not be determined.");

            _activeConnection.SendNotification(new NmqEnqueueQuery(queueName, waitingQuery.QueryId, payloadJson, payloadType, replyType));

            return await Task.Run(() =>
            {
                if (waitingQuery.Waiter.WaitOne(secondsTimeout * 1000))
                {
                    return JsonConvert.DeserializeObject<T>(waitingQuery.PayloadJson ?? string.Empty);
                }

                _queriesWaitingForReply.Use((o)
                    => o.Remove(waitingQuery.QueryId));

                throw new Exception("Query timeout expired.");
            });
        }

        /// <summary>
        /// Subscribes this client to the specified queue.
        /// </summary>
        /// <param name="queueName"></param>
        public void Subscribe(string queueName)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqSubscribe(queueName));
        }

        /// <summary>
        /// Unsubscribes the client from the specified queue.
        /// </summary>
        /// <param name="queueName"></param>
        public void Unsubscribe(string queueName)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqUnsubscribe(queueName));
        }

        /// <summary>
        /// Connects to a specified message server by its host name.
        /// </summary>
        /// <param name="hostName">The hostname of the message server.</param>
        /// <param name="port">The listenr port of the message server.</param>
        public void Connect(string hostName, int port)
        {
            if (_keepRunning)
            {
                return;
            }
            _keepRunning = true;

            _tcpClient.Connect(hostName, port);
            _activeConnection = new PeerConnection(this, _tcpClient);
            _activeConnection.RunAsync();
        }

        /// <summary>
        /// Connects to a specified message server by its IP Address.
        /// </summary>
        /// <param name="ipAddress">The IP address of the message server.</param>
        /// <param name="port">The listen port of the message server.</param>
        public void Connect(IPAddress ipAddress, int port)
        {
            if (_keepRunning)
            {
                return;
            }
            _keepRunning = true;

            _tcpClient.Connect(ipAddress, port);
            _activeConnection = new PeerConnection(this, _tcpClient);
            _activeConnection.RunAsync();
        }

        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        public void Disconnect()
        {
            _keepRunning = false;
            _activeConnection?.Disconnect(true);
        }

        void INmqMemoryQueue.InvokeOnConnected(Guid connectionId)
        {
            OnConnected?.Invoke(this, connectionId);
        }

        void INmqMemoryQueue.InvokeOnDisconnected(Guid connectionId)
        {
            _activeConnection = null;
            OnDisconnected?.Invoke(this, connectionId);
        }

        void INmqMemoryQueue.InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload)
        {
            if (payload is NmqClientBoundMessage clientBoundMessage)
            {
                if (OnMessageReceived == null)
                {
                    throw new Exception("The client bound notification event is not handled.");
                }

                var message = Utility.ExtractGenericType<INmqMessage>(clientBoundMessage.PayloadJson, clientBoundMessage.PayloadType);

                OnMessageReceived.Invoke(this, message);
            }
            else if (payload is NmqClientBoundQuery clientBoundQuery)
            {
                if (OnQueryReceived == null)
                {
                    throw new Exception("The client bound query event is not handled.");
                }

                var query = Utility.ExtractGenericType<INmqQuery>(clientBoundQuery.PayloadJson, clientBoundQuery.PayloadType);

                var queryResultPayload = OnQueryReceived.Invoke(this, query);

                string replyPayloadJson = JsonConvert.SerializeObject(queryResultPayload);

                EnqueueQueryReply(clientBoundQuery, replyPayloadJson, clientBoundQuery.PayloadType, clientBoundQuery.ReplyType);
            }
            else if (payload is NmqClientBoundQueryReply clientBoundQueryReply)
            {
                _queriesWaitingForReply.Use((o) =>
                {
                    if (o.TryGetValue(clientBoundQueryReply.QueryId, out var waitingQuery))
                    {
                        waitingQuery.SetReplyPayload(clientBoundQueryReply.PayloadJson);
                        waitingQuery.Waiter.Set();
                        o.Remove(clientBoundQueryReply.QueryId);
                    }
                    else
                    {
                        //The query has already been answered by another connection or it has expired.
                    }
                });
            }
            else
            {
                throw new Exception("The client bound notification type is not implemented.");
            }
        }
    }
}
