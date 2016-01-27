using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    /// <summary>
    /// Inform remote peer of delivery state changes.
    /// 
    /// <type name = "disposition" class="composite" source="list" provides="frame">
    ///     <descriptor name = "amqp:disposition:list" code="0x00000000:0x00000015"/>
    ///     <field name = "role" type="role" mandatory="true"/>
    ///     <field name = "first" type="delivery-number" mandatory="true"/>
    ///     <field name = "last" type="delivery-number"/>
    ///     <field name = "settled" type="boolean" default="false"/>
    ///     <field name = "state" type="*" requires="delivery-state"/>
    ///     <field name = "batchable" type="boolean" default="false"/>
    /// </type>
    /// 
    /// The disposition frame is used to inform the remote peer of local changes in the state of deliveries.The disposition
    /// frame MAY reference deliveries from many different links associated with a session, although all links MUST have
    /// the directionality indicated by the specified role.
    /// 
    /// Note that it is possible for a disposition sent from sender to receiver to refer to a delivery which has not yet
    /// completed (i.e., a delivery which is spread over multiple frames and not all frames have yet been sent). The use
    /// of such interleaving is discouraged in favor of carrying the modified state on the next transfer performative for
    /// the delivery.
    /// 
    /// The disposition performative MAY refer to deliveries on links that are no longer attached.As long as the links
    /// have not been closed or detached with an error then the deliveries are still “live” and the updated state MUST be
    /// applied.
    /// </summary>
    public sealed class Disposition : AmqpFrame
    {
        public Disposition() : base(DescribedListCodec.Disposition) { }

        /// <summary>
        /// The role identifies whether the disposition frame contains information about sending link endpoints
        /// or receiving link endpoints.
        /// </summary>
        [AmqpDescribedListIndex(0)]
        public bool Role { get; set; }

        /// <summary>
        /// Identifies the lower bound of delivery-ids for the deliveries in this set.
        /// </summary>
        [AmqpDescribedListIndex(1)]
        public uint First { get; set; }

        /// <summary>
        /// Identifies the upper bound of delivery-ids for the deliveries in this set. If not set, this is taken to be
        /// the same as first.
        /// </summary>
        [AmqpDescribedListIndex(2)]
        public uint? Last { get; set; }

        /// <summary>
        /// If true, indicates that the referenced deliveries are considered settled by the issuing endpoint
        /// </summary>
        [AmqpDescribedListIndex(3)]
        public bool Settled { get; set; } = false;

        /// <summary>
        /// Communicates the state of all the deliveries referenced by this disposition.
        /// </summary>
        [AmqpDescribedListIndex(4)]
        public Outcome State { get; set; }

        /// <summary>
        /// If true, then the issuer is hinting that there is no need for the peer to urgently communicate the
        /// impact of the updated delivery states.This hint MAY be used to artificially increase the amount of
        /// batching an implementation uses when communicating delivery states, and thereby save bandwidth.
        /// </summary>
        [AmqpDescribedListIndex(5)]
        public bool Batchable { get; set; } = false;
    }
}
