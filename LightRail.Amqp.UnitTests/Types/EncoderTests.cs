using System;
using System.Collections;
using NUnit.Framework;

namespace LightRail.Amqp.Types
{
    [TestFixture]
    public class EncoderTests
    {
        byte[] nullEncoded = new byte[] { 0x40 };

        bool boolTrue = true;
        byte[] boolTrueEncoded = new byte[] { 0x41 };
        byte[] boolTrueEncodedArray = new byte[] { 0x56, 0x01 };

        bool boolFalse = false;
        byte[] boolFalseEncoded = new byte[] { 0x42 };
        byte[] boolFalseEncodedArray = new byte[] { 0x56, 0x00 };

        byte ubyteValue = 51;
        byte[] ubyteValueEncoded = new byte[] { 0x50, 0x33 };

        ushort ushortValue = 4660;
        byte[] ushortValueEncoded = new byte[] { 0x60, 0x12, 0x34 };

        uint uint0Value = 0;
        byte[] uint0ValueEncoded = new byte[] { 0x43 };

        uint uintSmallValue = 225;
        byte[] uintSmallValueEncoded = new byte[] { 0x52, 0xe1 };

        uint uintValue = 3989545112;
        byte[] uintValueEncoded = new byte[] { 0x70, 0xed, 0xcb, 0xa0, 0x98 };

        ulong ulong0Value = 0;
        byte[] ulong0ValueEncoded = new byte[] { 0x44 };

        ulong ulongSmallValue = 242;
        byte[] ulongSmallValueEncoded = new byte[] { 0x53, 0xf2 };

        ulong ulongValue = 1311768468857266328;
        byte[] ulongValueEncoded = new byte[] { 0x80, 0x12, 0x34, 0x56, 0x78, 0xed, 0xcb, 0xa0, 0x98 };

        sbyte byteValue = -20;
        byte[] byteValueEncoded = new byte[] { 0x51, 0xec };

        short shortValue = 22136;
        byte[] shortValueEncoded = new byte[] { 0x61, 0x56, 0x78 };

        int intSmallValue = -77;
        byte[] intSmallValueEncoded = new byte[] { 0x54, 0xb3 };

        int intValue = 1450744320;
        byte[] intValueEncoded = new byte[] { 0x71, 0x56, 0x78, 0x9a, 0x00 };

        long longSmallValue = 34;
        byte[] longSmallValueEncoded = new byte[] { 0x55, 0x22 };

        long longValue = -111111111111;
        byte[] longValueEncoded = new byte[] { 0x81, 0xff, 0xff, 0xff, 0xe6, 0x21, 0x42, 0xfe, 0x39 };

        float floatValue = -88.88f;
        byte[] floatValueEncoded = new byte[] { 0x72, 0xc2, 0xb1, 0xc2, 0x8f };

        double doubleValue = 111111111111111.22222222222;
        byte[] doubleValueEncoded = new byte[] { 0x82, 0x42, 0xd9, 0x43, 0x84, 0x93, 0xbc, 0x71, 0xce };

        char charValue = 'A';
        byte[] charValueEncoded = new byte[] { 0x73, 0x00, 0x00, 0x00, 0x41 };

        DateTime dtValue = DateTime.Parse("2008-11-01T19:35:00.0000000Z").ToUniversalTime();
        byte[] dtValueEncoded = new byte[] { 0x83, 0x00, 0x00, 0x01, 0x1d, 0x59, 0x8d, 0x1e, 0xa0 };

        Guid uuidValue = Guid.Parse("f275ea5e-0c57-4ad7-b11a-b20c563d3b71");
        byte[] uuidValueEncoded = new byte[] { 0x98, 0xf2, 0x75, 0xea, 0x5e, 0x0c, 0x57, 0x4a, 0xd7, 0xb1, 0x1a, 0xb2, 0x0c, 0x56, 0x3d, 0x3b, 0x71 };

        byte[] bin8Value = new byte[56];
        byte[] bin32Value = new byte[500];
        byte[] bin8ValueEncoded = new byte[1 + 1 + 56];
        byte[] bin32ValueEncoded = new byte[1 + 4 + 500];

