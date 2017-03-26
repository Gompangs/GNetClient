using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UnityTcpClient
{
    public class NetworkManager
    {
        // The port number for the remote device.
        private Socket client;
        private IPEndPoint remoteEP;

        // Initial Buffer size is 256
        private int bufferSize = 256;
        
        // Called when Connection Established
        public delegate void ConnectDelegate(ConnectResult connectResult);
        public ConnectDelegate OnConnect;

        // Called when Data Received
        public delegate void ReceiveDelegate(string message);
        public ReceiveDelegate OnReceive;

        // Called when Connection Disconnected
        public delegate void DisconnectDelegate(string message);
        public DisconnectDelegate OnDisconnect;

        public NetworkManager(string ip, int port)
        {
            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                IPAddress ipAddress = IPAddress.Parse(ip);
                remoteEP = new IPEndPoint(ipAddress, port);

                // Create Buffer

                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void SetBufferSize(int bufferSize)
        {
            this.bufferSize = bufferSize;
        }

        /// <summary>
        /// Start Connect to Server
        /// </summary>
        public void Connect()
        {
            // Connect to the remote endpoint.
            client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
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
            OnConnect(connectResult);
        }

        public void Disconnect()
        {
            // Release the socket.
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // TODO : Byte Buffer Pooling
                state.buffer = new byte[bufferSize];
                state.tempBuffer = new byte[bufferSize];

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, bufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                client = state.workSocket;

                int bytesRead = client.EndReceive(ar);

                if (state.isFirstRead)
                {
                    // 처음 읽기라면 Header에서 패킷 사이즈를 가져온다
                    byte[] packetSize = new byte[4];
                    Buffer.BlockCopy(state.buffer, 0, packetSize, 0, 4);

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

                byte[] temp = new byte[bytesRead];

                Array.Copy(state.buffer, temp, bytesRead);

                if (bytesRead > 0)
                {
                    state.totalReadBytesSize += bytesRead;
                    Console.WriteLine("Read : {0}, Total : {1}", bytesRead, state.totalReadBytesSize);

                    // Header 포함한 크기만큼 다 읽었다면
                    if (state.totalReadBytesSize == state.packetSize + 4)
                    {
                        // 패킷 크기만큼 다 읽엇다면
                        Console.WriteLine("Received Complete.");

                        // Receive를 다시 호출해서 Read를 새로 한다.
                        Receive(client);
                    }
                    else
                    {
                        // 아직 
                        Array.Copy(state.buffer, state.tempBuffer, bytesRead);

                        // Get the rest of the data.
                        client.BeginReceive(state.buffer, 0, bufferSize, 0,
                            new AsyncCallback(ReceiveCallback), state);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Send(byte[] byteData)
        {
            if (client.Connected)
            {
                // Begin sending the data to the remote device.
                client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
