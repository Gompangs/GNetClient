using ServerToolkit.BufferManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace GNetwork.Network
{
    public class GNetClient : IDisposable
    {
        private Socket client;
        private IPEndPoint remoteEP;
        
        // Called when Connection Established
        public delegate void ConnectDelegate(ConnectResult connectResult);
        public ConnectDelegate OnConnect;

        // Called when Data Received
        public delegate void ReceiveDelegate(byte[] data);
        public ReceiveDelegate OnReceive;

        // Called when Connection Disconnected
        public delegate void DisconnectDelegate(Exception e);
        public DisconnectDelegate OnDisconnect;

        // 1MB Buffer Pool
        BufferPool pool = new BufferPool(1 * 1024 * 1024, 1, 1);

        // Send, Receive Buffer
        IBuffer recvBuffer;
        IBuffer sendBuffer;

        Queue<GPacket> sendQueue;

        // Receive Buffer Size
        private int RECEIVE_BUFFER_SIZE = 256;

        private static GNetClient networkManager;
        private static object lockObj = new object();

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
            // Connect to a remote device.
            try
            {
                // Ready for Connect

                IPAddress ipAddress = IPAddress.Parse(ip);
                remoteEP = new IPEndPoint(ipAddress, port);

                // Create Send Queue
                sendQueue = new Queue<GPacket>();
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        
        /// <summary>
        /// Start Connect to Server
        /// </summary>
        public void Connect()
        {
            // Create a TCP/IP socket.
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.
            client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
        }

        public void ReConnect()
        {
            if (client.Connected)
            {
                try
                {
                    client.Shutdown(SocketShutdown.Both);
                    client.Disconnect(true); // Reuse flag true
                    this.Connect();
                }
                catch (Exception e)
                {
                    // Ignore the exception. The client probably already disconnected.
                    Console.WriteLine(e.ToString());
                }
            }
        }

        /// <summary>
        /// Set Option to Socket This method must called before Connect();
        /// </summary>
        /// <param name="socketOptionLevel">SocketOptionLevel for Socket</param>
        /// <param name="socketOptionName">SocketOptionName for Socket</param>
        /// <param name="value">true if you want to apply, vise-versa</param>
        public void SetSocketOption(SocketOptionLevel socketOptionLevel, SocketOptionName socketOptionName, object value)
        {
            client.SetSocketOption(socketOptionLevel, socketOptionName, value);
        }

        /// <summary>
        /// Getting Socket Option on Opening Socket
        /// </summary>
        /// <param name="socketOptionLevel">SocketOptionLevel for Socket</param>
        /// <param name="socketOptionName">SocketOptionName for Socket</param>
        /// <returns></returns>
        public object GetSocketOption(SocketOptionLevel socketOptionLevel, SocketOptionName socketOptionName)
        {
            return client.GetSocketOption(socketOptionLevel, socketOptionName);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            ConnectResult connectResult = new ConnectResult();
            try
            {
                // Retrieve the socket from the state object.
                client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                connectResult.endpoint = client.RemoteEndPoint;
                connectResult.addressFamily = client.AddressFamily;
                connectResult.isSuccess = true;

                // Receive Start
                Receive(client);
            }
            catch (Exception e)
            {
                connectResult.isSuccess = false;
                connectResult.exception = e;
            }

            if(connectResult != null)
                OnConnect(connectResult);
            else
                OnConnect(null);
        }

        public void Disconnect()
        {
            // Disconnect from Server
            // It's different between Disconnect() and Dispose()
            // Disconnect() can reconnect again, Dispose() is totally remove all objects.
            if (client.Connected)
            {
                try
                {
                    client.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    // Ignore the exception. The client probably already disconnected.
                    Console.WriteLine(e.ToString());
                }
                client.Disconnect(true); // Reuse flag true

                Console.WriteLine(client.Connected);
            }
        }

        private void Receive(Socket client)
        {
            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = client;
            state.isFirstRead = true;
            state.dataBuffer = new List<byte[]>();

            if (recvBuffer == null || recvBuffer.IsDisposed)
            {
                //Get new receive buffer if it is not available.
                recvBuffer = pool.GetBuffer(RECEIVE_BUFFER_SIZE);
            }
            else if (recvBuffer.Size < RECEIVE_BUFFER_SIZE)
            {
                //If the receive buffer size is smaller than desired buffer size,
                //dispose receive buffer and acquire a new one that is long enough.
                recvBuffer.Dispose();
                recvBuffer = pool.GetBuffer(RECEIVE_BUFFER_SIZE);
            }

            client.BeginReceive(recvBuffer.GetSegments(), SocketFlags.None, ReceiveCallback, state);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            client = state.workSocket;

            try
            {
                int bytesRead = client.EndReceive(ar);

                byte[] data = new byte[bytesRead > 0 ? bytesRead : 0];

                if (recvBuffer != null && !recvBuffer.IsDisposed)
                {
                    if (bytesRead > 0)
                    {
                        recvBuffer.CopyTo(data, 0, bytesRead);

                        if (state.isFirstRead)
                        {
                            // 처음 읽기라면 Header에서 패킷 사이즈를 가져온다
                            byte[] packetSize = new byte[4];
                            recvBuffer.CopyTo(packetSize, 0, 4);
                            if (BitConverter.IsLittleEndian)
                            {
                                // Little Endian일경우 Array 뒤집어준다.
                                Array.Reverse(packetSize);
                            }

                            Console.WriteLine("Packet Body Size : " + BitConverter.ToInt32(packetSize, 0));

                            // 패킷 크기 읽음
                            state.packetSize = BitConverter.ToInt32(packetSize, 0);
                            state.isFirstRead = false;
                        }

                        // increment total bytes read
                        state.totalReadBytesSize += bytesRead;
                        state.dataBuffer.Add(data);

                        if (state.totalReadBytesSize == state.packetSize + 4)
                        {
                            // when all data received call delegate
                            // Removing Linq
                            byte[] resultArr = GetRecvArray(state.dataBuffer);
                            OnReceive(resultArr);

                            recvBuffer.Dispose();
                            recvBuffer = pool.GetBuffer(RECEIVE_BUFFER_SIZE);

                            // New Receive Buffer Allocate
                            Receive(client);
                        }
                        else
                        {
                            // if data need to read more
                            client.BeginReceive(recvBuffer.GetSegments(), SocketFlags.None, ReceiveCallback, state);
                        }   
                    }
                }

                if (bytesRead <= 0) return;

                //Read/Expect more data

                // TODO : Continuous Data Handling needed

                if (recvBuffer == null || recvBuffer.IsDisposed)
                {
                    //Get new receive buffer if it is not available.
                    recvBuffer = pool.GetBuffer(RECEIVE_BUFFER_SIZE);
                }
                else if (recvBuffer.Size < RECEIVE_BUFFER_SIZE)
                {
                    //If the receive buffer size is smaller than desired buffer size,
                    //dispose receive buffer and acquire a new one that is long enough.
                    recvBuffer.Dispose();
                    recvBuffer = pool.GetBuffer(RECEIVE_BUFFER_SIZE);
                }
            }
            catch (SocketException e)
            {
                // When socket exception occur -> disconnected
                OnDisconnect(e);
            }
            catch (Exception e)
            {
                // When Exception Occur -> maybe is not disconnected
                Console.WriteLine(e.ToString());
            }
        }

        public bool Send(byte[] data)
        {
            if (client.Connected)
            {
                // TODO : direct send -> using queue and send process
                sendBuffer = pool.GetBuffer(data.Length);
                sendBuffer.FillWith(data);
                client.BeginSend(sendBuffer.GetSegments(), SocketFlags.None, SendCallback, sendBuffer);
            }
            else
            {
                // TODO : send buffered data after socket ready
                Console.WriteLine("Socket Not Yet Ready");
            }
            return true;
        }

        public bool Send(string data)
        {
            if (client.Connected)
            {
                // TODO : direct send -> using queue and send process
                sendBuffer = pool.GetBuffer(data.Length);
                sendBuffer.FillWith(Encoding.UTF8.GetBytes(data));
                client.BeginSend(sendBuffer.GetSegments(), SocketFlags.None, SendCallback, sendBuffer);
            }
            return true;
        }

        public bool Send(object data)
        {
            if (client.Connected)
            {
                // TODO : direct send -> using queue and send process
                if (data == null)
                    return false;

                BinaryFormatter bf = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream())
                {
                    bf.Serialize(ms, data);
                    sendBuffer = pool.GetBuffer(ms.ToArray().Length);
                    sendBuffer.FillWith(ms.ToArray());
                    client.BeginSend(sendBuffer.GetSegments(), SocketFlags.None, SendCallback, sendBuffer);
                }
            }
            return true;
        }

        private void SendCallback(IAsyncResult ar)
        {
            var sendBuffer = (IBuffer)ar.AsyncState;
            try
            {
                int sentBytes = client.EndSend(ar);
                Console.WriteLine("{0} bytes sent", sentBytes);
            }
            catch (Exception ex)
            {
                // TODO : Packet Recovery -> doesn't sent to server
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (sendBuffer != null)
                {
                    sendBuffer.Dispose();
                }
            }
        }

        public byte[] GetRecvArray(List<byte[]> dest)
        {
            // get total size
            int totalSize = 0;
            foreach (byte[] arr in dest)
            {
                totalSize += arr.Length;
            }

            byte[] ret = new byte[totalSize];
            int offset = 0;

            foreach (byte[] data in dest)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }

        public void Dispose()
        {
            // Free resources of NetworkManager
            networkManager = null;

            // Buffers
            if(recvBuffer != null)
            {
                if (!recvBuffer.IsDisposed)
                    recvBuffer.Dispose();
            }
            
            if(sendBuffer != null)
            {
                if (!sendBuffer.IsDisposed)
                    sendBuffer.Dispose();
            }
            

            // Buffer Pool
            pool.TryFreeSlabs();
            pool = null;

            // Send Queue
            sendQueue.Clear();
            sendQueue = null;

            // Closing Socket
            if (client.Connected)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }
    }
}
