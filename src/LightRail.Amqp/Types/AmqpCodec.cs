﻿using System;
using System.Collections.Generic;
using System.Reflection;
using LightRail.Amqp.Framing;

namespace LightRail.Amqp.Types
{
    public static class AmqpCodec
    {
        public static AmqpFrame DecodeFrame(ByteBuffer buffer, out ushort channelNumber)
        {
#if DEBUG
            byte[] debugFrameBuffer = new byte[buffer.LengthAvailableToRead];
            Buffer.BlockCopy(buffer.Buffer, buffer.ReadOffset, debugFrameBuffer, 0, buffer.LengthAvailableToRead);
#endif

            int frameStartOffset = buffer.ReadOffset;

            // frame header
            uint frameSize = AmqpBitConverter.ReadUInt(buffer);
            byte dataOffset = AmqpBitConverter.ReadUByte(buffer);
            byte frameType = AmqpBitConverter.ReadUByte(buffer);
            channelNumber = AmqpBitConverter.ReadUShort(buffer); // out param

            if (dataOffset < 2)
            {
                throw new AmqpException(ErrorCode.FramingError, $"DOFF must be >= 2. Value is {dataOffset.ToHex()}");
            }

            // data offset is always counted in 4-byte words. header total length is 8 bytes
            int bodyStartOffset = 4 * dataOffset;
            // forward the reader the number of bytes needed to reach the frame body
            buffer.CompleteRead((bodyStartOffset - 8));

            if (frameSize == bodyStartOffset)
            {
                // empty frame body
                return null;
            }

            // we're expecting a described list...
            var formatCode = DecodeFormatCode(buffer);
            if (formatCode != FormatCode.Described)
                throw new AmqpException(ErrorCode.FramingError, $"Expected Format Code = {FormatCode.Described.ToHex()} but was {formatCode.ToHex()}");

            try
            {
                // decode
                return (AmqpFrame)DecodeDescribedType(buffer, formatCode);
            }
            catch (Exception)
            {
#if DEBUG
                TraceSource.FromClass().Debug(Environment.NewLine + debugFrameBuffer.ToHex());
#endif
                throw;
            }
        }

        public static byte DecodeFormatCode(ByteBuffer buffer)
        {
            return AmqpBitConverter.ReadUByte(buffer);
        }

        /// <summary>
        /// Reads a boxed object from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        public static object DecodeBoxedObject(ByteBuffer buffer)
        {
            var formatCode = DecodeFormatCode(buffer);
            return DecodeBoxedObject(buffer, formatCode);
        }

        /// <summary>
        /// Reads a strongly typed object from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        public static T DecodeObject<T>(ByteBuffer buffer)
        {
            var formatCode = DecodeFormatCode(buffer);
            return DecodeObject<T>(buffer, formatCode);
        }

        /// <summary>
        /// Reads a boxed object from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static object DecodeBoxedObject(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }
            else if (formatCode == FormatCode.Described)
            {
                return DecodeDescribedType(buffer, formatCode);
            }
            else
            {
                var codec = Encoder.GetTypeCodec(formatCode);
                if (codec != null)
                    return codec.DecodeBoxedValue(buffer, formatCode);
                else
                    throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a strongly typed object from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static T DecodeObject<T>(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return default(T);
            }
            else if (formatCode == FormatCode.Described)
            {
                return (T)DecodeDescribedType(buffer, formatCode);
            }
            else
            {
                var codec = Encoder.GetTypeCodec(formatCode);
                if (codec is PrimativeTypeCodec<T>)
                {
                    return ((PrimativeTypeCodec<T>)codec).Decode(buffer, formatCode);
                }
                else
                {
                    throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
                }
            }
        }

        /// <summary>
        /// Reads a Descriptor and DescribedType object from the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        public static object DecodeDescribedType(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode != FormatCode.Described)
                throw new ArgumentException(nameof(formatCode), "Format code must be described (0x00)");

            var descriptor = DecodeDescriptor(buffer);

            if (DescribedTypeCodec.IsKnownDescribedType(descriptor))
            {
                return DecodeKnownDescribedType(buffer, descriptor);
            }

            object value = DecodeBoxedObject(buffer); // TODO: performance. boxing
            return DescribedTypeCodec.GetDescribedTypeConstructor(value.GetType())(descriptor, value);
        }

        /// <summary>
        /// Reads a known DescribedType object from the buffer given the already read descriptor.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="descriptor"></param>
        public static object DecodeKnownDescribedType(ByteBuffer buffer, Descriptor descriptor)
        {
            Func<object> ctor;
            if (DescribedTypeCodec.TryGetKnownDescribedConstructor(descriptor.Code, out ctor))
            {
                var instance = ctor() as DescribedType;
                instance.Decode(buffer);
                return instance;
            }
            else
            {
                throw new AmqpException(ErrorCode.DecodeError, $"Missing Constructor For Known Described Type {descriptor.ToString()}");
            }
        }

