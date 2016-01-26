using System;
using System.Collections.Generic;

namespace LightRail.Amqp.Protocol
{
    public class InterceptingSocket : ISocket
    {
        public void Reset()
        {
            SentBufferFrames.Clear();
            CloseCount = 0;
            Closed = false;
        }

        public List<Tuple<byte[], int, int>> SentBufferFrames { get; set; } = new List<Tuple<byte[], int, int>>();
        public int CloseCount { get; set; } = 0;

        public ByteBuffer GetSentBufferFrame(int index)
        {
            return new ByteBuffer(SentBufferFrames[index].Item1, SentBufferFrames[index].Item2, SentBufferFrames[index].Item3, SentBufferFrames[index].Item3);
        }

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
            SentBufferFrames.Add(new Tuple<byte[], int, int>(buffer, offset, length));
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

        public void CloseRead()
        {
            CloseCount++;
            Closed = true;
            if (OnClose != null)
                OnClose();
        }

        public void CloseWrite()
        {
            CloseCount++;
            Closed = true;
            if (OnClose != null)
                OnClose();
        }
    }
}
