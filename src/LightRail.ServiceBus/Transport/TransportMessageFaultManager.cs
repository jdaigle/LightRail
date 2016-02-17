using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LightRail.ServiceBus.Transport
{
    public class TransportMessageFaultManager
    {
        static TransportMessageFaultManager()
        {
            // Sets up a timer to periodically flush and remove message IDs
            // after a defined expiration. This prevents the build up of messages
            // that are, for whatever reason, never replayed through this endpoint.
            flushExpirtedFailureActions = new List<Action>();
            expirationTimer = new Timer(
                s => {
                    var actions = new List<Action>(flushExpirtedFailureActions);
                    actions.ForEach(a => a());
                }
                , null
                , DefaultExpirationInterval
                , DefaultExpirationInterval);
        }

        private static readonly Timer expirationTimer;
        private static readonly List<Action> flushExpirtedFailureActions;

        public TransportMessageFaultManager(int maxRetries)
        {
            this.maxRetries = maxRetries;
            flushExpirtedFailureActions.Add(FlushExpiredFailures);
        }

        private readonly int maxRetries;

        private static readonly int DefaultExpirationInterval = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Accessed by multiple threads - lock using failuresPerMessageLocker.
        /// </summary>
        private readonly IDictionary<string, int> failuresPerMessage = new Dictionary<string, int>();
        private readonly ReaderWriterLockSlim failuresPerMessageLocker = new ReaderWriterLockSlim();
        private readonly IDictionary<string, Exception> lastExceptionsForMessage = new Dictionary<string, Exception>();
        private readonly IDictionary<string, DateTime> messageFailureExpiration = new Dictionary<string, DateTime>();

        /// <summary>
        /// Returns true if the max number of retries for the message have been exceeded, and removes the message Id at the same time. Also sets the last Exception
        /// raised. Otherwise returns false.
        /// </summary>
        public bool HasMaxRetriesExceeded(IncomingTransportMessage message, out Exception lastException)
        {
            string messageId = message.MessageId;
            failuresPerMessageLocker.EnterReadLock();

            if (failuresPerMessage.ContainsKey(messageId) &&
                (failuresPerMessage[messageId] >= maxRetries))
            {
                failuresPerMessageLocker.ExitReadLock();
                failuresPerMessageLocker.EnterWriteLock();

                lastException = lastExceptionsForMessage[messageId];
                failuresPerMessage.Remove(messageId);
                lastExceptionsForMessage.Remove(messageId);
                messageFailureExpiration.Remove(messageId);

                failuresPerMessageLocker.ExitWriteLock();

                return true;
            }

            lastException = null;
            failuresPerMessageLocker.ExitReadLock();
            return false;
        }

        public void ClearFailuresForMessage(string messageId)
        {
            failuresPerMessageLocker.EnterReadLock();
            if (failuresPerMessage.ContainsKey(messageId))
            {
                failuresPerMessageLocker.ExitReadLock();
                failuresPerMessageLocker.EnterWriteLock();

                failuresPerMessage.Remove(messageId);
                lastExceptionsForMessage.Remove(messageId);
                messageFailureExpiration.Remove(messageId);

                failuresPerMessageLocker.ExitWriteLock();
            }
            else
            {
                failuresPerMessageLocker.ExitReadLock();
            }
        }

        public void IncrementFailuresForMessage(string messageId, Exception e)
        {
            try
            {
                failuresPerMessageLocker.EnterWriteLock();

                if (!failuresPerMessage.ContainsKey(messageId))
                {
                    failuresPerMessage[messageId] = 1;
                }
                else
                {
                    failuresPerMessage[messageId] = failuresPerMessage[messageId] + 1;
                }

                lastExceptionsForMessage[messageId] = e;
                messageFailureExpiration[messageId] = DateTime.UtcNow.Add(DefaultExpiration);
            }
            catch { } //intentionally swallow exceptions here
            finally
            {
                failuresPerMessageLocker.ExitWriteLock();
            }
        }

        public void FlushExpiredFailures()
        {
            failuresPerMessageLocker.EnterReadLock();

            var messageIds = messageFailureExpiration
                .Where(x => x.Value <= DateTime.UtcNow)
                .Select(x => x.Key)
                .ToList();

            failuresPerMessageLocker.ExitReadLock();

            failuresPerMessageLocker.EnterWriteLock();

            foreach (var messageId in messageIds)
            {
                failuresPerMessage.Remove(messageId);
                lastExceptionsForMessage.Remove(messageId);
                messageFailureExpiration.Remove(messageId);
            }

            failuresPerMessageLocker.ExitWriteLock();
        }
    }
}
