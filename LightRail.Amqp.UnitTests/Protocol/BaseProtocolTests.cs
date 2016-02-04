using System;
using System.Linq;
using LightRail.Amqp.Framing;
using NUnit.Framework;

namespace LightRail.Amqp.Protocol
{
    public abstract class BaseProtocolTests
    {
        protected static readonly byte[] defaultAcceptedProtocol = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x00, 0x01, 0x00, 0x00 }; // AMQP01000

        private TestContainer container;
        protected InterceptingSocket socket;
        protected AmqpConnection connection;

        [SetUp]
        public void SetUp()
        {
            container = new TestContainer();
            socket = new InterceptingSocket();
            connection = new AmqpConnection(socket, container);
        }

        [TearDown]
        public void TearDown()
        {
            socket.Close(); // cleanup
        }

        protected void Given_Exchanged_Headers()
        {
            connection.HandleHeader(new ByteBuffer(defaultAcceptedProtocol));

            Assert.AreEqual(1, socket.WriteBuffer.Count);
            CollectionAssert.AreEqual(defaultAcceptedProtocol, socket.WriteBuffer[0].Buffer);
            Assert.AreEqual(ConnectionStateEnum.HDR_EXCH, connection.State);
            Assert.True(socket.IsNotClosed);

            socket.Reset();
        }

        protected void Given_Open_Connection()
        {
            Given_Exchanged_Headers();

            EncodeAndSend(new Open()
            {
                ContainerID = Guid.NewGuid().ToString(),
                Hostname = "localhost",
            });

            Assert.NotNull(DecodeLastFrame() as Open);
            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);

            socket.Reset();
        }

        protected void EncodeAndSend(AmqpFrame frame, ushort channelNumber = 0)
        {
            var buffer = new ByteBuffer(512, true);
            AmqpFrameCodec.EncodeFrame(buffer, frame, channelNumber);
            connection.HandleFrame(buffer);
        }

        protected AmqpFrame DecodeLastFrame()
        {
            ushort channelNumber = 0;
            return AmqpFrameCodec.DecodeFrame(socket.WriteBuffer.Last(), out channelNumber);
        }

        protected AmqpFrame DecodeLastFrame(out ushort channelNumber)
        {
            return AmqpFrameCodec.DecodeFrame(socket.WriteBuffer.Last(), out channelNumber);
        }
    }
}
