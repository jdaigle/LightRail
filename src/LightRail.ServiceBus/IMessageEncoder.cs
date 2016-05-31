using System;
using System.IO;

namespace LightRail.ServiceBus
{
    public interface IMessageEncoder
    {
        /// <summary>
        /// Encodes a message into a byte[] suitable for transmit over the wire.
        /// </summary>
        byte[] Encode(object message);
        /// <summary>
        /// Encodes a message into a unicode string for transmit over the wire.
        /// </summary>
        string EncodeAsString(object message);
        /// <summary>
        /// Encodes a message writing to TextWriter suitable for transmit over the wire.
        /// </summary>
        void Encode(object message, TextWriter output);

        /// <summary>
        /// Attempts to decodes specified object type from a byte[]
        /// </summary>
        object Decode(byte[] buffer, Type type);
        /// <summary>
        /// Attempts to decodes specified object type from a text reader
        /// </summary>
        object Decode(TextReader reader, Type type);

        /// <summary>
        /// AKA: MIME type or media type. For example: "application/json; charset=UTF-8"
        /// </summary>
        string ContentType { get; }
    }
}
