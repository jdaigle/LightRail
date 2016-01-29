﻿using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using LightRail.Amqp;
using LightRail.Amqp.Network;
using LightRail.Amqp.Protocol;

namespace LightRail.Server.Network
{
    public class TcpConnection : ISocket
    {
        public Socket Socket { get; }
        public int ReceiveBufferOffset { get; internal set; }
        public int ReceiverBufferSize { get; internal set; }
        public IPEndPoint IPAddress { get; }
        private readonly TcpListener tcpListener;
        private readonly SocketAsyncEventArgs receiveEventArgs;
        private readonly SocketAsyncEventArgs sendEventArgs;
        private readonly AsyncPump receivedFramePump;

        public AmqpConnection amqpConnection { get; }

        public TcpConnection(TcpListener tcpListener, Socket acceptSocket, IBufferPool bufferPool)
        {
            this.tcpListener = tcpListener;
            this.Socket = acceptSocket;
            this.IPAddress = acceptSocket.RemoteEndPoint as IPEndPoint;
            this.amqpConnection = new AmqpConnection(this, HostContainer.Instance);
            this.BufferPool = bufferPool;

            this.receiveEventArgs = new SocketAsyncEventArgs();
            this.receiveEventArgs.Completed += (s, a) => TcpSocket.CompleteAsyncIOOperation(((TaskCompletionSource<int>)a.UserToken), a, b => b.BytesTransferred);

            this.sendEventArgs = new SocketAsyncEventArgs();
            this.sendEventArgs.Completed += (s, a) => TcpSocket.CompleteAsyncIOOperation(((TcpSocket.SendAsyncBufferToken<int>)a.UserToken), a, b => b.bytesTransferred);

            receivedFramePump = new AsyncPump(amqpConnection, this);
            receivedFramePump.Start();
        }

        public IBufferPool BufferPool { get; }

        public void HandleSocketClosed()
        {
            if (amqpConnection != null)
                amqpConnection.HandleSocketClosed();
        }

        public Task SendAsync(ByteBuffer byteBuffer)
        {
            return SendAsync(byteBuffer.Buffer, byteBuffer.ReadOffset, byteBuffer.LengthAvailableToRead);
        }

        public Task SendAsync(byte[] buffer, int offset, int count)
        {
            return TcpSocket.SendAsync(Socket, sendEventArgs, buffer, offset, count);
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

        public Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            return TcpSocket.ReceiveAsync(this.Socket, receiveEventArgs, buffer, offset, count);
        }
    }
}