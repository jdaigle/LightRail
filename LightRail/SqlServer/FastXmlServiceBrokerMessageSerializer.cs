using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace LightRail.SqlServer
{
    public static class FastXmlServiceBrokerMessageSerializer
    {
        public static void Serialize(OutgoingTransportMessage transportMessage, Stream outputStream)
        {
            using (var writer = new XmlTextWriter(outputStream, Encoding.UTF8))
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
