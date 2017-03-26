using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class StateObject
{
    // Client socket.
    public Socket workSocket { get; set; }

    // check whether packet is first or not
    public bool isFirstRead { get; set; } = true;

    // Size of receive buffer.
    public const int BufferSize = 256;

    // total packet body size
    public int packetSize { get; set; }

    // current received bytes size
    public int totalReadBytesSize { get; set; }

    // Receive buffer.
    public byte[] buffer { get; set; } = new byte[BufferSize];

    // Receive buffer for temporary
    public byte[] tempBuffer { get; set; } = new byte[BufferSize];
}
