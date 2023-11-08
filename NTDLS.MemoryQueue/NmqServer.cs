using NTDLS.MemoryQueue.Engine;
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
    public class NmqServer : INmqMemoryQueue
    {
        private TcpListener? _listener;
        private readonly CriticalResource<List<PeerConnection>> _activeConnections = new();
        private Thread? _listenerThreadProc;
        private bool _keepRunning;

        private readonly CriticalResource<NmqQueueManager> _qeueManager = new();

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
        public delegate void ConnectedEvent(NmqServer server, Guid connectionId);

        /// <summary>
        /// Event fired when a client is disconnected from the server.
        /// </summary>
        public event DisconnectedEvent? OnDisconnected;
        /// <summary>
        /// Event fired when a client is disconnected from the server.
        /// </summary>
        /// <param name="server">The instance of the server that is calling the event.</param>
        /// <param name="connectionId">The id of the client which was disconnected.</param>
        public delegate void DisconnectedEvent(NmqServer server, Guid connectionId);

        #endregion

        public void CreateQueue(NmqQueueConfiguration config)
            => _qeueManager.Use((o) => o.Create(config));

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
                            var activeConnection = new PeerConnection(this, tcpClient);
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

        void INmqMemoryQueue.InvokeOnConnected(Guid connectionId)
        {
            OnConnected?.Invoke(this, connectionId);
        }

        void INmqMemoryQueue.InvokeOnDisconnected(Guid connectionId)
        {
            if (_keepRunning) //Avoid a race condition with the client thread waiting on a lock on _activeConnections that is held by Server.Stop().
            {
                _activeConnections.Use((o) => o.RemoveAll(o => o.Id == connectionId));
            }
            OnDisconnected?.Invoke(this, connectionId);
        }

        void INmqMemoryQueue.InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload)
        {
            //Intercept notifications to see if they are Client->Server commands.
            if (payload is NmqCreateQueue createQueue)
            {
                CreateQueue(createQueue.Configuration);
            }
            else if (payload is NmqDeleteQueue deleteQueue)
            {
                DeleteQueue(deleteQueue.QueueName);
            }
            else if (payload is NmqSubscribe subscribe)
            {
                _qeueManager.Use((o) => o.Subscribe(connectionId, subscribe.QueueName));
            }
            else if (payload is NmqUnsubscribe unsubscribe)
            {
                _qeueManager.Use((o) => o.Unsubscribe(connectionId, unsubscribe.QueueName));
            }
            else if (payload is NmqEnqueueMessage enqueue)
            {
                _qeueManager.Use((o) => o.EqueueMessage(enqueue.QueueName, enqueue.PayloadJson, enqueue.PayloadType));
            }
            else if (payload is NmqEnqueueQuery enqueueQuery)
            {
                _qeueManager.Use((o) => o.EqueueQuery(connectionId, enqueueQuery.QueueName, enqueueQuery.QueryId, enqueueQuery.PayloadJson, enqueueQuery.PayloadType, enqueueQuery.ReplyType));
            }
            else if (payload is NmqEnqueueQueryReply enqueueQueryReply)
            {
                _qeueManager.Use((o) => o.EqueueQueryReply(connectionId, enqueueQueryReply.QueueName, enqueueQueryReply.QueryId, enqueueQueryReply.PayloadJson, enqueueQueryReply.PayloadType, enqueueQueryReply.ReplyType));
            }
            else
            {
                throw new Exception("The server bound notification type is not implemented.");
            }
        }
    }
}
