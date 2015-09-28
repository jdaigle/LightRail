using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace LightRail.SqlServer
{
    public class ServiceBrokerMessage
    {
        public const string EventNotificationType = "http://schemas.microsoft.com/SQL/Notifications/EventNotification";
        public const string QueryNotificationType = "http://schemas.microsoft.com/SQL/Notifications/QueryNotification";
        public const string DialogTimerType = "http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer";
        public const string EndDialogType = "http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog";
        public const string ErrorType = "http://schemas.microsoft.com/SQL/ServiceBroker/Error";

        public static ServiceBrokerMessage Load(SqlDataReader reader)
        {
            var message = new ServiceBrokerMessage();
            message.ConversationHandle = reader.GetGuid(0);
            message.ServiceName = reader.GetString(1);
            message.ServiceContractName = reader.GetString(2);
            message.MessageTypeName = reader.GetString(3);
            if (!reader.IsDBNull(4))
            {
                message.Body = reader.GetSqlBytes(4).Buffer;
            }
            else
            {
                message.Body = new byte[0];
            }
            return message;
        }

        public Guid ConversationHandle { get; private set; }
        public string ServiceName { get; private set; }
        public string ServiceContractName { get; private set; }
        public string MessageTypeName { get; private set; }
        public byte[] Body { get; private set; }

        public bool IsDialogTimerMessage() { return MessageTypeName == DialogTimerType; }

        private ServiceBrokerMessage() { }
    }
}
