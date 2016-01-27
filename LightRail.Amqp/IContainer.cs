using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightRail.Amqp
{
    public interface IContainer
    {
        string ContainerId { get; }
    }
}
