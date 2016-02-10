using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
using NUnit.Framework;

namespace LightRail.Amqp.Types
{
    [TestFixture]
    public class DescribedListCodecTests
    {
        static Random randNum = new Random();

        [Test]
        public void Can_Encode_And_Decode_Simple_DescribedList()
        {
            var buffer = new ByteBuffer(1024, false);

            var value = new Open();
            value.ContainerID = Guid.NewGuid().ToString();
            value.IdleTimeOut = (uint)randNum.Next(0, 1000);
            value.ChannelMax = (ushort)randNum.Next(0, 1000);

            AmqpCodec.EncodeObject(buffer, value);

            var decodedValue = AmqpCodec.DecodeObject<Open>(buffer);

            Assert.AreEqual(value.ContainerID, decodedValue.ContainerID);
            Assert.AreEqual(value.IdleTimeOut, decodedValue.IdleTimeOut);
            Assert.AreEqual(value.ChannelMax, decodedValue.ChannelMax);
        }

        [Test]
        public void Can_Encode_And_Decode_Nested_DescribedList()
        {
            var buffer = new ByteBuffer(1024, false);

            var value = new Attach();
            value.Source = new Source();
            value.Source.Durable = (uint)randNum.Next(0, 1000);

            AmqpCodec.EncodeObject(buffer, value);

            var decodedValue = AmqpCodec.DecodeObject<Attach>(buffer);

            Assert.NotNull(decodedValue.Source);
            Assert.AreEqual(value.Source.Durable, decodedValue.Source.Durable);
        }

        [Test]
        public void Can_Encode_And_Decode_Multi_Nested_DescribedList()
        {
            var buffer = new ByteBuffer(1024, false);

            var value = new Attach();
            value.Source = new Source();
            value.Source.DefaultOutcome = new Rejected();
            ((Rejected)value.Source.DefaultOutcome).Error = new Error();
            ((Rejected)value.Source.DefaultOutcome).Error.Condition = Guid.NewGuid().ToString();
            ((Rejected)value.Source.DefaultOutcome).Error.Description = Guid.NewGuid().ToString();

            AmqpCodec.EncodeObject(buffer, value);

            var decodedValue = AmqpCodec.DecodeObject<Attach>(buffer);

            Assert.NotNull(decodedValue.Source);
            Assert.NotNull(decodedValue.Source.DefaultOutcome as Rejected);
            Assert.NotNull(((Rejected)decodedValue.Source.DefaultOutcome).Error);
            Assert.AreEqual(((Rejected)value.Source.DefaultOutcome).Error.Condition, ((Rejected)decodedValue.Source.DefaultOutcome).Error.Condition);
            Assert.AreEqual(((Rejected)value.Source.DefaultOutcome).Error.Description, ((Rejected)decodedValue.Source.DefaultOutcome).Error.Description);
        }

        [Test]
        public void Can_Encode_And_Decode_Nested_Polymorphic_DescribedList()
        {
            var buffer = new ByteBuffer(1024, false);

            var value = new Transfer();
            value.State = new Received()
            {
                SectionNumber = (uint)randNum.Next(0, 1000),
                SectionOffset = (uint)randNum.Next(0, 1000),
            };

            AmqpCodec.EncodeObject(buffer, value);

            var decodedValue = AmqpCodec.DecodeObject<Transfer>(buffer);
            var decodedState = decodedValue.State as Received;

            Assert.NotNull(decodedState);
            Assert.AreEqual(((Received)value.State).SectionNumber, decodedState.SectionNumber);
            Assert.AreEqual(((Received)value.State).SectionOffset, decodedState.SectionOffset);
        }

        [Test]
        public void Can_Encode_And_Decode_MulipleValue_Fields_SingleValue()
        {
            var buffer = new ByteBuffer(1024, false);

            var value = new Open();
            value.DesiredCapabilities = new Symbol[] { Guid.NewGuid().ToString() };

            AmqpCodec.EncodeObject(buffer, value);

            var decodedValue = AmqpCodec.DecodeObject<Open>(buffer);

            CollectionAssert.AreEqual(value.DesiredCapabilities, decodedValue.DesiredCapabilities);
        }

        [Test]
        public void Can_Encode_And_Decode_MulipleValue_Fields_SingleValue_Alt_Format()
        {
            var wireFrame = new byte[]
            {
0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
0x00, 0x10, 0xd0, 0x00, 0x00, 0x00, 0x3c, 0x00,
0x00, 0x00, 0x09, 0xb1, 0x00, 0x00, 0x00, 0x00,
0x40, 0x70, 0xff, 0xff, 0xff, 0xff, 0x60, 0xff,
0xff, 0x40, 0x40, 0x40, 0x40,
0xa3, 0x24, 0x30, 0x64, 0x36, 0x36, 0x32, 0x65,
0x64, 0x35, 0x2d, 0x33, 0x30, 0x62, 0x38, 0x2d,
0x34, 0x32, 0x62, 0x39, 0x2d, 0x61, 0x61, 0x37,
0x30, 0x2d, 0x66, 0x33, 0x30, 0x66, 0x30, 0x37,
0x66, 0x38, 0x61, 0x34, 0x62, 0x34, 0x40, 0x38,
0x61, 0x34, 0x62, 0x34,
            };

            var buffer = new ByteBuffer(wireFrame);

            var decodedValue = AmqpCodec.DecodeObject<Open>(buffer);

            Assert.NotNull(decodedValue.DesiredCapabilities);
            Assert.AreEqual(1, decodedValue.DesiredCapabilities.Length);
            Assert.IsNotNullOrEmpty(decodedValue.DesiredCapabilities[0]);
        }

        [Test]
        public void Can_Encode_And_Decode_MulipleValue_Fields_AsArray()
        {
            var buffer = new ByteBuffer(1024, false);

            var value = new Open();
            value.DesiredCapabilities = new Symbol[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), };

            AmqpCodec.EncodeObject(buffer, value);

            var decodedValue = AmqpCodec.DecodeObject<Open>(buffer);

            CollectionAssert.AreEqual(value.DesiredCapabilities, decodedValue.DesiredCapabilities);
        }
    }
}