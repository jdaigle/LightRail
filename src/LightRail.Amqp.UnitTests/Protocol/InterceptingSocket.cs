using System;
using System.Collections.Generic;
using LightRail.Amqp.Network;

namespace LightRail.Amqp.Protocol
{
    public class InterceptingSocket : ISocket
    {
        public event EventHandler OnClosed;

        public void Reset()
        {
            WriteBuffer.Clear();
            CloseCount = 0;
            Closed = false;
        }

        public List<ByteBuffer> WriteBuffer { get; set; } = new List<ByteBuffer>();
        public int CloseCount { get; set; } = 0;

        public ByteBuffer GetSentBufferFrame(int index)
        {
            return WriteBuffer[index];
        }

        public bool Closed { get; set; }
        public bool IsNotClosed { get { return !Closed; } }

        public void Write(ByteBuffer byteBuffer)
        {
            WriteBuffer.Add(byteBuffer);
        }

        public void ReceiveAsync(int count, Action<ByteBuffer> callback)
        {
            // no op
        }

        public void Close()
        {
            CloseCount++;
            Closed = true;
            var onClosedEvent = OnClosed;
            if (onClosedEvent != null)
                onClosedEvent(this, EventArgs.Empty);
        }

        public void CloseRead()
        {
            CloseCount++;
            Closed = true;
        }

        public void CloseWrite()
        {
            CloseCount++;
            Closed = true;
        }
    }
}
