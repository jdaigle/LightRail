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

            DeliveryCount = initialDeliveryCount = 0;
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

        public string SourceAddress { get; private set; }
        public string TargetAddress { get; private set; }

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
        public uint DeliveryCount { get; private set; }
        /// <summary>
        /// The maximum legal amount that the delivery-count can be increased by.
        /// 
        /// MUST be descreased when delivery-count is incremented to maintain delivery-limit
        /// 
        /// delivery-limit = link-credit + delivery-count
        /// 
        /// Only the receiver can independently choose a value for this field.
        /// </summary>
        public uint LinkCredit { get; private set; }
        /// <summary>
        /// The modulus operand for calculating when to send a Flow frame.
        /// </summary>
        private uint ReflowModulus;
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

        /// <summary>
        /// A "map" (actually a linked list for implementation purposes) of all
        /// unsettled deliveries either sent or received (depending on the role)
        /// by this link.
        /// </summary>
        private ConcurrentLinkedList<Delivery> unsettledMap = new ConcurrentLinkedList<Delivery>();

        public event EventHandler ReceivedFlow;

        private Delivery continuationDelivery;

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
                    else if (frame is Detach)
                        HandleDetachFrame(frame as Detach);
                    else
                        throw new AmqpException(ErrorCode.IllegalState, $"Received frame {frame.Descriptor.ToString()} but link state is {State.ToString()}.");
                }
                catch (AmqpException amqpException)
                {
                    trace.Error(amqpException);
                    DetachLink(amqpException.Error, destoryLink: true);
                }
                catch (Exception fatalException)
                {
                    trace.Fatal(fatalException, "Ending Session due to fatal exception.");
                    var error = new Error()
                    {
                        Condition = ErrorCode.InternalError,
                        Description = "Ending Session due to fatal exception: " + fatalException.Message,
                    };
                    DetachLink(error, destoryLink: true);
                }
            }
        }

        private void HandleAttachFrame(Attach attach)
        {
            if (State != LinkStateEnum.DETACHED && State != LinkStateEnum.ATTACH_SENT)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Attach frame but link state is {State.ToString()}.");

            if (!IsInitiatingLink && IsSenderLink)
            {
                senderSettlementMode = (LinkSenderSettlementModeEnum)attach.SenderSettlementMode;
                SourceAddress = attach.Source.Address;
            }
            if (!IsInitiatingLink && IsReceiverLink)
            {
                receiverSettlementMode = (LinkReceiverSettlementModeEnum)attach.ReceiverSettlementMode;
                TargetAddress = attach.Target.Address;
            }

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
                    DeliveryCount = attach.InitialDelieveryCount.Value;
                }
            }

            State = LinkStateEnum.ATTACHED;
            Session.Connection.Container.OnLinkAttached(this);
        }

        private void HandleFlowFrame(Flow flow)
        {
            if (State != LinkStateEnum.ATTACHED && State != LinkStateEnum.DETACH_SENT && State != LinkStateEnum.DESTROYED)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Flow frame but link state is {State.ToString()}.");
            if (State == LinkStateEnum.DESTROYED)
                throw new AmqpException(ErrorCode.ErrantLink, $"Received Flow frame but link state is {State.ToString()}."); // TODO end session
            if (State == LinkStateEnum.DETACH_SENT)
                return; // ignore

            if (IsReceiverLink)
            {
                // flow control from sender
                if (flow.DeliveryCount.HasValue)
                    DeliveryCount = flow.DeliveryCount.Value;
                // TODO: ignoring Available field for now
                //if (flow.Available.HasValue)
                //    available = flow.Available.Value;
                // TODO: respond to new flow control
            }

            if (IsSenderLink)
            {
                // flow control from receiver
                if (flow.LinkCredit.HasValue)
                    LinkCredit = flow.LinkCredit.Value;
                drainFlag = flow.Drain ?? false;
                // TODO respond to new flow control
                var receivedFlowCallback = this.ReceivedFlow;
                if (receivedFlowCallback != null)
                    receivedFlowCallback(this, EventArgs.Empty);
            }

            if (flow.Echo)
            {
                SendFlow(drain: false, echo: false);
            }
        }

        public void SetLinkCredit(uint value)
        {
            if (IsSenderLink)
                throw new InvalidOperationException("Cannot set link-credit on a sender link");

            LinkCredit = Math.Max(value, 0);
            if (LinkCredit >= 50)
                ReflowModulus = (uint)Math.Ceiling((double)LinkCredit * .1d); // flow frame after 10% of LinkCredit has been delivered
            else
                ReflowModulus = (uint)Math.Ceiling((double)LinkCredit * .5d); // flow frame after 50% of LinkCredit has been delivered

            SendFlow(drain: false, echo: false);
        }

        private void SendFlow(bool drain, bool echo)
        {
            Session.SendFlow(new Flow()
            {
                Handle = LocalHandle,
                DeliveryCount = this.DeliveryCount,
                LinkCredit = this.LinkCredit,
                Available = 0,
                Drain = drain,
                Echo = echo,
            });
        }

        private void HandleTransferFrame(Transfer transfer, ByteBuffer buffer)
        {
            if (State != LinkStateEnum.ATTACHED)
                throw new AmqpException(ErrorCode.IllegalState, $"Received Transfer frame but link state is {State.ToString()}.");
            if (LinkCredit <= 0)
                throw new AmqpException(ErrorCode.TransferLimitExceeded, "The link credit has dropped to 0. Wait for messages to finishing processing.");
            if (!IsReceiverLink)
                throw new AmqpException(ErrorCode.NotAllowed, "A Sender Link cannot receive Transfers.");

            Delivery delivery;
            if (continuationDelivery == null)
            {
                // new transfer
                delivery = new Delivery();
                delivery.Link = this;
                delivery.DeliveryId = transfer.DeliveryId.Value;
                delivery.DeliveryTag = transfer.DeliveryTag;
                delivery.Settled = transfer.Settled.IsTrue();
                delivery.State = transfer.State;
                delivery.PayloadBuffer = new ByteBuffer(buffer.LengthAvailableToRead, true);
                delivery.ReceiverSettlementMode = receiverSettlementMode;
                if (transfer.ReceiverSettlementMode.HasValue)
                {
                    delivery.ReceiverSettlementMode = (LinkReceiverSettlementModeEnum)transfer.ReceiverSettlementMode.Value;
                    if (receiverSettlementMode == LinkReceiverSettlementModeEnum.First &&
                        delivery.ReceiverSettlementMode == LinkReceiverSettlementModeEnum.Second)
                        throw new AmqpException(ErrorCode.InvalidField, "rcv-settle-mode: If the negotiated link value is first, then it is illegal to set this field to second.");
                }
            }
            else
            {
                // continuation
                if (transfer.DeliveryId.HasValue && transfer.DeliveryId.Value != continuationDelivery.DeliveryId)
                    throw new AmqpException(ErrorCode.NotAllowed, "Expecting Continuation Transfer but got a new Transfer.");
                if (transfer.DeliveryTag != null && !transfer.DeliveryTag.SequenceEqual(continuationDelivery.DeliveryTag))
                    throw new AmqpException(ErrorCode.NotAllowed, "Expecting Continuation Transfer but got a new Transfer.");
                delivery = continuationDelivery;
            }

            if (transfer.Aborted.IsTrue())
            {
                continuationDelivery = null;
                return; // ignore message
            }

            // copy and append the buffer (message payload) to the cached PayloadBuffer
            AmqpBitConverter.WriteBytes(delivery.PayloadBuffer, buffer.Buffer, buffer.ReadOffset, buffer.LengthAvailableToRead);

            if (transfer.More.IsTrue())
            {
                continuationDelivery = delivery;
                return; // expecting more payload
            }

            // assume transferred complete payload at this point
            continuationDelivery = null;

            if (!delivery.Settled)
            {
                Session.NotifyUnsettledIncomingDelivery(this, delivery);
            }

            LinkCredit--;
            DeliveryCount++;

            Session.Connection.Container.OnDelivery(this, delivery);
        }

        internal void NotifyOfDisposition(Delivery delivery, Disposition disposition)
        {
            try
            {
                // TODO: notify application of change in application state
            }
            catch (AmqpException amqpException)
            {
                trace.Error(amqpException);
                DetachLink(amqpException.Error, destoryLink: true);
            }
            catch (Exception fatalException)
            {
                trace.Fatal(fatalException, "Ending Session due to fatal exception.");
                var error = new Error()
                {
                    Condition = ErrorCode.InternalError,
                    Description = "Ending Session due to fatal exception: " + fatalException.Message,
                };
                DetachLink(error, destoryLink: true);
            }
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

            DetachLink(null, destoryLink: detach.Closed);
        }

        public void DetachLink(Error error, bool destoryLink)
        {
            if (State == LinkStateEnum.ATTACHED || State == LinkStateEnum.DETACH_RECEIVED || (State == LinkStateEnum.DETACH_SENT && destoryLink))
            {
                Session.SendFrame(new Detach()
                {
                    Handle = LocalHandle,
                    Error = error,
                    Closed = destoryLink,
                });
                if (State == LinkStateEnum.ATTACHED)
                {
                    State = LinkStateEnum.DETACH_SENT;
                }
                else if (State == LinkStateEnum.DETACH_RECEIVED)
                {
                    State = LinkStateEnum.DETACHED;
                    Session.UnmapLink(this, destoryLink);
                }
                else if (State == LinkStateEnum.DETACH_SENT && destoryLink)
                {
                    State = LinkStateEnum.DESTROYED;
                    Session.UnmapLink(this, destoryLink);
                }
            }
        }

        public void SetDeliveryTerminalState(Delivery delivery, DeliveryState state)
        {
            delivery.State = state;
            delivery.Settled = delivery.ReceiverSettlementMode == LinkReceiverSettlementModeEnum.First;
            if (delivery.Settled)
            {
                if (IsReceiverLink)
                {
                    LinkCredit++;
                    if (DeliveryCount % ReflowModulus == 0)
                        SendFlow(drain: false, echo: false);
                }
            }
            Session.SendDeliveryDisposition(this.IsReceiverLink, delivery, state, delivery.Settled);
        }

        public void SendTransfer(byte[] deliveryTag, ByteBuffer payloadBuffer)
        {
            var delivery = new Delivery();
            delivery.Link = this;
            delivery.DeliveryTag = deliveryTag;
            delivery.Settled = senderSettlementMode == LinkSenderSettlementModeEnum.Settled;
            delivery.State = null;
            delivery.PayloadBuffer = payloadBuffer;
            delivery.ReceiverSettlementMode = receiverSettlementMode;

            LinkCredit--;
            DeliveryCount++;

            Session.SendTransfer(delivery);
        }
    }
}

