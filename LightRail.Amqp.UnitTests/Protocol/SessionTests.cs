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
    public class SessionTests : BaseProtocolTests
    {
        static Random ran = new Random();

        [Test]
        public void Must_Have_Open_Connection_Before_Begin_Frame()
        {
            //Given_Open_Connection();

            EncodeAndSend(new Begin(), 1);

            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
            Assert.AreEqual(1, socket.SentBufferFrames.Count);
            Assert.AreEqual(8, socket.SentBufferFrames[0].Item3); // 8 byte for proto header
        }

        [Test]
        public void Must_Have_Open_Connection_After_HDR_EXCH_Before_Begin_Frame()
        {
            Given_Exchanged_Headers();
            //Given_Open_Connection();

            EncodeAndSend(new Begin(), 1);

            Assert.AreEqual(ConnectionStateEnum.DISCARDING, connection.State);
            Assert.True(socket.Closed);
        }

        [Test]
        public void Unmapped_Channel_Should_Close_Connection()
        {
            Given_Open_Connection();
            var chan = (ushort)ran.Next(1, 100);

            EncodeAndSend(new Flow(), chan);

            Assert.AreEqual(ConnectionStateEnum.DISCARDING, connection.State);
            Assert.True(socket.Closed);
        }

        [Test]
        public void Open_Session_Should_Get_Open_Frame_With_Remote_Channel_Number()
        {
            Given_Open_Connection();
            var chan = (ushort)ran.Next(1, 100);

            EncodeAndSend(new Begin(), chan);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);

            var _begin = DecodeLastFrame() as Begin;
            Assert.IsNotNull(_begin);
            Assert.AreEqual(chan, _begin.RemoteChannel);
        }

        [Test]
        public void Can_Flow_Open_Session()
        {
            Given_Open_Connection();
            var chan = (ushort)ran.Next(1, 100);

            EncodeAndSend(new Begin(), chan);
            EncodeAndSend(new Flow(), chan);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);
        }

        [Test]
        public void Can_Echo_Flow_Open_Session()
        {
            Given_Open_Connection();
            var chan = (ushort)ran.Next(1, 100);

            EncodeAndSend(new Begin(), chan);
            ushort _mappedChan;
            DecodeLastFrame(out _mappedChan);

            EncodeAndSend(new Flow()
            {
                Echo = true,
            }, chan);

            ushort _chan;
            var _flow = DecodeLastFrame(out _chan) as Flow;
            Assert.IsNotNull(_flow);
            Assert.False(_flow.Echo);
            Assert.AreEqual(_mappedChan, _chan);
        }

        [Test]
        public void Can_End_Open_Session()
        {
            Given_Open_Connection();
            var chan = (ushort)ran.Next(1, 100);

            EncodeAndSend(new Begin(), chan);
            ushort _mappedChan;
            DecodeLastFrame(out _mappedChan);

            EncodeAndSend(new End(), chan);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);

            ushort _chan;
            var _end = DecodeLastFrame(out _chan) as End;
            Assert.IsNotNull(_end);
            Assert.AreEqual(_mappedChan, _chan);
        }

        [Test]
        public void Can_Open_Session_Reuse_Channel_Number()
        {
            Given_Open_Connection();
            var chan = (ushort)ran.Next(1, 100);

            EncodeAndSend(new Begin(), chan);
            EncodeAndSend(new End(), chan);
            EncodeAndSend(new Begin(), chan);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);

            var _begin = DecodeLastFrame() as Begin;
            Assert.IsNotNull(_begin);
            Assert.AreEqual(chan, _begin.RemoteChannel);
        }

        [Test]
        public void Cannot_Open_Same_Channel_Number()
        {
            Given_Open_Connection();
            var chan = (ushort)ran.Next(1, 100);

            EncodeAndSend(new Begin(), chan);

            ushort _mappedChan;
            DecodeLastFrame(out _mappedChan);

            EncodeAndSend(new Begin(), chan);

            ushort _chan2;
            var _end = DecodeLastFrame(out _chan2) as End;
            Assert.IsNotNull(_end);

            Assert.AreEqual(_mappedChan, _chan2);
        }

        [Test]
        public void Can_Open_Close_Individual_Channels()
        {
            Given_Open_Connection();
            var chan1 = (ushort)ran.Next(1, 100);
            var chan2 = (ushort)ran.Next(1, 100);

            EncodeAndSend(new Begin(), chan1);
            ushort _mappedChan1;
            DecodeLastFrame(out _mappedChan1);

            EncodeAndSend(new Begin(), chan2);
            ushort _mappedChan2;
            DecodeLastFrame(out _mappedChan2);

            EncodeAndSend(new End(), chan1);
            ushort _chan1;
            var _end1 = DecodeLastFrame(out _chan1) as End;
            Assert.IsNotNull(_end1);
            Assert.AreEqual(_mappedChan1, _chan1);

            EncodeAndSend(new End(), chan2);
            ushort _chan2;
            var _end2 = DecodeLastFrame(out _chan2) as End;
            Assert.IsNotNull(_end2);
            Assert.AreEqual(_mappedChan2, _chan2);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);
        }

        [Test]
        public void Can_Pipeline_Begin()
        {
            //Given_Open_Connection();
            var chan = (ushort)ran.Next(1, 100);

            var buffer = new ByteBuffer(defaultAcceptedProtocol, 0, defaultAcceptedProtocol.Length, defaultAcceptedProtocol.Length, true);
            AmqpFrameCodec.EncodeFrame(buffer, new Open(), 0);
            AmqpFrameCodec.EncodeFrame(buffer, new Begin(), chan);
            connection.HandleReceivedBuffer(buffer);

            Assert.AreEqual(ConnectionStateEnum.OPENED, connection.State);
            Assert.True(socket.IsNotClosed);
            Assert.AreEqual(3, socket.SentBufferFrames.Count); // proto, open, begin

            var _begin = DecodeLastFrame() as Begin;
            Assert.IsNotNull(_begin);
            Assert.AreEqual(chan, _begin.RemoteChannel);
        }
    }
}