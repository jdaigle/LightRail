using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace LightRail.SqlServer
{
    public static class ServiceBrokerWatcher
    {
        public static IEnumerable<string> ListQueueNames(this IDbConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sys.service_queues ORDER BY name";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the estimated size of each queue. The results are NOT buffered (yield return from the internal reader).
        /// </summary>
        public static IEnumerable<QueueStatusView> QueryQueueStatus(this SqlConnection connection, params string[] queues)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = Query_vwQueuedMessageCount;

                if (queues.Any())
                {
                    var paramString = "";
                    for (int i = 0; i < queues.Length; i++)
                    {
                        var param = cmd.CreateParameter();
                        param.ParameterName = "@p" + i;
                        param.Value = queues[i].Trim();
                        param.DbType = DbType.String;
                        cmd.Parameters.Add(param);
                        paramString += param.ParameterName;
                        if (i != queues.Length - 1)
                        {
                            paramString += ",";
                        }
                    }

                    cmd.CommandText += string.Format("WHERE [Queue_Name] IN ({0})", paramString);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new QueueStatusView()
                        {
                            QueueName = reader.GetString(0),
                            ServiceName = reader.GetString(1),
                            EstimatedCount = (int)reader.GetInt64(2),
                            PoisonMessageCount = (int)reader.GetInt32(3),
                            LastPoisonMessageDateTimeUTC = !reader.IsDBNull(4) ? reader.GetDateTime(4) : (DateTime?)null,
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Returns the info for poison messages found in a queue. The results are NOT buffered (yield return from the internal reader).
        /// </summary>
        public static IEnumerable<PoisonMessageInfo> ListPoisonMessages(this IDbConnection connection, string queue, int page, int maxResults, SortOrder sortOrder = SortOrder.Descending)
        {
            using (var cmd = connection.CreateCommand())
            {
                var first = (page * maxResults) - maxResults + 1;
                var last = first + maxResults - 1;

                var paramQueue = cmd.CreateParameter();
                paramQueue.ParameterName = "@queue_name";
                paramQueue.Value = queue;
                paramQueue.DbType = DbType.String;
                cmd.Parameters.Add(paramQueue);

                var paramFirst = cmd.CreateParameter();
                paramFirst.ParameterName = "@first";
                paramFirst.Value = first;
                paramFirst.DbType = DbType.Int32;
                cmd.Parameters.Add(paramFirst);

                var paramLast = cmd.CreateParameter();
                paramLast.ParameterName = "@last";
                paramLast.Value = last;
                paramLast.DbType = DbType.Int32;
                cmd.Parameters.Add(paramLast);

                cmd.CommandText = string.Format(Query_PosionMessages, sortOrder == SortOrder.Ascending ? "ASC" : "DESC");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new PoisonMessageInfo()
                        {
                            ConversationHandle = reader.GetGuid(0),
                            InsertDateTimeUTC = reader.GetDateTime(1),
                            QueueName = reader.GetString(2),
                            ServiceName = reader.GetString(3),
                            OriginServiceName = reader.GetString(4),
                            Retries = reader.GetInt32(5),
                            ErrorCode = reader.GetString(6),
                            ErrorMessage = reader.GetString(7),
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Gets a single Poison Message, including message body and exception details.
        /// </summary>
        public static PoisonMessageInfo GetPoisonMessage(this SqlConnection connection, Guid conversation_handle)
        {
            using (var cmd = connection.CreateCommand())
            {
                var param = cmd.CreateParameter();
                param.ParameterName = "@conversation_handle";
                param.Value = conversation_handle;
                param.DbType = DbType.Guid;
                cmd.Parameters.Add(param);
                cmd.CommandText = Query_PosionMessage;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var message = new PoisonMessageInfo()
                        {
                            ConversationHandle = reader.GetGuid(0),
                            InsertDateTimeUTC = reader.GetDateTime(1),
                            QueueName = reader.GetString(2),
                            ServiceName = reader.GetString(3),
                            OriginServiceName = reader.GetString(4),
                            Retries = reader.GetInt32(5),
                            ErrorCode = reader.GetString(6),
                            ErrorMessage = reader.GetString(7),
                            MessageBody = reader.GetSqlBytes(8).Buffer,
                        };
                        return message;
                    }
                    return null;
                }
            }
        }

        /// <summary>
        /// Resends a single message to the queue from which it failed.
        /// </summary>
        public static void ResendPoisonMessage(this IDbConnection connection, Guid conversation_handle)
        {
            using (var cmd = connection.CreateCommand())
            {
                var param = cmd.CreateParameter();
                param.ParameterName = "@conversation_handle";
                param.Value = conversation_handle;
                param.DbType = DbType.Guid;
                cmd.Parameters.Add(param);
                cmd.CommandText = "dbo.spResendPoisonMessage";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Resends poison messages found in a queue
        /// </summary>
        public static void ResendAllPoisonMessages(this SqlConnection connection, string queue)
        {
            var count = (QueryQueueStatus(connection, queue).FirstOrDefault() ?? new QueueStatusView()).PoisonMessageCount;
            while (count > 0)
            {
                var top100 = ListPoisonMessages(connection, queue, 1, 100, SortOrder.Ascending).ToList(); // ToList() to buffer into memory and close the reader
                foreach (var item in top100)
                {
                    if (string.IsNullOrWhiteSpace(item.OriginServiceName))
                    {
                        // Cannot resend messages without origin service
                        continue;
                    }
                    ResendPoisonMessage(connection, item.ConversationHandle);
                }
                count = count - 100;
            }
        }

        /// <summary>
        /// Purges (deletes) a single message referenced by its MessageId
        /// </summary>
        public static int PurgePoisonMessage(this IDbConnection connection, Guid conversation_handle)
        {
            using (var cmd = connection.CreateCommand())
            {
                var param = cmd.CreateParameter();
                param.ParameterName = "@conversation_handle";
                param.Value = conversation_handle;
                param.DbType = DbType.Guid;
                cmd.Parameters.Add(param);
                cmd.CommandText = "DELETE FROM dbo.PoisonMessage WHERE conversation_handle = @conversation_handle";
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Purges (deletes) ALL messages for a given queue. USE WITH CAUTION!
        /// </summary>
        public static int PurgeAllPoisonMessage(this IDbConnection connection, string queue)
        {
            using (var cmd = connection.CreateCommand())
            {
                var param = cmd.CreateParameter();
                param.ParameterName = "@queue_name";
                param.Value = queue;
                param.DbType = DbType.String;
                cmd.Parameters.Add(param);
                cmd.CommandText = "DELETE FROM dbo.FailedMessage WHERE [queue_name] = @queue_name";
                return cmd.ExecuteNonQuery();
            }
        }

        private static readonly string Query_vwQueuedMessageCount = @"
SELECT
    Queue_Name
, Service_Name
, Estimated_Message_Count
, Poison_Message_Count
, LastPoisonMessageDateTimeUTC
FROM dbo.vwQueuedMessageCount
";

        private static readonly string Query_PosionMessages = @"
SELECT * FROM
 (SELECT 
    conversation_handle,
    InsertDateTimeUTC,
    queue_name,
    service_name,
    origin_service_name,
    retries,
    errorCode,
    CASE
        WHEN CHARINDEX(CHAR(13), errorMessage) > 0
            THEN SUBSTRING(errorMessage, 0, CHARINDEX(CHAR(13), errorMessage) - 1)
        ELSE
            SUBSTRING(errorMessage, 0, 100)
    END AS errorMessage,
    ROW_NUMBER() OVER (ORDER BY InsertDateTimeUTC {0}) as RowNumber
    FROM dbo.PoisonMessage WITH (NOLOCK)
    WHERE queue_name = @queue_name) _z
WHERE RowNumber BETWEEN @first AND @last;
";

        private static readonly string Query_PosionMessage = @"
SELECT 
    conversation_handle,
    InsertDateTimeUTC,
    queue_name,
    service_name,
    origin_service_name,
    retries,
    errorCode,
    errorMessage,
    message_body
FROM dbo.PoisonMessage WITH (NOLOCK)
WHERE conversation_handle = @conversation_handle;
";
    }
}
