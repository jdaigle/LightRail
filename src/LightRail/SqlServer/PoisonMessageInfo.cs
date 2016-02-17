using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.SqlServer
{
    public class PoisonMessageInfo
    {
        public Guid ConversationHandle { get; set; }
        public DateTime InsertDateTimeUTC { get; set; }
        public string QueueName { get; set; }
        public string ServiceName { get; set; }
        public string OriginServiceName { get; set; }
        public int Retries { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public byte[] MessageBody { get; set; }
        public string MessageBodyString
        {
            get
            {
                return Encoding.Unicode.GetString(MessageBody);
            }
        }
    }
}