        /// <summary>
        /// Reads a described type descriptor from the buffer. It may return a known static Descriptor.
        /// </summary>
        /// <param name="buffer"></param>
        public static Descriptor DecodeDescriptor(ByteBuffer buffer)
        {
            var descriptorFormatCode = DecodeFormatCode(buffer);
            if (descriptorFormatCode == FormatCode.ULong ||
                descriptorFormatCode == FormatCode.SmallULong)
            {
                ulong code = Encoder.ReadULong(buffer, descriptorFormatCode);
                Descriptor descriptor = null;
                if (DescribedTypeCodec.TryGetKnownDescribedType(code, out descriptor))
                    return descriptor;
                return new Descriptor(code);
            }
            if (descriptorFormatCode == FormatCode.Symbol8 ||
                descriptorFormatCode == FormatCode.Symbol32)
            {
                string symbol = Encoder.ReadSymbol(buffer, descriptorFormatCode);
                return new Descriptor(symbol);
            }
            throw new AmqpException(ErrorCode.FramingError, $"Invalid Descriptor Format Code{descriptorFormatCode.ToHex()}");
        }

        /// <summary>
        /// Returns the static method used to decode the specified type.
        /// </summary>
        /// <param name="type"></param>
        internal static MethodInfo GetDecodeMethod(Type type)
        {
            if (typeof(DescribedType).IsAssignableFrom(type))
                return typeof(AmqpCodec).GetMethod("DecodeDescribedType");
            return null;
        }

        public static void EncodeFrame(ByteBuffer buffer, AmqpFrame frame, ushort channelNumber)
        {
            buffer.ValidateWrite(8);

            var frameStartOffset = buffer.WriteOffset;

            // header
            buffer.AppendWrite(FixedWidth.UInt); // placeholder for frame size
            AmqpBitConverter.WriteUByte(buffer, 0x02); // data offset (2*4 = 8 bytes to account for header)
            AmqpBitConverter.WriteUByte(buffer, 0x00); // frame type = AMQP frame
            AmqpBitConverter.WriteUShort(buffer, channelNumber);

            // frame body, may be null/empty
            if (frame != null)
            {
                frame.Encode(buffer);
            }

            // frame size
            int frameSize = buffer.WriteOffset - frameStartOffset;
            AmqpBitConverter.WriteInt(buffer.Buffer, frameStartOffset, frameSize);
        }

        /// <summary>
        /// Writes a boxed AMQP object to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The boxed AMQP value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void EncodeBoxedObject(ByteBuffer buffer, object value, bool arrayEncoding = false)
        {
            if (value == null)
            {
                Encoder.WriteNull(buffer);
                return;
            }

            if (value is DescribedType)
            {
                (value as DescribedType).Encode(buffer, arrayEncoding);
                return;
            }

            var codec = Encoder.GetTypeCodec(value.GetType());
            if (codec != null)
            {
                codec.EncodeBoxedValue(buffer, value, arrayEncoding);
                return;
            }

            throw TypeNotSupportedException(value.GetType());
        }

        /// <summary>
        /// Writes a strongly typed AMQP object to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The AMQP value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void EncodeObject<T>(ByteBuffer buffer, T value, bool arrayEncoding = false)
        {
#if DEBUG
            if (typeof(T) == typeof(object))
            {
                System.Diagnostics.Debug.Fail("Cannot Call WriteObject<t> with base object. Call WriteBoxedObject() instead.");
            }
#endif

            if (EqualityComparer<T>.Default.Equals(value, default(T)) &&
                typeof(T).IsClass)
            {
                Encoder.WriteNull(buffer);
                return;
            }

            if (value is Array)
            {
                // TODO: performance. strongly typed arrays
                EncodeBoxedObject(buffer, value, arrayEncoding);
                return;
            }

            if (value is DescribedType)
            {
                (value as DescribedType).Encode(buffer, arrayEncoding);
                return;
            }

            var codec = Encoder.GetTypeCodec<T>();
            if (codec != null)
            {
                codec.Encode(buffer, value, arrayEncoding);
                return;
            }

            throw TypeNotSupportedException(value.GetType());
        }

        private static AmqpException InvalidFormatCodeException(byte formatCode, int offset)
        {
            return new AmqpException(ErrorCode.DecodeError,
                $"The format code '{formatCode.ToHex()}' at frame buffer offset '{offset}' is invalid.");
        }

        private static AmqpException TypeNotSupportedException(Type type)
        {
            return new AmqpException(ErrorCode.NotImplemented,
                $"The type '{type}' is not a valid AMQP type and cannot be encoded.");
        }
    }
}