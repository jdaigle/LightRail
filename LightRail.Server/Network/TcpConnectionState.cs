using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Server.Network
{
    public class TcpConnectionState
    {
        public Socket Socket { get; }
        public int ReceiveBufferOffset { get; internal set; }
        public int ReceiverBufferSize { get; internal set; }
        public IPEndPoint IPAddress { get; }

        private readonly TcpListener tcpListener;

        public TcpConnectionState(TcpListener tcpListener, Socket acceptSocket)
        {
            this.tcpListener = tcpListener;
            this.Socket = acceptSocket;
            this.IPAddress = acceptSocket.RemoteEndPoint as IPEndPoint;
        }
    }
}
