using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GNetwork.Network
{
    class GNetClient
    {
        // Receive Buffer Size
        private int RECEIVE_BUFFER_SIZE = 1024;

        // for connection
        private IPAddress ip;
        private int port;

        private BufferManager bufferManager;

        private SocketAsyncEventArgs clientSocket = null;

        private ManualResetEvent connectFlag;

        private ManualResetEvent disconnectFlag;

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

        private void Init()
        {
            clientSocket = new SocketAsyncEventArgs();
            disconnectFlag = new ManualResetEvent(false);
            connectFlag = new ManualResetEvent(false);
            bufferManager = new BufferManager(1 * 1024 * 1024, RECEIVE_BUFFER_SIZE);
            bufferManager.InitBuffer();
        }

        public bool Connect()
        {
            Init();

            // Create a socket and connect to the server
            Socket sock = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Completed += new EventHandler<SocketAsyncEventArgs>(SocketEventArg_Completed);
            clientSocket.RemoteEndPoint = new IPEndPoint(ip, port);
            clientSocket.UserToken = sock;
            clientSocket.DisconnectReuseSocket = true;

            return sock.ConnectAsync(clientSocket);
        }

        public bool Reconnect()
        {
            Socket sock = clientSocket.UserToken as Socket;

            if (sock.Connected)
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
            Socket sock = clientSocket.UserToken as Socket;

            if (sock.Connected && connectFlag.WaitOne())
            {
                bool willRaiseEvent = sock.SendAsync(clientSocket);
                if (!willRaiseEvent)
                {
                    ProcessSend(clientSocket);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Send(string data)
        {
            Socket sock = clientSocket.UserToken as Socket;

            if (sock.Connected && connectFlag.WaitOne())
            {
                byte[] encoded = Encoding.UTF8.GetBytes(data);
                bool willRaiseEvent = sock.SendAsync(clientSocket);
                if (!willRaiseEvent)
                {
                    ProcessSend(clientSocket);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Disconnect()
        {
            // Disconnect from Server
            // It's different between Disconnect() and Dispose()
            // Disconnect() can reconnect again, Dispose() is totally remove all objects.
            Socket client = clientSocket.UserToken as Socket;
            
            if (client.Connected)
            {
                try
                {
                    // Free Buffer
                    bufferManager.FreeBuffer(clientSocket);
                    client.DisconnectAsync(clientSocket);
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
        private void SocketEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    Disconnected(e);
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
                var result = bufferManager.SetBuffer(e);
                connectFlag.Set();

                // Connect Done
                connectResult.endpoint = clientSocket.RemoteEndPoint;
                connectResult.addressFamily = (clientSocket.UserToken as Socket).AddressFamily;
                connectResult.isSuccess = true;

                if(OnConnect != null)
                    OnConnect(connectResult);
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
                Socket sock = e.UserToken as Socket;
                
                bool willRaiseEvent = sock.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
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
                Socket sock = e.UserToken as Socket;
                GPacket packet = new GPacket();
                packet.data = e.Buffer;
                packet.size = e.BytesTransferred;
                if (OnReceive != null)
                    OnReceive(packet);
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
