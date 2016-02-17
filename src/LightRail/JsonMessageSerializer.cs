using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jil;

namespace LightRail
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public string ContentType { get { return "application/json"; } }

        public string Serialize(object data)
        {
            return Jil.JSON.Serialize(data, Options.ISO8601IncludeInheritedUtc);
        }

        public void Serialize(object data, TextWriter output)
        {
            JSON.Serialize(data, output, Options.ISO8601IncludeInheritedUtc);
        }

        public object Deserialize(string text, Type type)
        {
            return JSON.Deserialize(text, type, Options.ISO8601IncludeInheritedUtc);
        }

        public object Deserialize(TextReader reader, Type type)
        {
            return JSON.Deserialize(reader, type, Options.ISO8601IncludeInheritedUtc);
        }
    }
}
