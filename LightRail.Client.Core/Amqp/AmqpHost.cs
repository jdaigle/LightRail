using System;
using Amqp;
using LightRail.Client.Logging;
using LightRail.Client.Transport;

namespace LightRail.Client.Amqp
{
    public class AmqpHost : ITransportHost
    {
        public AmqpHost(string uri)
            :this(new Address(uri))
        {
        }

        public AmqpHost(Address address)
        {
            this.Address = address;
        }

        private static ILogger logger = LogManager.GetLogger("LightRail.Client.Amqp.Host");

        public Address Address { get; }
        private Connection _connection;
        private object connectionLock = new object();
        private Session _session;
        private object sessionLock = new object();

        public Connection GetOrOpenConnection()
        {
            if (_connection == null)
            {
                lock (connectionLock)
                {
                    if (_connection != null)
                        return _connection;
                    logger.Info("Opening Connection To {0}", Address.ToString());
                    _connection = new Connection(Address);
                    _connection.Closed = (sender, error) =>
                    {
                        logger.Info("Connection {0} Closed", Address.ToString());
                        if (error != null)
                        {
                            logger.Error("Connection {0} Closed With Error: {1}", Address.ToString(), error.Description);
                        }
                        _connection = null;
                        _session = null;
                    };
                }
            }
            return _connection;
        }

        public Session GetOrOpenSession()
        {
            if (_session == null)
            {
                lock (sessionLock)
                {
                    if (_session != null)
                        return _session;
                    var connection = GetOrOpenConnection();
                    logger.Info("Opening Session On {0}", Address.ToString());
                    _session = new Session(connection);
                    _session.Closed = (sender, error) =>
                    {
                        logger.Info("Session {0} Closed", Address.ToString());
                        if (error != null)
                        {
                            logger.Error("Session {0} Closed With Error: {1}", Address.ToString(), error.Description);
                        }
                        _session = null;
                    };
                }
            }
            return _session;
        }
    }
}
