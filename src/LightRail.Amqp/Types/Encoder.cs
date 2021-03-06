﻿//  ------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation
//  All rights reserved. 
//  
//  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
//  file except in compliance with the License. You may obtain a copy of the License at 
//  http://www.apache.org/licenses/LICENSE-2.0  
//  
//  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
//  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
//  NON-INFRINGEMENT. 
// 
//  See the Apache Version 2.0 License for specific language governing permissions and 
//  limitations under the License.
//  ------------------------------------------------------------------------------------
//
//  ------------------------------------------------------------------------------------
// Modifications Copyright (c) 2016 Joseph Daigle
// Licensed under the MIT License. See LICENSE file in the repository root for license information.
//  ------------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using LightRail.Amqp.Framing;

namespace LightRail.Amqp.Types
{
    /// <summary>
    /// Encodes or decodes AMQP primative types.
    /// </summary>
    public static class Encoder
    {
        const long epochTicks = 621355968000000000; // 1970-1-1 00:00:00 UTC
        const long ticksPerMillisecond = 10000;

        /// <summary>
        /// Converts a DateTime value to AMQP timestamp (milliseconds from Unix epoch)
        /// </summary>
        /// <param name="dateTime">The DateTime value to convert.</param>
        /// <returns></returns>
        public static long DateTimeToTimestamp(DateTime dateTime)
        {
            return (long)((dateTime.ToUniversalTime().Ticks - epochTicks) / ticksPerMillisecond);
        }

        /// <summary>
        /// Converts an AMQP timestamp ((milliseconds from Unix epoch)) to a DateTime.
        /// </summary>
        /// <param name="timestamp">The AMQP timestamp to convert.</param>
        /// <returns></returns>
        public static DateTime TimestampToDateTime(long timestamp)
        {
            return new DateTime(epochTicks + timestamp * ticksPerMillisecond, DateTimeKind.Utc);
        }

