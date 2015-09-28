using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace LightRail
{
    public static class FastXmlTransportMessageSerializer
    {
        public static void Serialize(OutgoingTransportMessage transportMessage, Stream outputStream)
        {
            using (var writer = new XmlTextWriter(outputStream, new UnicodeEncoding(false, false))) // SQL Server is UTF16, little endian, no BOM
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartElement("root");
                foreach (var item in transportMessage.Headers)
                {
                    writer.WriteElementString(item.Key, item.Value);
                }
                writer.WriteStartElement("body");
                writer.WriteCData(transportMessage.SerializedMessageData);
                writer.WriteEndElement(); // </body>
                writer.WriteEndElement(); // </root>
                writer.Flush();
            }
        }

        public static IncomingTransportMessage Deserialize(string messageId, Stream inputStream)
        {
            var headers = new Dictionary<string,string>();
            var serializedMessageData = "";
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
                            headers.Add(elementName, reader.Value);

                            elementName = reader.Name;
                            reader.Read(); // read the child;
                        }
                        if (string.Equals(elementName, "body", StringComparison.InvariantCultureIgnoreCase) && reader.NodeType == XmlNodeType.CDATA)
                        {
                            serializedMessageData = reader.Value;
                        }
                        else if (reader.NodeType == XmlNodeType.Text)
                        {
                            headers.Add(elementName, reader.Value);
                        }
                    }
                }
            }
            return new IncomingTransportMessage(messageId, headers, serializedMessageData);
        }
    }
}
