using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
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
            Assert.AreEqual(1, socket.SentBufferFrames.Count);
            Assert.AreEqual(8, socket.SentBufferFrames[0].Item3); // 8 byte for proto header
        }

        [Test]
        public void Can_Open_Connection()
        {
            Given_Exchanged_Headers();

            var open = new Open();
            EncodeAndSend(open);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);
            Assert.AreEqual(1, socket.SentBufferFrames.Count);
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
            AmqpFrameCodec.EncodeFrame(buffer, new Open(), 0);
            connection.HandleReceivedBuffer(buffer);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);
            Assert.AreEqual(2, socket.SentBufferFrames.Count);
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
            Assert.AreEqual(2, socket.SentBufferFrames.Count);
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
            AmqpFrameCodec.EncodeFrame(buffer, new Open(), 0);
            AmqpFrameCodec.EncodeFrame(buffer, new Close(), 0);
            connection.HandleReceivedBuffer(buffer);

            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
            Assert.AreEqual(3, socket.SentBufferFrames.Count);
        }
    }
}
