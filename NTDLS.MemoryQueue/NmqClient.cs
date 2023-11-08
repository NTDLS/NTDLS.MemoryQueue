using Newtonsoft.Json;
using NTDLS.MemoryQueue.Engine;
using NTDLS.MemoryQueue.Events;
using NTDLS.MemoryQueue.Payloads;
using NTDLS.MemoryQueue.Payloads.ClientBound;
using NTDLS.MemoryQueue.Payloads.ServerBound;
using NTDLS.Semaphore;
using NTDLS.StreamFraming.Payloads;
using System.Net;
using System.Net.Sockets;

namespace NTDLS.ReliableMessaging
{
    /// <summary>
    /// Connects to a MessageServer then sends/received and processes notifications/queries.
    /// </summary>
    public class NmqClient : IMessageHub
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
        public delegate void MessageReceivedEvent(NmqClient client, NmqMessageReceivedEventParam message);

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

        public void CreateQueue(NmqQueueConfiguration configuration)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqCreateQueue(configuration));
        }

        public void DeleteQueue(string queueName)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqDeleteQueue(queueName));
        }

        public void Enqueue(string queueName, string payload)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqEnqueueMessage(queueName, payload));
        }

        private void EnqueueQueryReply(NmqClientBoundQuery query, string payload, string payloadType, string replyType)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqEnqueueQueryReply(query.QueueName, query.QueryId, payload, payloadType, replyType));
        }

        public async Task<T?> Query<T>(string queueName, INmqQuery payload, int secondsTimeout = 30) where T : INmqQueryReply
        {
            Utility.EnsureNotNull(_activeConnection);

            var waitingQuery = new QueryWaitingForReply();

            _queriesWaitingForReply.Use((o)
                => o.Add(waitingQuery.QueryId, waitingQuery));

            var jsonString = JsonConvert.SerializeObject(payload);

            var payloadType = payload.GetType().AssemblyQualifiedName ?? throw new Exception("The payload type could not be determined.");
            var replyType = typeof(T).AssemblyQualifiedName ?? throw new Exception("The reply type could not be determined.");

            _activeConnection.SendNotification(new NmqEnqueueQuery(queueName, waitingQuery.QueryId, jsonString, payloadType, replyType));

            return await Task.Run(() =>
            {
                if (waitingQuery.Waiter.WaitOne(secondsTimeout * 1000))
                {
                    return JsonConvert.DeserializeObject<T>(waitingQuery.Payload ?? string.Empty);
                }

                _queriesWaitingForReply.Use((o)
                    => o.Remove(waitingQuery.QueryId));

                throw new Exception("Query timeout expired.");
            });
        }

        /*
        public async Task<string?> Query(string queueName, string payload, int secondsTimeout = 30)
        {
            Utility.EnsureNotNull(_activeConnection);

            var waitingQuery = new QueryWaitingForReply();

            _queriesWaitingForReply.Use((o)
                => o.Add(waitingQuery.QueryId, waitingQuery));

            _activeConnection.SendNotification(new NmqEnqueueQuery(queueName, waitingQuery.QueryId, payload, ));

            return await Task.Run(() =>
            {
                if (waitingQuery.Waiter.WaitOne(secondsTimeout * 1000))
                {
                    return waitingQuery.Payload;
                }

                _queriesWaitingForReply.Use((o)
                    => o.Remove(waitingQuery.QueryId));

                throw new Exception("Query timeout expired.");
            });
        }
        */

        public void Subscribe(string queueName)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(new NmqSubscribe(queueName));
        }

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
        /// <param name="port">The listenr port of the message server.</param>
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

        /*
        /// <summary>
        /// Dispatches a one way notification to the connected server.
        /// </summary>
        /// <param name="notification">The notification message to send.</param>
        public void Notify(IFrameNotification notification)
        {
            Utility.EnsureNotNull(_activeConnection);
            _activeConnection.SendNotification(notification);
        }
        */

        /*
        /// <summary>
        /// Sends a query to the specified client and expects a reply.
        /// </summary>
        /// <typeparam name="T">The type of reply that is expected.</typeparam>
        /// <param name="query">The query message to send.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<T?> Query<T>(IFrameQuery query) where T : IFrameQueryReply
        {
            Utility.EnsureNotNull(_activeConnection);
            return await _activeConnection.SendQuery<T>(query);
        }
        */

        void IMessageHub.InvokeOnConnected(Guid connectionId)
        {
            OnConnected?.Invoke(this, connectionId);
        }

        void IMessageHub.InvokeOnDisconnected(Guid connectionId)
        {
            _activeConnection = null;
            OnDisconnected?.Invoke(this, connectionId);
        }

        void IMessageHub.InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload)
        {
            if (payload is NmqClientBoundMessage broadcastMessage)
            {
                if (OnMessageReceived == null)
                {
                    throw new Exception("The notification message hander event was not handled.");
                }

                var param = new NmqMessageReceivedEventParam()
                {
                    Payload = broadcastMessage.Payload
                };

                OnMessageReceived.Invoke(this, param);
            }
            else if (payload is NmqClientBoundQuery broadcastQuery)
            {
                if (OnQueryReceived == null)
                {
                    throw new Exception("The query message hander event was not handled.");
                }

                var query = Utility.ExtractGenericType<INmqQuery>(broadcastQuery.Payload, broadcastQuery.PayloadType);

                var queryResultPayload = OnQueryReceived.Invoke(this, query);

                string replyPayloadJson = JsonConvert.SerializeObject(queryResultPayload);

                EnqueueQueryReply(broadcastQuery, replyPayloadJson, broadcastQuery.PayloadType, broadcastQuery.ReplyType);
            }
            else if (payload is NmqClientBoundQueryReply broadcastQueryReply)
            {
                _queriesWaitingForReply.Use((o) =>
                {
                    if (o.TryGetValue(broadcastQueryReply.QueryId, out var waitingQuery))
                    {
                        waitingQuery.SetReplyPayload(broadcastQueryReply.Payload);
                        waitingQuery.Waiter.Set();
                        o.Remove(broadcastQueryReply.QueryId);
                    }
                    else
                    {
                        //The query has already been answered by another client or it has expired.
                    }
                });
            }
            else
            {
                throw new Exception("The client notification container is unsupported.");
            }
        }
    }
}
