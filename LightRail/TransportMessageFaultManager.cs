using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightRail
{
    public class TransportMessageFaultManager
    {
        public TransportMessageFaultManager(int maxRetries)
        {
            this.maxRetries = maxRetries;
        }

        private readonly int maxRetries;

        /// <summary>
        /// Accessed by multiple threads - lock using failuresPerMessageLocker.
        /// </summary>
        private readonly IDictionary<string, int> failuresPerMessage = new Dictionary<string, int>();
        private readonly IDictionary<string, Exception> exceptionsForMessages = new Dictionary<string, Exception>();
        private readonly ReaderWriterLockSlim failuresPerMessageLocker = new ReaderWriterLockSlim();

        /// <summary>
        /// Returns true if the max number of retries for the message have been exceeded. Also sets the last Exception
        /// raised. Otherwise returns false.
        /// </summary>
        public bool MaxRetriesExceeded(IncomingTransportMessage message, out Exception lastException)
        {
            string messageId = message.MessageId;
            failuresPerMessageLocker.EnterReadLock();

            if (failuresPerMessage.ContainsKey(messageId) &&
                (failuresPerMessage[messageId] >= maxRetries))
            {
                failuresPerMessageLocker.ExitReadLock();
                failuresPerMessageLocker.EnterWriteLock();

                lastException = exceptionsForMessages[messageId];
                failuresPerMessage.Remove(messageId);
                exceptionsForMessages.Remove(messageId);

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
                exceptionsForMessages.Remove(messageId);

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

                exceptionsForMessages[messageId] = e;
            }
            catch { } //intentionally swallow exceptions here
            finally
            {
                failuresPerMessageLocker.ExitWriteLock();
            }
        }
    }
}
