# GNetClient
This Project for Unity(3D) TCP Client.<br/>
![Image of Yaktocat](https://secure.travis-ci.org/Gompangs/GNetClient.png)

## Versions
0.1v - Under Development on now

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

First, get instance from NetworkManager(only one object will be created by Singleton)
```csharp
GNetClient netClient = GNetClient.getInstance("127.0.0.1", 10100);
```

And, Adding Delegates for Network Operations.
```csharp
netClient.OnConnect += OnConnect;
netClient.OnDisconnect += OnDisconnect;
netClient.OnReceive += OnReceive;
```
Last, Try Connect to Server
```csharp
netClient.Connect();
```

**Use After Connect()**<br/>
Send data to server(It will be extent to more types)
```csharp
byte[] someData;
netClient.Send(someData);
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
* [ServerToolKit(Buffer Pool) - Slab Allocation by tenor](https://github.com/tenor/ServerToolkit)
