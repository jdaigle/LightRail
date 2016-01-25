using System;
using LightRail.Amqp.Types;

namespace LightRail.Amqp.Framing
{
    public sealed class Error : AmqpFrame
    {
        public Error()
            : base(FrameCodec.Error)
        {
        }

        /// <summary>
        /// A symbolic value indicating the error condition.
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// This text supplies any supplementary details not indicated by the condition field. This text can be
        /// logged as an aid to resolving issues.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Map carrying information about the error condition.
        /// </summary>
        public Fields Info { get; set; }

        protected override int CalculateListSize()
        {
            if (Info != null && Info.Count > 0) return 3;
            if (Description != null) return 2;
            return 1;
        }

        protected override void EncodeListItem(ByteBuffer buffer, int index, bool arrayEncoding)
        {
            if (index == 1)
                Encoder.WriteSymbol(buffer, Condition, arrayEncoding);
            if (index == 2)
                Encoder.WriteString(buffer, Description, arrayEncoding);
            if (index == 3)
                Encoder.WriteMap(buffer, Info, arrayEncoding);
        }

        protected override void DecodeListItem(ByteBuffer buffer, int index)
        {
            throw new NotImplementedException();
        }
    }
}
