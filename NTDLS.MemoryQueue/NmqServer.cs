using NTDLS.MemoryQueue.Engine;
using NTDLS.MemoryQueue.Payloads.Public;
using NTDLS.Semaphore;
using NTDLS.StreamFraming.Payloads;
using System.Net;
using System.Net.Sockets;

namespace NTDLS.ReliableMessaging
{
    /// <summary>
    /// Listens for connections from MessageClients and processes the incoming notifications/queries.
    /// </summary>
    public class NmqServer : IMessageHub
    {
        private TcpListener? _listener;
        private readonly CriticalResource<List<PeerConnection>> _activeConnections = new();
        private Thread? _listenerThreadProc;
        private bool _keepRunning;

        private readonly CriticalResource<NmqQueues> _queues = new();

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

        /// <summary>
        /// Event fired when a notification is received from a client.
        /// </summary>
        public event NotificationReceivedEvent? OnNotificationReceived;
        /// <summary>
        /// Event fired when a notification is received from a client.
        /// </summary>
        /// <param name="server">The instance of the server that is calling the event.</param>
        /// <param name="connectionId">The id of the client which send the notification.</param>
        /// <param name="payload"></param>
        public delegate void NotificationReceivedEvent(NmqServer server, Guid connectionId, IFrameNotification payload);

        /// <summary>
        /// Event fired when a query is received from a client.
        /// </summary>
        public event QueryReceivedEvent? OnQueryReceived;
        /// <summary>
        /// Event fired when a query is received from a client.
        /// </summary>
        /// <param name="server">The instance of the server that is calling the event.</param>
        /// <param name="connectionId">The id of the client which send the query.</param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public delegate IFrameQueryReply QueryReceivedEvent(NmqServer server, Guid connectionId, IFrameQuery payload);

        #endregion

        public void CreateQueue(NmqConfiguration config) => _queues.Use((o) => o.Add(config));
        public void Subscribe(Guid connectionId, NmqSubscribe subscribe) => _queues.Use((o) => o.Subscribe(connectionId, subscribe));
        public void Unsubscribe(Guid connectionId, NmqUnsubscribe unsubscribe) => _queues.Use((o) => o.Unsubscribe(connectionId, unsubscribe));
        public void Equeue(Guid connectionId, NmqEnqueue enqueue) => _queues.Use((o) => o.Equeue(connectionId, enqueue));

        /// <summary>
        /// Starts the message server.
        /// </summary>
        public void Start(int listenPort)
        {
            if (_keepRunning)
            {
                return;
            }
            _queues.Use((o) => o.SetServer(this));

            _listener = new TcpListener(IPAddress.Any, listenPort);
            _keepRunning = true;
            _listenerThreadProc = new Thread(ListenerThreadProc);
            _listenerThreadProc.Start();
        }

        /// <summary>
        /// Stops the message server.
        /// </summary>
        public void Stop()
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
        }

        void ListenerThreadProc()
        {
            try
            {
                Utility.EnsureNotNull(_listener);

                Thread.CurrentThread.Name = $"ListenerThreadProc:{Thread.CurrentThread.ManagedThreadId}";

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
                throw new Exception($"The connection with id {connectionId} was not found.");
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
                throw new Exception($"The connection with id {connectionId} was not found.");
            }

            return await connection.SendQuery<T>(query);
        }

        void IMessageHub.InvokeOnConnected(Guid connectionId)
        {
            OnConnected?.Invoke(this, connectionId);
        }

        void IMessageHub.InvokeOnDisconnected(Guid connectionId)
        {
            if (_keepRunning) //Avoid a race condition with the client thread waiting on a lock on _activeConnections that is held by Server.Stop().
            {
                _activeConnections.Use((o) => o.RemoveAll(o => o.Id == connectionId));
            }
            OnDisconnected?.Invoke(this, connectionId);
        }

        void IMessageHub.InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload)
        {
            //Intercept notifications to see if they are Client->Server commands.
            if (payload is NmqConfiguration config)
            {
                CreateQueue(config);
            }
            else if (payload is NmqSubscribe subscribe)
            {
                Subscribe(connectionId, subscribe);
            }
            else if (payload is NmqUnsubscribe unsubscribe)
            {
                Unsubscribe(connectionId, unsubscribe);
            }
            else if (payload is NmqEnqueue enqueue)
            {
                Equeue(connectionId, enqueue);
            }
            else
            {
                if (OnNotificationReceived == null)
                {
                    throw new Exception("The notification hander event was not handled.");
                }
                OnNotificationReceived.Invoke(this, connectionId, payload);
            }
        }

        IFrameQueryReply IMessageHub.InvokeOnQueryReceived(Guid connectionId, IFrameQuery payload)
        {
            if (OnQueryReceived == null)
            {
                throw new Exception("The query hander event was not handled.");
            }
            return OnQueryReceived.Invoke(this, connectionId, payload);
        }
    }
}
