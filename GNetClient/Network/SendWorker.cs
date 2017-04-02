using System;
using System.Collections.Generic;
using System.Text;

namespace GNetwork.Network
{
    public class SendWorker
    {
        // Called when Connection Established
        public delegate void SendCompleteDelegate();
        public SendCompleteDelegate OnSendComplete;

        GNetClient netClient;
        Queue<GPacket> packetQueue;

        public void Init(GNetClient client)
        {
            netClient = client;
            packetQueue = new Queue<GPacket>();
            Console.WriteLine("SendWorker initialized");
        }

        // This method will be called when the thread is started.
        public void Work()
        {
            while (!_shouldStop)
            {
                if(packetQueue.Count > 0)
                {
                    // Process when packet queue is not empty
                    netClient.SendPacket(packetQueue.Dequeue());
                    Console.WriteLine("Packet Send ! from thread");
                }
            }
            Console.WriteLine("worker thread : terminating gracefully.");
        }

        public bool putPacket(GPacket packet)
        {
            packetQueue.Enqueue(packet);
            return true;
        }

        public void StopWork()
        {
            _shouldStop = true;
        }
        // Volatile is used as hint to the compiler that this data
        // member will be accessed by multiple threads.
        private volatile bool _shouldStop;
    }
}
