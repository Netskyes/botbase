﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Network
{
    using Utility;
    public class Server
    {
        public bool Listening { get; private set; }
        public ushort Port { get; private set; }

        public ClientBase[] Clients
        {
            get
            {
                lock (clientsLock)
                {
                    return clients.ToArray();
                }
            }
        }

        private Socket socket;
        private SocketAsyncEventArgs sockArgs;
        private List<ClientBase> clients;
        private readonly object clientsLock = new object();
        protected bool ProcessDisconnect { get; set; }

        #region Event Handlers

        public event ServerStateEventHandler ServerState;
        public delegate void ServerStateEventHandler(bool isListening);

        private void OnServerState(bool isListening)
        {
            if (Listening == isListening)
                return;

            Listening = isListening;
            ServerState?.Invoke(isListening);
        }


        public event ClientRecvEventHandler ClientRecv;
        public delegate void ClientRecvEventHandler(Server server, ClientBase client, Packet packet);

        private void OnClientRecv(ClientBase client, Packet packet) 
            => ClientRecv?.Invoke(this, client, packet);


        public event ClientSendEventHandler ClientSend;
        public delegate void ClientSendEventHandler(Server server, ClientBase client, Packet packet);

        private void OnClientSend(ClientBase client, Packet packet) 
            => ClientSend?.Invoke(this, client, packet);


        public event ClientStateEventHandler ClientState;
        public delegate void ClientStateEventHandler(Server server, ClientBase client, bool isConnected);

        private void OnClientState(ClientBase client, bool isConnected)
        {
            ClientState?.Invoke(this, client, isConnected);

            if (!isConnected)
            {
                RemoveClient(client);
            }
        }

        #endregion


        public Server()
        {
            clients = new List<ClientBase>();
        }

        public void Listen(ushort port)
        {
            Port = port;

            try
            {
                if (Listening)
                    return;

                if (socket != null)
                    DisposeSocket();

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                socket.Listen(1000);

                ProcessDisconnect = false;
                OnServerState(true);


                if (sockArgs != null)
                    DisposeSockArgs();

                sockArgs = new SocketAsyncEventArgs();
                sockArgs.Completed += AcceptClient;

                if (!socket.AcceptAsync(sockArgs)) AcceptClient(null, sockArgs);
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10048)
                {
                    // Port already in use!
                }

                Logger.LogException(e);
                Disconnect();
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                Disconnect();
            }
        }

        protected virtual ClientBase OnSocketSuccess(Socket s)
        {
            // Default client
            return new ProxyLocal(this, s);
        }

        private void AcceptClient(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                do
                {
                    switch (e.SocketError)
                    {
                        case SocketError.Success:
                            var client = OnSocketSuccess(e.AcceptSocket);
                            if (client != null)
                            {
                                AddClient(client);
                            }
                            break;

                        case SocketError.ConnectionReset:
                            break;

                        default:
                            throw new Exception("Socket error");
                    }

                    e.AcceptSocket = null;
                }
                while (!socket.AcceptAsync(e));
            }
            catch (ObjectDisposedException ex)
            {
                Logger.LogException(ex);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Disconnect();
            }
        }

        private void AddClient(ClientBase client)
        {
            lock (clientsLock)
            {
                client.ClientState += OnClientState;
                client.ClientRecv += OnClientRecv;
                client.ClientRecv += OnClientSend;
                clients.Add(client);
            }
        }

        private void RemoveClient(ClientBase client)
        {
            lock (clientsLock)
            {
                client.ClientState -= OnClientState;
                client.ClientRecv -= OnClientRecv;
                client.ClientRecv -= OnClientSend;
                clients.Remove(client);
            }
        }

        private void DisposeSocket()
        {
            socket.Close();
            socket = null;
        }

        private void DisposeSockArgs()
        {
            sockArgs.Dispose();
            sockArgs = null;
        }

        private void Disconnect()
        {
            if (ProcessDisconnect)
                return;

            ProcessDisconnect = true;


            if (socket != null)
                DisposeSocket();

            if (sockArgs != null)
                DisposeSockArgs();


            lock (clientsLock)
            {
                while (clients.Count != 0)
                {
                    try
                    {
                        clients[0].ClientRecv -= OnClientRecv;
                        clients[0].ClientSend -= OnClientSend;
                        clients[0].ClientState -= OnClientState;
                        clients.RemoveAt(0);
                    }
                    catch (Exception e)
                    {
                        Logger.LogException(e);
                    }
                }
            }

            ProcessDisconnect = false;
            OnServerState(false);
        }
    }
}
