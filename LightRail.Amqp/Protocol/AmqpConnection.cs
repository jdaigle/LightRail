using System;
using LightRail.Amqp.Framing;
using NLog;

namespace LightRail.Amqp.Protocol
{
    public class AmqpConnection
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.Amqp.Protocol.AmqpConnection");

        private static readonly byte[] protocol0 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x00, 0x01, 0x00, 0x00 };
        private static readonly byte[] protocol1 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x01, 0x01, 0x00, 0x00 };
        private static readonly byte[] protocol2 = new byte[] { 0x41, 0x4D, 0x51, 0x50, 0x02, 0x01, 0x00, 0x00 };

        public AmqpConnection(ISocket socket)
        {
            this.socket = socket;
            State = ConnectionStateEnum.START;
        }

        private readonly ISocket socket;
        public ConnectionStateEnum State { get; private set; }
        private Open openFrame;

        public void HandleReceivedFrame(ByteBuffer frameBuffer)
        {
            try
            {
                HandleReceivedFrameInner(frameBuffer);
            }
            catch (AmqpException amqpException)
            {
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

        private void HandleReceivedFrameInner(ByteBuffer frameBuffer)
        {
            while (frameBuffer.LengthAvailableToRead > 0)
            {
                if (State == ConnectionStateEnum.OPENED)
                {
                    // TODO pass frame to Session
                    throw new NotImplementedException("TODO pass frame to session");
                }
                else if (State == ConnectionStateEnum.CLOSED_RCVD)
                {
                    // no more packets expects, so just ignore
                    throw new NotImplementedException("TODO pass frame to session");
                }
                else if (State == ConnectionStateEnum.DISCARDING || State == ConnectionStateEnum.CLOSE_SENT)
                {
                    // TODO: check for close frame, ignore all others.
                    throw new NotImplementedException("Check for close Frame. Ignore All Others.");
                    //return;
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
                    var frame = FrameCodec.DecodeFrame(frameBuffer);
                    openFrame = frame as Open;
                    if (openFrame == null)
                    {
                        throw new AmqpException(ErrorCode.IllegalState, $"Excepted Open Frame. Instead Frame is {frame.Descriptor.ToString()}");
                    }
                    if (State != ConnectionStateEnum.OPEN_SENT)
                    {
                        SendOpenFrame();
                    }
                    State = ConnectionStateEnum.OPENED;
                }
                else if (State == ConnectionStateEnum.OPEN_RCVD)
                {
                    if (openFrame == null)
                    {
                        throw new AmqpException(ErrorCode.InternalError, $"Current state == OPEN_RCVD but openFrame == null");
                    }
                    SendOpenFrame();
                    // TODO what data did we get? Negotiate open and send back frame.
                    EndConnection();
                }
                else if (State == ConnectionStateEnum.CLOSE_PIPE)
                {
                    // TODO: currently illegal state
                    EndConnection();
                }
                else
                {
                    // have not finished protocol header negotiation
                    HandleHeaderNegotiation(frameBuffer);
                }
            }
        }

        private void SendOpenFrame()
        {
            // TODO:
        }

        private void TrySendErrorFrame(Error errorFrame)
        {
            try
            {
                var buffer = new ByteBuffer(100, true);
                errorFrame.Encode(buffer);
                socket.SendAsync(buffer);
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
            if (frameBuffer.LengthAvailableToRead >= 8)
            {
                if (!TryParseProtocolHeader(frameBuffer, out protocolId, out protocolVersion))
                {
                    logger.Debug("Received Invalid Protocol Header");
                    // invalid protocol header
                    EndConnection();
                    return;
                }
            }
            else
            {

            }

            if (protocolVersion[0] != 1 ||
                protocolVersion[1] != 0 ||
                protocolVersion[2] != 0)
            {
                logger.Debug("Received Invalid Protocol Version");
                // invalid protocol version
                socket.SendAsync(protocol0, 0, 8);
                EndConnection();
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
                EndConnection();
            }
            else if (protocolId == 0x02)
            {
                // TODO: not yet supported
                socket.SendAsync(protocol0, 0, 8);
                EndConnection();
            }
            else
            {
                logger.Debug("Invalid Protocol ID AMQP.{0}.1.0.0!!", ((int)protocolId));
                // invalid protocol id
                socket.SendAsync(protocol0, 0, 8);
                EndConnection();
            }
        }

        private void EndConnection()
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
            if (frameBuffer.Buffer[frameBuffer.ReadOffset + 0] != 'A' &&
                frameBuffer.Buffer[frameBuffer.ReadOffset + 1] != 'M' &&
                frameBuffer.Buffer[frameBuffer.ReadOffset + 2] != 'Q' &&
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
