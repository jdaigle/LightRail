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
    public class VersionNegotiationTests : BaseProtocolTests
    {
        private static readonly byte[] protocol0 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x00, 0x01, 0x00, 0x00 }; // AMQP01000
        private static readonly byte[] protocol1 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x01, 0x01, 0x00, 0x00 }; // AMQP11000
        private static readonly byte[] protocol2 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x02, 0x01, 0x00, 0x00 }; // AMQP21000

        private static readonly byte[] malformedProtocol0 = new byte[] { 0x52, 0x4D, 0x51, 0x50, 0x00, 0x01, 0x00, 0x00 };
        private static readonly byte[] malformedProtocol1 = new byte[] { 0x52, 0x4D, 0x51, 0x50, 0x01, 0x01, 0x00, 0x00 };
        private static readonly byte[] malformedProtocol2 = new byte[] { 0x52, 0x4D, 0x51, 0x50, 0x02, 0x01, 0x00, 0x00 };

        private static readonly byte[] incorrectVerProtocol0 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x00, 0x01, 0x01, 0x00 };
        private static readonly byte[] incorrectVerProtocol1 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x01, 0x01, 0x01, 0x00 };
        private static readonly byte[] incorrectVerProtocol2 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x02, 0x01, 0x01, 0x00 };

        [Test]
        public void Accepts_Protocol_0_Version_1_0_0()
        {
            //If the requested protocol version is supported, the server MUST send its own protocol header with the
            // requested version to the socket, and then proceed according to the protocol definition.
            connection.HandleReceivedBuffer(new ByteBuffer(protocol0));

            Assert.AreEqual(1, socket.SentBuffers.Count);
            CollectionAssert.AreEqual(protocol0, socket.SentBuffers[0].Item1);
            Assert.AreEqual(ConnectionStateEnum.HDR_EXCH, connection.State);
            Assert.True(socket.IsNotClosed);
        }

        [Test]
        public void Can_Pipeline_Frames_After_Accepted_Protocol_Header()
        {
            //If the requested protocol version is supported, the server MUST send its own protocol header with the
            // requested version to the socket, and then proceed according to the protocol definition.
            var buffer = new ByteBuffer(protocol0, 0, protocol0.Length, protocol0.Length, true);
            AmqpFrameCodec.EncodeFrame(buffer, new Open(), 0);
            connection.HandleReceivedBuffer(buffer);

            Assert.GreaterOrEqual(socket.SentBuffers.Count, 1);
            CollectionAssert.AreEqual(protocol0, socket.SentBuffers[0].Item1);
            Assert.False(ConnectionStateEnum.HDR_EXCH.IsExpectingProtocolHeader());
        }

        [Test]
        public void Rejects_Protocol_TLS()
        {
            // If the requested protocol version is not supported, the server MUST send a protocol header with a supported
            // protocol version and then close the socket.

            connection.HandleReceivedBuffer(new ByteBuffer(protocol1));

            Assert.AreEqual(1, socket.SentBuffers.Count);
            CollectionAssert.AreEqual(protocol0, socket.SentBuffers[0].Item1);
            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
        }

        [Test]
        public void Rejects_Protocol_SASL()
        {
            // If the requested protocol version is not supported, the server MUST send a protocol header with a supported
            // protocol version and then close the socket.

            connection.HandleReceivedBuffer(new ByteBuffer(protocol2));

            Assert.AreEqual(1, socket.SentBuffers.Count);
            CollectionAssert.AreEqual(protocol0, socket.SentBuffers[0].Item1);
            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
        }

        [Test]
        public void Rejects_Malformed_Protocol_0()
        {
            // If the server cannot parse the protocol header, the server MUST send a valid protocol header with a supported
            // protocol version and then close the socket.

            connection.HandleReceivedBuffer(new ByteBuffer(malformedProtocol0));

            Assert.AreEqual(1, socket.SentBuffers.Count);
            CollectionAssert.AreEqual(protocol0, socket.SentBuffers[0].Item1);
            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
        }

        [Test]
        public void Rejects_Incorrect_Protocol_0_Version()
        {
            // If the requested protocol version is not supported, the server MUST send a protocol header with a supported
            // protocol version and then close the socket.

            connection.HandleReceivedBuffer(new ByteBuffer(incorrectVerProtocol0));

            Assert.AreEqual(1, socket.SentBuffers.Count);
            CollectionAssert.AreEqual(protocol0, socket.SentBuffers[0].Item1);
            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
        }

    }
}
