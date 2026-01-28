using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;
using Logic;
using Logic.Database;
using Newtonsoft.Json;
using Utils;
using System;

namespace Net
{
    /// <summary>
    /// Network message types for inter-thread communication.
    /// </summary>
    public enum NetworkMessageType
    {
        NewConnection,      // New client socket accepted
        DataReceived,       // Data received from client
        ClientDisconnected, // Client disconnected
    }

    /// <summary>
    /// Network message for thread-safe communication between IO threads and main thread.
    /// </summary>
    public class NetworkMessage
    {
        public NetworkMessageType Type { get; set; }
        public Socket Socket { get; set; }
        public byte[] Data { get; set; }
        public int DataLength { get; set; }
    }

    public class Tcp : Basic.Manager
    {
        private static Tcp instance;
        public static Tcp Instance { get { if (instance == null) { instance = new Tcp(); } return instance; } }
        
        // Server socket
        private Socket _serverSocket;
        
        // Thread-safe collections for client management
        private readonly ConcurrentDictionary<Socket, Client> _socketToClient = new ConcurrentDictionary<Socket, Client>();
        private readonly List<Socket> _clientSockets = new List<Socket>();
        private readonly object _clientSocketsLock = new object();
        
        // Message queues for thread communication
        private readonly ConcurrentQueue<NetworkMessage> _incomingQueue = new ConcurrentQueue<NetworkMessage>();
        
        // Worker threads
        private Thread _acceptThread;
        private Thread _receiveThread;
        private volatile bool _running;
        
        // Statistics
        private int _tickCount = 0;
        private long _totalMessagesProcessed = 0;
        private long _lastStatTime = Environment.TickCount64;

        public override void Init(params object[] args)
        {
            Utils.Debug.Log.Info("NET", "[Tcp.Init] Starting network initialization with multi-threading...");
            
            // Create and bind server socket
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            var ip = Logic.Agent.Instance.InternalIp;
            IPEndPoint bindEndPoint = new IPEndPoint(ip, 19881);
            
            Utils.Debug.Log.Info("NET", $"[Tcp.Init] Binding to {bindEndPoint.Address}:{bindEndPoint.Port}...");
            
            try
            {
                _serverSocket.Bind(bindEndPoint);
                Utils.Debug.Log.Info("NET", "[Tcp.Init] Bind successful");
            }
            catch (SocketException ex)
            {
                Utils.Debug.Log.Error("NET", $"[Tcp.Init] BIND FAILED! Port {bindEndPoint.Port} may be in use. Error: {ex.SocketErrorCode} - {ex.Message}");
                throw;
            }
            
            _serverSocket.Listen(50);
            
            var localEndPoint = (IPEndPoint)_serverSocket.LocalEndPoint;
            Utils.Debug.Log.Info("NET", $"[TCP] Server listening on {localEndPoint.Address}:{localEndPoint.Port}");
            
            // Register client add/remove handlers
            Content.Add.Register(typeof(Client), OnClientAdded);
            Content.Remove.Register(typeof(Client), OnClientRemoved);
            
            // Start network threads
            _running = true;
            
            _acceptThread = new Thread(AcceptLoop)
            {
                Name = "Net.Accept",
                IsBackground = true
            };
            _acceptThread.Start();
            Utils.Debug.Log.Info("NET", "[Tcp.Init] Accept thread started");
            
            _receiveThread = new Thread(ReceiveLoop)
            {
                Name = "Net.Receive",
                IsBackground = true
            };
            _receiveThread.Start();
            Utils.Debug.Log.Info("NET", "[Tcp.Init] Receive thread started");
            
            Utils.Debug.Log.Info("NET", "[Tcp.Init] Network initialization complete - multi-threaded mode active");
        }

        /// <summary>
        /// Stop all network threads. Called during shutdown.
        /// </summary>
        public void Shutdown()
        {
            Utils.Debug.Log.Info("NET", "[Tcp.Shutdown] Stopping network threads...");
            _running = false;
            
            try
            {
                _serverSocket?.Close();
            }
            catch { }
            
            _acceptThread?.Join(3000);
            _receiveThread?.Join(3000);
            
            Utils.Debug.Log.Info("NET", "[Tcp.Shutdown] Network threads stopped");
        }

        #region Network Threads
        
