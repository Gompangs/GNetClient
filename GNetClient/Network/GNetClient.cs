using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GNetwork.Network
{
    public class GNetClient
    {
        // Receive Buffer Size. default : 8K
        private int RECEIVE_BUFFER_SIZE = 8192;

        // for connection
        private IPAddress ip;
        private int port;
        private Socket socket;

        private BufferManager bufferManager;

        private SocketAsyncEventArgs socketArgs = null;

        private ManualResetEvent connectFlag;

        private ManualResetEvent disconnectFlag;

        private AutoResetEvent clientDataSentFlag = new AutoResetEvent(true);

        // Called when Connection Established
        public delegate void ConnectDelegate(ConnectResult connectResult);
        public ConnectDelegate OnConnect;

        // Called when Data Received
        public delegate void ReceiveDelegate(GPacket data);
        public ReceiveDelegate OnReceive;

        // Called when Connection Disconnected
        public delegate void DisconnectDelegate();
        public DisconnectDelegate OnDisconnect;

        private static GNetClient networkManager;
        private static object lockObj = new object();
        
        private SendWorker workerObject;
        
        #region for abstract layer to Unity
        // Applying Singleton Pattern
        public static GNetClient GetInstance(string ip, int port)
        {
            if (networkManager == null)
            {
                lock (lockObj)
                {
                    if (networkManager == null)
                    {
                        networkManager = new GNetClient(ip, port);
                        Console.WriteLine("New GNetClient Created");
                    }

                }
            }
            return networkManager;
        }

        public GNetClient(string ip, int port)
        {
            this.ip = IPAddress.Parse(ip);
            this.port = port;
        }

        public void SetRecvBufferSize(int size)
        {
            this.RECEIVE_BUFFER_SIZE = size;
        }

        private void Init()
        {
            socketArgs = new SocketAsyncEventArgs();
            
            disconnectFlag = new ManualResetEvent(false);
            connectFlag = new ManualResetEvent(false);
            bufferManager = new BufferManager(10 * RECEIVE_BUFFER_SIZE * RECEIVE_BUFFER_SIZE, RECEIVE_BUFFER_SIZE); // default : 8MB
            bufferManager.InitBuffer();

            workerObject = new SendWorker();
            workerObject.Init(this);
            Thread workerThread = new Thread(workerObject.Work);
            // Start the worker thread.
            workerThread.Start();

        }

        public bool Connect()
        {
            // TODO : IF delegates are null -> throw error
            CheckDelegates();

            Init();

            // Create a socket and connect to the server
            socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socketArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            socketArgs.RemoteEndPoint = new IPEndPoint(ip, port);
            socketArgs.UserToken = socket;
            socketArgs.DisconnectReuseSocket = true;
            
            return socket.ConnectAsync(socketArgs);
        }

        private void CheckDelegates()
        {
            if (OnConnect == null)
            {
                throw new InvalidOperationException("GNetClient Delegate cannot be null. please set \"OnConnect\" delegate on your Class");
            }

            if (OnDisconnect == null)
            {
                throw new InvalidOperationException("GNetClient Delegate cannot be null. please set \"OnDisconnect\" delegate on your Class");
            } 

            if (OnReceive == null)
            {
                throw new InvalidOperationException("GNetClient Delegate cannot be null. please set \"OnReceive\" delegate on your Class");
            }
        }

        public bool Reconnect()
        {
            if (socket.Connected)
            {
                if (Disconnect())
                {
                    if (disconnectFlag.WaitOne())
                    {
                        if (Connect())
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool Send(byte[] data)
        {
            if (socket.Connected && connectFlag.WaitOne())
            {
                GPacket packet = new GPacket();
                packet.data = data;
                packet.size = data.Length;

                // Put Packet to Queue
                workerObject.putPacket(packet);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Send(string data)
        {
            if (socket.Connected && connectFlag.WaitOne())
            {
                GPacket packet = new GPacket();
                packet.data = Encoding.UTF8.GetBytes(data);
                packet.size = data.Length;

                // Put Packet to Queue
                workerObject.putPacket(packet);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool SendPacket(GPacket packet)
        {
            // Send Packet Directly which is pop from queue
            // Data Setting for Socket
            socketArgs.SetBuffer(packet.data, 0, packet.size);

            GStatistics.incSent(packet.size);

            bool willRaiseEvent = socket.SendAsync(socketArgs);
            if (!willRaiseEvent)
            {
                ProcessSend(socketArgs);
            }

            // wait for send complete
            clientDataSentFlag.WaitOne();

            return true;
        }

        public bool Disconnect()
        {
            // Disconnect from Server
            // It's different between Disconnect() and Dispose()
            // Disconnect() can reconnect again, Dispose() is totally remove all objects.
            if (socket.Connected)
            {
                try
                {
                    // Free Buffer
                    bufferManager.FreeBuffer(socketArgs);
                    workerObject.StopWork();
                    socket.DisconnectAsync(socketArgs);
                }
                catch (Exception e)
                {
                    // Ignore the exception. The client probably already disconnected.
                    Console.WriteLine(e.ToString());
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region private Region for internal behavior
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // called when operation is completed
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    Disconnected(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new Exception("Invalid operation completed");
            }
        }
        
        private void Disconnected(SocketAsyncEventArgs e)
        {
            // called after disconnected from server
            disconnectFlag.Set();

            if (OnDisconnect != null)
                OnDisconnect();
        }
        
        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            ConnectResult connectResult = new ConnectResult();
            if (e.SocketError == SocketError.Success)
            {
                connectFlag.Set();

                // Connect Done
                connectResult.endpoint = socketArgs.RemoteEndPoint;
                connectResult.addressFamily = socket.AddressFamily;
                connectResult.isSuccess = true;

                if(OnConnect != null)
                    OnConnect(connectResult);

                Console.WriteLine(e != null);
                Console.WriteLine(e.UserToken != null);

                // Start Receive
                if (connectFlag.WaitOne())
                {
                    if (bufferManager.SetBuffer(e))
                    {
                        bool willRaiseEvent = socket.ReceiveAsync(e);
                        if (!willRaiseEvent)
                        {
                            ProcessReceive(e);
                        }
                    }
                    else
                    {
                        // buffer is exceeded
                    }
                }
            }
            else
            {
                connectResult.isSuccess = false;

                if (OnConnect != null)
                    OnConnect(connectResult);

                Console.WriteLine("Connect Exception");
                throw new SocketException((int)e.SocketError);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                //Read data sent from the server
                if (bufferManager.SetBuffer(e))
                {
                    bool willRaiseEvent = socket.ReceiveAsync(e);
                    if (!willRaiseEvent)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    // buffer is exceeded
                }
                
                clientDataSentFlag.Set();
            }
            else
            {
                Console.WriteLine("Send Exception");
                throw new SocketException((int)e.SocketError);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                GPacket packet = new GPacket();
                packet.data = e.Buffer;
                packet.size = e.BytesTransferred;

                GStatistics.incRecv(packet.size);

                if (OnReceive != null)
                    OnReceive(packet);

                // when data is truncated -> more receive data
                // TODO : collecting data
                if(e.BytesTransferred == RECEIVE_BUFFER_SIZE)
                {
                    if (bufferManager.SetBuffer(e))
                    {
                        bool willRaiseEvent = socket.ReceiveAsync(e);
                        if (!willRaiseEvent)
                        {
                            ProcessReceive(e);
                        }
                    }
                    else
                    {
                        // buffer is exceeded
                    }
                }
                else
                {
                    // All data received
                    Console.WriteLine("All data Received {0}", e.BytesTransferred);

                    // Try receive again ...
                    socket.ReceiveAsync(e);
                }
            }
            else
            {
                Console.WriteLine("Receive Exception");
                throw new SocketException((int)e.SocketError);
            }
        }
    }
    #endregion
}
