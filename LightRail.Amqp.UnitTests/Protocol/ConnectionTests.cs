using System;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Types;
using NUnit.Framework;

namespace LightRail.Amqp.Protocol
{
    [TestFixture]
    public class ConnectionTests : BaseProtocolTests
    {
        [Test]
        public void Must_Have_Received_Header_Before_Open_Frame()
        {
            //Given_Exchanged_Headers();

            var open = new Open();
            EncodeAndSend(open);

            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
            Assert.AreEqual(1, socket.WriteBuffer.Count);
            Assert.AreEqual(8, socket.WriteBuffer[0].LengthAvailableToRead); // 8 byte for proto header
        }

        [Test]
        public void Can_Open_Connection()
        {
            Given_Exchanged_Headers();

            var open = new Open();
            EncodeAndSend(open);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);
            Assert.AreEqual(1, socket.WriteBuffer.Count);
        }

        [Test]
        public void Receives_Open_Frame_In_Response_To_Open()
        {
            Given_Exchanged_Headers();

            var open = new Open();
            EncodeAndSend(open);
            var responseOpen = DecodeLastFrame() as Open;

            Assert.NotNull(responseOpen);
        }

        [Test]
        public void Can_Negotiate_Open_Limitations()
        {
            Given_Exchanged_Headers();

            var open = new Open()
            {
                ContainerID = Guid.NewGuid().ToString(),
                Hostname = "localhost",
                MaxFrameSize = 600,
                ChannelMax = 10,
            };
            EncodeAndSend(open);
            var response = DecodeLastFrame() as Open;

            Assert.AreEqual(600, response.MaxFrameSize, "MaxFrameSize");
            Assert.AreEqual(10, response.ChannelMax, "ChannelMax");
        }

        [Test]
        public void Can_Pipeline_Open()
        {
            //Given_Exchanged_Headers();

            var buffer = new ByteBuffer(defaultAcceptedProtocol, 0, defaultAcceptedProtocol.Length, defaultAcceptedProtocol.Length, true);
            AmqpCodec.EncodeFrame(buffer, new Open(), 0);
            connection.HandleHeader(buffer);
            connection.HandleFrame(buffer);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);
            Assert.AreEqual(2, socket.WriteBuffer.Count);
        }

        [Test]
        public void Can_Close()
        {
            Given_Exchanged_Headers();

            var open = new Open();
            EncodeAndSend(open);

            var close = new Close();
            EncodeAndSend(close);

            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
            Assert.AreEqual(2, socket.WriteBuffer.Count);
        }

        [Test]
        public void Sends_Close_on_Close()
        {
            Given_Exchanged_Headers();

            var open = new Open();
            EncodeAndSend(open);

            var close = new Close();
            EncodeAndSend(close);

            var response = DecodeLastFrame() as Close;
            Assert.NotNull(response);
        }

        [Test]
        public void Can_Pipeline_Open_Close()
        {
            //Given_Exchanged_Headers();

            var buffer = new ByteBuffer(defaultAcceptedProtocol, 0, defaultAcceptedProtocol.Length, defaultAcceptedProtocol.Length, true);
            AmqpCodec.EncodeFrame(buffer, new Open(), 0);
            AmqpCodec.EncodeFrame(buffer, new Close(), 0);
            connection.HandleHeader(buffer);
            connection.HandleFrame(buffer);
            connection.HandleFrame(buffer);

            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
            Assert.AreEqual(3, socket.WriteBuffer.Count);
        }

        [Test]
        public void Can_Specify_IdleTimeOut()
        {
            Given_Exchanged_Headers();

            var open = new Open()
            {
                ContainerID = Guid.NewGuid().ToString(),
                Hostname = "localhost",
                MaxFrameSize = 600,
                ChannelMax = 10,
                IdleTimeOut = 1000,
            };
            EncodeAndSend(open);
            var response = DecodeLastFrame() as Open;

            Assert.AreEqual(1000, response.IdleTimeOut, "IdleTimeOut");
        }

        [Test]
        public void Can_Ignore_IdleTimeOut()
        {
            Given_Exchanged_Headers();

            var open = new Open()
            {
                ContainerID = Guid.NewGuid().ToString(),
                Hostname = "localhost",
                MaxFrameSize = 600,
                ChannelMax = 10,
                //IdleTimeOut = 1000,
            };
            EncodeAndSend(open);
            var response = DecodeLastFrame() as Open;

            Assert.AreEqual(AmqpConnection.DefaultIdleTimeout, response.IdleTimeOut, "IdleTimeOut");
        }

        [Test]
        public void Can_Close_Connection()
        {
            Given_Exchanged_Headers();

            var open = new Open();
            EncodeAndSend(open);
            connection.CloseConnection(null);

            var response = DecodeLastFrame() as Close;
            Assert.NotNull(response);
            Assert.AreEqual(ConnectionStateEnum.CLOSE_SENT, connection.State);
        }

        [Test]
        public void On_Idle_Sends_Close()
        {
            Given_Exchanged_Headers();

            var open = new Open();
            EncodeAndSend(open);
            connection.CloseDueToTimeout();

            var response = DecodeLastFrame() as Close;
            Assert.NotNull(response);
            Assert.AreEqual(ConnectionStateEnum.DISCARDING, connection.State);
        }

        [Test]
        public void Can_Gracefully_Close_Connection()
        {
            Given_Exchanged_Headers();

            EncodeAndSend(new Open());
            connection.CloseConnection(null);
            EncodeAndSend(new Close());

            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
        }
    }
}