        /// <summary>
        /// Writes an AMQP null value to a buffer.
        /// </summary>
        public static void WriteNull(ByteBuffer buffer)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Null);
        }

        /// <summary>
        /// Writes a boolean value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The boolean value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteBoolean(ByteBuffer buffer, bool value, bool arrayEncoding)
        {
            if (!arrayEncoding)
            {
                AmqpBitConverter.WriteUByte(buffer, value ? FormatCode.BooleanTrue : FormatCode.BooleanFalse);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Boolean);
                AmqpBitConverter.WriteUByte(buffer, (byte)(value ? 1 : 0));
            }
        }

        /// <summary>
        /// Writes an unsigned byte value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned byte value.</param>
        public static void WriteUByte(ByteBuffer buffer, byte value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.UByte);
            AmqpBitConverter.WriteUByte(buffer, value);
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned 16-bit integer value.</param>
        public static void WriteUShort(ByteBuffer buffer, ushort value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.UShort);
            AmqpBitConverter.WriteUShort(buffer, value);
        }

        /// <summary>
        /// Writes an unsigned 32-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned 32-bit integer value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteUInt(ByteBuffer buffer, uint value, bool arrayEncoding)
        {
            if (arrayEncoding || value > byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.UInt);
                AmqpBitConverter.WriteUInt(buffer, value);
            }
            else if (value == 0)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.UInt0);
            }
            else if (value <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallUInt);
                AmqpBitConverter.WriteUByte(buffer, (byte)value);
            }
        }

        /// <summary>
        /// Writes an unsigned 64-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The unsigned 64-bit integer value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteULong(ByteBuffer buffer, ulong value, bool arrayEncoding)
        {
            if (arrayEncoding || value > byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.ULong);
                AmqpBitConverter.WriteULong(buffer, value);
            }
            else if (value == 0)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.ULong0);
            }
            else if (value <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallULong);
                AmqpBitConverter.WriteUByte(buffer, (byte)value);
            }
        }

        /// <summary>
        /// Writes a signed byte value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed byte value.</param>
        public static void WriteByte(ByteBuffer buffer, sbyte value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Byte);
            AmqpBitConverter.WriteByte(buffer, value);
        }

        /// <summary>
        /// Writes a signed 16-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed 16-bit integer value.</param>
        public static void WriteShort(ByteBuffer buffer, short value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Short);
            AmqpBitConverter.WriteShort(buffer, value);
        }

        /// <summary>
        /// Writes a signed 32-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed 32-bit integer value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteInt(ByteBuffer buffer, int value, bool arrayEncoding)
        {
            if (!arrayEncoding && value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallInt);
                AmqpBitConverter.WriteByte(buffer, (sbyte)value);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Int);
                AmqpBitConverter.WriteInt(buffer, value);
            }
        }

        /// <summary>
        /// Writes a signed 64-bit integer value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The signed 64-bit integer value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteLong(ByteBuffer buffer, long value, bool arrayEncoding)
        {
            if (!arrayEncoding && value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.SmallLong);
                AmqpBitConverter.WriteByte(buffer, (sbyte)value);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Long);
                AmqpBitConverter.WriteLong(buffer, value);
            }
        }

        /// <summary>
        /// Writes a char value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The char value.</param>
        public static void WriteChar(ByteBuffer buffer, char value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Char);
            AmqpBitConverter.WriteInt(buffer, value);   // TODO: utf32
        }

        /// <summary>
        /// Writes a 32-bit floating-point value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The 32-bit floating-point value.</param>
        public static void WriteFloat(ByteBuffer buffer, float value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Float);
            AmqpBitConverter.WriteFloat(buffer, value);
        }

        /// <summary>
        /// Writes a 64-bit floating-point value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The 64-bit floating-point value.</param>
        public static void WriteDouble(ByteBuffer buffer, double value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Double);
            AmqpBitConverter.WriteDouble(buffer, value);
        }

        /// <summary>
        /// Writes a timestamp value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The timestamp value which is the milliseconds since UNIX epoch.</param>
        public static void WriteTimestamp(ByteBuffer buffer, DateTime value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.TimeStamp);
            AmqpBitConverter.WriteLong(buffer, DateTimeToTimestamp(value));
        }

        /// <summary>
        /// Writes a uuid value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The uuid value.</param>
        public static void WriteUuid(ByteBuffer buffer, Guid value)
        {
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Uuid);
            AmqpBitConverter.WriteUuid(buffer, value);
        }

        /// <summary>
        /// Writes a binary value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The binary value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteBinary(ByteBuffer buffer, byte[] value, bool arrayEncoding)
        {
            if (value == null)
            {
                Encoder.WriteNull(buffer);
            }
            else if (!arrayEncoding && value.Length <= byte.MaxValue)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Binary8);
                AmqpBitConverter.WriteUByte(buffer, (byte)value.Length);
                AmqpBitConverter.WriteBytes(buffer, value, 0, value.Length);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.Binary32);
                AmqpBitConverter.WriteUInt(buffer, (uint)value.Length);
                AmqpBitConverter.WriteBytes(buffer, value, 0, value.Length);
            }
        }

        /// <summary>
        /// Writes a string value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The string value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteString(ByteBuffer buffer, string value, bool arrayEncoding)
        {
            if (value == null)
            {
                Encoder.WriteNull(buffer);
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                if (!arrayEncoding && data.Length <= byte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(buffer, FormatCode.String8Utf8);
                    AmqpBitConverter.WriteUByte(buffer, (byte)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(buffer, FormatCode.String32Utf8);
                    AmqpBitConverter.WriteUInt(buffer, (uint)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Writes a symbol value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The symbol value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteSymbol(ByteBuffer buffer, Symbol value, bool arrayEncoding)
        {
            if (value == null)
            {
                Encoder.WriteNull(buffer);
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                if (!arrayEncoding && data.Length <= byte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(buffer, FormatCode.Symbol8);
                    AmqpBitConverter.WriteUByte(buffer, (byte)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(buffer, FormatCode.Symbol32);
                    AmqpBitConverter.WriteUInt(buffer, (uint)data.Length);
                    AmqpBitConverter.WriteBytes(buffer, data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Writes a list value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The list value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteBoxedList(ByteBuffer buffer, IList value, bool arrayEncoding)
        {
            if (value == null)
            {
                Encoder.WriteNull(buffer);
            }
            else
            {
                // trim tailing nulls
                int last = value.Count - 1;
                while (last >= 0 && value[last] == null)
                {
                    --last;
                }
                int listSize = last + 1;

                WriteList(buffer, listSize, (_buffer, _index, _arrayEncoding) =>
                {
                    AmqpCodec.EncodeBoxedObject(_buffer, value[_index], _arrayEncoding);
                }, arrayEncoding);
            }
        }

        /// <summary>
        /// Writes a list value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="listLength">The length of the list to write</param>
        /// <param name="writeListValue">A callback to encode the value at the specific list index to the buffer.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteList(ByteBuffer buffer, int listLength, Action<ByteBuffer, int, bool> writeListValue, bool arrayEncoding)
        {
            if (listLength == 0 && !arrayEncoding)
            {
                AmqpBitConverter.WriteUByte(buffer, FormatCode.List0);
            }
            else
            {
                int pos = buffer.WriteOffset;
                AmqpBitConverter.WriteUByte(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);

                for (int i = 0; i < listLength; ++i)
                {
                    writeListValue(buffer, i, arrayEncoding);
                }

                int size = buffer.WriteOffset - pos - 9;

                if (!arrayEncoding && size < byte.MaxValue && listLength <= byte.MaxValue)
                {
                    buffer.Buffer[pos] = FormatCode.List8;
                    buffer.Buffer[pos + 1] = (byte)(size + 1);
                    buffer.Buffer[pos + 2] = (byte)listLength;
                    Buffer.BlockCopy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                    buffer.ShrinkWrite(6);
                }
                else
                {
                    buffer.Buffer[pos] = FormatCode.List32;
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, listLength);
                }
            }
        }

        /// <summary>
        /// Writes an array value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The array value.</param>
        public static void WriteArray(ByteBuffer buffer, Array value)
        {
            if (value == null)
            {
                Encoder.WriteNull(buffer);
            }
            else
            {
                int count = value.Length;
                if (count <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "array cannot be empty");
                }
                int pos = buffer.WriteOffset;
                AmqpBitConverter.WriteUByte(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);

                for (int i = 0; i < count; ++i)
                {
                    object item = value.GetValue(i);
                    if (i == 0)
                    {
                        // TODO: strongly typed arrays
                        AmqpCodec.EncodeBoxedObject(buffer, item, true);
                    }
                    else
                    {
                        int lastPos = buffer.WriteOffset - 1;
                        byte lastByte = buffer.Buffer[lastPos];
                        buffer.ShrinkWrite(1);
                        AmqpCodec.EncodeBoxedObject(buffer, item, true);
                        buffer.Buffer[lastPos] = lastByte;
                    }
                }

                int size = buffer.WriteOffset - pos - 9;

                if (size < byte.MaxValue && count <= byte.MaxValue)
                {
                    buffer.Buffer[pos] = FormatCode.Array8;
                    buffer.Buffer[pos + 1] = (byte)(size + 1);
                    buffer.Buffer[pos + 2] = (byte)count;
                    Buffer.BlockCopy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                    buffer.ShrinkWrite(6);
                }
                else
                {
                    buffer.Buffer[pos] = FormatCode.Array32;
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, count);
                }
            }
        }

        /// <summary>
        /// Writes a map value to a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="value">The map value.</param>
        /// <param name="arrayEncoding">if true, will force the primative to be written in it's largest representation.</param>
        public static void WriteMap(ByteBuffer buffer, Map value, bool arrayEncoding)
        {
            if (value == null)
            {
                Encoder.WriteNull(buffer);
            }
            else
            {
                int pos = buffer.WriteOffset;
                AmqpBitConverter.WriteUByte(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);
                AmqpBitConverter.WriteUInt(buffer, 0);

                foreach (var key in value.Keys)
                {
                    AmqpCodec.EncodeBoxedObject(buffer, key);
                    AmqpCodec.EncodeBoxedObject(buffer, value[key]);
                }

                int size = buffer.WriteOffset - pos - 9;
                int count = value.Count * 2;

                if (!arrayEncoding && size < byte.MaxValue && count <= byte.MaxValue)
                {
                    buffer.Buffer[pos] = FormatCode.Map8;
                    buffer.Buffer[pos + 1] = (byte)(size + 1);
                    buffer.Buffer[pos + 2] = (byte)count;
                    Buffer.BlockCopy(buffer.Buffer, pos + 9, buffer.Buffer, pos + 3, size);
                    buffer.ShrinkWrite(6);
                }
                else
                {
                    buffer.Buffer[pos] = FormatCode.Map32;
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 1, size + 4);
                    AmqpBitConverter.WriteInt(buffer.Buffer, pos + 5, count);
                }
            }
        }

        /// <summary>
        /// Reads a boolean value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static bool ReadBoolean(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.BooleanTrue)
            {
                return true;
            }
            else if (formatCode == FormatCode.BooleanFalse)
            {
                return false;
            }
            else if (formatCode == FormatCode.Boolean)
            {
                byte data = AmqpBitConverter.ReadUByte(buffer);
                return data != 0;
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads an unsigned byte value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static byte ReadUByte(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.UByte)
            {
                return AmqpBitConverter.ReadUByte(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static ushort ReadUShort(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.UShort)
            {
                return AmqpBitConverter.ReadUShort(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static uint ReadUInt(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.UInt0)
            {
                return 0;
            }
            else if (formatCode == FormatCode.SmallUInt)
            {
                return AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.UInt)
            {
                return AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static ulong ReadULong(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.ULong0)
            {
                return 0;
            }
            else if (formatCode == FormatCode.SmallULong)
            {
                return AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.ULong)
            {
                return AmqpBitConverter.ReadULong(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a signed byte from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static sbyte ReadByte(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Byte)
            {
                return AmqpBitConverter.ReadByte(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a signed 16-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static short ReadShort(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Short)
            {
                return AmqpBitConverter.ReadShort(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a signed 32-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static int ReadInt(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.SmallInt)
            {
                return AmqpBitConverter.ReadByte(buffer);
            }
            else if (formatCode == FormatCode.Int)
            {
                return AmqpBitConverter.ReadInt(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a signed 64-bit integer from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static long ReadLong(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.SmallLong)
            {
                return AmqpBitConverter.ReadByte(buffer);
            }
            else if (formatCode == FormatCode.Long)
            {
                return AmqpBitConverter.ReadLong(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a char value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static char ReadChar(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Char)
            {
                return (char)AmqpBitConverter.ReadInt(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a 32-bit floating-point value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static float ReadFloat(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Float)
            {
                return AmqpBitConverter.ReadFloat(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a 64-bit floating-point value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static double ReadDouble(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Double)
            {
                return AmqpBitConverter.ReadDouble(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a timestamp value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static DateTime ReadTimestamp(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.TimeStamp)
            {
                return TimestampToDateTime(AmqpBitConverter.ReadLong(buffer));
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a uuid value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Guid ReadUuid(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Uuid)
            {
                return AmqpBitConverter.ReadUuid(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }
        }

        /// <summary>
        /// Reads a binary value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static byte[] ReadBinary(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int count;
            if (formatCode == FormatCode.Binary8)
            {
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.Binary32)
            {
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }

            buffer.ValidateRead(count);
            byte[] value = new byte[count];
            Buffer.BlockCopy(buffer.Buffer, buffer.ReadOffset, value, 0, count);
            buffer.CompleteRead(count);

            return value;
        }

        /// <summary>
        /// Reads a string value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static string ReadString(ByteBuffer buffer, byte formatCode)
        {
            return ReadString(buffer, formatCode, FormatCode.String8Utf8, FormatCode.String32Utf8, "string");
        }

        /// <summary>
        /// Reads a symbol value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Symbol ReadSymbol(ByteBuffer buffer, byte formatCode)
        {
            return (Symbol)ReadString(buffer, formatCode, FormatCode.Symbol8, FormatCode.Symbol32, "symbol");
        }


        /// <summary>
        /// Reads a list value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static AmqpList ReadBoxedList(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            var list = new AmqpList();
            ReadList(buffer, formatCode, (_byteBuffer, _index) =>
            {
                list.Add(AmqpCodec.DecodeBoxedObject(buffer));
            });
            return list;
        }

        /// <summary>
        /// Reads a list value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        /// <param name="decodeValue">A callback to decode the value of the specific list inde.x</param>
        public static void ReadList(ByteBuffer buffer, byte formatCode, Action<ByteBuffer, int> decodeValue)
        {
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
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }

            for (int i = 0; i < count; ++i)
            {
                decodeValue(buffer, i);
            }
        }

        /// <summary>
        /// Reads an strongly typed array from the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="arrayFormatCode">The format code of the array.</param>
        public static T[] ReadStronglyTypedArray<T>(ByteBuffer buffer, byte arrayFormatCode)
        {
            if (arrayFormatCode == FormatCode.Null)
            {
                return null;
            }

            int size;
            int count;
            if (arrayFormatCode == FormatCode.Array8)
            {
                size = AmqpBitConverter.ReadUByte(buffer);
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (arrayFormatCode == FormatCode.Array32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(arrayFormatCode, buffer.ReadOffset);
            }

            var arrayElementFormatCode = AmqpCodec.DecodeFormatCode(buffer);
            var arrayElementDecoder = Encoder.GetTypeCodec(arrayElementFormatCode) as PrimativeTypeCodec<T>;

            // TODO: handle described types?
            if (arrayElementDecoder == null)
                throw InvalidFormatCodeException(arrayFormatCode, buffer.ReadOffset);

            var array = new T[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = arrayElementDecoder.Decode(buffer, arrayElementFormatCode);
            }

            return array;
        }

        /// <summary>
        /// Reads an array value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Array ReadArray(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int size;
            int count;
            if (formatCode == FormatCode.Array8)
            {
                size = AmqpBitConverter.ReadUByte(buffer);
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.Array32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }

            formatCode = AmqpCodec.DecodeFormatCode(buffer);

            var codec = GetTypeCodec(formatCode);
            if (codec == null)
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }

            Array value = Array.CreateInstance(codec.Type, count);
            IList list = value;
            for (int i = 0; i < count; ++i)
            {
                list[i] = codec.DecodeBoxedValue(buffer, formatCode);
            }

            return value;
        }

        /// <summary>
        /// Reads a map value from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <param name="formatCode">The format code of the value.</param>
        public static Map ReadMap(ByteBuffer buffer, byte formatCode)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int size;
            int count;
            if (formatCode == FormatCode.Map8)
            {
                size = AmqpBitConverter.ReadUByte(buffer);
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == FormatCode.Map32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }

            if (count % 2 > 0)
            {
                throw InvalidMapCountException(count);
            }

            Map value = new Map();
            for (int i = 0; i < count; i += 2)
            {
                value.Add(AmqpCodec.DecodeBoxedObject(buffer), AmqpCodec.DecodeBoxedObject(buffer));
            }

            return value;
        }

        static string ReadString(ByteBuffer buffer, byte formatCode, byte code8, byte code32, string type)
        {
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            int count;
            if (formatCode == code8)
            {
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == code32)
            {
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw InvalidFormatCodeException(formatCode, buffer.ReadOffset);
            }

            buffer.ValidateRead(count);
            string value = new string(Encoding.UTF8.GetChars(buffer.Buffer, buffer.ReadOffset, count));
            buffer.CompleteRead(count);

            return value;
        }

        private static AmqpException InvalidFormatCodeException(byte formatCode, int offset)
        {
            return new AmqpException(ErrorCode.DecodeError,
                $"The format code '{formatCode}' at frame buffer offset '{offset}' is invalid.");
        }

        private static AmqpException InvalidMapCountException(int count)
        {
            return new AmqpException(ErrorCode.DecodeError,
                $"The map count {count} is invalid. It must be an even number.");
        }

        private static AmqpException TypeNotSupportedException(Type type)
        {
            return new AmqpException(ErrorCode.NotImplemented,
                $"The type '{type}' is not a valid AMQP type and cannot be encoded.");
        }

        static Encoder()
        {
            typeCodecs = new PrimativeTypeCodec[]
            {
                // 0: null
                new NullTypeCodec(),
                // 1: boolean
                new PrimativeTypeCodec<bool>
                {
                    Encode = WriteBoolean,
                    Decode = ReadBoolean,
                },
                // 2: ubyte
                new PrimativeTypeCodec<byte>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteUByte(buffer, value),
                    Decode = ReadUByte,
                },
                // 3: ushort
                new PrimativeTypeCodec<ushort>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteUShort(buffer, value),
                    Decode = ReadUShort
                },
                // 4: uint
                new PrimativeTypeCodec<uint>
                {
                    Encode = WriteUInt,
                    Decode = ReadUInt
                },
                // 5: ulong
                new PrimativeTypeCodec<ulong>
                {
                    Encode = WriteULong,
                    Decode = ReadULong,
                },
                // 6: byte
                new PrimativeTypeCodec<sbyte>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteByte(buffer, value),
                    Decode = ReadByte,
                },
                // 7: short
                new PrimativeTypeCodec<short>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteShort(buffer, value),
                    Decode = ReadShort,
                },
                // 8: int
                new PrimativeTypeCodec<int>
                {
                    Encode = WriteInt,
                    Decode = ReadInt,
                },
                // 9: long
                new PrimativeTypeCodec<long>
                {
                    Encode = WriteLong,
                    Decode = ReadLong,
                },
                // 10: float
                new PrimativeTypeCodec<float>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteFloat(buffer, value),
                    Decode = ReadFloat
                },
                // 11: double
                new PrimativeTypeCodec<double>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteDouble(buffer, value),
                    Decode = ReadDouble,
                },
                // 12: char
                new PrimativeTypeCodec<char>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteChar(buffer, value),
                    Decode = ReadChar,
                },
                // 13: timestamp
                new PrimativeTypeCodec<DateTime>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteTimestamp(buffer, value),
                    Decode = ReadTimestamp,
                },
                // 14: uuid
                new PrimativeTypeCodec<Guid>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteUuid(buffer, value),
                    Decode = ReadUuid,
                },
                // 15: binary
                new PrimativeTypeCodec<byte[]>
                {
                    Encode = WriteBinary,
                    Decode = ReadBinary,
                },
                // 16: string
                new PrimativeTypeCodec<string>
                {
                    Encode = WriteString,
                    Decode = ReadString,
                },
                // 17: symbol
                new PrimativeTypeCodec<Symbol>
                {
                    Encode = WriteSymbol,
                    Decode = ReadSymbol,
                },
                // 18: list
                new PrimativeTypeCodec<AmqpList>
                {
                    Encode = WriteBoxedList,
                    Decode = ReadBoxedList,
                },
                // 19: map
                new PrimativeTypeCodec<Map>
                {
                    Encode = WriteMap,
                    Decode = ReadMap,
                },
                // 20: array
                new PrimativeTypeCodec<Array>
                {
                    Encode = (buffer, value, arrayEncoding) => WriteArray(buffer, value),
                    Decode = ReadArray,
                },
                // 21: invalid
                null
            };

            codecByType = new Dictionary<Type, PrimativeTypeCodec>()
            {
                { typeof(bool),     typeCodecs[1] },
                { typeof(byte),     typeCodecs[2] },
                { typeof(ushort),   typeCodecs[3] },
                { typeof(uint),     typeCodecs[4] },
                { typeof(ulong),    typeCodecs[5] },
                { typeof(sbyte),    typeCodecs[6] },
                { typeof(short),    typeCodecs[7] },
                { typeof(int),      typeCodecs[8] },
                { typeof(long),     typeCodecs[9] },
                { typeof(float),    typeCodecs[10] },
                { typeof(double),   typeCodecs[11] },
                { typeof(char),     typeCodecs[12] },
                { typeof(DateTime), typeCodecs[13] },
                { typeof(Guid),     typeCodecs[14] },
                { typeof(byte[]),   typeCodecs[15] },
                { typeof(string),   typeCodecs[16] },
                { typeof(Symbol),   typeCodecs[17] },
                { typeof(AmqpList), typeCodecs[18] },
                { typeof(Map),      typeCodecs[19] },
                { typeof(Fields),   typeCodecs[19] },
                { typeof(Array),    typeCodecs[20] },
            };

            codecIndexTable = new PrimativeTypeCodec[][]
            {
                // 0x40:null, 0x41:boolean.true, 0x42:boolean.false, 0x43:uint0, 0x44:ulong0, 0x45:list0
                new PrimativeTypeCodec[] { typeCodecs[0], typeCodecs[1], typeCodecs[1], typeCodecs[4], typeCodecs[5], typeCodecs[18] },

                // 0x50:ubyte, 0x51:byte, 0x52:small.uint, 0x53:small.ulong, 0x54:small.int, 0x55:small.long, 0x56:boolean
                new PrimativeTypeCodec[] { typeCodecs[2], typeCodecs[6], typeCodecs[4], typeCodecs[5], typeCodecs[8], typeCodecs[9], typeCodecs[1] },

                // 0x60:ushort, 0x61:short
                new PrimativeTypeCodec[] { typeCodecs[3], typeCodecs[7] },

                // 0x70:uint, 0x71:int, 0x72:float, 0x73:char, 0x74:decimal32
                new PrimativeTypeCodec[] { typeCodecs[4], typeCodecs[8], typeCodecs[10], typeCodecs[12] },

                // 0x80:ulong, 0x81:long, 0x82:double, 0x83:timestamp, 0x84:decimal64
                new PrimativeTypeCodec[] { typeCodecs[5], typeCodecs[9], typeCodecs[11], typeCodecs[13] },

                // 0x98:uuid
                new PrimativeTypeCodec[] { typeCodecs[21], typeCodecs[21], typeCodecs[21], typeCodecs[21], typeCodecs[21], typeCodecs[21], typeCodecs[21], typeCodecs[21], typeCodecs[14] },
            
                // 0xa0:bin8, 0xa1:str8, 0xa3:sym8
                new PrimativeTypeCodec[] { typeCodecs[15], typeCodecs[16], typeCodecs[21], typeCodecs[17] },

                // 0xb0:bin32, 0xb1:str32, 0xb3:sym32
                new PrimativeTypeCodec[] { typeCodecs[15], typeCodecs[16], typeCodecs[21], typeCodecs[17] },

                // 0xc0:list8, 0xc1:map8
                new PrimativeTypeCodec[] { typeCodecs[18], typeCodecs[19] },

                // 0xd0:list32, 0xd1:map32
                new PrimativeTypeCodec[] { typeCodecs[18], typeCodecs[19] },

                // 0xe0:array8
                new PrimativeTypeCodec[] { typeCodecs[20] },

                // 0xf0:array32
                new PrimativeTypeCodec[] { typeCodecs[20] }
            };
        }

        private static readonly PrimativeTypeCodec[] typeCodecs;
        private static readonly Dictionary<Type, PrimativeTypeCodec> codecByType;
        private static readonly PrimativeTypeCodec[][] codecIndexTable;

        internal static PrimativeTypeCodec GetTypeCodec(Type type)
        {
            PrimativeTypeCodec codec = null;
            codecByType.TryGetValue(type, out codec);
            if (codec == null)
            {
                if (type.IsArray)
                {
                    // TODO: strongly typed arrays
                    codec = GetTypeCodec<Array>();
                }
            }
            return codec;
        }

        internal static PrimativeTypeCodec<T> GetTypeCodec<T>()
        {
            return (PrimativeTypeCodec<T>)GetTypeCodec(typeof(T));
        }

        internal static PrimativeTypeCodec GetTypeCodec(byte formatCode)
        {
            int type = ((formatCode & 0xF0) >> 4) - 4;
            if (type >= 0 && type < codecIndexTable.Length)
            {
                int index = formatCode & 0x0F;
                if (index < codecIndexTable[type].Length)
                {
                    return codecIndexTable[type][index];
                }
            }
            return null;
        }
    }
}
