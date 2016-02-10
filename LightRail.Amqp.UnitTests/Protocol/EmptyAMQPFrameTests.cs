using System.Threading;
using LightRail.Amqp.Types;
using NUnit.Framework;

namespace LightRail.Amqp.Protocol
{
    [TestFixture]
    public class EmptyAMQPFrameTests : BaseProtocolTests
    {
        // An AMQP frame with no body MAY be used to generate artificial traffic as needed to satisfy any negotiated idle
        // timeout interval.

        // If a peer needs to satisfy the need to send traffic to prevent idle timeout, and has nothing to send, it MAY send
        // an empty frame, i.e., a frame consisting solely of a frame header, with no frame body.Implementations MUST be
        // prepared to handle empty frames arriving on any valid channel, though implementations SHOULD use channel 0
        // when sending empty frames, and MUST use channel 0 if a maximum channel number has not yet been negotiated
        // (i.e., before an open frame has been received). Apart from this use, empty frames have no meaning.

        // Empty frames can only be sent after the open frame is sent. As they are a frame, they MUST NOT be sent after
        // the close frame has been sent.

        [Test]
        public void May_Send_Empty_AMQP_Frame()
        {
            Given_Open_Connection();

            var buffer = new ByteBuffer(8, true);
            AmqpCodec.EncodeFrame(buffer, null, 0);
            connection.HandleFrame(buffer);

            Assert.True(socket.IsNotClosed);
            CollectionAssert.IsEmpty(socket.WriteBuffer);
        }

        [Test]
        public void Receiving_Frame_Increments_Idle_Timer()
        {
            Given_Open_Connection();

            var last = connection.LastFrameReceivedDateTime;
            Thread.Sleep(10);
            EncodeAndSend(null);

            Assert.Greater(connection.LastFrameReceivedDateTime, last);
        }
    }
}