        /// <summary>
        /// Accept thread - runs independently, accepts new connections.
        /// Never blocked by game logic.
        /// </summary>
        private void AcceptLoop()
        {
            Utils.Debug.Log.Info("NET", "[AcceptLoop] Thread started");
            
            while (_running)
            {
                try
                {
                    // Blocking accept - this thread is dedicated to accepting
                    Socket clientSocket = _serverSocket.Accept();
                    
                    if (clientSocket != null && clientSocket.Connected)
                    {
                        var remoteEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                        Utils.Debug.Log.Info("NET", $"[AcceptLoop] New connection from {remoteEndPoint.Address}:{remoteEndPoint.Port}");
                        
                        // Configure socket
                        clientSocket.NoDelay = true;
                        clientSocket.ReceiveTimeout = 0;
                        clientSocket.SendTimeout = 5000;
                        
                        // Queue for main thread processing
                        _incomingQueue.Enqueue(new NetworkMessage
                        {
                            Type = NetworkMessageType.NewConnection,
                            Socket = clientSocket
                        });
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    // Server socket closed, exit gracefully
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Server socket disposed, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        Utils.Debug.Log.Warning("NET", $"[AcceptLoop] Exception: {ex.Message}");
                    }
                }
            }
            
            Utils.Debug.Log.Info("NET", "[AcceptLoop] Thread exiting");
        }

        /// <summary>
        /// Receive thread - polls all client sockets for data.
        /// Never blocked by game logic.
        /// </summary>
        private void ReceiveLoop()
        {
            Utils.Debug.Log.Info("NET", "[ReceiveLoop] Thread started");
            
            byte[] buffer = new byte[8192];
            List<Socket> socketsToCheck = new List<Socket>();
            
            while (_running)
            {
                try
                {
                    // Get current list of sockets
                    socketsToCheck.Clear();
                    lock (_clientSocketsLock)
                    {
                        socketsToCheck.AddRange(_clientSockets);
                    }
                    
                    if (socketsToCheck.Count == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }
                    
                    foreach (var socket in socketsToCheck)
                    {
                        if (!_running) break;
                        
                        try
                        {
                            if (!socket.Connected)
                            {
                                EnqueueDisconnect(socket);
                                continue;
                            }
                            
                            // Non-blocking check for data
                            if (socket.Poll(1000, SelectMode.SelectRead))
                            {
                                int bytesRead = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                                
                                if (bytesRead > 0)
                                {
                                    // Copy data and queue for main thread
                                    byte[] data = new byte[bytesRead];
                                    Array.Copy(buffer, data, bytesRead);
                                    
                                    _incomingQueue.Enqueue(new NetworkMessage
                                    {
                                        Type = NetworkMessageType.DataReceived,
                                        Socket = socket,
                                        Data = data,
                                        DataLength = bytesRead
                                    });
                                }
                                else
                                {
                                    // Connection closed by remote
                                    EnqueueDisconnect(socket);
                                }
                            }
                        }
                        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset ||
                                                          ex.SocketErrorCode == SocketError.ConnectionAborted)
                        {
                            EnqueueDisconnect(socket);
                        }
                        catch (ObjectDisposedException)
                        {
                            EnqueueDisconnect(socket);
                        }
                        catch (Exception ex)
                        {
                            Utils.Debug.Log.Warning("NET", $"[ReceiveLoop] Socket error: {ex.Message}");
                            EnqueueDisconnect(socket);
                        }
                    }
                    
                    // Small sleep to prevent CPU spinning
                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        Utils.Debug.Log.Error("NET", $"[ReceiveLoop] Exception: {ex.Message}");
                        Thread.Sleep(100);
                    }
                }
            }
            
