using System;

namespace LightRail.SqlServer
{
    public class QueueStatusView
    {
        public string QueueName { get; set; }
        public string ServiceName { get; set; }
        public int EstimatedCount { get; set; }
        public int PoisonMessageCount { get; set; }
        public DateTime? LastPoisonMessageDateTimeUTC { get; set; }
    }
}