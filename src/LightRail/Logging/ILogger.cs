using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightRail.Logging
{
    // Sourced from: http://stackoverflow.com/a/5646876/507
    public interface ILogger
    {
        void Log(LogEntry entry);
    }
}
