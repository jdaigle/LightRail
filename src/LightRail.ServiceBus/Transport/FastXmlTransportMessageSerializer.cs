using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace LightRail.ServiceBus.Transport
{
    public static class FastXmlTransportMessageSerializer
    {
        private const string RootElementName = "root";
        private const string BodyElementName = "body";

        public static void Serialize(IDictionary<string, string> headers, string body, Stream outputStream)
        {
            var writer = new XmlTextWriter(outputStream, new UnicodeEncoding(false, false)); // SQL Server is UTF16, little endian, no BOM

            writer.Formatting = Formatting.Indented;
            writer.WriteStartElement(RootElementName);
            foreach (var item in headers)
            {
                writer.WriteElementString(item.Key, item.Value);
            }
            writer.WriteStartElement(BodyElementName);
            writer.WriteCData(body);
            writer.WriteEndElement(); // </body>
            writer.WriteEndElement(); // </root>
            writer.Flush();
        }

        public static FastXmlTransportMessageSerializerResult Deserialize(Stream inputStream)
        {
            var result = new FastXmlTransportMessageSerializerResult();
            using (var reader = new XmlTextReader(inputStream))
            {
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.Read(); // read <root>
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var elementName = reader.Name;
                        reader.Read(); // read the child;
                        while (reader.NodeType == XmlNodeType.Element)
                        {
                            // likely an empty header element node
                            result.Headers.Add(elementName, reader.Value);

                            elementName = reader.Name;
                            reader.Read(); // read the child;
                        }
                        if (string.Equals(elementName, BodyElementName, StringComparison.InvariantCultureIgnoreCase) && reader.NodeType == XmlNodeType.CDATA)
                        {
                            result.Body = reader.Value;
                        }
                        else if (reader.NodeType == XmlNodeType.Text)
                        {
                            result.Headers.Add(elementName, reader.Value);
                        }
                    }
                }
            }
            return result;
        }

        public class FastXmlTransportMessageSerializerResult
        {
            public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
            public string Body { get; internal set; } = "";
        }
    }
}
