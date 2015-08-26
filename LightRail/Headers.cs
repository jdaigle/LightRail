using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public static class Headers
    {
        public const string TimeSent = "LightRail.TimeSent";
        public const string MessageId = "LightRail.MessageId";
        /// <summary>
        /// The MessageId that caused this message to be sent
        /// </summary>
        public const string RelatedTo = "LightRail.RelatedTo";
        public const string ReplyToAddress = "LightRail.ReplyToAddress";
        public const string ContentType = "LightRail.ContentType";
        public const string EnclosedMessageTypes = "LightRail.EnclosedMessageTypes";

        /// <summary>
        /// The number of second-level retries that has been performed for this message.
        /// </summary>
        public const string Retries = "LightRail.Retries";

        /// <summary>
        /// The number of first-level retries that has been performed for this message.
        /// </summary>
        public const string FLRetries = "LightRail.FLRetries";
    }
}
