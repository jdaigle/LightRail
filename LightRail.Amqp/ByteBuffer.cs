//  ------------------------------------------------------------------------------------
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
using System.Diagnostics;

namespace LightRail.Amqp
{
    public class ByteBuffer
    {
        //
        //   +---------+--------------+----------------+
        // start      read          write             end
        //
        // read - start: already consumed
        // write - read: Length (bytes to be consumed)
        // end - write: Size (free space to write)
        // end - start: Capacity
        //
        private byte[] buffer;
        private int startOffset;
        private int endOffset;
        private readonly bool autoGrow;

        public int ReadOffset { get; private set; }
        public int WriteOffset { get; private set; }

        /// <summary>
        /// Initializes a new buffer from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        public ByteBuffer(byte[] buffer)
            :this(buffer, 0, buffer.Length, buffer.Length, false)
        {
        }

        /// <summary>
        /// Initializes a new buffer from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The start position.</param>
        /// <param name="count">The number of bytes available to read.</param>
        /// <param name="capacity">The total size of the byte array from offset.</param>
        public ByteBuffer(byte[] buffer, int offset, int count, int capacity)
            : this(buffer, offset, count, capacity, false)
        {
        }

        /// <summary>
        /// Initializes a new buffer of a specified size.
        /// </summary>
        /// <param name="size">The size in bytes.</param>
        /// <param name="autoGrow">If the buffer should auto-grow when a write size is larger than the buffer size.</param>
        public ByteBuffer(int size, bool autoGrow)
            : this(new byte[size], 0, 0, size, autoGrow)
        {
        }

        /// <summary>
        /// Initializes a new buffer from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array.</param>
        /// <param name="offset">The start position.</param>
        /// <param name="count">The number of bytes available to read.</param>
        /// <param name="capacity">The total size of the byte array from offset.</param>
        /// <param name="autoGrow">If the buffer should auto-grow when a write size is larger than the buffer size.</param>
        public ByteBuffer(byte[] buffer, int offset, int count, int capacity, bool autoGrow)
        {
            this.buffer = buffer;
            this.startOffset = offset;
            this.ReadOffset = offset;
            this.WriteOffset = offset + count;
            this.endOffset = offset + capacity;
            this.autoGrow = autoGrow;
        }

        public byte[] Buffer
        {
            get { return buffer; }
        }

        /// <summary>
        /// Returns the starting offset for reads/writes in the underlying byte[]
        /// </summary>
        public int StartOffset
        {
            get { return startOffset; }
        }

        public int TotalCapacity
        {
            get { return endOffset - startOffset; }
        }

        public int LengthAvailableToWrite
        {
            get { return endOffset - WriteOffset; }
        }

        public int LengthAvailableToRead
        {
            get { return WriteOffset - ReadOffset; }
        }

        /// <summary>
        /// If True, calling AppendWrite() or ShrinkWrite() will throw an exception
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Advances the write position. As a result, LengthAvailableToRead is increased by size.
        /// </summary>
        public void AppendWrite(int size)
        {
            if (ReadOnly)
                throw new InvalidOperationException("Buffer is read-only, cannot write.");
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "size must be positive");
            if (size > LengthAvailableToWrite)
                throw new ArgumentOutOfRangeException(nameof(size), "size cannot exceed LengthAvailableToWrite");
            WriteOffset += size;
        }

        /// <summary>
        /// Moves back the write position. As a result, LengthAvailableToRead is decreased by size.
        /// </summary>
        public void ShrinkWrite(int size)
        {
            if (ReadOnly)
                throw new InvalidOperationException("Buffer is read-only, cannot write.");
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "size must be positive");
            if (size > LengthAvailableToRead)
                throw new ArgumentOutOfRangeException(nameof(size), "size cannot exceed LengthAvailableToRead");
            WriteOffset -= size;
        }

        /// <summary>
        /// Advances the read position.
        /// </summary>
        public void CompleteRead(int size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "size must be positive");
            if (size > LengthAvailableToRead)
                throw new ArgumentOutOfRangeException(nameof(size), "size cannot exceed LengthAvailableToRead");
            ReadOffset += size;
        }

        /// <summary>
        /// Resets read and write position to the initial state.
        /// </summary>
        public void ResetReadWrite()
        {
            ReadOffset = startOffset;
            WriteOffset = startOffset;
        }

        /// <summary>
        /// Adjusts the read and write position.
        /// </summary>
        /// <param name="readOffset">Read position to set.</param>
        /// <param name="length">Length from read position to set the write position.</param>
        public void AdjustPosition(int readOffset, int length)
        {
            if (readOffset < startOffset)
                throw new ArgumentOutOfRangeException(nameof(readOffset), "Invalid Offset");
            if (readOffset + length > endOffset)
                throw new ArgumentOutOfRangeException(nameof(readOffset), "Length too large");
            ReadOffset = readOffset;
            WriteOffset = ReadOffset + length;
        }

        /// <summary>
        /// Adjusts the write offset of the buffer.
        /// </summary>
        /// <param name="writeOffset">The write offset to set.</param>
        public void AdjustWriteOffset(int writeOffset)
        {
            if (writeOffset > endOffset)
                throw new ArgumentOutOfRangeException(nameof(writeOffset), "Invalid Offset. Must be before end of buffer.");
            WriteOffset = writeOffset;
        }

        /// <summary>
        /// Throws an exception if "LengthAvailableToRead < size"
        /// </summary>
        public void ValidateRead(int size)
        {
            if (LengthAvailableToRead < size)
            {
                throw new InvalidOperationException("buffer too small to read");
            }
        }

        /// <summary>
        /// Throws an exception if "LengthAvailableToWrite < size",
        /// unless "autoGrow == true" in which case will grow the array.
        /// </summary>
        public void ValidateWrite(int size)
        {
            if (LengthAvailableToWrite < size && autoGrow)
            {
                int newSize = Math.Max(TotalCapacity * 2, TotalCapacity + size);
                byte[] newBuffer;
                int newBufferStartOffset;
                int newBufferSize;
                DuplicateBuffer(buffer, startOffset, newSize, WriteOffset - startOffset, out newBuffer, out newBufferStartOffset, out newBufferSize);

                // TODO: explain this voodoo
                int bufferOffset = startOffset - newBufferStartOffset;
                buffer = newBuffer;
                startOffset = newBufferStartOffset;
                ReadOffset -= bufferOffset;
                WriteOffset -= bufferOffset;
                endOffset = newBufferStartOffset + newBufferSize;
            }

            if (LengthAvailableToWrite < size)
            {
                throw new InvalidOperationException("buffer too small to write");
            }
        }

        private static void DuplicateBuffer(byte[] oldBuffer, int oldBufferStartOffset, int requestedNewBufferSize, int dataSizeToCopy, out byte[] newBuffer, out int newBufferStartOffset, out int newBufferSize)
        {
            newBuffer = new byte[requestedNewBufferSize];
            newBufferStartOffset = 0;
            newBufferSize = requestedNewBufferSize;
            System.Buffer.BlockCopy(oldBuffer, oldBufferStartOffset, newBuffer, 0, dataSizeToCopy);
        }

        [Conditional("DEBUG")]
        public static void WriteBufferToConsole(ByteBuffer buffer, int offset, int length)
        {
            for (int i = 0; i < length; i = i + 8)
            {
                var len = Math.Min(8, buffer.buffer.Length - offset - i);
                Console.WriteLine(BitConverter.ToString(buffer.buffer, offset + i, len));
            }
        }
    }
}