        string strValue = "amqp";
        byte[] str8Utf8ValueEncoded = new byte[] { 0xa1, 0x04, 0x61, 0x6d, 0x71, 0x70 };
        byte[] str32Utf8ValueEncoded = new byte[] { 0xb1, 0x00, 0x00, 0x00, 0x04, 0x61, 0x6d, 0x71, 0x70 };
        byte[] sym8ValueEncoded = new byte[] { 0xa3, 0x04, 0x61, 0x6d, 0x71, 0x70 };
        byte[] sym32ValueEncoded = new byte[] { 0xb3, 0x00, 0x00, 0x00, 0x04, 0x61, 0x6d, 0x71, 0x70 };

        DescribedType described1 = CreateDescribed(100, null, "value1");
        DescribedType described2 = CreateDescribed(0, "v2", (float)3.14159);
        DescribedType described3 = CreateDescribed(0, "v3", Guid.NewGuid());
        DescribedType described4 = CreateDescribed(ulong.MaxValue, null, new AmqpList() { 100, "200" });
        DescribedType described5 = CreateDescribed(12345L, "", new string[] { "string1", "string2", "string3", "string4" });

        static DescribedValue<T> CreateDescribed<T>(ulong code, string symbol, T value)
        {
            return new DescribedValue<T>(new Descriptor(code, symbol), value);
        }

        public EncoderTests()
        {
            Random random = new Random();
            for (int i = 0; i < bin8Value.Length; i++) bin8Value[i] = (byte)random.Next(255);
            for (int i = 0; i < bin32Value.Length; i++) bin32Value[i] = (byte)random.Next(255);
            bin8ValueEncoded[0] = 0xa0;
            bin8ValueEncoded[1] = 56;
            bin32ValueEncoded[0] = 0xb0;
            bin32ValueEncoded[1] = 0x00;
            bin32ValueEncoded[2] = 0x00;
            bin32ValueEncoded[3] = 0x01;
            bin32ValueEncoded[4] = 0xf4;
            Buffer.BlockCopy(bin8Value, 0, bin8ValueEncoded, 2, bin8Value.Length);
            Buffer.BlockCopy(bin32Value, 0, bin32ValueEncoded, 5, bin32Value.Length);
        }

        [Test]
        public void AmqpCodecSingleValueTest()
        {
            byte[] workBuffer = new byte[2048];

            RunSingleValueTest(workBuffer, boolTrue, boolTrueEncoded, "Boolean value is not true.");
            RunSingleValueTest(workBuffer, boolFalse, boolFalseEncoded, "Boolean value is not false.");
            RunSingleValueTest(workBuffer, ubyteValue, ubyteValueEncoded, "UByte value is not equal.");
            RunSingleValueTest(workBuffer, ushortValue, ushortValueEncoded, "UShort value is not equal.");
            RunSingleValueTest(workBuffer, uint0Value, uint0ValueEncoded, "UInt0 value is not equal.");
            RunSingleValueTest(workBuffer, uintSmallValue, uintSmallValueEncoded, "UIntSmall value is not equal.");
            RunSingleValueTest(workBuffer, uintValue, uintValueEncoded, "UInt value is not equal.");
            RunSingleValueTest(workBuffer, ulong0Value, ulong0ValueEncoded, "ULong0 value is not equal.");
            RunSingleValueTest(workBuffer, ulongSmallValue, ulongSmallValueEncoded, "ULongSmall value is not equal.");
            RunSingleValueTest(workBuffer, ulongValue, ulongValueEncoded, "ULong value is not equal.");
            RunSingleValueTest(workBuffer, byteValue, byteValueEncoded, "Byte value is not equal.");
            RunSingleValueTest(workBuffer, shortValue, shortValueEncoded, "Short value is not equal.");
            RunSingleValueTest(workBuffer, intSmallValue, intSmallValueEncoded, "Int small value is not equal.");
            RunSingleValueTest(workBuffer, intValue, intValueEncoded, "Int value is not equal.");
            RunSingleValueTest(workBuffer, longSmallValue, longSmallValueEncoded, "Long small value is not equal.");
            RunSingleValueTest(workBuffer, longValue, longValueEncoded, "Long value is not equal.");
            RunSingleValueTest(workBuffer, floatValue, floatValueEncoded, "Float value is not equal.");
            RunSingleValueTest(workBuffer, doubleValue, doubleValueEncoded, "Double value is not equal.");
            RunSingleValueTest(workBuffer, charValue, charValueEncoded, "Char value is not equal.");
            RunSingleValueTest(workBuffer, dtValue, dtValueEncoded, "Timestamp value is not equal.");
            RunSingleValueTest(workBuffer, uuidValue, uuidValueEncoded, "Uuid value is not equal.");
            RunSingleValueTest(workBuffer, bin8Value, bin8ValueEncoded, "Binary8 value is not equal.");
            RunSingleValueTest(workBuffer, bin32Value, bin32ValueEncoded, "Binary32 value is not equal.");
            RunSingleValueTest(workBuffer, (Symbol)strValue, sym8ValueEncoded, "Symbol8 string value is not equal.");
            RunSingleValueTest(workBuffer, strValue, str8Utf8ValueEncoded, "UTF8 string8 string value is not equal.");

            // symbol 32
            Symbol symbol32v = AmqpCodec.DecodeObject<Symbol>(new ByteBuffer(sym32ValueEncoded, 0, sym32ValueEncoded.Length, sym32ValueEncoded.Length));
            Assert.AreEqual((string)symbol32v, strValue, "Symbol32 string value is not equal.");

            // string 32 UTF8
            string str32Utf8 = AmqpCodec.DecodeObject<string>(new ByteBuffer(str32Utf8ValueEncoded, 0, str32Utf8ValueEncoded.Length, str32Utf8ValueEncoded.Length));
            Assert.AreEqual(str32Utf8, strValue, "UTF8 string32 string value is not equal.");
        }

