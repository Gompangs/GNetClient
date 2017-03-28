using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using GNetwork.GNetClient;

public class Program
{
    public static int Main(string[] args)
    {

        GNetClient netClient = GNetClient.getInstance("127.0.0.1", 10100);
        
        string longText = "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
            + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
            + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd";

        netClient.OnConnect += OnConnect;
        netClient.OnDisconnect += OnDisconnect;
        netClient.OnReceive += OnReceive;

        netClient.Connect();
        
        while (true)
        {
            netClient.Send(Encoding.UTF8.GetBytes(longText));
            Thread.Sleep(20);
        }
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
