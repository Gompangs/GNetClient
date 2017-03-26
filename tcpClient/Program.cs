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
        NetworkManager networkManager = new NetworkManager("127.0.0.1", 10100);

        string longText = "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
            + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd"
            + "asdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasdasd";

        networkManager.OnConnect += OnConnect;
        networkManager.OnDisconnect += OnDisconnect;
        networkManager.OnReceive += OnReceive;

        networkManager.Connect();
        
        while (true)
        {
            Console.ReadLine();
            networkManager.Send(Encoding.UTF8.GetBytes(longText));
        }
    }

    private static void OnReceive(string message)
    {
        throw new NotImplementedException();
    }

    private static void OnDisconnect(string message)
    {
        throw new NotImplementedException();
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
