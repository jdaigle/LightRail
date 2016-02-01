using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Messaging;

namespace LightRail.Amqp.Protocol
{
    public class AmqpLink
    {
        private static readonly TraceSource trace = TraceSource.FromClass();

        private readonly object stateSyncRoot = new object();

        public AmqpLink(AmqpSession session, string name, uint localHandle, bool isReceiverLink, bool isInitiatingLink, uint remoteHandle)
        {
            this.Name = name;
            this.Session = session;
            this.LocalHandle = remoteHandle;
            this.IsReceiverLink = isReceiverLink;
            this.IsSenderLink = !isReceiverLink;
            this.IsInitiatingLink = isInitiatingLink;
            this.RemoteHandle = localHandle;
            this.State = LinkStateEnum.DETACHED;

            senderSettlementMode = LinkSenderSettlementModeEnum.Mixed;
            receiverSettlementMode = LinkReceiverSettlementModeEnum.First;

            deliveryCount = initialDeliveryCount = 0;
        }

        public string Name { get; }
        public uint LocalHandle { get; private set; }
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

        // sender fields
        private readonly uint initialDeliveryCount;
        /// <summary>
        /// Incremented whenever a message is sent.
        /// 
        /// delivery-limit = link-credit + delivery-count
        /// 
        /// Only the sender MAY independently modify this field.
        /// </summary>
        private uint deliveryCount;
        /// <summary>
        /// The maximum legal amount that the delivery-count can be increased by.
        /// 
        /// MUST be descreased when delivery-count is incremented to maintain delivery-limit
        /// 
        /// delivery-limit = link-credit + delivery-count
        /// 
        /// Only the receiver can independently choose a value for this field.
        /// </summary>
        private uint linkCredit;
        /// <summary>
        /// Indicates how the sender SHOULD behave when insufficient messages are
        /// available to consume the current link-creditt. If set, the sender will (after sending all available
        /// messages) advance the delivery-count as much as possible, consuming all link-credit, and
        /// send the flow state to the receiver.
        /// 
        /// The sender’s value is always the last known value indicated by the receiver.
        /// 
        /// Only the receiver can independently modify this field.
        /// </summary>
        private bool drainFlag;

        public void HandleLinkFrame(AmqpFrame frame, ByteBuffer buffer = null)
        {
            lock (stateSyncRoot)
            {
                try
                {
                    if (frame is Attach)
                        HandleAttachFrame(frame as Attach);
                    else if (frame is Flow)
                        HandleFlowFrame(frame as Flow);
                    else if (frame is Transfer)
                        HandleTransferFrame(frame as Transfer, buffer);
                    else if (frame is Disposition)
                        HandleDispositionFrame(frame as Disposition);
                    else if (frame is Detach)
                        HandleDetachFrame(frame as Detach);
                    else
                        throw new AmqpException(ErrorCode.IllegalState, $"Received frame {frame.Descriptor.ToString()} but link state is {State.ToString()}.");
                }
                catch (AmqpException amqpException)
                {
                    trace.Error(amqpException);
                    throw;
                    //DetachLink(amqpException.Error);
                }
                catch (Exception fatalException)
                {
                    trace.Fatal(fatalException, "Ending Session due to fatal exception.");
                    var error = new Error()
                    {
                        Condition = ErrorCode.InternalError,
                        Description = "Ending Session due to fatal exception: " + fatalException.Message,
                    };
                    throw;
                    //DetachLink(error);
                }
            }
        }

