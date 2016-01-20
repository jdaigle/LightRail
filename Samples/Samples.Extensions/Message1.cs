using System;

namespace Samples.Extensions
{
    public class Message1
    {
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string Data { get; set; } = Guid.NewGuid().ToString();

        public override string ToString()
        {
            return $"{TimeStamp} - {Data}";
        }
    }
}
