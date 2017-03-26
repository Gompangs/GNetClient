using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UnityTcpClient
{
    public class ConnectResult
    {
        public bool isSuccess { get; internal set; }
        public Exception exception { get; internal set; }
        public EndPoint endpoint { get; internal set; }
        public AddressFamily addressFamily { get; internal set; }
    }
}