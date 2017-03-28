using System;
using System.Collections.Generic;
using System.Text;

namespace UnityTcpClient.Network
{
    public class GPacket
    {
        // Send, Recv Data Structure
        public byte[] data { get; internal set; }
        public int size { get; internal set; }
    }
}
