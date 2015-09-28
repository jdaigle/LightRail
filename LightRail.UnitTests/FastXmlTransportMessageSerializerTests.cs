using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LightRail
{
    [TestFixture]
    public class FastXmlTransportMessageSerializerTests
    {
        [Test]
        public void Can_Serialize_And_Deserialize()
        {
            var outgoing = new OutgoingTransportMessage(new Dictionary<string,string>(), new object(), "foobar");
            var incoming = TestSerializeDeserialize(outgoing);
            Assert.IsNotNull(incoming);
        }

        [Test]
        public void Can_Serialize_And_Deserialize_Empty_Headers()
        {
            var headers = new Dictionary<string, string>();
            headers["first"] = Guid.NewGuid().ToString();
            headers["second"] = "";
            headers["third"] = Guid.NewGuid().ToString();
            
            var outgoing = new OutgoingTransportMessage(headers, new object(), "foobar");
            var incoming = TestSerializeDeserialize(outgoing);

            Assert.AreEqual(headers["first"], incoming.Headers["first"]);
            Assert.AreEqual(headers["second"], incoming.Headers["second"]);
            Assert.AreEqual(headers["third"], incoming.Headers["third"]);
        }

        [Test]
        public void Can_Serialize_And_Deserialize_Complex_Body()
        {
            var body = "<xml>test</xml>!@#$%^&*()blahblahblahasdf asdf 241-3-4 1234 -12!!~";
            var outgoing = new OutgoingTransportMessage(new Dictionary<string, string>(), new object(), body);
            var incoming = TestSerializeDeserialize(outgoing);

            Assert.AreEqual(body, incoming.SerializedMessageData);
        }

        [Test]
        public void Can_Serialize_And_Deserialize_Unicode()
        {
            var body = "،؛؟ءآأؤإئابةتثجحخدذرزسشصضطظعغـفقكلمنهوىيًٌٍَُِّْ٠١٢٣٤٥٦٧٨٩٪٫٬٭ٰٱٲٳٴٵٶٷٸٹٺٻټٽپٿڀځڂڃڄڅچڇڈډڊڋڌڍڎڏڐڑڒړڔڕږڗژڙښڛڜڝڞڟڠڡڢڣڤڥڦڧڨکڪګڬڭڮگڰڱ...";
            var outgoing = new OutgoingTransportMessage(new Dictionary<string, string>(), new object(), body);
            var incoming = TestSerializeDeserialize(outgoing);

            Assert.AreEqual(body, incoming.SerializedMessageData);
        }

        private static IncomingTransportMessage TestSerializeDeserialize(OutgoingTransportMessage outgoing)
        {
            string buffer;
            using (var stream = new MemoryStream())
            {
                FastXmlTransportMessageSerializer.Serialize(outgoing, stream);
                buffer = Encoding.Unicode.GetString(stream.ToArray());
            }
            using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(buffer)))
            {
                return FastXmlTransportMessageSerializer.Deserialize(Guid.NewGuid().ToString(), stream);
            }
        }
    }
}
