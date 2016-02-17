using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
using NUnit.Framework;

namespace LightRail.Amqp.Types
{
    [TestFixture]
    public class CaclulateMinimumFrameSize
    {
        [Test]
        public void Calc_Min_Transfer_Frame_Size()
        {
            var buffer = new ByteBuffer(1, true);
            AmqpCodec.EncodeFrame(buffer, new Transfer
            {
                DeliveryId = 123456,
                DeliveryTag = Guid.NewGuid().ToByteArray(),
                Handle = 123456,
                Settled = false,
                More = false,
                MessageFormat = 0,
                Aborted = false,
                ReceiverSettlementMode = 1,
            }, 1);

            Console.WriteLine("Transfer Frame Size = " + buffer.LengthAvailableToRead + " bytes");

            Assert.Pass();
        }
    }
}
