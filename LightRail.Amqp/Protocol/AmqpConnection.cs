using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Network;
using NLog;

namespace LightRail.Amqp.Protocol
{
    public class AmqpConnection
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.Amqp.Protocol.AmqpConnection");

        private static readonly byte[] protocol0 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x00, 0x01, 0x00, 0x00 };
        private static readonly byte[] protocol1 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x01, 0x01, 0x00, 0x00 };
        private static readonly byte[] protocol2 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x02, 0x01, 0x00, 0x00 };

        public const uint DefaultMaxFrameSize = 256 * 1024;
        public const ushort DefaultMaxChannelCount = 256;
        public const uint DefaultIdleTimeout = 30 * 60 * 1000; // 30 min

        public AmqpConnection(ISocket socket, IContainer container)
        {
            this.socket = socket;
            Container = container;
            State = ConnectionStateEnum.START;
        }

        private readonly ISocket socket;

        /// <summary>
        /// The current state of the Connection.
        /// </summary>
        public ConnectionStateEnum State { get; private set; }

        public IContainer Container { get; }

        // start connection settings
        private uint connectionMaxFrameSize = 512; // initial
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

        private Dictionary<ushort, AmqpSession> RemoteChannelToSessionMap = new Dictionary<ushort, AmqpSession>();
        private Dictionary<ushort, AmqpSession> LocalChannelToSessionMap = new Dictionary<ushort, AmqpSession>();
        private ConcurrentStack<ushort> freeChannelNumbers = new ConcurrentStack<ushort>();
        private ushort nextChannelNumber = 1;

        /// <summary>
        /// Handles a buffered header (should be 8 byte buffer). Returns false the underyling connection should stop receiving.
        /// </summary>
        public bool HandleHeader(ByteBuffer buffer)
        {
            try
            {
                HandleHeaderNegotiation(buffer);
                return true;
            }
            catch (AmqpException amqpException)
            {
                logger.Error(amqpException);
                CloseConnection(amqpException.Error);
                return false;
            }
            catch (Exception fatalException)
            {
                logger.Fatal(fatalException, "Closing Connection due to fatal exception.");
                var error = new Error()
                {
                    Condition = ErrorCode.InternalError,
                    Description = "Closing Connection due to fatal exception: " + fatalException.Message,
                };
                CloseConnection(error);
                return false;
            }
        }

        /// <summary>
        /// Handles a buffered frame (variable length). Returns false the underyling connection should stop receiving.
        /// </summary>
        public bool HandleFrame(ByteBuffer buffer)
        {
            try
            {
                if (State.ShouldIgnoreReceivedData())
                    return true;
                if (State.IsExpectingProtocolHeader())
                {
                    socket.SendAsync(protocol0, 0, 8);
                    CloseConnection(new Error());
                    return false;
                }
                ushort remoteChannelNumber = 0;
                var frame = AmqpFrameCodec.DecodeFrame(buffer, out remoteChannelNumber);
                LastFrameReceivedDateTime = DateTime.UtcNow;
                if (frame == null)
                {
                    logger.Trace("Received Empty Frame");
                    return true;
                }
                if (logger.IsTraceEnabled)
                    logger.Trace("Received Frame: {0}", frame.ToString());
                HandleReceivedFrame(frame, remoteChannelNumber);
                return true;
            }
            catch (AmqpException amqpException)
            {
                logger.Error(amqpException);
                CloseConnection(amqpException.Error);
                return false;
            }
            catch (Exception fatalException)
            {
                logger.Fatal(fatalException, "Closing Connection due to fatal exception.");
                var error = new Error()
                {
                    Condition = ErrorCode.InternalError,
                    Description = "Closing Connection due to fatal exception: " + fatalException.Message,
                };
                CloseConnection(error);
                return false;
            }
        }

        public void OnIoException(Exception ex)
        {
            if (State != ConnectionStateEnum.END)
            {
                logger.Error(ex, "IO Exception Handled");
                CloseConnection(new Error()
                {
                    Condition = ErrorCode.InternalError,
                    Description = ex.Message,
                });
            }
        }

        private void HandleReceivedFrame(AmqpFrame frame, ushort remoteChannelNumber)
        {
            // TODO: clean up the Connection state machine
            if (State == ConnectionStateEnum.OPENED)
            {
                if (frame is Close)
                {
                    HandleCloseFrame(frame as Close);
                    return;
                }
                if (frame is Begin)
                {
                    HandleBeginFrame(frame as Begin, remoteChannelNumber);
                    return;
                }
                HandleSessionFrame(frame, remoteChannelNumber);
            }
            else if (State == ConnectionStateEnum.HDR_EXCH ||
                     State == ConnectionStateEnum.OPEN_SENT)
            {
                // expecting an "open" frame here
                var openFrame = frame as Open;
                if (openFrame == null)
                {
                    HandleExpectedOpenFrameMissing(frame);
                    return;
                }
                HandleOpenFrame(openFrame);
            }
            else if (State == ConnectionStateEnum.DISCARDING || State == ConnectionStateEnum.CLOSE_SENT)
            {
                // Check for close frame. Ignore all others.
                if (frame is Close)
                {
                    HandleCloseFrame(frame as Close);
                }
                return;
            }
            else if (State == ConnectionStateEnum.CLOSED_RCVD)
            {
                // no more packets expects, so just ignore
                // technically this is an invalid state for this implementation
                // since we will always immediately send the "close" frame in response
                return;
            }
            else if (State == ConnectionStateEnum.END)
            {
                // illegal for either endpoint to write anything, so just ignore
                return;
            }
            else if (State == ConnectionStateEnum.CLOSE_PIPE || State == ConnectionStateEnum.OC_PIPE)
            {
                throw new NotImplementedException("TODO: Not Yet Supporting Pipelined Connections.");
            }
            else
            {
                throw new AmqpException(ErrorCode.IllegalState, $"Received Frame {frame.Descriptor.ToString()} but current state is {State.ToString()}.");
            }
        }

        private void HandleBeginFrame(Begin begin, ushort remoteChannel)
        {
            AmqpSession session;
            if (RemoteChannelToSessionMap.TryGetValue(remoteChannel, out session))
            {
                session.HandleSessionFrame(begin);
                return;
            }
            if (begin.RemoteChannel.HasValue)
            {
                if (LocalChannelToSessionMap.TryGetValue(begin.RemoteChannel.Value, out session))
                {
                    RemoteChannelToSessionMap.Add(remoteChannel, session); // new mapping
                    session.HandleSessionFrame(begin);
                    return;
                }
                else
                {
                    throw new AmqpException(ErrorCode.NotFound, $"Session for remote channel number [{begin.RemoteChannel.Value}] not found.");
                }
            }
            ushort channelNumber;
            if (!freeChannelNumbers.TryPop(out channelNumber))
                channelNumber = nextChannelNumber++;

            // new session
            session = new AmqpSession(this, channelNumber, remoteChannel);
            RemoteChannelToSessionMap.Add(remoteChannel, session);
            LocalChannelToSessionMap.Add(session.ChannelNumber, session);
            session.HandleSessionFrame(begin);
        }

        private void HandleSessionFrame(AmqpFrame frame, ushort remoteChannel)
        {
            AmqpSession session;
            if (!RemoteChannelToSessionMap.TryGetValue(remoteChannel, out session))
            {
                throw new AmqpException(ErrorCode.NotFound, $"Session for channel number [{remoteChannel}] not found.");
            }
            session.HandleSessionFrame(frame);
        }

        internal void OnSessionUnmapped(AmqpSession session)
        {
            logger.Debug("Session {0} Unmapped", session.ChannelNumber);
            RemoteChannelToSessionMap.Remove(session.RemoteChannelNumber);
            LocalChannelToSessionMap.Remove(session.ChannelNumber);
            freeChannelNumbers.Push(session.ChannelNumber);
        }

        private void HandleExpectedOpenFrameMissing(AmqpFrame frame)
        {
            logger.Trace($"Excepted Open Frame. Instead Frame is {frame.Descriptor.ToString()}");
            CloseConnection(new Error()
            {
                Condition = ErrorCode.IllegalState,
                Description = $"Excepted Open Frame. Instead Frame is {frame.Descriptor.ToString()}",
            });
        }

        private void HandleOpenFrame(Open openFrame)
        {
            RemoteContainerId = openFrame.ContainerID;
            receivedHostname = openFrame.Hostname;
            receivedMaxFrameSize = openFrame.MaxFrameSize;
            receiveChannelMax = openFrame.ChannelMax;
            receivedIdleTimeout = openFrame.IdleTimeOut;

            connectionMaxFrameSize = Math.Min(DefaultMaxFrameSize, openFrame.MaxFrameSize);
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
                    MaxFrameSize = connectionMaxFrameSize,
                    ChannelMax = connectionChannelMax,
                    IdleTimeOut = connectionIdleTimeout,
                }, 0);
            }

            State = ConnectionStateEnum.OPENED;
        }

        private void HandleCloseFrame(Close close)
        {
            if (State == ConnectionStateEnum.DISCARDING || State == ConnectionStateEnum.CLOSE_SENT)
            {
                State = ConnectionStateEnum.END;
                CloseConnection(null);
                return;
            }
            State = ConnectionStateEnum.CLOSED_RCVD;
            if (close.Error != null)
            {
                logger.Debug("Closing with Error {0}-{1}", close.Error.Condition, close.Error.Description);
            }
            CloseConnection(null);
        }

        private void HandleHeaderNegotiation(ByteBuffer frameBuffer)
        {
            byte protocolId = 0;
            byte[] protocolVersion = null;
            if (!TryParseProtocolHeader(frameBuffer, out protocolId, out protocolVersion))
            {
                logger.Debug("Received Invalid Protocol Header");
                // invalid protocol header
                socket.SendAsync(protocol0, 0, 8);
                CloseConnection(new Error());
                return;
            }

            if (protocolVersion[0] != 1 ||
                protocolVersion[1] != 0 ||
                protocolVersion[2] != 0)
            {
                logger.Debug("Received Invalid Protocol Version");
                // invalid protocol version
                socket.SendAsync(protocol0, 0, 8);
                CloseConnection(new Error());
                return;
            }

            logger.Debug("Received Protocol Header AMQP.{0}.1.0.0", ((int)protocolId));

            // expecting a protocol header
            if (protocolId == 0x00)
            {
                socket.SendAsync(protocol0, 0, 8);
                State = ConnectionStateEnum.HDR_EXCH;
            }
            else if (protocolId == 0x01)
            {
                // TODO: not yet supported
                socket.SendAsync(protocol0, 0, 8);
                CloseConnection(new Error());
            }
            else if (protocolId == 0x02)
            {
                // TODO: not yet supported
                socket.SendAsync(protocol0, 0, 8);
                CloseConnection(new Error());
            }
            else
            {
                logger.Debug("Invalid Protocol ID AMQP.{0}.1.0.0!!", ((int)protocolId));
                // invalid protocol id
                socket.SendAsync(protocol0, 0, 8);
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

        public void SendFrame(AmqpFrame frame, ushort channelNumber)
        {
            // TODO: get pinned send buffer from socket to prevent an unneccessary array copy
            var buffer = new ByteBuffer((int)connectionMaxFrameSize, false);
            AmqpFrameCodec.EncodeFrame(buffer, frame, channelNumber);
            if (logger.IsTraceEnabled)
                logger.Trace("Sending Frame: {0}", frame.ToString());
            socket.SendAsync(buffer);
        }

        public void HandleSocketClosed()
        {
            logger.Debug("Closing connection due to socket closed.");
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
            foreach (var session in RemoteChannelToSessionMap.Values.ToList())
            {
                session.OnConnectionClosed(error);
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
                    logger.Debug("Closing Sending Side of Socket");
                    socket.CloseWrite();
                }
                if (State == ConnectionStateEnum.CLOSED_RCVD)
                {
                    State = ConnectionStateEnum.END;
                    logger.Debug("Closing Socket");
                    socket.Close();
                }
                return;
            }
            if (error != null)
            {
                State = ConnectionStateEnum.END;
                logger.Debug("Closing Socket");
                socket.Close();
            }
        }
    }
}
