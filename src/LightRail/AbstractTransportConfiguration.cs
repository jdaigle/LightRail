using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public class AbstractTransportConfiguration
    {
        protected AbstractTransportConfiguration()
        {
            MaxRetries = 5;
            MaxConcurrency = 1;
        }

        public int MaxRetries { get; set; }
        /// <remarks>
        /// Set this value to 0 to make it a send-only transport
        /// </remarks>
        public int MaxConcurrency { get; set; }
    }
}
