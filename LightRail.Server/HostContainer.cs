using System;
using LightRail.Amqp;

namespace LightRail.Server
{
    public class HostContainer : IContainer
    {
        public static readonly HostContainer Instance = new HostContainer();

        private HostContainer()
        {
            ContainerId = Guid.NewGuid().ToString(); // TODO: Generate once and then persist?
        }

        public string ContainerId { get; }
    }
}
