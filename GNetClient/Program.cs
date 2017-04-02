using GNetwork.Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Program
{
    public static int Main(string[] args)
    {
        //GNetClient netClient = GNetClient.GetInstance("127.0.0.1", 10100);

        //string longText = "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
        //    + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
        //    + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd";

        //netClient.Connect();

        //Thread.Sleep(2000);

        //// try Reconnect
        //netClient.ReConnect();

        //// Send some data
        //netClient.Send(Encoding.UTF8.GetBytes(longText));

        //Thread.Sleep(500);

        //netClient.Send(Encoding.UTF8.GetBytes(longText));

        //Console.ReadLine();

        GNetClient netClient = GNetClient.GetInstance("127.0.0.1", 10100);
        netClient.OnConnect += OnConnect;
        netClient.OnDisconnect += OnDisconnect;
        netClient.OnReceive += OnReceive;

        netClient.Connect();
        
        while (true)
        {
            netClient.Send("Hello");
            string line = Console.ReadLine();

            if (line.Equals("rec"))
            {
                netClient.Reconnect();
            }
        }
    }

    private static void OnReceive(GPacket data)
    {
        Console.WriteLine("Received({0}) : {1}", data.size, Encoding.UTF8.GetString(data.data, 0, data.size));
    }

    private static void OnDisconnect()
    {
        Console.WriteLine("Disconnected from server");
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
