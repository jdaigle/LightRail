﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Network;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Protocol
{
    public class AmqpConnection
    {
        private static readonly TraceSource trace = TraceSource.FromClass();

        private static readonly byte[] protocol0 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x00, 0x01, 0x00, 0x00 };
        private static readonly byte[] protocol2 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x02, 0x01, 0x00, 0x00 };
        private static readonly byte[] protocol3 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x03, 0x01, 0x00, 0x00 };

        public const uint DefaultMaxFrameSize = 64 * 1024;
        public const ushort DefaultMaxChannelCount = 256;
        public const uint DefaultIdleTimeout = 30 * 60 * 1000; // 30 min
        public const uint MinMaxFrameSize = 512;

        private readonly object stateSyncRoot = new object();

        public AmqpConnection(ISocket socket, IContainer container)
        {
            this.socket = socket;
            Container = container;
            State = ConnectionStateEnum.START;

            StartReceiveHeader(); // this begins the async receive pump
        }

        private readonly ISocket socket;

        /// <summary>
        /// The current state of the Connection.
        /// </summary>
        public ConnectionStateEnum State { get; private set; }

        public IContainer Container { get; }

        // start connection settings
        public uint MaxFrameSize { get; private set; } = MinMaxFrameSize; // initial
        private ushort connectionChannelMax = 0; // initial
        private uint connectionIdleTimeout = DefaultIdleTimeout;
        // end connection settings

        public string RemoteContainerId { get; private set; }
        private string receivedHostname;
        private uint? receivedMaxFrameSize;
        private ushort? receiveChannelMax;
        private uint? receivedIdleTimeout;

        /// <summary>
        /// The last DateTime at which a valid frame was received.
        /// </summary>
        public DateTime LastFrameReceivedDateTime { get; private set; }

        /// <summary>
        /// Returns true if we have not received a frame within
        /// the idle timeout window.
        /// </summary>
        public bool IsIdle()
        {
            // To avoid spurious timeouts, the value in idle-time-out SHOULD
            // be half the peer’s actual timeout threshold.
            return LastFrameReceivedDateTime.AddMilliseconds(DefaultIdleTimeout * 2) < DateTime.UtcNow;
        }

        private BoundedList<AmqpSession> localSessionMap = new BoundedList<AmqpSession>(2, DefaultMaxChannelCount);
        private BoundedList<AmqpSession> remoteSessionMap = new BoundedList<AmqpSession>(2, DefaultMaxChannelCount);

        private void StartReceiveHeader()
        {
            socket.ReceiveAsync(FixedWidth.ULong, OnHeaderReceived); // 8 bytes for header
        }

        /// <summary>
        /// Handles a buffered header (should be 8 byte buffer). Returns false the underyling connection should stop receiving.
        /// </summary>
        private void OnHeaderReceived(ByteBuffer buffer)
        {
            if (HandleHeader(buffer))
                StartReceiveFrame(buffer);
            // TODO: handle SASL frames?
        }

        /// <summary>
        /// Handles a buffered header (should be 8 byte buffer). Returns false the underyling connection should stop receiving.
        /// </summary>
        public bool HandleHeader(ByteBuffer buffer)
        {
            lock (stateSyncRoot)
            {
                try
                {
                    HandleHeaderNegotiation(buffer);
                    return true;
                }
                catch (AmqpException amqpException)
                {
                    trace.Error(amqpException);
                    CloseConnection(amqpException.Error);
                    return false;
                }
                catch (Exception fatalException)
                {
                    trace.Fatal(fatalException, "Closing Connection due to fatal exception.");
                    var error = new Error()
                    {
                        Condition = ErrorCode.InternalError,
                        Description = "Closing Connection due to fatal exception: " + fatalException.Message,
                    };
                    CloseConnection(error);
                    return false;
                }
            }
        }

        private void StartReceiveFrame(ByteBuffer buffer)
        {
            buffer.ResetReadWrite();
            socket.ReceiveAsync(FixedWidth.UInt, OnFrameLengthReceived);
        }

        private void OnFrameLengthReceived(ByteBuffer buffer)
        {
            int frameSize = (int)AmqpBitConverter.ReadUInt(buffer);
            if (frameSize > MaxFrameSize)
            {
                throw new AmqpException(ErrorCode.InvalidField, $"Invalid frame size:{frameSize}, maximum frame size:{MaxFrameSize}");
            }
            buffer.ResetReadWrite(); // back to 0 to start reading
            buffer.AppendWrite(FixedWidth.UInt); // advance the write the 4 bytes we already received
            // receive the rest of the frame
            socket.ReceiveAsync((frameSize - FixedWidth.UInt), OnFrameReceived);
        }

        private void OnFrameReceived(ByteBuffer buffer)
        {
            if (HandleFrame(buffer) && State.CanReceiveFrames())
                StartReceiveFrame(buffer); // loop if HandleFrame returns true
        }

        /// <summary>
        /// Handles a buffered frame (variable length). Returns false the underyling connection should stop receiving.
        /// </summary>
        public bool HandleFrame(ByteBuffer buffer)
        {
            lock (stateSyncRoot)
            {
                try
                {
                    if (State.IsExpectingProtocolHeader())
                        HandleHeaderNegotiation(buffer);
                    if (State.ShouldIgnoreReceivedData())
                        return true;
                    ushort remoteChannelNumber = 0;
                    var frame = AmqpCodec.DecodeFrame(buffer, out remoteChannelNumber);
                    LastFrameReceivedDateTime = DateTime.UtcNow;
                    if (frame == null)
                    {
                        trace.Debug("Received Empty Frame");
                        return true;
                    }
                    if (Trace.IsDebugEnabled)
                    {
                        var payloadSize = buffer.LengthAvailableToRead > 0 ? "Payload " + buffer.LengthAvailableToRead.ToString() + " Bytes" : "";
                        trace.Debug("RCVD CH({0}) {1} {2}", remoteChannelNumber.ToString(), frame.ToString(), payloadSize);

                    }
                    HandleConnectionFrame(frame, remoteChannelNumber, buffer);
                    return true;
                }
                catch (AmqpException amqpException)
                {
                    trace.Error(amqpException);
                    CloseConnection(amqpException.Error);
                    return false;
                }
                catch (Exception fatalException)
                {
                    trace.Fatal(fatalException, "Closing Connection due to fatal exception.");
                    var error = new Error()
                    {
                        Condition = ErrorCode.InternalError,
                        Description = "Closing Connection due to fatal exception: " + fatalException.Message,
                    };
                    CloseConnection(error);
                    return false;
                }
            }
        }

        public void NotifyIoException(Exception ex)
        {
            if (State != ConnectionStateEnum.END)
            {
                State = ConnectionStateEnum.END;
                trace.Error(ex, "IO Exception Handled");
                CloseConnection(new Error()
                {
                    Condition = ErrorCode.InternalError,
                    Description = ex.Message,
                });
            }
        }

        private void HandleConnectionFrame(AmqpFrame frame, ushort remoteChannelNumber, ByteBuffer buffer)
        {
            if (frame is Open)
                HandleOpenFrame(frame as Open);
            else if (frame is Begin)
                InterceptBeginFrame(frame as Begin, remoteChannelNumber);
            else if (frame is End)
                InterceptEndFrame(frame as End, remoteChannelNumber);
            else if (frame is Close)
                HandleCloseFrame(frame as Close);
            else
                HandleSessionFrame(frame, remoteChannelNumber, buffer);
        }

        private void HandleHeaderNegotiation(ByteBuffer frameBuffer)
        {
            byte protocolId = 0;
            byte[] protocolVersion = null;
            if (!TryParseProtocolHeader(frameBuffer, out protocolId, out protocolVersion))
            {
                trace.Debug("Received Invalid Protocol Header");
                // invalid protocol header
                if (!State.HasSentHeader())
                    socket.Write(new ByteBuffer(protocol0, 0, 8, 8));
                CloseConnection(new Error());
                return;
            }

            if (protocolVersion[0] != 1 ||
                protocolVersion[1] != 0 ||
                protocolVersion[2] != 0)
            {
                trace.Debug("Received Invalid Protocol Version");
                // invalid protocol version
                if (!State.HasSentHeader())
                    socket.Write(new ByteBuffer(protocol0, 0, 8, 8));
                CloseConnection(new Error());
                return;
            }

            trace.Debug("Received Protocol Header AMQP.{0}.1.0.0", ((int)protocolId));

            // expecting a protocol header
            if (protocolId == 0x00)
            {
                if (!State.HasSentHeader())
                    socket.Write(new ByteBuffer(protocol0, 0, 8, 8));
                if (State == ConnectionStateEnum.OPEN_PIPE)
                    State = ConnectionStateEnum.OPEN_SENT;
                else
                    State = ConnectionStateEnum.HDR_EXCH;
            }
            else if (protocolId == 0x02)
            {
                // TLS... no supported
                if (!State.HasSentHeader())
                    socket.Write(new ByteBuffer(protocol0, 0, 8, 8));
                CloseConnection(new Error());
            }
            else if (protocolId == 0x03)
            {
                // SASL... not yet supported
                if (!State.HasSentHeader())
                    socket.Write(new ByteBuffer(protocol3, 0, 8, 8));
                if (State == ConnectionStateEnum.OPEN_PIPE)
                    State = ConnectionStateEnum.OPEN_SENT;
                else
                    State = ConnectionStateEnum.HDR_EXCH;
            }
            else
            {
                trace.Debug("Invalid Protocol ID AMQP.{0}.1.0.0!!", ((int)protocolId));
                // invalid protocol id
                if (!State.HasSentHeader())
                    socket.Write(new ByteBuffer(protocol0, 0, 8, 8));
                CloseConnection(new Error());
            }
        }

        private static bool TryParseProtocolHeader(ByteBuffer frameBuffer, out byte protocolID, out byte[] protocolVersion)
        {
            protocolID = 0;
            protocolVersion = new byte[] { 0, 0, 0 };

            if (frameBuffer.LengthAvailableToRead < 8)
                return false;

            var valid = true;
            if (frameBuffer.Buffer[frameBuffer.ReadOffset + 0] != 'A' ||
                frameBuffer.Buffer[frameBuffer.ReadOffset + 1] != 'M' ||
                frameBuffer.Buffer[frameBuffer.ReadOffset + 2] != 'Q' ||
                frameBuffer.Buffer[frameBuffer.ReadOffset + 3] != 'P')
            {
                valid = false;
            }

            protocolID = frameBuffer.Buffer[frameBuffer.ReadOffset + 4];
            protocolVersion[0] = frameBuffer.Buffer[frameBuffer.ReadOffset + 5];
            protocolVersion[1] = frameBuffer.Buffer[frameBuffer.ReadOffset + 6];
            protocolVersion[2] = frameBuffer.Buffer[frameBuffer.ReadOffset + 7];

            frameBuffer.CompleteRead(8);
            return valid;
        }

        private void HandleOpenFrame(Open openFrame)
        {
            if (State != ConnectionStateEnum.HDR_EXCH && State != ConnectionStateEnum.OPEN_SENT)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Open Frame but current state is {State.ToString()}.");

            RemoteContainerId = openFrame.ContainerID;
            receivedHostname = openFrame.Hostname;
            receivedMaxFrameSize = openFrame.MaxFrameSize;
            receiveChannelMax = openFrame.ChannelMax;
            receivedIdleTimeout = openFrame.IdleTimeOut;

            MaxFrameSize = Math.Min(DefaultMaxFrameSize, openFrame.MaxFrameSize);
            connectionChannelMax = Math.Min(DefaultMaxChannelCount, openFrame.ChannelMax);
            connectionIdleTimeout = DefaultIdleTimeout;
            if (openFrame.IdleTimeOut.HasValue && openFrame.IdleTimeOut > 0)
            {
                connectionIdleTimeout = Math.Min(openFrame.IdleTimeOut.Value, DefaultIdleTimeout);
            }

            if (State != ConnectionStateEnum.OPEN_SENT)
            {
                SendFrame(new Open()
                {
                    ContainerID = Container.ContainerId,
                    Hostname = openFrame.Hostname,
                    MaxFrameSize = MaxFrameSize,
                    ChannelMax = connectionChannelMax,
                    IdleTimeOut = connectionIdleTimeout,
                }, 0);
            }

            State = ConnectionStateEnum.OPENED;
        }

        private void InterceptBeginFrame(Begin begin, ushort remoteChannel)
        {
            if (State != ConnectionStateEnum.OPENED)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Begin Frame but current state is {State.ToString()}.");

            AmqpSession session;
            session = GetSessionFromRemoteChannel(remoteChannel, false);
            if (session != null)
            {
                session.HandleSessionFrame(begin);
                return;
            }
            if (begin.RemoteChannel.HasValue)
            {
                session = GetSessionFromLocalChannel(begin.RemoteChannel.Value, false);
                if (session != null)
                {
                    remoteSessionMap[remoteChannel] = session; // new mapping
                    session.HandleSessionFrame(begin);
                    return;
                }
                else
                {
                    throw new AmqpException(ErrorCode.NotFound, $"Session for remote channel number [{begin.RemoteChannel.Value}] not found.");
                }
            }

            var nextLocalChannel = (ushort?)localSessionMap.IndexOfFirstNullItem() ?? (ushort)localSessionMap.Length; // reuse existing channel, or just grab the next one
            // new session
            session = new AmqpSession(this, nextLocalChannel, remoteChannel);
            localSessionMap[session.ChannelNumber] = session;
            remoteSessionMap[session.RemoteChannelNumber] = session;
            session.HandleSessionFrame(begin);
        }

        private void InterceptEndFrame(End end, ushort remoteChannelNumber)
        {
            if (State != ConnectionStateEnum.OPENED)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Begin Frame but current state is {State.ToString()}.");

            HandleSessionFrame(end, remoteChannelNumber);
        }

        private AmqpSession GetSessionFromRemoteChannel(ushort remoteChannel, bool throwException)
        {
            AmqpSession session = null;
            if (remoteChannel < remoteSessionMap.Length)
                session = remoteSessionMap[remoteChannel];
            if (session == null && throwException)
                throw new AmqpException(ErrorCode.NotFound, $"The remote session channel {remoteChannel} could not be found.");
            return session;
        }

        public AmqpSession GetSessionFromLocalChannel(ushort channel, bool throwException)
        {
            AmqpSession session = null;
            if (channel < localSessionMap.Length)
                session = localSessionMap[channel];
            if (session == null && throwException)
                throw new AmqpException(ErrorCode.NotFound, $"The local session channel {channel} could not be found.");
            return session;
        }

        private void HandleSessionFrame(AmqpFrame frame, ushort remoteChannel, ByteBuffer buffer = null)
        {
            if (State != ConnectionStateEnum.OPENED)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Begin Frame but current state is {State.ToString()}.");

            var session = GetSessionFromRemoteChannel(remoteChannel, true);
            session.HandleSessionFrame(frame, buffer);
        }

        internal void NotifySessionUnmapped(AmqpSession session)
        {
            trace.Debug("Session {0} Unmapped", session.ChannelNumber);
            localSessionMap[session.ChannelNumber] = null;
            remoteSessionMap[session.RemoteChannelNumber] = null;
        }

        private void HandleCloseFrame(Close close)
        {
            if (State != ConnectionStateEnum.OPENED && State != ConnectionStateEnum.CLOSE_SENT && State != ConnectionStateEnum.DISCARDING)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Open Frame but current state is {State.ToString()}.");

            if (State == ConnectionStateEnum.DISCARDING || State == ConnectionStateEnum.CLOSE_SENT)
            {
                State = ConnectionStateEnum.END;
                CloseConnection(null);
                return;
            }

            State = ConnectionStateEnum.CLOSED_RCVD;
            if (close.Error != null)
            {
                trace.Debug("Closing with Error {0}-{1}", close.Error.Condition, close.Error.Description);
            }
            CloseConnection(null);
        }

        internal void SendFrame(AmqpFrame frame, ushort channelNumber)
        {
            // TODO: get pinned send buffer from socket to prevent an unneccessary array copy
            var buffer = new ByteBuffer((int)MaxFrameSize, false);
            AmqpCodec.EncodeFrame(buffer, frame, channelNumber);
            if (Trace.IsDebugEnabled)
                trace.Debug("SEND CH({0}) {1}", channelNumber.ToString(), frame.ToString());
            socket.Write(buffer);
        }

        internal void SendBuffer(ByteBuffer buffer)
        {
            socket.Write(buffer);
        }

        public void HandleSocketClosed()
        {
            trace.Debug("Closing connection due to socket closed.");
            State = ConnectionStateEnum.END;
            CloseConnection(new Error());
        }

        public void CloseDueToTimeout()
        {
            CloseConnection(new Error()
            {
                Condition = ErrorCode.ConnectionForced,
                Description = "Idle Timeout",
            });
        }

        public void CloseConnection(Error error)
        {
            if (localSessionMap != null)
            {
                for (ushort i = 0; i < localSessionMap.Length; i++)
                {
                    if (localSessionMap[i] != null)
                        localSessionMap[i].NotifyConnectionClosed(error);
                }
            }

            if (State.CanSendFrames())
            {
                SendFrame(new Close()
                {
                    Error = error
                }, 0);
                if (State != ConnectionStateEnum.CLOSED_RCVD)
                {
                    State = ConnectionStateEnum.CLOSE_SENT;
                    if (error != null)
                    {
                        State = ConnectionStateEnum.DISCARDING;
                    }
                    trace.Debug("Closing Sending Side of Socket");
                    socket.CloseWrite();
                }
                if (State == ConnectionStateEnum.CLOSED_RCVD)
                {
                    State = ConnectionStateEnum.END;
                    trace.Debug("Closing Socket");
                    socket.Close();
                }
                return;
            }
            if (error != null)
            {
                State = ConnectionStateEnum.END;
                trace.Debug("Closing Socket Due To Error");
                socket.Close();
            }
        }
    }
}
