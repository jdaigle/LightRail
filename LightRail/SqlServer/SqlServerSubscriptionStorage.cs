using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail.Logging;

namespace LightRail.SqlServer
{
    public class SqlServerSubscriptionStorage : ISubscriptionStorage
    {
        private static ILogger logger = LogManager.GetLogger("LightRail.SqlServer");

        public SqlServerSubscriptionStorage(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public string ConnectionString { get; set; }

        public void Subscribe(string destination, IEnumerable<string> messageTypes)
        {
            using (var connection = new SqlConnection(this.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var messageType in messageTypes)
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = "IF (NOT EXISTS (SELECT 1 FROM dbo.Subscription WHERE MessageType = @MessageType AND Destination = @Destination)) INSERT INTO Subscription ([MessageType], [Destination]) VALUES (@MessageType, @Destination);";
                        cmd.Parameters.AddWithValue("@MessageType", messageType);
                        cmd.Parameters.AddWithValue("@Destination", destination);
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                        logger.Debug("Subscribed {0} to Destination {1}", messageType, destination);
                    }
                    transaction.Commit();
                }
            }
        }

        public void Unsubscribe(string destination, IEnumerable<string> messageTypes)
        {
            using (var connection = new SqlConnection(this.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var messageType in messageTypes)
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = "DELETE FROM dbo.Subscription WHERE MessageType = @MessageType AND Destination = @Destination;";
                        cmd.Parameters.AddWithValue("@MessageType", messageType);
                        cmd.Parameters.AddWithValue("@Destination", destination);
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        public List<string> GetSubscribersForMessage(IEnumerable<Type> messageTypes)
        {
            var subscribers = new List<string>();
            if (!messageTypes.Any())
            {
                return subscribers;
            }
            using (var connection = new SqlConnection(this.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    var messageTypesString = new StringBuilder();
                    for (int i = 0; i < messageTypes.Count(); i++)
                    {
                        messageTypesString.AppendFormat("@p{0},", i);
                        command.Parameters.AddWithValue("@p" + i.ToString(), messageTypes.ElementAt(i).FullName);
                    }
                    command.CommandText = string.Format("SELECT [Destination] FROM [dbo].[Subscription] WHERE [MessageType] IN ({0})", messageTypesString.ToString().TrimEnd(','));
                    using (var transaction = connection.BeginTransaction())
                    {
                        command.Transaction = transaction;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                subscribers.Add(reader.GetString(0));
                            }
                        }
                        transaction.Commit();
                    }
                }
            }
            return subscribers.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
        }
    }
}
