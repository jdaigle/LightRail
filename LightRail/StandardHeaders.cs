using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public class StandardHeaders
    {
        /// <summary>
        /// The time (UTC) the current message was sent
        /// </summary>
        public const string TimeSent = "LightRail.TimeSent";
        /// <summary>
        /// The MessageId of the current message being handled.
        /// </summary>
        public const string MessageId = "LightRail.MessageId";
        /// <summary>
        /// The MessageId that caused this message to be sent.
        /// </summary>
        public const string RelatedTo = "LightRail.RelatedTo";
        /// <summary>
        /// The address to which replies can be sent. It may be different from "FromAddress".
        /// </summary>
        public const string ReplyToAddress = "LightRail.ReplyToAddress";
        /// <summary>
        /// The address from which the current message was sent. It may be different from "ReplyToAddress".
        /// </summary>
        public const string OriginatingAddress = "LightRail.OriginatingAddress";
        /// <summary>
        /// The encoded message's content type (e.g. json or xml).
        /// </summary>
        public const string ContentType = "LightRail.ContentType";
        /// <summary>
        /// A comma delimited list of known messages contracts implemented by the enclosed message body.
        /// </summary>
        public const string EnclosedMessageTypes = "LightRail.EnclosedMessageTypes";
        /// <summary>
        /// If this message is a timeout message, the amount of time requested in the timeout (in seconds).
        /// </summary>
        public const string TimeoutMessageTimeout = "LightRail.TimeoutMessageTimeout";
    }
}
