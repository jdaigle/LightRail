using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
using NLog;

namespace LightRail.Amqp.Protocol
{
    public class AmqpSession
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.Amqp.Protocol.AmqpSession");

        public AmqpSession(AmqpConnection connection, ushort channelNumber, ushort remoteChannelNumber)
        {
            this.connection = connection;
            this.ChannelNumber = channelNumber;
            this.RemoteChannelNumber = remoteChannelNumber;
            State = SessionStateEnum.UNMAPPED;

            nextOutgoingId = InitialOutgoingId;
        }

        public ushort ChannelNumber { get; }
        public ushort RemoteChannelNumber { get; private set; }
        public SessionStateEnum State { get; private set; }
        private readonly AmqpConnection connection;

        public const uint DefaultWindowSize = 1024;
        public const uint InitialOutgoingId = 1;

        /// <summary>
        /// The expected transfer-id of the next incoming transfer frame.
        /// </summary>
        private uint nextIncomingId;
        /// <summary>
        /// The max number of incoming transfer frames that the endpoint can currently receive.
        /// 
        /// This identifies a current max incoming transfer-id that can be computed by substracting
        /// one from the sum of nextIncomingId and incomingWindow.
        /// </summary>
        private uint incomingWindow;

        /// <summary>
        /// The transfer-id assigned to the next transfer frame.
        /// </summary>
        private uint nextOutgoingId;
        /// <summary>
        /// The max number of outgoing transfer frames that the endpoint can currently send.
        /// 
        /// This identifies a current max outgoing transfer-id that can be computed by substracting
        /// one from the sum of nextOutgoingId and outgoingWindow.
        /// </summary>
        private uint outgoingWindow;

        /// <summary>
        /// The remote-incoming-window reflects the maximum number of outgoing transfers that can
        /// be sent without exceeding the remote endpoint’s incoming-window. This value MUST be
        /// decremented after every transfer frame is sent, and recomputed when informed of the
        /// remote session endpoint state.
        /// </summary>
        private uint remoteIncomingWindow;

        /// <summary>
        /// The remote-outgoing-window reflects the maximum number of incoming transfers that MAY
        /// arrive without exceeding the remote endpoint’s outgoing-window. This value MUST be
        /// decremented after every incoming transfer frame is received, and recomputed when informed
        /// of the remote session endpoint state. When this window shrinks, it is an indication
        /// of outstanding transfers. Settling outstanding transfers can cause the window to grow.
        /// </summary>
        private uint remoteOutgoingWindow;

        internal void HandleSessionFrame(AmqpFrame frame)
        {
            try
            {
                if (frame is Begin)
                {
                    HandleBeginFrame(frame as Begin);
                    return;
                }
                if (!State.CanReceiveFrames())
                {
                    throw new AmqpException(ErrorCode.IllegalState, $"Received frame {frame.Descriptor.ToString()} but session state is {State.ToString()}.");
                }
                if (frame is End)
                {
                    HandleEndFrame(frame as End);
                    return;
                }
                if (frame is Flow)
                {
                    HandleFlowFrame(frame as Flow);
                    return;
                }
                // TODO: handle link frames
                throw new NotImplementedException("handle link frames");
                //return;
            }
            catch (AmqpException amqpException)
            {
                logger.Error(amqpException);
                EndSession(amqpException.Error);
            }
            catch (Exception fatalException)
            {
                logger.Fatal(fatalException, "Caught Fatal Top Level Exception in AmqpSession. Ending Session.");
                var error = new Error()
                {
                    Condition = ErrorCode.InternalError,
                    Description = "Caught Fatal Top Level Exception in AmqpSession. Ending Session.",
                };
                EndSession(error);
            }
        }

        public void SendFlow(Flow flow)
        {
            incomingWindow = DefaultWindowSize; // reset window
            flow.NextIncomingId = nextIncomingId;
            flow.NextOutgoingId = nextOutgoingId;
            flow.IncomingWindow = incomingWindow;
            flow.OutgoingWindow = outgoingWindow;
            SendFrame(flow);
        }

        public void SendFrame(Flow frame)
        {
            if (!State.CanSendFrames())
            {
                throw new AmqpException(ErrorCode.IllegalState, $"Cannot send frame when session state is {State.ToString()}.");
            }
            connection.SendFrame(frame, ChannelNumber);
        }

        private void HandleBeginFrame(Begin begin)
        {
            nextOutgoingId = InitialOutgoingId; // our next id
            incomingWindow = DefaultWindowSize; // our incoming window

            outgoingWindow = begin.IncomingWindow; // their incoming window
            nextIncomingId = begin.NextOutgoingId; // their next id

            remoteOutgoingWindow = begin.OutgoingWindow;
            remoteIncomingWindow = InitialOutgoingId + begin.IncomingWindow - nextOutgoingId;

            if (State == SessionStateEnum.BEGIN_SENT)
            {
                if (begin.RemoteChannel == null)
                {
                    throw new AmqpException(ErrorCode.InvalidField, "Expecting to receive RemoteChannel");
                }
                RemoteChannelNumber = begin.RemoteChannel.Value;
                State = SessionStateEnum.MAPPED;
                return;
            }
            else
            {
                State = SessionStateEnum.BEGIN_RCVD;

                // reset value and send back the frame
                begin.RemoteChannel = RemoteChannelNumber;
                begin.NextOutgoingId = nextOutgoingId;
                begin.IncomingWindow = incomingWindow;
                begin.OutgoingWindow = outgoingWindow;
                connection.SendFrame(begin, ChannelNumber);

                State = SessionStateEnum.MAPPED;
            }
        }

        private void HandleEndFrame(End end)
        {
            // TODO close links
            if (State == SessionStateEnum.MAPPED)
            {
                State = SessionStateEnum.END_RCVD;
            }
            else
            {
                State = SessionStateEnum.UNMAPPED;
            }
            EndSession(null);
        }

        private void HandleFlowFrame(Flow flow)
        {
            nextIncomingId = flow.NextOutgoingId; // their next id
            remoteOutgoingWindow = flow.OutgoingWindow; // their window

            if (flow.NextIncomingId.HasValue)
                remoteIncomingWindow = flow.NextIncomingId.Value + flow.IncomingWindow - nextOutgoingId;
            else
                remoteIncomingWindow = InitialOutgoingId + flow.IncomingWindow - nextOutgoingId;

            if (outgoingWindow > 0)
            {
                // TODO: flush queued outgoing transfers
            }

            if (flow.Handle.HasValue)
            {
                throw new NotImplementedException("TODO: Handle Link Flow Frame");
            }
        }

        public void EndSession(Error error)
        {
            if (State == SessionStateEnum.MAPPED || State == SessionStateEnum.END_RCVD)
            {
                connection.SendFrame(new End()
                {
                    Error = error,
                }, ChannelNumber);

                if (State == SessionStateEnum.MAPPED)
                    State = SessionStateEnum.END_SENT;

                if (State == SessionStateEnum.END_RCVD)
                    State = SessionStateEnum.UNMAPPED;
            }
            else if (error != null)
            {
                // no session to end, close the connection
                connection.CloseConnection(error);
            }
        }
    }
}
