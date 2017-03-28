using GNetwork.Network;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GNetwork.Tests
{
    public class ReconnectTest
    {
        public static void main(string[] args)
        {
            GNetClient netClient = GNetClient.GetInstance("127.0.0.1", 10100);

            string longText = "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
                + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
                + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd";

            netClient.OnConnect += OnConnect;
            netClient.OnDisconnect += OnDisconnect;
            netClient.OnReceive += OnReceive;

            netClient.Connect();

            Thread.Sleep(2000);

            // Disconnect after 2s
            netClient.Disconnect();

            Thread.Sleep(2000);
            // Reconnect after 2s
            netClient.Connect();

            // Send some data
            netClient.Send(Encoding.UTF8.GetBytes(longText));
        }

        private static void OnReceive(byte[] data)
        {
            Console.WriteLine("Received : {0}", data.Length);
        }

        private static void OnDisconnect(Exception e)
        {
            Console.WriteLine("Disconnected with exception : {0}", e.ToString());
        }

        private static void OnConnect(ConnectResult connectResult)
        {
            Console.WriteLine("Connection Result : " + connectResult.isSuccess);
            if (connectResult.isSuccess)
            {
                // when connection is success
                Console.WriteLine("Connection EndPoint : " + connectResult.endpoint);
                Console.WriteLine("Connection EndPoint : " + connectResult.addressFamily);
            }
            else
            {
                Console.WriteLine("Connection Fail : " + connectResult.exception);
            }
        }
    }
}
