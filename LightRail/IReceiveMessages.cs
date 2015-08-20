using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public interface IReceiveMessages : IObservable<MessageAvailable>
    {
        void Start();
        void Stop();
    }
}
