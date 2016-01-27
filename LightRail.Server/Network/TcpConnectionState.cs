using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp;
using LightRail.Amqp.Protocol;

namespace LightRail.Server.Network
{
    public class TcpConnectionState : ISocket
    {
        public Socket Socket { get; }
        public int ReceiveBufferOffset { get; internal set; }
        public int ReceiverBufferSize { get; internal set; }
        public IPEndPoint IPAddress { get; }
        private readonly TcpListener tcpListener;

        public AmqpConnection amqpConnection { get; }

        public TcpConnectionState(TcpListener tcpListener, Socket acceptSocket)
        {
            this.tcpListener = tcpListener;
            this.Socket = acceptSocket;
            this.IPAddress = acceptSocket.RemoteEndPoint as IPEndPoint;
            this.amqpConnection = new AmqpConnection(this, HostContainer.Instance);
        }

        public void HandleReceived(ByteBuffer buffer)
        {
            amqpConnection.HandleReceivedBuffer(buffer);
        }

        public void HandleSocketClosed()
        {
            if (amqpConnection != null)
                amqpConnection.HandleSocketClosed();
        }

        public void SendAsync(ByteBuffer byteBuffer)
        {
            tcpListener.SendAsync(this, byteBuffer.Buffer, byteBuffer.ReadOffset, byteBuffer.LengthAvailableToRead);
        }

        public void SendAsync(byte[] buffer, int offset, int length)
        {
            tcpListener.SendAsync(this, buffer, offset, length);
        }

        public void Close()
        {
            tcpListener.Disconnect(this, SocketShutdown.Both);
        }

        public void CloseRead()
        {
            tcpListener.Disconnect(this, SocketShutdown.Receive);
        }

        public void CloseWrite()
        {
            tcpListener.Disconnect(this, SocketShutdown.Send);
        }
    }
}
