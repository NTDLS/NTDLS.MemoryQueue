using NTDLS.MemoryQueue.Engine;
using NTDLS.MemoryQueue.Engine.Payloads;
using NTDLS.MemoryQueue.Engine.Payloads.ServerBound;
using NTDLS.Semaphore;
using NTDLS.StreamFraming.Payloads;
using System.Net;
using System.Net.Sockets;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Listens for connections from MessageClients and processes the incoming notifications/queries.
    /// </summary>
    public class MqServer : IMqMemoryQueue
    {
        private TcpListener? _listener;
        private readonly CriticalResource<List<MqPeerConnection>> _activeConnections = new();
        private Thread? _listenerThreadProc;
        private bool _keepRunning;

        private readonly CriticalResource<MqQueueCollectionManager> _qeueManager = new();

        #region Events.

        /// <summary>
        /// Event fired when a client connects to the server.
        /// </summary>
        public event ConnectedEvent? OnConnected;
        /// <summary>
        /// Event fired when a client connects to the server.
        /// </summary>
        /// <param name="server">The instance of the server that is calling the event.</param>
        /// <param name="connectionId">The id of the client which was connected.</param>
        public delegate void ConnectedEvent(MqServer server, Guid connectionId);

        /// <summary>
        /// Event fired when a client is disconnected from the server.
        /// </summary>
        public event DisconnectedEvent? OnDisconnected;
        /// <summary>
        /// Event fired when a client is disconnected from the server.
        /// </summary>
        /// <param name="server">The instance of the server that is calling the event.</param>
        /// <param name="connectionId">The id of the client which was disconnected.</param>
        public delegate void DisconnectedEvent(MqServer server, Guid connectionId);

        #endregion

        /// <summary>
        /// Creates a new queue with the given configuration.
        /// </summary>
        /// <param name="configuration">The configuration for the new queue.</param>
        public void CreateQueue(MqQueueConfiguration configuration)
            => _qeueManager.Use((o) => o.Create(configuration));

        /// <summary>
        /// Shuts down and deletes an existing queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to delete.</param>
        public void DeleteQueue(string queueName)
            => _qeueManager.Use((o) => o.Delete(queueName));

        /// <summary>
        /// Starts the message server.
        /// </summary>
        public void Start(int listenPort)
        {
            if (_keepRunning)
            {
                return;
            }
            _qeueManager.Use((o) => o.SetServer(this));

            _listener = new TcpListener(IPAddress.Any, listenPort);
            _keepRunning = true;
            _listenerThreadProc = new Thread(ListenerThreadProc);
            _listenerThreadProc.Start();
        }

        /// <summary>
        /// Stops the message server.
        /// </summary>
        public void Shutdown()
        {
            if (_keepRunning == false)
            {
                return;
            }
            _keepRunning = false;
            Utility.TryAndIgnore(() => _listener?.Stop());
            _listenerThreadProc?.Join();

            _activeConnections.Use((o) =>
            {
                o.ForEach(c => c.Disconnect(true));
                o.Clear();
            });

            _qeueManager.Use((o) => o.Shutdown(true));
        }

        void ListenerThreadProc()
        {
            try
            {
                Utility.EnsureNotNull(_listener);

                Thread.CurrentThread.Name = $"NmqServer:ListenerThreadProc:{Environment.CurrentManagedThreadId}";

                _listener.Start();

                while (_keepRunning)
                {
                    var tcpClient = _listener.AcceptTcpClient(); //Wait for an inbound connection.

                    if (tcpClient.Connected)
                    {
                        if (_keepRunning) //Check again, we may have received a connection while shutting down.
                        {
                            var activeConnection = new MqPeerConnection(this, tcpClient);
                            _activeConnections.Use((o) => o.Add(activeConnection));
                            activeConnection.RunAsync();
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.Interrupted
                    && ex.SocketErrorCode != SocketError.Shutdown)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Dispatches a one way notification to the specified connection.
        /// </summary>
        /// <param name="connectionId">The connection id of the client</param>
        /// <param name="notification">The notification message to send.</param>
        /// <exception cref="Exception"></exception>
        public void Notify(Guid connectionId, IFrameNotification notification)
        {
            var connection = _activeConnections.Use((o) => o.Where(c => c.Id == connectionId).FirstOrDefault());
            if (connection == null)
            {
                throw new Exception($"The connection with id '{connectionId}' was not found.");
            }

            connection.SendNotification(notification);
        }

        /// <summary>
        /// Sends a query to the specified client and expects a reply.
        /// </summary>
        /// <typeparam name="T">The type of reply that is expected.</typeparam>
        /// <param name="connectionId">The connection id of the client</param>
        /// <param name="query">The query message to send.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<T?> Query<T>(Guid connectionId, IFrameQuery query) where T : IFrameQueryReply
        {
            var connection = _activeConnections.Use((o) => o.Where(c => c.Id == connectionId).FirstOrDefault());
            if (connection == null)
            {
                throw new Exception($"The connection with id '{connectionId}' was not found.");
            }

            return await connection.SendQuery<T>(query);
        }

        void IMqMemoryQueue.InvokeOnConnected(Guid connectionId)
        {
            OnConnected?.Invoke(this, connectionId);
        }

        void IMqMemoryQueue.InvokeOnDisconnected(Guid connectionId)
        {
            if (_keepRunning) //Avoid a race condition with the client thread waiting on a lock on _activeConnections that is held by Server.Stop().
            {
                _activeConnections.Use((o) => o.RemoveAll(o => o.Id == connectionId));
            }
            OnDisconnected?.Invoke(this, connectionId);
        }

        void IMqMemoryQueue.InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload)
        {
            try
            {
                //Intercept notifications to see if they are Client->Server commands.

                throw new Exception("The server bound notification type is not implemented.");
            }
            catch (Exception ex)
            {
                //TODO: log this.
            }
        }

        IFrameQueryReply IMqMemoryQueue.InvokeOnQueryReceived(Guid connectionId, IFrameQuery payload)
        {
            try
            {
                //Intercept queries to see if they are Client->Server commands.

                if (payload is MqSubscribe subscribe)
                {
                    return MqInternalQueryReplyBoolean.Try(()
                        => _qeueManager.Use((o) => o.Subscribe(connectionId, subscribe.QueueName)));
                }
                else if (payload is MqCreateQueue createQueue)
                {
                    return MqInternalQueryReplyBoolean.Try(()
                        => CreateQueue(createQueue.Configuration));
                }
                else if (payload is MqDeleteQueue deleteQueue)
                {
                    return MqInternalQueryReplyBoolean.Try(()
                        => DeleteQueue(deleteQueue.QueueName));
                }
                else if (payload is MqUnsubscribe unsubscribe)
                {
                    return MqInternalQueryReplyBoolean.Try(()
                        => _qeueManager.Use((o) => o.Unsubscribe(connectionId, unsubscribe.QueueName)));
                }
                else if (payload is MqEnqueueMessage enqueue)
                {
                    return MqInternalQueryReplyBoolean.Try(()
                        => _qeueManager.Use((o) => o.EqueueMessage(enqueue.QueueName, enqueue.PayloadJson, enqueue.PayloadType)));
                }
                else if (payload is MqEnqueueQuery enqueueQuery)
                {
                    return MqInternalQueryReplyBoolean.Try(()
                        => _qeueManager.Use((o) => o.EqueueQuery(connectionId, enqueueQuery.QueueName, enqueueQuery.QueryId,
                                                                    enqueueQuery.PayloadJson, enqueueQuery.PayloadType, enqueueQuery.ReplyType)));
                }
                else if (payload is MqEnqueueQueryReply enqueueQueryReply)
                {
                    return MqInternalQueryReplyBoolean.Try(()
                        => _qeueManager.Use((o) => o.EqueueQueryReply(connectionId, enqueueQueryReply.QueueName, enqueueQueryReply.QueryId,
                                                                        enqueueQueryReply.PayloadJson, enqueueQueryReply.PayloadType, enqueueQueryReply.ReplyType)));
                }

                else
                {
                    throw new Exception("The server bound notification type is not implemented.");
                }
            }
            catch (Exception ex)
            {
                //TODO: log this.
                throw;
            }
        }
    }
}