        [Test]
        public void AmqpCodecListTest()
        {
            byte[] workBuffer = new byte[4096];
            ByteBuffer buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);
            string strBig = new string('A', 512);

            var list = new AmqpList();
            list.Add(boolTrue);
            list.Add(boolFalse);
            list.Add(ubyteValue);
            list.Add(ushortValue);
            list.Add(uintValue);
            list.Add(ulongValue);
            list.Add(byteValue);
            list.Add(shortValue);
            list.Add(intValue);
            list.Add(longValue);
            list.Add(null);
            list.Add(floatValue);
            list.Add(doubleValue);
            list.Add(charValue);
            list.Add(dtValue);
            list.Add(uuidValue);
            list.Add(bin8ValueEncoded);
            list.Add(bin32ValueEncoded);
            list.Add((Symbol)null);
            list.Add(new Symbol(strValue));
            list.Add(new Symbol(strBig));
            list.Add(strValue);
            list.Add(strBig);
            list.Add(described1);
            list.Add(described2);
            list.Add(described3);
            list.Add(described4);

            AmqpCodec.EncodeObject(buffer, list);

            // make sure the size written is correct (it has to be List32)
            // the first byte is FormatCode.List32
            int listSize = (workBuffer[1] << 24) | (workBuffer[2] << 16) | (workBuffer[3] << 8) | workBuffer[4];
            Assert.AreEqual(buffer.LengthAvailableToRead - 5, listSize);

            IList decList = AmqpCodec.DecodeObject<AmqpList>(buffer);
            int index = 0;

            Assert.IsTrue(decList[index++].Equals(true), "Boolean true expected.");
            Assert.IsTrue(decList[index++].Equals(false), "Boolean false expected.");
            Assert.IsTrue(decList[index++].Equals(ubyteValue), "UByte value not equal.");
            Assert.IsTrue(decList[index++].Equals(ushortValue), "UShort value not equal.");
            Assert.IsTrue(decList[index++].Equals(uintValue), "UInt value not equal.");
            Assert.IsTrue(decList[index++].Equals(ulongValue), "ULong value not equal.");
            Assert.IsTrue(decList[index++].Equals(byteValue), "Byte value not equal.");
            Assert.IsTrue(decList[index++].Equals(shortValue), "Short value not equal.");
            Assert.IsTrue(decList[index++].Equals(intValue), "Int value not equal.");
            Assert.IsTrue(decList[index++].Equals(longValue), "Long value not equal.");
            Assert.IsTrue(decList[index++] == null, "Null object expected.");
            Assert.IsTrue(decList[index++].Equals(floatValue), "Float value not equal.");
            Assert.IsTrue(decList[index++].Equals(doubleValue), "Double value not equal.");
            Assert.IsTrue(decList[index++].Equals(charValue), "Char value not equal.");
            Assert.IsTrue(decList[index++].Equals(dtValue), "TimeStamp value not equal.");
            Assert.IsTrue(decList[index++].Equals(uuidValue), "Uuid value not equal.");

