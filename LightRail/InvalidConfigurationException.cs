using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail
{
    public class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException(string message) : base(message) { }
    }
}