        private void HandleAttachFrame(Attach attach)
        {
            if (State != LinkStateEnum.DETACHED && State != LinkStateEnum.ATTACH_SENT)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Attach frame but link state is {State.ToString()}.");

            if (!IsInitiatingLink && IsSenderLink)
                senderSettlementMode = (LinkSenderSettlementModeEnum)attach.SenderSettlementMode;
            if (!IsInitiatingLink && IsReceiverLink)
                receiverSettlementMode = (LinkReceiverSettlementModeEnum)attach.ReceiverSettlementMode;

            if (State == LinkStateEnum.DETACHED)
            {
                State = LinkStateEnum.ATTACH_RECEIVED;

                attach.Handle = this.LocalHandle;
                attach.IsReceiver = this.IsReceiverLink;
                attach.SenderSettlementMode = (byte)senderSettlementMode;
                attach.ReceiverSettlementMode = (byte)receiverSettlementMode;
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
            Session.Connection.Container.OnLinkAttached(this);
        }

        private void HandleFlowFrame(Flow flow)
        {
            if (State != LinkStateEnum.ATTACHED && State != LinkStateEnum.DETACH_SENT && State != LinkStateEnum.DESTROYED)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Detach frame but link state is {State.ToString()}.");
            if (State == LinkStateEnum.DESTROYED)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Detach frame but link state is {State.ToString()}."); // TODO end session
            if (State == LinkStateEnum.DETACH_SENT)
                return; // ignore

            if (IsReceiverLink)
            {
                // flow control from sender
                if (flow.DeliveryCount.HasValue)
                    deliveryCount = flow.DeliveryCount.Value;
                // TODO: ignoring Available field for now
                //if (flow.Available.HasValue)
                //    available = flow.Available.Value;
                // TODO: respond to new flow control
            }

            if (IsSenderLink)
            {
                // flow control from receiver
                if (flow.LinkCredit.HasValue)
                    linkCredit = flow.LinkCredit.Value;
                drainFlag = flow.Drain ?? false;
                // TODO respond to new flow control
            }

            if (flow.Echo)
            {
                Session.SendFlow(new Flow()
                {
                    Handle = LocalHandle,
                    DeliveryCount = this.deliveryCount,
                    LinkCredit = this.linkCredit,
                    Available = 0,
                    Drain = false,
                    Echo = drainFlag,
                });
            }
        }

        public void SetLinkCredit(uint value)
        {
            if (IsSenderLink)
                throw new InvalidOperationException("Cannot set link-credit on a sender link");

            linkCredit = Math.Max(value, 0);

            Session.SendFlow(new Flow()
            {
                Handle = LocalHandle,
                DeliveryCount = this.deliveryCount,
                LinkCredit = this.linkCredit,
                Available = 0,
                Drain = false,
                Echo = drainFlag,
            });
        }

        private void HandleTransferFrame(Transfer transfer, ByteBuffer buffer)
        {
            if (State != LinkStateEnum.ATTACHED)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Transfer frame but link state is {State.ToString()}.");
            if (linkCredit <= 0)
                throw new AmqpException(ErrorCode.TransferLimitExceeded, "The link credit has dropped to 0. Wait for messages to finishing processing.");

            linkCredit--;
            deliveryCount++;

            Session.Connection.Container.OnTransferReceived(this, transfer, buffer);
        }

        private void HandleDispositionFrame(Disposition disposition)
        {
            throw new NotImplementedException();
        }

        private void HandleDetachFrame(Detach detach)
        {
            if (State != LinkStateEnum.ATTACHED && State != LinkStateEnum.DETACH_SENT && State != LinkStateEnum.DESTROYED)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Detach frame but link state is {State.ToString()}.");

            if (detach.Error != null)
            {
                trace.Debug("Detaching Link {0} Due to Error From Remote Link Endpoint: '{1}'", LocalHandle, detach.Error);
            }

            if (State == LinkStateEnum.ATTACHED)
                State = LinkStateEnum.DETACH_RECEIVED;

            DetachLink(detach.Closed);
        }

        public void DetachLink(bool destoryLink)
        {
            if (State == LinkStateEnum.ATTACHED || State == LinkStateEnum.DETACH_RECEIVED)
            {
                Session.SendFrame(new Detach()
                {
                    Handle = LocalHandle,
                    Closed = destoryLink,
                });

                if (State == LinkStateEnum.ATTACHED)
                {
                    State = LinkStateEnum.DETACH_SENT;
                    Session.UnmapLocalLink(this, destoryLink);
                }
                if (State == LinkStateEnum.DETACH_RECEIVED)
                {
                    State = LinkStateEnum.DETACHED;
                    Session.UnmapRemoteLink(this, destoryLink);
                }

            }
        }

        internal void Attach(string address)
        {
            lock (stateSyncRoot)
            {
                Session.SendFrame(new Attach()
                {
                    Name = this.Name,
                    Handle = this.LocalHandle,
                    IsReceiver = this.IsReceiverLink,
                    Source = IsReceiverLink ? new Source()
                    {
                        Address = address,
                    } : null,
                    Target = IsSenderLink ? new Target()
                    {
                        Address = address,
                    } : null,
                    InitialDelieveryCount = 0,
                });
                State = LinkStateEnum.ATTACH_SENT;
            }
        }
    }
}

