using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Logging
{
    public class ConsoleLogFactory : ILoggerFactory
    {
        public ILogger GetLogger(string name)
        {
            return new ConsoleLogger(name);
        }


        public ILogger GetLogger(Type type)
        {
            return new ConsoleLogger(type.FullName);
        }
    }
}
