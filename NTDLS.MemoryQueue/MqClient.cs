﻿using Newtonsoft.Json;
using NTDLS.MemoryQueue.Client.QueryHandlers;
using NTDLS.MemoryQueue.Payloads.Queries.ClientToServer;
using NTDLS.ReliableMessaging;
using System.Net;

namespace NTDLS.MemoryQueue
{
    /// <summary>
    /// Connects to a MessageServer then sends/received and processes notifications/queries.
    /// </summary>
    public class MqClient
    {
        private readonly RmClient _rmClient;
        private bool _explicitDisconnect = false;
        private MqClientConfiguration _configuration;

        private string? _lastReconnectHost;
        private int _lastReconnectPort;
        private IPAddress? _lastReconnectIpAddress;

        /// <summary>
        /// Delegate used for server-to-client delivery notifications.
        /// </summary>
        /// <returns>Return true to mark the message as consumed by the client.</returns>
        public delegate bool OnReceivedEvent(MqClient client, IMqMessage message);

        /// <summary>
        /// Event used for server-to-client delivery notifications.
        /// </summary>
        /// <returns>Return true to mark the message as consumed by the client.</returns>
        public event OnReceivedEvent? OnReceived;

        /// <summary>
        /// Delegate used client connectivity notifications.
        /// </summary>
        public delegate void OnConnectedEvent(MqClient client);

        /// <summary>
        /// Event used client connectivity notifications.
        /// </summary>
        public event OnConnectedEvent? OnConnected;

        /// <summary>
        /// Event used client connectivity notifications.
        /// </summary>
        public event OnConnectedEvent? OnDisconnected;

        /// <summary>
        /// Creates a new instance of the queue client.
        /// </summary>
        public MqClient()
        {
            _configuration = new MqClientConfiguration();
            _rmClient = new RmClient();

            _rmClient.OnConnected += RmClient_OnConnected;
            _rmClient.OnDisconnected += RmClient_OnDisconnected;

            _rmClient.AddHandler(new InternalClientQueryHandlers(this));
        }

        /// <summary>
        /// Creates a new instance of the queue service.
        /// </summary>
        public MqClient(MqClientConfiguration configuration)
        {
            _configuration = configuration;

            var rmConfiguration = new RmConfiguration()
            {
                AsynchronousQueryWaiting = configuration.AsynchronousQueryWaiting,
                InitialReceiveBufferSize = configuration.InitialReceiveBufferSize,
                MaxReceiveBufferSize = configuration.MaxReceiveBufferSize,
                QueryTimeout = configuration.QueryTimeout,
                ReceiveBufferGrowthRate = configuration.ReceiveBufferGrowthRate
            };

            _rmClient = new RmClient(rmConfiguration);
            _rmClient.AddHandler(new InternalClientQueryHandlers(this));
        }

        private void RmClient_OnConnected(RmContext context)
        {
            OnConnected?.Invoke(this);
        }

        private void RmClient_OnDisconnected(RmContext context)
        {
            OnDisconnected?.Invoke(this);

            if (_explicitDisconnect == false && _configuration.AutoReconnect)
            {
                new Thread((o) =>
                {
                    while (!_explicitDisconnect && !_rmClient.IsConnected)
                    {
                        if (_lastReconnectHost != null)
                        {
                            _rmClient.Connect(_lastReconnectHost, _lastReconnectPort);
                        }
                        else if (_lastReconnectIpAddress != null)
                        {
                            _rmClient.Connect(_lastReconnectIpAddress, _lastReconnectPort);
                        }
                        else
                        {
                            break; //What else can we do.
                        }

                        Thread.Sleep(1000);
                    }
                }).Start();
            }
        }

        internal bool InvokeOnReceived(MqClient client, IMqMessage message)
            => (OnReceived?.Invoke(client, message) == true);

        /// <summary>
        /// Connects the client to a queue server.
        /// </summary>
        public void Connect(string hostName, int port)
        {
            _lastReconnectHost = hostName;
            _lastReconnectIpAddress = null;
            _lastReconnectPort = port;

            _explicitDisconnect = false;

            _rmClient.Connect(hostName, port);
        }

        /// <summary>
        /// Connects the client to a queue server.
        /// </summary>
        public void Connect(IPAddress ipAddress, int port)
        {
            _lastReconnectHost = null;
            _lastReconnectIpAddress = ipAddress;
            _lastReconnectPort = port;

            _explicitDisconnect = false;

            _rmClient.Connect(ipAddress, port);
        }

        /// <summary>
        /// Disconnects the client from the queue server.
        /// </summary>
        public void Disconnect(bool wait = false)
        {
            _explicitDisconnect = true;
            _rmClient.Disconnect(wait);
        }

        /// <summary>
        /// Instructs the server to create a queue with the given name.
        /// </summary>
        public void CreateQueue(string queueName)
        {
            var result = _rmClient.Query(new CreateQueueQuery(new MqQueueConfiguration(queueName))).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Instructs the server to create a queue with the given name.
        /// </summary>
        public void CreateQueue(MqQueueConfiguration queueConfiguration)
        {
            var result = _rmClient.Query(new CreateQueueQuery(queueConfiguration)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Instructs the server delete the queue with the given name.
        /// </summary>
        public void DeleteQueue(string queueName)
        {
            var result = _rmClient.Query(new DeleteQueueQuery(queueName)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Instructs the server to notify the client of messages sent to the given queue.
        /// </summary>
        public void Subscribe(string queueName)
        {
            var result = _rmClient.Query(new SubscribeToQueueQuery(queueName)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Instructs the server to stop notifying the client of messages sent to the given queue.
        /// </summary>
        public void Unsubscribe(string queueName)
        {
            var result = _rmClient.Query(new UnsubscribeFromQueueQuery(queueName)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        /// <summary>
        /// Dispatches a message to the queue server to be enqueued in the given queue.
        /// </summary>
        public void Enqueue<T>(string queueName, T message)
            where T : IMqMessage
        {
            var messageJson = JsonConvert.SerializeObject(message);

            var objectType = message.GetType()?.AssemblyQualifiedName ?? string.Empty;

            var result = _rmClient.Query(new EnqueueMessageToQueue(queueName, objectType, messageJson)).Result;
            if (result.IsSuccess == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }
    }
}
