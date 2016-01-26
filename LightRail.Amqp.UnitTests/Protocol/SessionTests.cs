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
        [Test]
        public void Must_Have_Open_Connection_Before_Begin_Frame()
        {
            //Given_Open_Connection();

            EncodeAndSend(new Begin());

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

            EncodeAndSend(new Begin());

            Assert.AreEqual(ConnectionStateEnum.END, connection.State);
            Assert.True(socket.Closed);
        }
    }
}