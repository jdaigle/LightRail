using System;
using System.Collections.Generic;

namespace LightRail.Amqp.Protocol
{
    public class InterceptingSocket : ISocket
    {
        public void Reset()
        {
            SentBuffers.Clear();
            CloseCount = 0;
            Closed = false;
        }

        public List<Tuple<byte[], int, int>> SentBuffers { get; set; } = new List<Tuple<byte[], int, int>>();
        public int CloseCount { get; set; } = 0;

        public Action<byte[], int, int> OnSendAsync { get; set; }
        public Action OnClose { get; set; }
        public bool Closed { get; set; }
        public bool IsNotClosed { get { return !Closed; } }

        public void SendAsync(ByteBuffer byteBuffer)
        {
            SendAsync(byteBuffer.Buffer, byteBuffer.ReadOffset, byteBuffer.LengthAvailableToRead);
        }

        public void SendAsync(byte[] buffer, int offset, int length)
        {
            SentBuffers.Add(new Tuple<byte[], int, int>(buffer, offset, length));
            if (OnSendAsync != null)
                OnSendAsync(buffer, offset, length);
        }

        public void Close()
        {
            CloseCount++;
            Closed = true;
            if (OnClose != null)
                OnClose();
        }
    }
}
