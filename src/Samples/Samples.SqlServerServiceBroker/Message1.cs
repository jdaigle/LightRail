using System;

namespace Samples.SqlServerServiceBroker
{
    public class Message1
    {
        private static int c = 0;

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string Data { get; set; } = Guid.NewGuid().ToString();
        public int MessageCounter { get; set; } = c++;

        public override string ToString()
        {
            return $"{MessageCounter} - {TimeStamp} - {Data}";
        }
    }
}
