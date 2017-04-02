using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GNetwork.Network
{
    // TODO : offer statistics for Network Operations.
    public class GStatistics
    {
        private static int sentBytes = 0;
        private static int recvBytes = 0;
        private static object _sentLock = new object();
        private static object _recvLock = new object();

        public static void incSent(int size)
        {
            lock (_sentLock)
            {
                sentBytes += size;
            }
        }

        public static void incRecv(int size)
        {
            lock (_recvLock)
            {
                recvBytes += size;
            }
        }

        public static int getTotalSentBytes()
        {
            lock (_sentLock)
            {
                return sentBytes;
            }
        }

        public static int getTotalRecvBytes()
        {
            lock (_recvLock)
            {
                return recvBytes;
            }
        }
    }
}
