﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Msmq
{
    public class MsmqTransportConfiguration : AbstractTransportConfiguration
    {
        public string InputQueue { get; set; }
    }
}
