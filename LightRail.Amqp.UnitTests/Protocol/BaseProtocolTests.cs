using NUnit.Framework;

namespace LightRail.Amqp.Protocol
{
    public abstract class BaseProtocolTests
    {
        private static readonly byte[] protocol0 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x00, 0x01, 0x00, 0x00 }; // AMQP01000

        protected InterceptingSocket socket;
        protected AmqpConnection connection;

        [SetUp]
        public void SetUp()
        {
            socket = new InterceptingSocket();
            connection = new AmqpConnection(socket);
        }

        protected void Given_Exchanged_Headers()
        {
            connection.HandleReceivedBuffer(new ByteBuffer(protocol0));

            Assert.AreEqual(1, socket.SentBufferFrames.Count);
            CollectionAssert.AreEqual(protocol0, socket.SentBufferFrames[0].Item1);
            Assert.AreEqual(ConnectionStateEnum.HDR_EXCH, connection.State);
            Assert.True(socket.IsNotClosed);

            socket.Reset();
        }
    }
}
