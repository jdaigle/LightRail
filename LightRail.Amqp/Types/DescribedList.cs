using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LightRail.Amqp.Types
{
    /// <summary>
    /// AMQP composite types are represented as a described list.
    /// </summary>
    /// <remarks>
    /// AMQP composite types are represented as a described list. Each element in the list is positionally correlated with
    /// the fields listed in the composite type definition. The permitted element values are determined by the type speci-
    /// fication and multiplicity of the corresponding field definitions. When the trailing elements of the list representation
    /// are null, they MAY be omitted. The descriptor of the list indicates the specific composite type being represented.
    /// </remarks>
    public abstract class DescribedList : DescribedType
    {
        protected DescribedList(Descriptor descriptor)
            : base(descriptor)
        {
        }

        protected override void EncodeValue(ByteBuffer buffer)
        {
            int pos = buffer.WriteOffset;

            // initial values for ctor, size, length
            AmqpBitConverter.WriteUByte(buffer, 0);
            AmqpBitConverter.WriteUInt(buffer, 0);
            AmqpBitConverter.WriteUInt(buffer, 0);

            var propertyCount = GetEncodablePropertyCount(GetType());
            int lastNotNullIndex = -1;
            int lastNotNullBufferWriteOffset = buffer.WriteOffset;

            var thisType = GetType();
            for (int i = 0; i < propertyCount; i++)
            {
                var encoderFunc = GetEncoderDelegate(thisType, i);
                var valueWasNull = encoderFunc(this, buffer);
                if (!valueWasNull || i == 0)
                {
                    lastNotNullIndex = i;
                    lastNotNullBufferWriteOffset = buffer.WriteOffset;
                }
            }

            // rewind the buffer to just after we wrote the last not-null value
            var listLength = lastNotNullIndex + 1;
            buffer.AdjustWriteOffset(lastNotNullBufferWriteOffset);

            int size = buffer.WriteOffset - pos - 9; // recalculate list size

            // set ctor, size, length
            buffer.Buffer[pos] = FormatCode.List32;
            AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
            AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, listLength);
        }

        protected override void DecodeValue(ByteBuffer buffer)
        {
            var formatCode = AmqpCodec.DecodeFormatCode(buffer);

            if (formatCode == FormatCode.Null)
            {
                return; // nothing to decode
            }

            int size;
            int count;
            if (formatCode == FormatCode.List0)
            {
                size = count = 0;
            }
            else if (formatCode == FormatCode.List8)
            {
                size = AmqpBitConverter.ReadUByte(buffer);
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.List32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError, $"The format code '{formatCode}' at frame buffer offset '{buffer.ReadOffset}' is invalid.");
            }

            var thisType = GetType();
            for (int i = 0; i < count; i++)
            {
                var itemFormatCode = AmqpCodec.DecodeFormatCode(buffer);
                if (itemFormatCode == FormatCode.Null)
                    continue; // null value, ignore and continue
                GetDecoderDelegate(thisType, i)(this, buffer, itemFormatCode);
            }
        }

        private static Dictionary<Type, Dictionary<int, Action<object, ByteBuffer, byte>>> cachedDecoderDelegates = new Dictionary<Type, Dictionary<int, Action<object, ByteBuffer, byte>>>();
        private static Dictionary<Type, Dictionary<int, Func<object, ByteBuffer, bool>>> cachedEncoderDelegates = new Dictionary<Type, Dictionary<int, Func<object, ByteBuffer, bool>>>();

        private static Action<object, ByteBuffer, byte> GetDecoderDelegate(Type describedListType, int index)
        {
            if (!cachedDecoderDelegates.ContainsKey(describedListType))
                CompilePropertyDecoderDelegates(describedListType);
            return cachedDecoderDelegates[describedListType][index];
        }

        private static Func<object, ByteBuffer, bool> GetEncoderDelegate(Type describedListType, int index)
        {
            if (!cachedEncoderDelegates.ContainsKey(describedListType))
                CompilePropertyEncoderDelegates(describedListType);
            return cachedEncoderDelegates[describedListType][index];
        }

        private static int GetEncodablePropertyCount(Type describedListType)
        {
            if (!cachedEncoderDelegates.ContainsKey(describedListType))
                CompilePropertyEncoderDelegates(describedListType);
            return cachedEncoderDelegates[describedListType].Count;
        }

        private static void CompilePropertyDecoderDelegates(Type describedListType)
        {
            var decoders = new Dictionary<int, Action<object, ByteBuffer, byte>>();

            var properties = describedListType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite)
                .Where(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false) != null);

            foreach (var propertyInfo in properties)
            {
                var index = propertyInfo.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false).Index;
                decoders.Add(index, CompilePropertyDecoderDelegate(describedListType, propertyInfo));
            }

            cachedDecoderDelegates[describedListType] = decoders;
        }

        private static Action<object, ByteBuffer, byte> CompilePropertyDecoderDelegate(Type instanceType, PropertyInfo propertyInfo)
        {
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
            var decodeMethod = Expression.Call(genericMethod, bufferParameter, formatCodeParameter);

            if (propertyInfo.PropertyType.IsArray && propertyInfo.PropertyType.GetElementType() != typeof(byte))
            {
                // special handling of arrays (excluding byte[] which is actually a binary)
                // 1) we need to be able to decode single value into an array object
                // 2) we need to be able to get the correct decoder for an array
                // DescribedList.DecodeArrayWrapper<[PropertyTypeForDecode]>(buffer, formatCode)
                method = typeof(DescribedList).GetMethod("DecodeArrayWrapper", BindingFlags.NonPublic | BindingFlags.Static);
                genericMethod = method.MakeGenericMethod(propertyTypeForDecode, propertyInfo.PropertyType.GetElementType());
                decodeMethod = Expression.Call(genericMethod, bufferParameter, formatCodeParameter);
            }

            // ((T)instance).[PropertyName] = ([PropertyType])AmqpCodec.DecodeObject<[PropertyType]>(buffer, formatCode);
            var assignment = Expression.Assign(propertyExpression, Expression.Convert(decodeMethod, propertyInfo.PropertyType));

            // compile
            return Expression.Lambda<Action<object, ByteBuffer, byte>>(assignment, instanceParameter, bufferParameter, formatCodeParameter).Compile();
        }

        private static bool FormatCodeIsArrayType(byte formatCode)
        {
            return formatCode == FormatCode.Array32 || formatCode == FormatCode.Array8;
        }

        private static object DecodeArrayWrapper<TArray, TArrayElement>(ByteBuffer buffer, byte formatCode)
        {
            if (FormatCodeIsArrayType(formatCode))
                return Encoder.ReadStronglyTypedArray<TArrayElement>(buffer, formatCode);
            else
                return new TArrayElement[] { AmqpCodec.DecodeObject<TArrayElement>(buffer, formatCode) };
        }

        private static void CompilePropertyEncoderDelegates(Type describedListType)
        {
            var decoders = new Dictionary<int, Func<object, ByteBuffer, bool>>();

            var properties = describedListType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite)
                .Where(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false) != null);

            foreach (var propertyInfo in properties)
            {
                var index = propertyInfo.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false).Index;
                decoders.Add(index, CompilePropertyEncoderDelegate(describedListType, propertyInfo));
            }

            cachedEncoderDelegates[describedListType] = decoders;
        }

        private static Func<object, ByteBuffer, bool> CompilePropertyEncoderDelegate(Type instanceType, PropertyInfo propertyInfo)
        {
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

            var testIfPropertyValueIsNull = propertyIsNullable || propertyInfo.PropertyType.IsClass;

            var encodeObjectMethod = typeof(DescribedList).GetMethod("EncodeObjectWrapper", BindingFlags.NonPublic | BindingFlags.Static);
            var genericEncodeObjectMethod = encodeObjectMethod.MakeGenericMethod(propertyTypeForEncode);

            // AmqpCodec.EncodeObject<[PropertyTypeForDecode]>(buffer, ((T)instance).[PropertyName], false)
            var encodeMethod = Expression.Call(genericEncodeObjectMethod, bufferParameter, propertyExpression, Expression.Constant(true));
            Expression lambdaExpressionBody = encodeMethod;

            if (testIfPropertyValueIsNull)
            {
                // if the property is null, just write null and return 'true'. We need to do this because EncodeObject<T> technically
                // cannot accept nullable types, so this will make it easier just to quickly check.
                var expressionPropertyForComparison = Expression.MakeMemberAccess(instanceCast, propertyInfo);
                var propertyValueIsNull = Expression.Equal(expressionPropertyForComparison, Expression.Constant(null));
                var encodeNullMethod = Expression.Call(typeof(DescribedList).GetMethod("EncodeNull", BindingFlags.NonPublic | BindingFlags.Static), bufferParameter);
                lambdaExpressionBody = Expression.Condition(propertyValueIsNull, encodeNullMethod, encodeMethod);
            }

            // compile
            return Expression.Lambda<Func<object, ByteBuffer, bool>>(lambdaExpressionBody, instanceParameter, bufferParameter).Compile();
        }

        private static bool EncodeNull(ByteBuffer buffer)
        {
            Encoder.WriteNull(buffer);
            return true;
        }

        private static bool EncodeObjectWrapper<T>(ByteBuffer buffer, T value, bool arrayEncoding)
        {
            AmqpCodec.EncodeObject(buffer, value, arrayEncoding);
            if (EqualityComparer<T>.Default.Equals(value, default(T)) &&
                typeof(T).IsClass)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

#if DEBUG

        public override string ToString()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead)
                .Where(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>(false) != null)
                .OrderBy(x => x.GetCustomAttribute<AmqpDescribedListIndexAttribute>().Index)
                .ToList();

            var buffer = new System.Text.StringBuilder();
            buffer.Append(GetType().Name.ToString());
            buffer.Append("(");
            bool addComma = false;
            foreach (var p in properties)
            {
                if (p.GetValue(this) == null)
                    continue;
                var value = p.GetValue(this);
                if (value is byte[])
                    value = $"byte[{(value as byte[]).Length}]" + Convert.ToBase64String(value as byte[]);
                if (value is Symbol[])
                    value = string.Join(",", (object[])value);
                if (addComma)
                    buffer.Append(",");
                buffer.AppendFormat("{0}:{1}", p.Name, value.ToString());
                addComma = true;
            }
            buffer.Append(")");
            return buffer.ToString().ToLowerInvariant();
        }
#endif
    }
}
