using ServerToolkit.BufferManagement;
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
        
        // Called when Connection Established
        public delegate void ConnectDelegate(ConnectResult connectResult);
        public ConnectDelegate OnConnect;

        // Called when Data Received
        public delegate void ReceiveDelegate(string message);
        public ReceiveDelegate OnReceive;

        // Called when Connection Disconnected
        public delegate void DisconnectDelegate(string message);
        public DisconnectDelegate OnDisconnect;

        BufferPool pool = new BufferPool(1 * 1024 * 1024, 1, 1);
        IBuffer recvBuffer;
        IBuffer sendBuffer;
        
        private int RECEIVE_BUFFER_SIZE = 1024;

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
            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = client;

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

            int bytesRead = client.EndReceive(ar);

            byte[] data = new byte[bytesRead > 0 ? bytesRead : 0];

            if (recvBuffer != null && !recvBuffer.IsDisposed)
            {
                if (bytesRead > 0)
                {
                    recvBuffer.CopyTo(data, 0, bytesRead);

                    //Do anything else you wish with read data here.
                    Console.WriteLine("{0} readed", data.Length);
                    Console.WriteLine("{0}", Encoding.UTF8.GetString(data));
                }

                //Dispose buffer if it's larger than a specified threshold
                //if (recvBuffer.Size > BUFFER_SIZE_DISPOSAL_THRESHOLD)
                //{
                //    recvBuffer.Dispose();
                //}
            }

            if (bytesRead <= 0) return;

            //Read/Expect more data
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

        public void Send(byte[] byteData)
        {
            if (client.Connected)
            {
                sendBuffer = pool.GetBuffer(byteData.Length);
                sendBuffer.FillWith(byteData);
                client.BeginSend(sendBuffer.GetSegments(), SocketFlags.None, SendCallback, sendBuffer);
            }
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
                //Handle Exception here
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
    }
}
