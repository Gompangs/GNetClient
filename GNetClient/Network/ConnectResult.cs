using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GNetwork.GNetClient
{
    public class ConnectResult
    {
        public bool isSuccess { get; internal set; }
        public Exception exception { get; internal set; }
        public EndPoint endpoint { get; internal set; }
        public AddressFamily addressFamily { get; internal set; }
    }
}