            Utils.Debug.Log.Info("NET", "[ReceiveLoop] Thread exiting");
        }

        private void EnqueueDisconnect(Socket socket)
        {
            _incomingQueue.Enqueue(new NetworkMessage
            {
                Type = NetworkMessageType.ClientDisconnected,
                Socket = socket
            });
        }
        
        #endregion

        #region Main Thread Processing
        
        /// <summary>
        /// Process all pending network messages. Called from main thread.
        /// This should be called every frame BEFORE game logic.
        /// </summary>
        public void ProcessNetwork()
        {
            int processed = 0;
            const int maxPerFrame = 200; // Limit to prevent starvation
            
            while (processed < maxPerFrame && _incomingQueue.TryDequeue(out NetworkMessage msg))
            {
                try
                {
                    switch (msg.Type)
                    {
                        case NetworkMessageType.NewConnection:
                            HandleNewConnection(msg.Socket);
                            break;
                            
                        case NetworkMessageType.DataReceived:
                            HandleDataReceived(msg.Socket, msg.Data, msg.DataLength);
                            break;
                            
                        case NetworkMessageType.ClientDisconnected:
                            HandleClientDisconnected(msg.Socket);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Error("NET", $"[ProcessNetwork] Error processing message: {ex.Message}");
                    
                    if (Logic.Agent.Instance.IsDevelopment)
                    {
                        throw;
                    }
                }
                
                processed++;
                _totalMessagesProcessed++;
            }
            
            // Log stats periodically
            _tickCount++;
            if (_tickCount % 100 == 0)
            {
                long now = Environment.TickCount64;
                long elapsed = now - _lastStatTime;
                if (elapsed > 10000) // Every 10 seconds
                {
                    int clientCount;
                    lock (_clientSocketsLock) { clientCount = _clientSockets.Count; }
                    int queueSize = _incomingQueue.Count;
                    // Utils.Debug.Log.Info("NET", $"[TCP Stats] Clients={clientCount}, QueueSize={queueSize}, TotalProcessed={_totalMessagesProcessed}");
                    _lastStatTime = now;
                }
            }
        }

        private void HandleNewConnection(Socket socket)
        {
            try
            {
                var remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
                Utils.Debug.Log.Info("NET", $"[HandleNewConnection] Creating client for {remoteEndPoint.Address}:{remoteEndPoint.Port}");
                
                var client = Create<Client>(socket);
                if (client != null)
                {
                    _socketToClient[socket] = client;
                    Utils.Debug.Log.Info("NET", $"[HandleNewConnection] Client created: {client.Name}");
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("NET", $"[HandleNewConnection] Failed: {ex.Message}");
                try { socket.Close(); } catch { }
            }
        }

        private void HandleDataReceived(Socket socket, byte[] data, int length)
        {
            if (!_socketToClient.TryGetValue(socket, out Client client))
            {
                return;
            }
            
            try
            {
                // Append to client buffer
                if (client.Buffer.Remain < length)
                {
                    client.Buffer.Compact();
                }
                
                if (client.Buffer.Remain < length)
                {
                    Utils.Debug.Log.Warning("NET", $"[HandleDataReceived] Buffer full for {client.Name}");
                    HandleClientDisconnected(socket);
                    return;
                }
                
                Array.Copy(data, 0, client.Buffer.Content, client.Buffer.Write, length);
                client.Buffer.Write += length;
                
                // Process complete packets
                ProcessClientBuffer(client);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("NET", $"[HandleDataReceived] Error for {client?.Name}: {ex.Message}");
                
                if (Logic.Agent.Instance.IsDevelopment)
                {
                    throw;
                }
            }
        }

        private void HandleClientDisconnected(Socket socket)
        {
            if (_socketToClient.TryRemove(socket, out Client client))
            {
                Utils.Debug.Log.Info("NET", $"[HandleClientDisconnected] {client.Name}");
                client.monitor.Fire(Client.Event.Disconnected, client);
                Remove(client);
            }
            
            try { socket.Close(); } catch { }
        }

        private void ProcessClientBuffer(Client client)
        {
            int length;
            while (IsByteArrayComplete(client, out length))
            {
                client.Buffer.Read += 4;
                int nameCount;
                string name = DecodeName(client.Buffer.Content, client.Buffer.Read, out nameCount);
                int bodyCount = length - nameCount;
                
                if (IsClientLegal(name, bodyCount))
                {
                    client.Buffer.Read += nameCount;
                    Protocol.Base protocol = Decode(name, client.Buffer.Content, client.Buffer.Read, bodyCount);
                    client.Buffer.Read += bodyCount;
                    client.Buffer.CheckAndMoveBytes();
                    
                    client.monitor.Fire(Net.Client.Event.Receive, protocol);
                }
                else
                {
                    Utils.Debug.Log.Warning("NET", $"[ProcessClientBuffer] Invalid protocol from {client.Name}");
                    HandleClientDisconnected(client.Socket);
                    return;
                }
            }
        }

        private void OnClientAdded(params object[] args)
        {
            Client client = (Client)args[1];
            lock (_clientSocketsLock)
            {
                _clientSockets.Add(client.Socket);
            }
        }

        private void OnClientRemoved(params object[] args)
        {
            Client client = (Client)args[1];
            lock (_clientSocketsLock)
            {
                _clientSockets.Remove(client.Socket);
            }
            _socketToClient.TryRemove(client.Socket, out _);
        }
        
        #endregion

        #region 辅助方法
        public void Broadcast(Character character, Logic.Channel channel, int id)
        {
            switch (channel)
            {
                case Logic.Channel.All:
                case Logic.Channel.Rumor:
                    Logic.Agent.Instance.Content.Gets<Logic.Player>().ForEach(player => Information(player, channel, id));
                    break;
                case Logic.Channel.Local:
                    if (character.Map != null) { character.Map.Content.Gets<Logic.Player>().ForEach(player => Information(player, channel, id)); }
                    break;
                case Logic.Channel.Private:
                    break;
                case Logic.Channel.System:
                case Logic.Channel.Automation:
                    if (character is Logic.Player player) { Information(player, channel, id); }
                    break;
            }
        }

        public void Information(Logic.Player player, Logic.Channel channel, int id)
        {
            Client client = Content.Get<Client>(c => c.Player == player);
            if (Logic.Text.Instance.Multilingual.TryGetValue(id, out var map) && map.TryGetValue(player.Language, out var text))
                Send(client, new Net.Protocol.Information(channel, text));
        }
        #endregion

        #region 数据验证
        private bool IsByteArrayComplete(Client client, out int length)
        {
            length = 0;

            if (client.Buffer.ReadableCount <= 4)
            {
                return false;
            }
            else
            {
                int read = client.Buffer.Read;
                byte[] bytes = client.Buffer.Content;
                length = BitConverter.ToInt32(bytes, read);
                return client.Buffer.ReadableCount >= length + 4;
            }
        }
        private bool IsClientLegal(string protoName, int bodyCount)
        {
            return !string.IsNullOrEmpty(protoName) && bodyCount > 0;
        }
        #endregion

        #region 协议处理
        private string DecodeName(byte[] bytes, int offset, out int count)
        {
            count = 0;
            if (offset + 2 > bytes.Length) { return ""; }
            Int16 len = (Int16)((bytes[offset + 1] << 8) | bytes[offset]);
            if (len <= 0) { return ""; }
            if (offset + 2 + len > bytes.Length) { return ""; }
            count = 2 + len;
            string name = System.Text.Encoding.UTF8.GetString(bytes, offset + 2, len);
            return name;
        }

        private Protocol.Base Decode(string name, byte[] bytes, int offset, int count)
        {
            try
            {
                var typeName = $"Net.Protocol.{name}";
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    Utils.Debug.Log.Error("NET", $"[Decode] Type.GetType returned null for: {typeName}");
                    return new Protocol.Base();
                }
                
                var json = System.Text.Encoding.UTF8.GetString(bytes, offset, count);
                // Utils.Debug.Log.Info("NET", $"[Decode] Deserializing {name}, JSON: {json}");
                
                var result = (Protocol.Base)JsonConvert.DeserializeObject(json, type);
                if (result == null)
                {
                    Utils.Debug.Log.Error("NET", $"[Decode] Deserialization returned null for: {name}");
                    return new Protocol.Base();
                }
                
                // Utils.Debug.Log.Info("NET", $"[Decode] Success: {name} -> {result.GetType().Name}");
                return result;
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("NET", $"[Decode] Exception for {name}: {ex.Message}", new { StackTrace = ex.StackTrace });
                return new Protocol.Base();
            }
        }
        #endregion

        #region Send Operations
        
        public void Send(Client client, Protocol.Base protocol)
        {
            if (client == null)
            {
                Utils.Debug.Log.Warning("NET", $"[SEND] Client is null, cannot send {protocol.GetType().Name}");
                return;
            }
            if (!client.Socket.Connected)
            {
                Utils.Debug.Log.Warning("NET", $"[SEND] Socket not connected, cannot send {protocol.GetType().Name} to {client.Name}");
                return;
            }
            
            byte[] nameBytes = protocol.EncodeName();
            byte[] bodyBytes = protocol.Encode();
            int len = nameBytes.Length + bodyBytes.Length;
            byte[] sendBytes = new byte[4 + len];
            BitConverter.GetBytes(len).CopyTo(sendBytes, 0);
            Array.Copy(nameBytes, 0, sendBytes, 4, nameBytes.Length);
            Array.Copy(bodyBytes, 0, sendBytes, 4 + nameBytes.Length, bodyBytes.Length);

            // Log the JSON being sent (disabled for cleaner logs)
            // string jsonContent = System.Text.Encoding.UTF8.GetString(bodyBytes);
            // Utils.Debug.Log.Info("NET", $"[SEND] Protocol={protocol.GetType().Name} to {client.Name}, JSON={jsonContent}");

            try
            {
                client.monitor.Fire(Net.Client.Event.Send, sendBytes);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("NET", $"[SEND] Failed to send {protocol.GetType().Name} to {client.Name}: {ex.Message}");
                client.monitor.Fire(Client.Event.Disconnected, client);
                Remove(client);
            }
        }

        public void Send(Logic.Player player, Protocol.Base protocol)
        {
            Send(Content.Get<Client>(c => c.Player == player), protocol);
        }
        #endregion


    }
}

