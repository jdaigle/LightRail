using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public interface ISendMessages
    {
        void Send(OutgoingTransportMessage transportMessage, IEnumerable<string> destinations);
    }
}
