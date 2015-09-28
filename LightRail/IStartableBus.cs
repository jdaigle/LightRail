using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail
{
    public interface IStartableBus : IBus
    {
        IBus Start();
    }
}
