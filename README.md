# UnityTcpClient
This Project for Unity(3D) TCP Client.

## Background
There are few projects in github which "correctly" work with TCP Unity3D.

I want use some library for my project, but many projects have lack of usage or very oldest of last commit.

So, I thought this is worthy to make library to Unity TCP Client who want to use simply like me.
## Goal
The goal is simple. "working on Unity3D" and "Well working"

But, current state is unstable to use official.
I'm trying enhance structures and build additional convenience for developer.

I'll release when it comes to stable version to use.
(Not Recommend using now)

## Usage(beta)
*Below instructions must called sequencially.*

First, creating NetworkManager Instance
```csharp
NetworkManager networkManager = new NetworkManager("127.0.0.1", 10100);
```

And, Adding Delegates for Network Operations.
```csharp
networkManager.OnConnect += OnConnect;
networkManager.OnDisconnect += OnDisconnect;
networkManager.OnReceive += OnReceive;
```
Last, Try Connect to Server
```csharp
networkManager.Connect();
```

Send data to server(It will be extent to more types)
```csharp
byte[] someData;
networkManager.Send(someData);
```

Receiving Data from Server -> OnReceive() function will called.
```csharp
private void OnReceive(byte[] data)
{
    Console.WriteLine("Received : {0}", data.Length);
}
```

## Reminder
When use this project, Server have to send data with "Header" on first 4bytes.
Because, it will use in collect bytes and aggregation when buffer not yet finish.

So, Header of Packet(first 4bytes) forced to be use.

## References
* [ServerToolKit(Buffer Pool) by tenor](https://github.com/tenor/ServerToolkit)
