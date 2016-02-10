using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace LightRail.Amqp.Types
{
    [TestFixture]
    public class LambdaExpressionTests
    {
        static Random randNum = new Random();

        [Test]
        public void Can_Decode_And_Assign_Int_Without_Boxing()
        {
            // write to buffer
            var buffer = new ByteBuffer(1024, true);
            int value = randNum.Next(0, 1000);
            Encoder.WriteInt(buffer, value, false);

            // compile lambda expression
            var compiled = CompilePropertyDecodeExpression(typeof(TestCompositeClass), "IntValue", "ReadInt");

            // execute
            var instance = new TestCompositeClass();
            var formatCode = AmqpCodec.DecodeFormatCode(buffer);
            compiled(instance, buffer, formatCode);
            Assert.AreEqual(value, instance.IntValue);
        }

        [Test]
        public void Can_Decode_And_Assign_NullableInt_Without_Boxing()
        {
            // write to buffer
            var buffer = new ByteBuffer(1024, true);
            int value = randNum.Next(0, 1000);
            Encoder.WriteInt(buffer, value, false);

            // compile lambda expression
            var compiled = CompilePropertyDecodeExpression(typeof(TestCompositeClass), "NullableIntValue", "ReadInt");

            // execute
            var instance = new TestCompositeClass();
            var formatCode = AmqpCodec.DecodeFormatCode(buffer);
            compiled(instance, buffer, formatCode);
            Assert.AreEqual(value, instance.NullableIntValue);
        }

        [Test]
        public void Can_Decode_And_Assign_Int_Without_Boxing_Using_Generic_DecodeObject()
        {
            // write to buffer
            var buffer = new ByteBuffer(1024, true);
            int value = randNum.Next(0, 1000);
            Encoder.WriteInt(buffer, value, false);

            // compile lambda expression
            var compiled = CompileGenericPropertyDecodeExpression(typeof(TestCompositeClass), "IntValue");

            // execute
            var instance = new TestCompositeClass();
            var formatCode = AmqpCodec.DecodeFormatCode(buffer);
            compiled(instance, buffer, formatCode);
            Assert.AreEqual(value, instance.IntValue);
        }

        [Test]
        public void Can_Decode_And_Assign_NullableInt_Without_Boxing_Using_Generic_DecodeObject()
        {
            // write to buffer
            var buffer = new ByteBuffer(1024, true);
            int value = randNum.Next(0, 1000);
            Encoder.WriteInt(buffer, value, false);

            // compile lambda expression
            var compiled = CompileGenericPropertyDecodeExpression(typeof(TestCompositeClass), "NullableIntValue");

            // execute
            var instance = new TestCompositeClass();
            var formatCode = AmqpCodec.DecodeFormatCode(buffer);
            compiled(instance, buffer, formatCode);
            Assert.AreEqual(value, instance.NullableIntValue);
        }

        [Test]
        public void Can_Encode_Int_Without_Boxing_Using_Generic_DecodeObject()
        {
            var buffer = new ByteBuffer(1024, true);
            var instance = new TestCompositeClass();
            instance.IntValue = randNum.Next(300, 1000);

            // compile lambda expression
            var compiled = CompileGenericPropertyEncodeExpression(typeof(TestCompositeClass), "IntValue");

            // execute
            compiled(instance, buffer);

            // assert
            var formatCode = AmqpBitConverter.ReadByte(buffer);
            Assert.AreEqual(FormatCode.Int, formatCode);
            var value = AmqpBitConverter.ReadInt(buffer);
            Assert.AreEqual(instance.IntValue, value);
        }

        [Test]
        public void Can_Encode_NullableInt_Without_Boxing_Using_Generic_DecodeObject()
        {
            var buffer = new ByteBuffer(1024, true);
            var instance = new TestCompositeClass();
            instance.NullableIntValue = randNum.Next(300, 1000);

            // compile lambda expression
            var compiled = CompileGenericPropertyEncodeExpression(typeof(TestCompositeClass), "NullableIntValue");

            // execute
            compiled(instance, buffer);

            // assert
            var formatCode = AmqpBitConverter.ReadByte(buffer);
            Assert.AreEqual(FormatCode.Int, formatCode);
            var value = AmqpBitConverter.ReadInt(buffer);
            Assert.AreEqual(instance.NullableIntValue, value);
        }

        private static Action<object, ByteBuffer, byte> CompilePropertyDecodeExpression(Type instanceType, string propertyName, string decodeMethod)
        {
            var propertyInfo = instanceType.GetProperty(propertyName);

            // delegate parameters
            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var bufferParameter = Expression.Parameter(typeof(ByteBuffer), "buffer");
            var formatCodeParameter = Expression.Parameter(typeof(byte), "formatCode");

            // (T)instance
            var instanceCast = Expression.Convert(instanceParameter, instanceType);

            // ((T)instance).[PropertyName]
            var propertyExpression = Expression.MakeMemberAccess(instanceCast, propertyInfo);

            // Encoder.[DecodeMethod](buffer, formatCode)
            var readIntMethod = Expression.Call(typeof(Encoder).GetMethod(decodeMethod), bufferParameter, formatCodeParameter);

            // ((T)instance).[PropertyName] = ([PropertyType])Encoder.[DecodeMethod](buffer, formatCode);
            var assignment = Expression.Assign(propertyExpression, Expression.Convert(readIntMethod, propertyInfo.PropertyType));

            // compile
            return Expression.Lambda<Action<object, ByteBuffer, byte>>(assignment, instanceParameter, bufferParameter, formatCodeParameter).Compile();
        }

        private static Action<object, ByteBuffer, byte> CompileGenericPropertyDecodeExpression(Type instanceType, string propertyName)
        {
            var propertyInfo = instanceType.GetProperty(propertyName);

            // delegate parameters
            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var bufferParameter = Expression.Parameter(typeof(ByteBuffer), "buffer");
            var formatCodeParameter = Expression.Parameter(typeof(byte), "formatCode");

            // (T)instance
            var instanceCast = Expression.Convert(instanceParameter, instanceType);

            // ((T)instance).[PropertyName]
            var propertyExpression = Expression.MakeMemberAccess(instanceCast, propertyInfo);

            // special handling for nullable types
            var propertyTypeForDecode = propertyInfo.PropertyType;
            var propertyIsNullable = propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (propertyIsNullable)
                propertyTypeForDecode = Nullable.GetUnderlyingType(propertyInfo.PropertyType);

            // AmqpCodec.DecodeObject<[PropertyTypeForDecode]>(buffer, formatCode)
            var method = typeof(AmqpCodec).GetMethod("DecodeObject", new Type[] { typeof(ByteBuffer), typeof(byte) });
            var genericMethod = method.MakeGenericMethod(propertyTypeForDecode);
            var readIntMethod = Expression.Call(genericMethod, bufferParameter, formatCodeParameter);

            // ((T)instance).[PropertyName] = ([PropertyType])AmqpCodec.DecodeObject<[PropertyType]>(buffer, formatCode);
            var assignment = Expression.Assign(propertyExpression, Expression.Convert(readIntMethod, propertyInfo.PropertyType));

            // compile
            return Expression.Lambda<Action<object, ByteBuffer, byte>>(assignment, instanceParameter, bufferParameter, formatCodeParameter).Compile();
        }

        private static Action<object, ByteBuffer> CompileGenericPropertyEncodeExpression(Type instanceType, string propertyName)
        {
            var propertyInfo = instanceType.GetProperty(propertyName);

            // delegate parameters
            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var bufferParameter = Expression.Parameter(typeof(ByteBuffer), "buffer");

            // (T)instance
            var instanceCast = Expression.Convert(instanceParameter, instanceType);

            // ((T)instance).[PropertyName]
            Expression propertyExpression = Expression.MakeMemberAccess(instanceCast, propertyInfo);

            // special handling for nullable types
            var propertyTypeForEncode = propertyInfo.PropertyType;
            var propertyIsNullable = propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (propertyIsNullable)
            {
                propertyTypeForEncode = Nullable.GetUnderlyingType(propertyInfo.PropertyType);
                // (PropertyTypeForDecode)((T)instance).[PropertyName]
                propertyExpression = Expression.Convert(propertyExpression, propertyTypeForEncode);
            }

            var method = typeof(AmqpCodec).GetMethod("EncodeObject");
            var genericMethod = method.MakeGenericMethod(propertyTypeForEncode);

            // AmqpCodec.EncodeObject<[PropertyTypeForDecode]>(buffer, ((T)instance).[PropertyName], false)
            var encodeMethod = Expression.Call(genericMethod, bufferParameter, propertyExpression, Expression.Constant(true));

            // compile
            return Expression.Lambda<Action<object, ByteBuffer>>(encodeMethod, instanceParameter, bufferParameter).Compile();
        }

    }

    public class TestCompositeClass
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public int? NullableIntValue { get; set; }
        public Map MapValue { get; set; }
    }
}
