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

## References
* [ServerToolKit(Buffer Pool) by tenor](https://github.com/tenor/ServerToolkit)
