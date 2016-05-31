using System;
using System.Data.SqlClient;

namespace LightRail.ServiceBus.SqlServer
{
    public sealed class ServiceBrokerMessage
    {
        public const string EventNotificationType = "http://schemas.microsoft.com/SQL/Notifications/EventNotification";
        public const string QueryNotificationType = "http://schemas.microsoft.com/SQL/Notifications/QueryNotification";
        public const string DialogTimerType = "http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer";
        public const string EndDialogType = "http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog";
        public const string ErrorType = "http://schemas.microsoft.com/SQL/ServiceBroker/Error";

        public static ServiceBrokerMessage Load(SqlDataReader reader)
        {
            return new ServiceBrokerMessage(reader);
        }

        public Guid ConversationHandle { get; }
        public string ServiceName { get; }
        public string ServiceContractName { get; }
        public string MessageTypeName { get; }
        public byte[] Body { get; }

        public bool IsDialogTimerMessage() { return MessageTypeName == DialogTimerType; }

        private ServiceBrokerMessage(SqlDataReader reader)
        {
            ConversationHandle = reader.GetGuid(0);
            ServiceName = reader.GetString(1);
            ServiceContractName = reader.GetString(2);
            MessageTypeName = reader.GetString(3);
            if (!reader.IsDBNull(4))
            {
                Body = reader.GetSqlBytes(4).Buffer;
            }
            else
            {
                Body = new byte[0];
            }
        }
    }
}
