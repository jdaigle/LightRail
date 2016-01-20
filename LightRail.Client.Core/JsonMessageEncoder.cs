using System;
using System.IO;
using System.Text;
using Jil;

namespace LightRail.Client
{
    public class JsonMessageEncoder : IMessageEncoder
    {
        public string ContentType { get { return "application/json; charset=UTF-8"; } }

        public byte[] Encode(object message)
        {
            return Encoding.UTF8.GetBytes(JSON.Serialize(message, Options.ISO8601IncludeInheritedUtc));
        }

        public void Encode(object message, TextWriter output)
        {
            JSON.Serialize(message, output, Options.ISO8601IncludeInheritedUtc);
        }

        public object Decode(byte[] buffer, Type type)
        {
            return JSON.Deserialize(Encoding.UTF8.GetString(buffer), type, Options.ISO8601IncludeInheritedUtc);
        }

        public object Decode(TextReader reader, Type type)
        {
            return JSON.Deserialize(reader, type, Options.ISO8601IncludeInheritedUtc);
        }
    }
}
