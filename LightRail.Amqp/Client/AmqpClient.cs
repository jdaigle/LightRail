using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Amqp.Framing;
using LightRail.Amqp.Protocol;

namespace LightRail.Amqp.Client
{
    public sealed class AmqpClient : IDisposable
    {
        /// <summary>
        /// Creates an Amqp Client instance using the "amqp" URI scheme.
        /// </summary>
        /// <remarks>
        /// The syntax of an AMQP URI is defined by the following ABNF rules. All names in these rules not defined here are taken from RFC3986.
        /// 
        /// amqp_URI       = "amqp://" amqp_authority [ "/" address ]
        /// 
        /// amqps_URI      = "amqps://" amqp_authority [ "/" address ]
        /// 
        /// amqp_authority = [ amqp_userinfo "@" ] host [ ":" port ]
        /// 
        /// amqp_userinfo  = username [ ":" password ]
        /// 
        /// username       = *( unreserved / pct-encoded / sub-delims )
        /// 
        /// password       = *( unreserved / pct-encoded / sub-delims )
        /// 
        /// address        = segment
        /// </remarks>
        public static AmqpClient CreateFromURI(string uri)
        {
            return CreateFromURI(new Uri(uri));
        }

        public static AmqpClient CreateFromURI(Uri uri)
        {
            return new AmqpClient(ClientContainer.GetGlobalContainerFor(uri));
        }

        private AmqpClient(ClientContainer container)
        {
            this.container = container;
            this.container.AddAmqpClientRef(this);
        }

        private readonly ClientContainer container;

        /// <summary>
        /// Asynchronously sends a message to the specified endpoint using the default
        /// message encoding.
        /// </summary>
        public async Task SendAsync(object message)
        {
            AssertNotDisposed();
            await Task.FromResult(0);
            throw new NotImplementedException();
        }

        private void AssertNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AmqpClient));
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed = false;
        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                container.RemoveAmqpClientRef(this);
            }
        }
    }
}