            byte[] bin8 = (byte[])decList[index++];
            EnsureEqual(bin8, 0, bin8.Length, bin8ValueEncoded, 0, bin8ValueEncoded.Length);
            byte[] bin32 = (byte[])decList[index++];
            EnsureEqual(bin32, 0, bin32.Length, bin32ValueEncoded, 0, bin32ValueEncoded.Length);

            Assert.IsTrue(decList[index++] == null, "Null symbol expected.");
            Symbol symDecode = (Symbol)decList[index++];
            Assert.IsTrue(symDecode.Equals((Symbol)strValue), "AmqpSymbol value not equal.");
            symDecode = (Symbol)decList[index++];
            Assert.IsTrue(symDecode.Equals((Symbol)strBig), "AmqpSymbol value (big) not equal.");

            string strDecode = (string)decList[index++];
            Assert.IsTrue(strDecode.Equals(strValue), "string value not equal.");
            strDecode = (string)decList[index++];
            Assert.IsTrue(strDecode.Equals(strBig), "string value (big) not equal.");

            //Assert.AreEqual(described1, (DescribedType)decList[index++]);
            //Assert.AreEqual(described2, (DescribedType)decList[index++]);
            //Assert.AreEqual(described3, (DescribedType)decList[index++]);
            //Assert.AreEqual(described4, (DescribedType)decList[index++]);

            DescribedType described;

            described = (DescribedType)decList[index++];
            Assert.IsTrue(described.Descriptor.Equals(described1.Descriptor), "Described value 1 descriptor is different");
            Assert.IsTrue(described.Equals(described1), "Described value 1 value is different");

            described = (DescribedType)decList[index++];
            Assert.IsTrue(described.Descriptor.Equals(described2.Descriptor), "Described value 2 descriptor is different");
            Assert.IsTrue(described.Equals(described2), "Described value 2 value is different");

            described = (DescribedType)decList[index++];
            Assert.IsTrue(described.Descriptor.Equals(described3.Descriptor), "Described value 3 descriptor is different");
            Assert.IsTrue(described.Equals(described3), "Described value 3 value is different");

            described = (DescribedType)decList[index++];
            Assert.IsTrue(described.Descriptor.Equals(described4.Descriptor), "Described value 4 descriptor is different");
            EnsureEqual(((DescribedValue<AmqpList>)described).Value, ((DescribedValue<AmqpList>)described4).Value);
        }

        [Test]
        public void AmqpCodecList0Test()
        {
            byte[] list0Bin = new byte[] { 0x45 };
            byte[] workBuffer = new byte[128];
            ByteBuffer buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);

            var list0 = new AmqpList();
            AmqpCodec.EncodeObject(buffer, list0);
            EnsureEqual(list0Bin, 0, list0Bin.Length, buffer.Buffer, buffer.ReadOffset, buffer.LengthAvailableToRead);

