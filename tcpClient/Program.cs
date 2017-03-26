using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class Program
{
    public static int Main(string[] args)
    {
        NetworkManager networkManager = new NetworkManager("127.0.0.1", 10100);

        networkManager.Connect();

        networkManager.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.NoDelay, true);

        int val = networkManager.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.NoDelay);
        
        Console.WriteLine(val);

        networkManager.OnConnect += OnConnect;
        
        while (true)
        {
            Console.ReadLine();
            networkManager.Send(Encoding.UTF8.GetBytes("test send"));
        }
    }

    private static void OnConnect(ConnectResult connectResult)
    {
        Console.WriteLine("Main Connected");
    }
}
