namespace LightRail.Amqp.Protocol
{
    public interface ISocket
    {
        /// <summary>
        /// Sends the specified byte buffer on the socket.
        /// </summary>
        void SendAsync(ByteBuffer byteBuffer);

        /// <summary>
        /// Sends the specified byte buffer on the socket.
        /// </summary>
        void SendAsync(byte[] buffer, int offset, int length);

        /// <summary>
        /// Immediately closes the underlying socket and cleans up.
        /// </summary>
        void Close();
    }
}
