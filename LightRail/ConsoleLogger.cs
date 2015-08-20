using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public class ConsoleLogger : ILogger
    {
        private string name;

        public ConsoleLogger(string name)
        {
            this.name = name;
        }

        public void Log(LogEntry entry)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff|") + "{0}[{1}] {2}", name, entry.Severity.ToString(), entry.Message);
            if (entry.Exception != null)
            {
                Console.WriteLine("Exception: " + entry.Exception.GetType());
                Console.WriteLine(entry.Exception.Message);
                Console.WriteLine(entry.Exception.StackTrace);
            }
        }
    }
}
