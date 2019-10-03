using System;

namespace Network
{
    public class ProxyRemote : ClientBase
    {
        public int ID { get; set; }
        public ClientBase Parent { get; set; }

        public ProxyRemote(ClientBase parent)
        {
            Parent = parent;
            BUFFER_SIZE = (1024 * 1024) * 1; // 1MB
        }

        protected override void AsyncRecvProcess(Packet packet)
        {
            Parent.Send(packet);
        }

        protected override void SendProcess(Packet packet)
        {
            base.SendProcess(packet);
        }
    }
}
