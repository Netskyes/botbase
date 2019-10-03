using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Network
{
    using Utility;
    public class UdpServer : ClientBase
    {
        public bool Listening { get; private set; }
        public Dictionary<IPEndPoint, IPEndPoint> UdpLinks { get; } = new Dictionary<IPEndPoint, IPEndPoint>();

        public UdpServer()
        {
            BUFFER_SIZE = (1024 * 1024) * 50; // 50MB
        }
        
        public void Listen(ushort port) 
            => BeginListenUdp(port);

        protected override void OnClientState(bool isConnected)
        {
            Listening = isConnected;
            base.OnClientState(isConnected);
        }

        protected override void AsyncRecvProcess(Packet packet)
        {
            if (IsPacketAddress(packet.Buffer))
            {
                var destination = GetEndPoint(packet.Buffer);

                Logger.Log($"Received UDP destination: {destination.ToString()}" + Environment.NewLine);

                lock (UdpLinks)
                {
                    AddLink(packet.EndPoint, destination);
                }

                return;
            }


            IPEndPoint endPoint = null;

            lock (UdpLinks)
            {
                if (!UdpLinks.ContainsKey(packet.EndPoint))
                    return;

                endPoint = UdpLinks[packet.EndPoint];
            }

            Send(ProcessPacket
                (new Packet { Buffer = packet.Buffer, EndPoint = endPoint, UdpPacket = true }));
        }

        private void AddLink(IPEndPoint ep1, IPEndPoint ep2)
        {
            if (UdpLinks.ContainsKey(ep1))
                UdpLinks.Remove(ep1);
            if (UdpLinks.ContainsKey(ep2))
                UdpLinks.Remove(ep2);

            UdpLinks.Add(ep1, ep2);
            UdpLinks.Add(ep2, ep1);
        }

        private Packet ProcessPacket(Packet packet)
        {
            byte[] buffer = packet.Buffer;
            // Do whatever with packet before its sent to destination.

            return packet;
        }


        private IPEndPoint GetEndPoint(byte[] buffer)
        {
            var port = GetDestPort(buffer);
            var address = GetDestAddress(buffer);

            return new IPEndPoint(new IPAddress(address), port);
        }

        private ushort GetDestPort(byte[] buffer)
        {
            try
            {
                ushort port = BitConverter.ToUInt16(buffer, 2);
                return (ushort)IPAddress.NetworkToHostOrder((short)port);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return 0;
            }
        }

        private uint GetDestAddress(byte[] buffer)
        {
            byte[] temp = new byte[4];

            try
            {
                Array.Copy(buffer, 4, temp, 0, temp.Length);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return 0;
            }

            return (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(temp, 0));
        }

        private bool IsPacketAddress(byte[] buffer)
        {
            ushort opcode;

            try
            {
                opcode = BitConverter.ToUInt16(buffer, 0);
                opcode = (ushort)IPAddress.NetworkToHostOrder((short)opcode);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                return false;
            }

            return (opcode == 1337);
        }
    }
}
