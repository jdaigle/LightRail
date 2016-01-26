using System;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Types;
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

        public AmqpConnection(ISocket socket)
            : this(socket, null)
        {
        }

        public AmqpConnection(ISocket socket, AmqpSettings settings)
        {
            this.socket = socket;
            State = ConnectionStateEnum.START;
            containerId = settings?.ContainerId ?? Guid.NewGuid().ToString("N");
        }

        private readonly ISocket socket;
        public ConnectionStateEnum State { get; private set; }
        private string containerId = Guid.NewGuid().ToString("N");
        private Open receivedOpenFrame;
        private uint connectionMaxFrameSize = 512; // initial
        private ushort connectionChannelMax = 0; // initial
        private uint connectionIdleTimeout = DefaultIdleTimeout;
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

        public void HandleReceivedBuffer(ByteBuffer buffer)
        {
            try
            {
                while (buffer.LengthAvailableToRead > 0)
                {
                    if (State.ShouldIgnoreReceivedData())
                    {
                        return;
                    }
                    if (State.IsExpectingProtocolHeader())
                    {
                        HandleHeaderNegotiation(buffer);
                        continue;
                    }
                    var frame = AmqpFrameCodec.DecodeFrame(buffer);
                    LastFrameReceivedDateTime = DateTime.UtcNow;
                    if (frame == null)
                    {
                        logger.Trace("Received Empty Frame");
                        continue;
                    }
                    if (logger.IsTraceEnabled)
                        logger.Trace("Received Frame: {0}", frame.ToString());
                    HandleReceivedFrame(frame);
                }
            }
            catch (AmqpException amqpException)
            {
                logger.Error(amqpException);
                TrySendErrorFrame(amqpException.Error);
                //socket.Close();
            }
            catch (Exception fatalException)
            {
                logger.Fatal(fatalException, "Caught Fatal Top Level Exception. Closing Connection.");
                var errorFrame = new Error()
                {
                    Condition = ErrorCode.InternalError,
                    Description = "Caught Fatal Top Level Exception. Closing Connection.",
                };
                TrySendErrorFrame(errorFrame);
                //socket.Close();
            }
        }

        private static bool IsLikelyProtocolHeader(ByteBuffer frameBuffer)
        {
            if (frameBuffer.LengthAvailableToRead >= 8)
            {
                if (frameBuffer.Buffer[frameBuffer.ReadOffset + 0] == protocol0[0] &&
                    frameBuffer.Buffer[frameBuffer.ReadOffset + 1] == protocol0[1] &&
                    frameBuffer.Buffer[frameBuffer.ReadOffset + 2] == protocol0[2] &&
                    frameBuffer.Buffer[frameBuffer.ReadOffset + 3] == protocol0[3])
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleReceivedFrame(AmqpFrame frame)
        {
            if (State == ConnectionStateEnum.OPENED)
            {
                if (frame is Close)
                {
                    State = ConnectionStateEnum.CLOSED_RCVD;
                    var close = frame as Close;
                    if (close.Error != null)
                    {
                        logger.Debug("Closing with Error {0}-{1}", close.Error.Condition, close.Error.Description);
                    }
                    SendFrame(new Close(), 0);
                    CloseSocketConnection();
                    return;
                }
                // TODO pass frame to Session
                throw new NotImplementedException("TODO pass frame to session");
            }
            else if (State == ConnectionStateEnum.CLOSED_RCVD)
            {
                // no more packets expects, so just ignore
                // technically this is an invalid state for this implementation
                // since we will always immediately send the "close" frame in response
                return;
            }
            else if (State == ConnectionStateEnum.DISCARDING || State == ConnectionStateEnum.CLOSE_SENT)
            {
                // Check for close frame. Ignore all others.
                if (frame is Close)
                {
                    CloseSocketConnection();
                }
                return;
            }
            else if (State == ConnectionStateEnum.END)
            {
                // illegal for either endpoint to write anything, so just ignore
                return;
            }
            else if (State == ConnectionStateEnum.HDR_EXCH ||
                     State == ConnectionStateEnum.OPEN_SENT)
            {
                // expecting an "open" frame here
                receivedOpenFrame = frame as Open;
                if (receivedOpenFrame == null)
                {
                    throw new AmqpException(ErrorCode.IllegalState, $"Excepted Open Frame. Instead Frame is {frame.Descriptor.ToString()}");
                }
                connectionMaxFrameSize = Math.Min(DefaultMaxFrameSize, receivedOpenFrame.MaxFrameSize);
                connectionChannelMax = Math.Min(DefaultMaxChannelCount, receivedOpenFrame.ChannelMax);
                connectionIdleTimeout = DefaultIdleTimeout;
                if (receivedOpenFrame.IdleTimeOut.HasValue && receivedOpenFrame.IdleTimeOut > 0)
                {
                    connectionIdleTimeout = Math.Min(receivedOpenFrame.IdleTimeOut.Value, DefaultIdleTimeout);
                }
                if (State != ConnectionStateEnum.OPEN_SENT)
                {
                    SendFrame(new Open()
                    {
                        ContainerID = containerId,
                        Hostname = receivedOpenFrame.Hostname,
                        MaxFrameSize = connectionMaxFrameSize,
                        ChannelMax = connectionChannelMax,
                        IdleTimeOut = connectionIdleTimeout,
                    }, 0);
                }
                State = ConnectionStateEnum.OPENED;
            }
            else if (State == ConnectionStateEnum.OPEN_RCVD)
            {
                // technically this is an invalid state for this implementation
                // since we will always immediately send the "open" frame in response

                if (receivedOpenFrame == null)
                {
                    throw new AmqpException(ErrorCode.InternalError, $"Current state == OPEN_RCVD but openFrame == null");
                }

                SendFrame(new Open()
                {
                    ContainerID = containerId,
                    Hostname = receivedOpenFrame.Hostname,
                    MaxFrameSize = connectionMaxFrameSize,
                    ChannelMax = connectionChannelMax,
                    IdleTimeOut = connectionIdleTimeout,
                }, 0);
                // TODO what data did we get? Negotiate open and send back frame.
                CloseSocketConnection();
            }
            else if (State == ConnectionStateEnum.CLOSE_PIPE)
            {
                // TODO: currently illegal state
                CloseSocketConnection();
            }
            else
            {
                // have not finished protocol header negotiation
                throw new AmqpException(ErrorCode.IllegalState, $"Received Frame {frame.Descriptor.ToString()} but expecting a protocol header");
            }
        }

        private void SendFrame(AmqpFrame frame, ushort channelNumber)
        {
            // TODO: get pinned send buffer from socket to prevent an unneccessary array copy
            var buffer = new ByteBuffer((int)connectionMaxFrameSize, false);
            AmqpFrameCodec.EncodeFrame(buffer, frame, channelNumber);
            if (logger.IsTraceEnabled)
                logger.Trace("Sending Frame: {0}", frame.ToString());
            socket.SendAsync(buffer);
        }

        private void TrySendErrorFrame(Error errorFrame)
        {
            try
            {
                SendFrame(errorFrame, 0);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Fatal Exception Trying to Send Error Frame");
                socket.Close();
            }
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
                CloseSocketConnection();
                return;
            }

            if (protocolVersion[0] != 1 ||
                protocolVersion[1] != 0 ||
                protocolVersion[2] != 0)
            {
                logger.Debug("Received Invalid Protocol Version");
                // invalid protocol version
                socket.SendAsync(protocol0, 0, 8);
                CloseSocketConnection();
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
                CloseSocketConnection();
            }
            else if (protocolId == 0x02)
            {
                // TODO: not yet supported
                socket.SendAsync(protocol0, 0, 8);
                CloseSocketConnection();
            }
            else
            {
                logger.Debug("Invalid Protocol ID AMQP.{0}.1.0.0!!", ((int)protocolId));
                // invalid protocol id
                socket.SendAsync(protocol0, 0, 8);
                CloseSocketConnection();
            }
        }

        public void CloseDueToTimeout()
        {
            CloseConnection(new Error()
            {
                Condition = ErrorCode.ConnectionForced,
                Description = "Idle Timeout",
            });
        }

        public void CloseConnection(Error errorFrame)
        {
            if (State.CanWriteFrames())
            {
                SendFrame(new Close()
                {
                    Error = errorFrame
                }, 0);
                State = ConnectionStateEnum.CLOSE_SENT;
                if (errorFrame != null)
                {
                    State = ConnectionStateEnum.DISCARDING;
                }
                socket.CloseWrite();
            }
        }

        public void CloseSocketConnection()
        {
            State = ConnectionStateEnum.END;
            socket.Close();
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
    }
}
