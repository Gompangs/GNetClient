# UnityTcpClient
This Project for Unity(3D) TCP Client.

## Background
There are few projects in github which "correctly" work with TCP Unity3D.

I want use some library for my project, but many projects have lack of usage or very oldest of last commit.

So, I thought this is worthy to make library to Unity TCP Client who want to use simply like me.
## Goal
The goal is simple. "working on Unity3D" and "Well working"


## Usage
```csharp
// create instance NetworkManager
NetworkManager networkManager = new NetworkManager("127.0.0.1", 10100);

// Adding Delegates for Operations.
networkManager.OnConnect += OnConnect;
networkManager.OnDisconnect += OnDisconnect;
networkManager.OnReceive += OnReceive;

// Start Connect to server
networkManager.Connect();

```

## References
* [ServerToolKit(Buffer Pool) by tenor](https://github.com/tenor/ServerToolkit)
