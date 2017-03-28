using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace GNetwork.Network
{
    internal class StateObject
    {
        // Client socket.
        internal Socket workSocket { get; set; }

        // check whether packet is first or not
        internal bool isFirstRead { get; set; }

        // total packet body size
        internal int packetSize { get; set; }

        // current received bytes size
        internal int totalReadBytesSize { get; set; }

        // Receive buffer.
        internal List<byte[]> dataBuffer { get; set; }
    }
}