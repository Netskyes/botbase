using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Network
{
    using Utility;
    public class ProxyLocal : ClientBase
    {
        public int ID { get; set; }
        public Server Parent { get; set; }
        public ProxyRemote Relay { get; set; }

        #region Fields

        private bool appendHeader;
        private bool handshakeComplete;

        private int dataLen;
        private int readOffset;
        private int tempHeaderOffset;
        private int headerSize;

        private byte[] tempHeader;

        #endregion


        public ProxyLocal(Server server, Socket socket)
        {
            Parent = server;
            ID = new Random().Next(100, 9999);
            BUFFER_SIZE = (1024 * 1024) * 1; // 1MB
            headerSize = 6;
            tempHeader = new byte[6];
            
            BeginListen(server, socket);
        }

        private byte[] ProcessPacket(byte[] packet)
        {
            // Do whatever with packet before its sent to destination.

            return packet;
        }

        // Process packets received locally
        protected override void AsyncRecvProcess(Packet packet)
        {
            var readBuffer = packet.Buffer;
            
            int bufferLen = readBuffer.Length;
            dataLen += bufferLen;

            while (true)
            {
                if (handshakeComplete)
                {
                    if (!Relay.Connected)
                    {
                        Disconnect();
                        break;
                    }

                    if (readOffset > 0)
                    {
                        byte[] temp = new byte[bufferLen - readOffset];

                        try
                        {
                            Array.Copy(readBuffer, readOffset, temp, 0, temp.Length);
                        }
                        catch (Exception)
                        {
                            Disconnect();
                            break;
                        }


                        Relay.Send(ProcessPacket(temp));
                        readOffset = 0;
                    }
                    else
                    {
                        Relay.Send(ProcessPacket(readBuffer));
                    }

                    break;
                }


                if (dataLen < headerSize)
                {
                    try
                    {
                        Array.Copy(readBuffer, 0, tempHeader, tempHeaderOffset, bufferLen);
                    }
                    catch (Exception)
                    {
                        Disconnect();
                        break;
                    }

                    tempHeaderOffset += bufferLen;
                    appendHeader = true;

                    break;
                }
                else
                {
                    int headerLen = (appendHeader)
                        ? (headerSize - tempHeaderOffset) : headerSize;

                    try
                    {
                        if (appendHeader)
                        {
                            Array.Copy(readBuffer, 0, tempHeader, tempHeaderOffset, headerLen);
                        }

                        byte[] header = (headerLen < headerSize)
                            ? tempHeader : readBuffer;


                        handshakeComplete = ConnectRelay(GetRelayEndPoint(header));
                    }
                    catch (Exception)
                    {
                        Disconnect();
                        break;
                    }

                    if ((bufferLen - headerLen) < 1)
                        break;


                    readOffset += headerLen;
                }
            }
        }

        private bool ConnectRelay(IPEndPoint endPoint)
        {
            Logger.Log($"Received TCP destination: {endPoint.ToString()}" + Environment.NewLine);

            Relay = new ProxyRemote(this);
            Relay.ID = ID;
            Relay.ClientRecv += (client, packet) => { OnClientRecv(packet, client); };
            Relay.ClientSend += (client, packet) => { OnClientSend(packet, client); };
            Relay.Connect(endPoint);

            return Relay != null && Relay.Connected;
        }

        private IPEndPoint GetRelayEndPoint(byte[] buffer)
        {
            ushort port = BitConverter.ToUInt16(buffer, 0);
            port = (ushort)IPAddress.NetworkToHostOrder((short)port);

            byte[] temp = new byte[4];
            Array.Copy(buffer, 2, temp, 0, temp.Length);

            uint address = (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(temp, 0));

            // Relay destination
            return new IPEndPoint(new IPAddress(address), port);
        }

        private byte[] BuildPacket(byte[] payload)
        {
            byte[] packet = new byte[4 + payload.Length];

            try
            {
                Array.Copy(BitConverter.GetBytes(payload.Length), packet, 4);
                Array.Copy(payload, 0, packet, 4, payload.Length);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }

            return packet;
        }

        public override void Disconnect()
        {
            base.Disconnect();
            CleanUp();
        }

        public void CleanUp()
        {
            tempHeader = null;
            dataLen = 0;
            readOffset = 0;
            tempHeaderOffset = 0;
            headerSize = 0;

            if (Relay != null)
            {
                Relay.Disconnect();
            }
        }
    }
}
