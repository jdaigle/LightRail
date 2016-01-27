using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
using NLog;

namespace LightRail.Amqp.Protocol
{
    public class AmqpLink
    {
        private static readonly ILogger logger = LogManager.GetLogger("LightRail.Amqp.Protocol.AmqpLink");

        public AmqpLink(AmqpSession session, string name, uint localHandle, bool isReceiverLink, bool isInitiatingLink, uint remoteHandle)
        {
            this.Name = name;
            this.Session = session;
            this.LocalHandle = remoteHandle;
            this.IsReceiverLink = isReceiverLink;
            this.IsSenderLink = !isReceiverLink;
            this.IsInitiatingLink = isInitiatingLink;
            this.RemoteHandle = localHandle;
            this.State = LinkStateEnum.START;

            senderSettlementMode = LinkSenderSettlementModeEnum.Mixed;
            receiverSettlementMode = LinkReceiverSettlementModeEnum.First;

            deliveryCount = initialDeliveryCount = 0;
        }

        public string Name { get; }
        public uint LocalHandle { get; }
        /// <summary>
        /// This link receives messages.
        /// </summary>
        public bool IsReceiverLink { get; } // i.e. "Role" == true
        /// <summary>
        /// This link sends messages.
        /// </summary>
        public bool IsSenderLink { get; } // i.e. "Role" == false
        /// <summary>
        /// This link was the initiating end of link.
        /// </summary>
        public bool IsInitiatingLink { get; }
        public uint RemoteHandle { get; private set; }
        public AmqpSession Session { get; }

        public LinkStateEnum State { get; private set; }

        private LinkSenderSettlementModeEnum senderSettlementMode;
        private LinkReceiverSettlementModeEnum receiverSettlementMode;

        private readonly uint initialDeliveryCount;
        private uint deliveryCount;

        public void HandleLinkFrame(AmqpFrame frame)
        {
            try
            {
                if (frame is Attach)
                    HandleAttachFrame(frame as Attach);
                else if (frame is Flow)
                    HandleFlowFrame(frame as Flow);
                else if (frame is Transfer)
                    HandleTransferFrame(frame as Transfer);
                else if (frame is Disposition)
                    HandleDispositionFrame(frame as Disposition);
                else if (frame is Detach)
                    HandleDetachFrame(frame as Detach);
                else
                    throw new AmqpException(ErrorCode.IllegalState, $"Received frame {frame.Descriptor.ToString()} but link state is {State.ToString()}.");
            }
            catch (AmqpException amqpException)
            {
                logger.Error(amqpException);
                throw;
                //DetachLink(amqpException.Error);
            }
            catch (Exception fatalException)
            {
                logger.Fatal(fatalException, "Ending Session due to fatal exception.");
                var error = new Error()
                {
                    Condition = ErrorCode.InternalError,
                    Description = "Ending Session due to fatal exception: " + fatalException.Message,
                };
                throw;
                //DetachLink(error);
            }
        }

        private void HandleAttachFrame(Attach attach)
        {
            if (State != LinkStateEnum.START && State != LinkStateEnum.ATTACH_SENT)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Attach frame but link state is {State.ToString()}.");

            if (!IsInitiatingLink && IsSenderLink)
                senderSettlementMode = (LinkSenderSettlementModeEnum)attach.SendSettleMode;
            if (!IsInitiatingLink && IsReceiverLink)
                receiverSettlementMode = (LinkReceiverSettlementModeEnum)attach.ReceiveSettleMode;

            if (State == LinkStateEnum.START)
            {
                State = LinkStateEnum.ATTACH_RECEIVED;

                attach.Handle = this.LocalHandle;
                attach.Role = this.IsReceiverLink;
                attach.SendSettleMode = (byte)senderSettlementMode;
                attach.ReceiveSettleMode = (byte)receiverSettlementMode;
                attach.InitialDelieveryCount = this.initialDeliveryCount;

                // send back a cooresponding attach frame
                Session.SendFrame(attach);
            }

            if (State == LinkStateEnum.ATTACH_SENT)
            {
                if (IsReceiverLink)
                {
                    if (attach.InitialDelieveryCount == null)
                        throw new AmqpException(ErrorCode.InvalidField, "initial-delivery-count must be set on attach from of sender.");
                    // expecting initial delivery count
                    deliveryCount = attach.InitialDelieveryCount.Value;
                }
            }

            State = LinkStateEnum.ATTACHED;
        }

        private void HandleFlowFrame(Flow flow)
        {
            throw new NotImplementedException();
        }

        private void HandleTransferFrame(Transfer transfer)
        {
            throw new NotImplementedException();
        }

        private void HandleDispositionFrame(Disposition disposition)
        {
            throw new NotImplementedException();
        }

        private void HandleDetachFrame(Detach detach)
        {
            throw new NotImplementedException();
        }
    }
}
