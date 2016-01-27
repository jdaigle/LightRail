using System;

namespace LightRail.Amqp.Protocol
{
    public class TestContainer : IContainer
    {
        public TestContainer()
        {
            ContainerId = Guid.NewGuid().ToString();
        }

        public string ContainerId { get; }
    }
}
