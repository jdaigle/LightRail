using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Network;
using LightRail.Amqp.Protocol;

namespace LightRail.Amqp.Client
{
    public class ClientContainer : IContainer
    {
        private static readonly Dictionary<string, ClientContainer> globalContainers = new Dictionary<string, ClientContainer>();

        public static ClientContainer GetGlobalContainerFor(Uri uri)
        {
            var key = uri.Scheme.ToLowerInvariant()
                + uri.Host.ToLowerInvariant()
                + uri.Port.ToString().ToLowerInvariant()
                + uri.AbsolutePath.ToLowerInvariant();
            lock (globalContainers)
            {
                if (globalContainers.ContainsKey(key))
                    return globalContainers[key];
                var container = new ClientContainer(uri);
                globalContainers.Add(key, container);
                return container;
            }
        }

        public ClientContainer(Uri uri)
        {
            ValidateUri(uri);

            ContainerId = Guid.NewGuid().ToString();

            this.host = uri.Host;
            this.useTLS = string.Equals(uri.Scheme, Constants.Amqps, StringComparison.InvariantCultureIgnoreCase);
            this.address = uri.AbsolutePath.Substring(1); // remove beginning "/";
            this.port = uri.Port;
            if (uri.IsDefaultPort)
                this.port = useTLS ? Constants.AmqpsPort : Constants.AmqpPort;

            this.clientSocket = new AsyncClientSocket(host, port, useTLS);
            this.connection = new AmqpConnection(clientSocket, this);
        }

        private static void ValidateUri(Uri uri)
        {
            if (!(string.Equals(uri.Scheme, Constants.Amqp, StringComparison.InvariantCultureIgnoreCase) ||
                  string.Equals(uri.Scheme, Constants.Amqps, StringComparison.InvariantCultureIgnoreCase)))
                throw new UriFormatException($"URI Scheme must be {Constants.Amqp}:// or {Constants.Amqps}://");
            if (string.IsNullOrEmpty(uri.Host))
                throw new UriFormatException("Host is required.");
            if (uri.AbsolutePath.Length <= 1)
                throw new UriFormatException("URI must contain an address.");
        }

        private readonly string host;
        private readonly int port;
        private readonly bool useTLS;
        private readonly string address;
        private readonly AsyncClientSocket clientSocket;
        private readonly AmqpConnection connection;

        private readonly List<AmqpClient> clientReferences = new List<AmqpClient>();

        public string ContainerId { get; private set; }

        public void OnLinkAttached(AmqpLink link)
        {
        }

        public void OnTransferReceived(AmqpLink link, Transfer transfer, ByteBuffer buffer)
        {
        }

        internal async Task SendAsync(object message)
        {
            if (!clientSocket.IsConnected)
            {
                await clientSocket.ConnectAsync();
            }

            //// open the connection
            //connection.Open();

            //var session = connection.GetSessionFromLocalChannel(1, false);
            //if (session == null || session.State != SessionStateEnum.MAPPED)
            //    session = connection.BeginSession(1);

            ////var link = session.AttachSenderLink(address);
            //// TODO open link
        }

        internal void AddAmqpClientRef(AmqpClient client)
        {
            lock (clientReferences)
            {
                clientReferences.Add(client);
            }
        }

        internal void RemoveAmqpClientRef(AmqpClient client)
        {
            lock (clientReferences)
            {
                clientReferences.Remove(client);
                if (clientReferences.Count == 0)
                {
                    // no more clients, start a timer to eventually shutdown the connection.
                    // we'll leave it open for a bit, just in case a client decides to connect again.
                    throw new NotImplementedException("Start Timer to Shutdown the Connection.");
                }
            }
        }
    }
}
