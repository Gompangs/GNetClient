using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

internal class StateObject
{
    // Client socket.
    internal Socket workSocket { get; set; }

    // check whether packet is first or not
    internal bool isFirstRead { get; set; } = true;
    
    // total packet body size
    internal int packetSize { get; set; }

    // current received bytes size
    internal int totalReadBytesSize { get; set; }

    // Receive buffer.
    internal byte[] buffer { get; set; }

    // Receive buffer for temporary
    internal byte[] tempBuffer { get; set; }
}
