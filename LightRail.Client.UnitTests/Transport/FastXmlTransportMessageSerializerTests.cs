using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LightRail.Client.Transport
{
    [TestFixture]
    public class FastXmlTransportMessageSerializerTests
    {
        [Test]
        public void Can_Serialize_And_Deserialize()
        {
            var result = TestSerializeDeserialize(body: "foobar");
            Assert.IsNotNull(result);
        }

        [Test]
        public void Can_Serialize_And_Deserialize_Empty_Headers()
        {
            var headers = new Dictionary<string, string>();
            headers["first"] = Guid.NewGuid().ToString();
            headers["second"] = "";
            headers["third"] = Guid.NewGuid().ToString();
            
            var result = TestSerializeDeserialize(headers: headers, body: "foofar");

            Assert.AreEqual(headers["first"], result.Headers["first"]);
            Assert.AreEqual(headers["second"], result.Headers["second"]);
            Assert.AreEqual(headers["third"], result.Headers["third"]);
        }

        [Test]
        public void Can_Serialize_And_Deserialize_Complex_Body()
        {
            var body = "<xml>test</xml>!@#$%^&*()blahblahblahasdf asdf 241-3-4 1234 -12!!~";
            var result = TestSerializeDeserialize(body: body);

            Assert.AreEqual(body, result.Body);
        }

        [Test]
        public void Can_Serialize_And_Deserialize_Unicode()
        {
            var body = "،؛؟ءآأؤإئابةتثجحخدذرزسشصضطظعغـفقكلمنهوىيًٌٍَُِّْ٠١٢٣٤٥٦٧٨٩٪٫٬٭ٰٱٲٳٴٵٶٷٸٹٺٻټٽپٿڀځڂڃڄڅچڇڈډڊڋڌڍڎڏڐڑڒړڔڕږڗژڙښڛڜڝڞڟڠڡڢڣڤڥڦڧڨکڪګڬڭڮگڰڱ...";
            var result = TestSerializeDeserialize(body: body);

            Assert.AreEqual(body, result.Body);
        }

        private static FastXmlTransportMessageSerializer.FastXmlTransportMessageSerializerResult TestSerializeDeserialize(Dictionary<string, string> headers = null, string body = "")
        {
            string buffer;
            using (var stream = new MemoryStream())
            {
                FastXmlTransportMessageSerializer.Serialize(headers ?? new Dictionary<string,string>(), body ?? "", stream);
                buffer = Encoding.Unicode.GetString(stream.ToArray());
            }
            using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(buffer)))
            {
                return FastXmlTransportMessageSerializer.Deserialize(stream);
            }
        }
    }
}
