using Newtonsoft.Json;
using NTDLS.MemoryQueue.Engine;
using NTDLS.MemoryQueue.Engine.Payloads;
using NTDLS.MemoryQueue.Engine.Payloads.ClientBound;
using NTDLS.MemoryQueue.Engine.Payloads.ServerBound;
using NTDLS.Semaphore;
using NTDLS.StreamFraming.Payloads;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using static NTDLS.MemoryQueue.MqTypes;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Connects to a MessageServer then sends/received and processes notifications/queries.
    /// </summary>
    public class MqClient : IMqMemoryQueue
    {
        private readonly TcpClient _tcpClient = new();
        private MqPeerConnection? _activeConnection;
        private bool _keepRunning;
        private readonly CriticalResource<Dictionary<Guid, MqQueryWaitingForReply>> _queriesWaitingForReply = new();

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
        public delegate void ConnectedEvent(MqClient client, Guid connectionId);

        /// <summary>
        /// Event fired when a client is disconnected from the server.
        /// </summary>
        public event DisconnectedEvent? OnDisconnected;
        /// <summary>
        /// Event fired when a client is disconnected from the server.
        /// </summary>
        /// <param name="client">The instance of the client that is calling the event.</param>
        /// <param name="connectionId">The id of the client which was disconnected.</param>
        public delegate void DisconnectedEvent(MqClient client, Guid connectionId);

        /// <summary>
        /// Event fired when a message is received from a client.
        /// </summary>
        public event MessageReceivedEvent? OnMessageReceived;
        /// <summary>
        /// Event fired when a message is received from a client.
        /// </summary>
        /// <param name="client">The instance of the client that is calling the event.</param>
        /// <param name="message">The message which has been received.</param>
        public delegate void MessageReceivedEvent(MqClient client, IMqMessage message);

        /// <summary>
        /// Event fired when a query is received from a client.
        /// </summary>
        public event QueryReceivedEvent? OnQueryReceived;
        /// <summary>
        /// Event fired when a query is received from a client.
        /// </summary>
        /// <param name="client">The instance of the client that is calling the event.</param>
        /// <param name="query">The query which has been received.</param>
        /// <returns></returns>
        public delegate IMqQueryReply QueryReceivedEvent(MqClient client, IMqQuery query);

        /// <summary>
        /// Event fired a log entry needs to be recorded.
        /// </summary>
        public event ClientLogEvent? OnLog;
        /// <summary>
        /// Event fired a log entry needs to be recorded.
        /// </summary>
        /// <param name="client">The instance of the client that is calling the event.</param>
        /// <param name="entry">The log entry that is being reported.</param>
        public delegate void ClientLogEvent(MqClient client, MqLogEntry entry);

        #endregion

        private void EnsureConnected([NotNull] MqPeerConnection? activeConnection)
        {
            try
            {
                if (activeConnection == null)
                {
                    throw new Exception("The client connection has not been established.");
                }

                if (activeConnection.IsHealthy == false)
                {
                    throw new Exception("The client is not connected.");
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke(this, new MqLogEntry(ex));
                throw;
            }
        }

        /// <summary>
        /// Creates a new queue with the given configuration.
        /// </summary>
        /// <param name="configuration">The configuration for the new queue.</param>
        /// <param name="timeoutSeconds">The number of seconds to wait on the operation to be acknoledged by the server.</param>
        public void CreateQueue(MqQueueConfiguration configuration, int timeoutSeconds = 30)
        {
            EnsureConnected(_activeConnection);

            if (!_activeConnection.SendQuery<MqInternalQueryReplyBoolean>(new MqCreateQueue(configuration))
                .ContinueWith(TestAsyncResult).Wait(timeoutSeconds * 1000))
            {
                throw new Exception("The operation timed-out.");
            }
        }

        /// <summary>
        /// Shuts down and deletes an existing queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to delete.</param>
        /// <param name="timeoutSeconds">The number of seconds to wait on the operation to be acknoledged by the server.</param>
        public void DeleteQueue(string queueName, int timeoutSeconds = 30)
        {
            EnsureConnected(_activeConnection);

            if (!_activeConnection.SendQuery<MqInternalQueryReplyBoolean>(new MqDeleteQueue(queueName))
                .ContinueWith(TestAsyncResult).Wait(timeoutSeconds * 1000))
            {
                throw new Exception("The operation timed-out.");
            }
        }

        /// <summary>
        /// Enqueues a one-way message for distirbution to all subscribers.
        /// </summary>
        /// <param name="queueName">The name of the queue to add the message to.</param>
        /// <param name="message">The message to dispatch to the server for distribution.</param>
        /// <param name="timeoutSeconds">The number of seconds to wait on the operation to be acknoledged by the server.</param>
        /// <exception cref="Exception"></exception>
        public void EnqueueMessage(string queueName, IMqMessage message, int timeoutSeconds = 30)
        {
            EnsureConnected(_activeConnection);

            var payloadJson = JsonConvert.SerializeObject(message);
            var payloadType = message.GetType().AssemblyQualifiedName ?? throw new Exception("The message type could not be determined.");

            if (!_activeConnection.SendQuery<MqInternalQueryReplyBoolean>(new MqEnqueueMessage(queueName, payloadJson, payloadType))
                .ContinueWith(TestAsyncResult).Wait(timeoutSeconds * 1000))
            {
                throw new Exception("The operation timed-out.");
            }
        }

        /// <summary>
        /// Enqueues a reply to a query, this will be directed only towards the connection that originally sent the query.
        /// </summary>
        /// <param name="query">The original query.</param>
        /// <param name="payloadJson">The payload of the reply</param>
        /// <param name="payloadType">The type of the original query.</param>
        /// <param name="replyType">The type that the reply payload can be deserialized to.</param>
        /// <param name="timeoutSeconds">The number of seconds to wait on the operation to be acknoledged by the server.</param>
        private void EnqueueQueryReply(MqClientBoundQuery query, string payloadJson, string payloadType, string replyType, int timeoutSeconds = 30)
        {
            EnsureConnected(_activeConnection);

            if (!_activeConnection.SendQuery<MqInternalQueryReplyBoolean>(new MqEnqueueQueryReply(query.QueueName, query.QueryId, payloadJson, payloadType, replyType))
                .ContinueWith(TestAsyncResult).Wait(timeoutSeconds * 1000))
            {
                throw new Exception("The operation timed-out.");
            }
        }

        /// <summary>
        /// Enqueues a query. This is distributed to all subscribers of the queue.
        /// </summary>
        /// <typeparam name="T">The expected reply type.</typeparam>
        /// <param name="queueName">The name of the queue to send the query to.</param>
        /// <param name="query">The query to dispatch to the server for distribution.</param>
        /// <param name="timeoutSeconds">The number of seconds to wait on the operation to be acknoledged by the server.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<T?> EnqueueQuery<T>(string queueName, IMqQuery query, int timeoutSeconds = 30) where T : IMqQueryReply
        {
            EnsureConnected(_activeConnection);

            try
            {
                var waitingQuery = new MqQueryWaitingForReply();

                _queriesWaitingForReply.Use((o)
                    => o.Add(waitingQuery.QueryId, waitingQuery));

                var payloadJson = JsonConvert.SerializeObject(query);
                var payloadType = query.GetType().AssemblyQualifiedName ?? throw new Exception("The query type could not be determined.");
                var replyType = typeof(T).AssemblyQualifiedName ?? throw new Exception("The reply type could not be determined.");

                if (!_activeConnection.SendQuery<MqInternalQueryReplyBoolean>(new MqEnqueueQuery(queueName, waitingQuery.QueryId, payloadJson, payloadType, replyType))
                    .ContinueWith(TestAsyncResult).Wait(timeoutSeconds * 1000))
                {
                    throw new Exception("The operation timed-out.");
                }

                return await Task.Run(() =>
                {
                    if (waitingQuery.Waiter.WaitOne(timeoutSeconds * 1000))
                    {
                        return JsonConvert.DeserializeObject<T>(waitingQuery.PayloadJson ?? string.Empty);
                    }

                    _queriesWaitingForReply.Use((o)
                        => o.Remove(waitingQuery.QueryId));

                    throw new Exception("Query timeout expired.");
                });
            }
            catch (Exception ex)
            {
                OnLog?.Invoke(this, new MqLogEntry(ex));
                throw;
            }
        }

        /// <summary>
        /// Subscribes this client to the specified queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to subscribe to.</param>
        /// <param name="timeoutSeconds">The number of seconds to wait on the operation to be acknoledged by the server.</param>
        public void Subscribe(string queueName, int timeoutSeconds = 30)
        {
            EnsureConnected(_activeConnection);

            if (!_activeConnection.SendQuery<MqInternalQueryReplyBoolean>(new MqSubscribe(queueName))
                .ContinueWith(TestAsyncResult).Wait(timeoutSeconds * 1000))
            {
                throw new Exception("The operation timed-out.");
            }
        }

        /// <summary>
        /// Unsubscribes the client from the specified queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to unsubscribe from.</param>
        /// <param name="timeoutSeconds">The number of seconds to wait on the operation to be acknoledged by the server.</param>
        public void Unsubscribe(string queueName, int timeoutSeconds = 30)
        {
            EnsureConnected(_activeConnection);

            if (!_activeConnection.SendQuery<MqInternalQueryReplyBoolean>(new MqUnsubscribe(queueName))
                .ContinueWith(TestAsyncResult).Wait(timeoutSeconds * 1000))
            {
                throw new Exception("The operation timed-out.");
            }
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
            _activeConnection = new MqPeerConnection(this, _tcpClient);
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
            _activeConnection = new MqPeerConnection(this, _tcpClient);
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

        void IMqMemoryQueue.InvokeOnConnected(Guid connectionId)
        {
            OnConnected?.Invoke(this, connectionId);
        }

        void IMqMemoryQueue.InvokeOnDisconnected(Guid connectionId)
        {
            _activeConnection = null;
            OnDisconnected?.Invoke(this, connectionId);
        }

        void IMqMemoryQueue.InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload)
        {
            try
            {
                if (payload is MqClientBoundMessage clientBoundMessage)
                {
                    if (OnMessageReceived == null)
                    {
                        throw new Exception("The client bound notification event is not handled.");
                    }

                    var message = Utility.ExtractGenericType<IMqMessage>(clientBoundMessage.PayloadJson, clientBoundMessage.PayloadType);

                    OnMessageReceived.Invoke(this, message);
                }
                else if (payload is MqClientBoundQuery clientBoundQuery)
                {
                    if (OnQueryReceived == null)
                    {
                        throw new Exception("The client bound query event is not handled.");
                    }

                    var query = Utility.ExtractGenericType<IMqQuery>(clientBoundQuery.PayloadJson, clientBoundQuery.PayloadType);

                    var queryResultPayload = OnQueryReceived.Invoke(this, query);

                    string replyPayloadJson = JsonConvert.SerializeObject(queryResultPayload);

                    EnqueueQueryReply(clientBoundQuery, replyPayloadJson, clientBoundQuery.PayloadType, clientBoundQuery.ReplyType);
                }
                else if (payload is MqClientBoundQueryReply clientBoundQueryReply)
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
                            OnLog?.Invoke(this, new MqLogEntry(MqLogSeverity.Warning, $"The query with id '{clientBoundQueryReply.QueryId}' has alreay been answered or has expired."));
                        }
                    });
                }
                else
                {
                    throw new Exception("The client bound notification type is not implemented.");
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke(this, new MqLogEntry(ex));
            }
        }

        IFrameQueryReply IMqMemoryQueue.InvokeOnQueryReceived(Guid connectionId, IFrameQuery payload)
        {
            throw new Exception("The client bound query type is not implemented.");
        }

        private void TestAsyncResult(Task<MqInternalQueryReplyBoolean> o)
        {
            if (o.Exception != null)
            {
                throw new Exception($"The task failed. Exception: {o.Exception}");
            }
            else if (o.IsCompletedSuccessfully)
            {
                if (o.Result.Value != true)
                {
                    throw new Exception($"The task failed. Exception: {o.Result.Message}");
                }
            }
            else
            {
                throw new Exception($"The task failed with an unknown exception.");
            }
        }
    }
}
