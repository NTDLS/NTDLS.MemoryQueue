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

        /// <summary>
        /// Event fired a log entry needs to be recorded.
        /// </summary>
        public event LogEvent? OnLog;
        /// <summary>
        /// Event fired a log entry needs to be recorded.
        /// </summary>
        /// <param name="sender">The instance of the client that is calling the event.</param>
        /// <param name="entry">The log entry that is being reported.</param>
        public delegate void LogEvent(IMqMemoryQueue sender, MqLogEntry entry);

        #endregion

        /// <summary>
        /// Writes an event to the log.
        /// </summary>
        /// <param name="sender">The memory queue (client or server) that is writing the event.</param>
        /// <param name="entry">The event which is to be recorded.</param>
        private void WriteLog(IMqMemoryQueue sender, MqLogEntry entry) => OnLog?.Invoke(sender, entry);

        /// <summary>
        /// Creates a new queue with the given configuration.
        /// </summary>
        /// <param name="configuration">The configuration for the new queue.</param>
        public void CreateQueue(MqQueueConfiguration configuration)
        {
            try
            {
                _qeueManager.Use((o) => o.Create(configuration));
            }
            catch (Exception ex)
            {
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
        }

        /// <summary>
        /// Shuts down and deletes an existing queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to delete.</param>
        public void DeleteQueue(string queueName)
        {
            try
            {
                _qeueManager.Use((o) => o.Delete(queueName));
            }
            catch (Exception ex)
            {
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
        }

        /// <summary>
        /// Starts the message server.
        /// </summary>
        public void Start(int listenPort)
        {
            try
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
            catch (Exception ex)
            {
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
        }

        /// <summary>
        /// Stops the message server.
        /// </summary>
        public void Shutdown()
        {
            try
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
            catch (Exception ex)
            {
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
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
                    WriteLog(this, new MqLogEntry(ex));
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
            try
            {
                var connection = _activeConnections.Use((o) => o.Where(c => c.Id == connectionId).FirstOrDefault());
                if (connection == null)
                {
                    throw new Exception($"The connection with id '{connectionId}' was not found.");
                }

                connection.SendNotification(notification);
            }
            catch (Exception ex)
            {
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
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
            try
            {
                var connection = _activeConnections.Use((o) => o.Where(c => c.Id == connectionId).FirstOrDefault());
                if (connection == null)
                {
                    throw new Exception($"The connection with id '{connectionId}' was not found.");
                }

                return await connection.SendQuery<T>(query);
            }
            catch (Exception ex)
            {
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
        }

        void IMqMemoryQueue.InvokeOnConnected(Guid connectionId)
        {
            try
            {
                OnConnected?.Invoke(this, connectionId);
            }
            catch (Exception ex)
            {
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
        }

        void IMqMemoryQueue.InvokeOnDisconnected(Guid connectionId)
        {
            try
            {
                if (_keepRunning) //Avoid a race condition with the client thread waiting on a lock on _activeConnections that is held by Server.Stop().
                {
                    _activeConnections.Use((o) => o.RemoveAll(o => o.Id == connectionId));
                }
                OnDisconnected?.Invoke(this, connectionId);
            }
            catch (Exception ex)
            {
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
        }

        void IMqMemoryQueue.InvokeOnNotificationReceived(Guid connectionId, IFrameNotification payload)
        {
            try
            {
                throw new Exception("The server bound notification type is not implemented.");
            }
            catch (Exception ex)
            {
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
        }

        IFrameQueryReply IMqMemoryQueue.InvokeOnQueryReceived(Guid connectionId, IFrameQuery payload)
        {
            try
            {
                //Intercept queries to see if they are Client->Server commands.

                if (payload is MqSubscribe subscribe)
                {
                    return MqInternalQueryReplyBoolean.CollapseExceptionToResult(()
                        => _qeueManager.Use((o) => o.Subscribe(connectionId, subscribe.QueueName)));
                }
                else if (payload is MqCreateQueue createQueue)
                {
                    return MqInternalQueryReplyBoolean.CollapseExceptionToResult(()
                        => CreateQueue(createQueue.Configuration));
                }
                else if (payload is MqDeleteQueue deleteQueue)
                {
                    return MqInternalQueryReplyBoolean.CollapseExceptionToResult(()
                        => DeleteQueue(deleteQueue.QueueName));
                }
                else if (payload is MqUnsubscribe unsubscribe)
                {
                    return MqInternalQueryReplyBoolean.CollapseExceptionToResult(()
                        => _qeueManager.Use((o) => o.Unsubscribe(connectionId, unsubscribe.QueueName)));
                }
                else if (payload is MqEnqueueMessage enqueue)
                {
                    return MqInternalQueryReplyBoolean.CollapseExceptionToResult(()
                        => _qeueManager.Use((o) => o.EqueueMessage(enqueue.QueueName, enqueue.PayloadJson, enqueue.PayloadType)));
                }
                else if (payload is MqEnqueueQuery enqueueQuery)
                {
                    return MqInternalQueryReplyBoolean.CollapseExceptionToResult(()
                        => _qeueManager.Use((o) => o.EqueueQuery(connectionId, enqueueQuery.QueueName, enqueueQuery.QueryId,
                                                                    enqueueQuery.PayloadJson, enqueueQuery.PayloadType, enqueueQuery.ReplyType)));
                }
                else if (payload is MqEnqueueQueryReply enqueueQueryReply)
                {
                    return MqInternalQueryReplyBoolean.CollapseExceptionToResult(()
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
                WriteLog(this, new MqLogEntry(ex));
                throw;
            }
        }

        /// <summary>
        /// Writes an event to the log.
        /// </summary>
        /// <param name="sender">The memory queue (client or server) that is writing the event.</param>
        /// <param name="entry">The event which is to be recorded.</param>
        void IMqMemoryQueue.WriteLog(IMqMemoryQueue sender, MqLogEntry entry)
        {
            OnLog?.Invoke(sender, entry);
        }
    }
}
