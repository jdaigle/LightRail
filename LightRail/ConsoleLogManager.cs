using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public class ConsoleLogManager : ILogManager
    {
        public ILogger GetLogger(string name)
        {
            return new ConsoleLogger(name);
        }
    }
}
