﻿using System.Net;

namespace Network
{
    public class Packet
    {
        public byte[] Buffer { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public bool UdpPacket { get; set; }
    }
}
