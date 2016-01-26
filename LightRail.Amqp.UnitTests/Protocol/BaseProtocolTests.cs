using NUnit.Framework;

namespace LightRail.Amqp.Protocol
{
    public abstract class BaseProtocolTests
    {
        protected InterceptingSocket socket;
        protected AmqpConnection connection;

        [SetUp]
        public void SetUp()
        {
            socket = new InterceptingSocket();
            connection = new AmqpConnection(socket);
        }
    }
}
