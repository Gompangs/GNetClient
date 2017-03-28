using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityTcpClient;

public class Program
{
    public static int Main(string[] args)
    {

        NetworkManager networkManager = NetworkManager.getInstance("127.0.0.1", 10100);
        
        string longText = "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
            + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
            + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd";

        networkManager.OnConnect += OnConnect;
        networkManager.OnDisconnect += OnDisconnect;
        networkManager.OnReceive += OnReceive;

        networkManager.Connect();
        
        while (true)
        {
            networkManager.Send(Encoding.UTF8.GetBytes(longText));
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