            IList list0v = (IList)AmqpCodec.DecodeBoxedObject(buffer);
            Assert.AreEqual(0, list0v.Count, "The list should contain 0 items.");
        }


        [Test]
        public void AmqpCodecArrayTest()
        {
            byte[] workBuffer = new byte[4096];

            RunArrayTest<int>(workBuffer, i => i, 100);
            RunArrayTest<string>(workBuffer, i => "string" + i, 16);
        }

        [Test]
        public void AmqpCodecMapTest()
        {
            byte[] workBuffer = new byte[4096];
            ByteBuffer buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);
            string strBig = new string('A', 512);

            Map map = new Map();
            map.Add(new Symbol("boolTrue"), boolTrue);
            map.Add(new Symbol("boolFalse"), boolFalse);
            map.Add(new Symbol("ubyte"), ubyteValue);
            map.Add(new Symbol("ushort"), ushortValue);
            map.Add(new Symbol("uint"), uintValue);
            map.Add(new Symbol("ulong"), ulongValue);
            map.Add(new Symbol("byte"), byteValue);
            map.Add(new Symbol("short"), shortValue);
            map.Add(new Symbol("int"), intValue);
            map.Add(new Symbol("long"), longValue);
            map.Add(new Symbol("null"), null);
            map.Add(new Symbol("float"), floatValue);
            map.Add(new Symbol("double"), doubleValue);
            map.Add(new Symbol("char"), charValue);
            map.Add(new Symbol("datetime"), dtValue);
            map.Add(new Symbol("uuid"), uuidValue);
            map.Add(new Symbol("binaryNull"), null);
            map.Add(new Symbol("binary8"), bin8ValueEncoded);
            map.Add(new Symbol("binary32"), bin32ValueEncoded);
            map.Add(new Symbol("symbolNull"), (Symbol)null);
            map.Add(new Symbol("symbol8"), new Symbol(strValue));
            map.Add(new Symbol("symbol32"), new Symbol(strBig));
            map.Add(new Symbol("string8"), strValue);
            map.Add(new Symbol("string32"), strBig);
            map.Add(new Symbol("described1"), described1);

            AmqpCodec.EncodeObject(buffer, map);

            // make sure the size written is correct (it has to be Map32)
            // the first byte is FormatCode.Map32
            int mapSize = (workBuffer[1] << 24) | (workBuffer[2] << 16) | (workBuffer[3] << 8) | workBuffer[4];
            Assert.AreEqual(buffer.LengthAvailableToRead - 5, mapSize);

            Map decMap = (Map)AmqpCodec.DecodeBoxedObject(buffer);

            Assert.IsTrue(decMap[new Symbol("boolTrue")].Equals(true), "Boolean true expected.");
            Assert.IsTrue(decMap[new Symbol("boolFalse")].Equals(false), "Boolean false expected.");
            Assert.IsTrue(decMap[new Symbol("ubyte")].Equals(ubyteValue), "UByte value not equal.");
            Assert.IsTrue(decMap[new Symbol("ushort")].Equals(ushortValue), "UShort value not equal.");
            Assert.IsTrue(decMap[new Symbol("uint")].Equals(uintValue), "UInt value not equal.");
            Assert.IsTrue(decMap[new Symbol("ulong")].Equals(ulongValue), "ULong value not equal.");
            Assert.IsTrue(decMap[new Symbol("byte")].Equals(byteValue), "Byte value not equal.");
            Assert.IsTrue(decMap[new Symbol("short")].Equals(shortValue), "Short value not equal.");
            Assert.IsTrue(decMap[new Symbol("int")].Equals(intValue), "Int value not equal.");
            Assert.IsTrue(decMap[new Symbol("long")].Equals(longValue), "Long value not equal.");
            Assert.IsTrue(decMap[new Symbol("null")] == null, "Null object expected.");
            Assert.IsTrue(decMap[new Symbol("float")].Equals(floatValue), "Float value not equal.");
            Assert.IsTrue(decMap[new Symbol("double")].Equals(doubleValue), "Double value not equal.");
            Assert.IsTrue(decMap[new Symbol("char")].Equals(charValue), "Char value not equal.");
            Assert.IsTrue(decMap[new Symbol("datetime")].Equals(dtValue), "TimeStamp value not equal.");
            Assert.IsTrue(decMap[new Symbol("uuid")].Equals(uuidValue), "Uuid value not equal.");
            Assert.IsTrue(decMap[new Symbol("binaryNull")] == null, "Null binary expected.");
            byte[] bin8 = (byte[])decMap[new Symbol("binary8")];
            EnsureEqual(bin8, 0, bin8.Length, bin8ValueEncoded, 0, bin8ValueEncoded.Length);
            byte[] bin32 = (byte[])decMap[new Symbol("binary32")];
            EnsureEqual(bin32, 0, bin32.Length, bin32ValueEncoded, 0, bin32ValueEncoded.Length);

            Assert.Null(decMap[new Symbol("symbolNull")], "Null symbol expected.");
            Symbol symDecode = (Symbol)decMap[new Symbol("symbol8")];
            Assert.AreEqual(symDecode, (Symbol)strValue, "AmqpSymbol value not equal.");
            symDecode = (Symbol)decMap[new Symbol("symbol32")];
            Assert.AreEqual(symDecode, (Symbol)strBig, "AmqpSymbol value (big) not equal.");

            string strDecode = (string)decMap[new Symbol("string8")];
            Assert.AreEqual(strDecode, strValue, "string value not equal.");
            strDecode = (string)decMap[new Symbol("string32")];
            Assert.AreEqual(strDecode, strBig, "string value (big) not equal.");

            DescribedType described = (DescribedType)decMap[new Symbol("described1")];
            Assert.AreEqual(described1.Descriptor, described.Descriptor, "Described value 1 descriptor is different");
            Assert.AreEqual(described1, described, "Described value 1 value is different");
        }

        [Test]
        public void AmqpCodecDescribedValueTest()
        {
            byte[] workBuffer = new byte[2048];

            Action<object, object, byte[]> runTest = (d, v, b) =>
            {
            };

            runTest(0, "uri", workBuffer);
            runTest(long.MaxValue, (Symbol)"abcd", workBuffer);
            runTest("descriptor", new AmqpList() { 0, "x" }, workBuffer);
            runTest((Symbol)"null", null, workBuffer);
        }

        static void RunDescribedValueTest<T>(object descriptor, T value, byte[] workBuffer)
        {
            var dv = new DescribedValue<T>(new Descriptor(descriptor), value);
            ByteBuffer buffer;
            AmqpCodec.EncodeObject(buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length), dv);
            var dv2 = (DescribedValue<T>)AmqpCodec.DecodeObject<DescribedValue<T>>(buffer);

            Assert.AreEqual(dv.Descriptor, dv2.Descriptor);
            if (dv.Value == null)
            {
                Assert.IsNull(dv2.Value);
            }
            else if (dv.Value.GetType() == typeof(List))
            {
                EnsureEqual((IList)dv.Value, (IList)dv2.Value);
            }
            else
            {
                Assert.AreEqual(dv.Value, dv2.Value);
            }
        }

        static void RunSingleValueTest<T>(byte[] workBuffer, T value, byte[] result, string message)
        {
            var buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);

            AmqpCodec.EncodeObject(buffer, value);

            EnsureEqual(result, 0, result.Length, buffer.Buffer, buffer.ReadOffset, buffer.LengthAvailableToRead);

            T decodeValue = AmqpCodec.DecodeObject<T>(new ByteBuffer(result, 0, result.Length, result.Length));

            if (typeof(T) == typeof(byte[]))
            {
                byte[] b1 = (byte[])(object)value;
                byte[] b2 = (byte[])(object)decodeValue;
                EnsureEqual(b1, 0, b1.Length, b2, 0, b2.Length);
            }
            else
            {
                Assert.AreEqual(value, decodeValue, message);
            }

            System.Diagnostics.Trace.WriteLine($"Test Passed For Type={typeof(T).FullName}");
        }

        static void RunArrayTest<T>(byte[] workBuffer, Func<int, T> generator, int count)
        {
            ByteBuffer buffer = new ByteBuffer(workBuffer, 0, 0, workBuffer.Length);

            T[] array = new T[count];
            for (int i = 0; i < count; i++) array[i] = generator(i);
            AmqpCodec.EncodeObject(buffer, array);

            var array2 = (T[])AmqpCodec.DecodeBoxedObject(buffer);
            Assert.AreEqual(array.Length, array2.Length);
        }


        static void EnsureEqual(byte[] data1, int offset1, int count1, byte[] data2, int offset2, int count2)
        {
            Assert.AreEqual(count1, count2, "Count is not equal.");
            for (int i = 0; i < count1; ++i)
            {
                byte b1 = data1[offset1 + i];
                byte b2 = data2[offset2 + i];
                Assert.AreEqual(b1, b2, string.Format("The {0}th byte is not equal ({1} != {2}).", i, b1, b2));
            }
        }

        static void EnsureEqual(IList list1, IList list2)
        {
            if (list1 == null && list2 == null)
            {
                return;
            }

            Assert.IsTrue(list1 != null && list2 != null, "One of the list is null");

            Assert.AreEqual(list1.Count, list2.Count, "Count not equal.");
            for (int i = 0; i < list1.Count; i++)
            {
                EnsureEqual(list1[i], list2[i]);
            }
        }

        static void EnsureEqual(object x, object y)
        {
            if (x == null && y == null)
            {
                return;
            }

            Assert.IsTrue(x != null && y != null);
            Assert.AreEqual(x.GetType(), y.GetType());

            if (x is IList)
            {
                EnsureEqual((IList)x, (IList)y);
            }
            else if (x is Map)
            {
                EnsureEqual((Map)x, (Map)y);
            }
            else if (x is byte[])
            {
                byte[] b1 = (byte[])x;
                byte[] b2 = (byte[])y;
                EnsureEqual(b1, 0, b1.Length, b2, 0, b2.Length);
            }
            else if (x is DateTime)
            {
                EnsureEqual((DateTime)x, (DateTime)y);
            }
            else
            {
                Assert.AreEqual(x, y);
            }
        }
    }
}
