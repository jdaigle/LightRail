using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LightRail
{
    public interface IMessageSerializer
    {
        string Serialize(object data);
        void Serialize(object data, TextWriter output);
        object Deserialize(string text, string type);
        object Deserialize(TextReader reader, string type);
        string ContentType { get; }
    }
}
