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
using System.Net.Sockets;
using System.Threading.Tasks;
using LightRail.Amqp.Protocol;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Network
{
    public class AsyncReceiverEventLoop
    {
        private static readonly TraceSource trace = TraceSource.FromClass();

        public AsyncReceiverEventLoop(AmqpConnection connection, ISocket socket)
        {
            this.connection = connection;
            this.socket = socket;
        }

        private readonly AmqpConnection connection;
        private readonly ISocket socket;
        private Task eventLoopTask;
        private bool continuePumping = true;

        public void Start()
        {
            continuePumping = true;
            if (eventLoopTask != null && eventLoopTask.Status == TaskStatus.Running)
                return;
            eventLoopTask = StartAsync();
        }

        public void Stop()
        {
            continuePumping = false;
            eventLoopTask = null;
        }

        private async Task StartAsync()
        {
            try
            {
                await this.PumpAsync(connection.HandleHeader, connection.HandleFrame);
            }
            catch (Exception exception)
            {
                connection.OnIoException(exception);
            }

        }

        public async Task PumpAsync(Func<ByteBuffer, bool> onHeader, Func<ByteBuffer, bool> onFrame)
        {
            trace.Debug("AsyncReceiverEventLoop() Starting");
            try
            {
                ByteBuffer frameBuffer;
                if (!socket.BufferPool.TryGetByteBuffer(out frameBuffer))
                    throw new Exception("No free buffers available to receive on the underlying socket.");

                if (onHeader != null)
                {
                    // header
                    frameBuffer.ResetReadWrite();
                    await ReceiveBufferAsync(frameBuffer.Buffer, frameBuffer.WriteOffset, FixedWidth.ULong); // read 8 bytes from socket
                    frameBuffer.AppendWrite(FixedWidth.ULong);
                    if (!onHeader(frameBuffer))
                    {
                        return; // stop immediately
                    }
                }

                // frames
                while (continuePumping)
                {
                    frameBuffer.ResetReadWrite();
                    await ReceiveBufferAsync(frameBuffer.Buffer, frameBuffer.WriteOffset, FixedWidth.UInt); // read 4 bytes from socket
                    frameBuffer.AppendWrite(FixedWidth.UInt);

                    int frameSize = (int)AmqpBitConverter.ReadUInt(frameBuffer);

                    if (frameSize > connection.MaxFrameSize)
                    {
                        throw new AmqpException(ErrorCode.InvalidField, $"Invalid frame size:{frameSize}, maximum frame size:{connection.MaxFrameSize}");
                    }

                    await ReceiveBufferAsync(frameBuffer.Buffer, frameBuffer.WriteOffset, (frameSize - FixedWidth.UInt)); // read sizeof(frame) - 4 bytes from socket

                    frameBuffer.ResetReadWrite(); // back to 0 to start reading
                    frameBuffer.AppendWrite(frameSize); // we shouldn't write... but that's okay

                    frameBuffer.ReadOnly = true; // mark read-only
                    if (!onFrame(frameBuffer))
                    {
                        break; // stop immediately
                    }
                    frameBuffer.ReadOnly = false;
                }
            }
            finally
            {
                trace.Debug("AsyncReceiverEventLoop() Stopping");
            }
        }

        /// <summary>
        /// Loop until "count" bytes have been read.
        /// </summary>
        private async Task ReceiveBufferAsync(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int received = await this.socket.ReceiveAsync(buffer, offset, count);
                offset += received;
                count -= received;
            }
        }
    }
}
