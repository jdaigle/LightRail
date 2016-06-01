using System;
using System.Data;
using System.Data.SqlClient;

namespace LightRail.ServiceBus.SqlServer
{
    internal static class PoisonMessageSqlHelper
    {
        public static void EnsureTableExists(string connectionString)
        {
            using (var connection = SqlServerTransactionManager.OpenConnection(connectionString))
            {
                using (var transaction = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID('dbo.PoisonMessage'))
CREATE TABLE [dbo].[PoisonMessage]
(
    PoisonMessageId bigint NOT NULL IDENTITY(1,1),
    InsertDateTimeUtc datetime2 NOT NULL,
    ConversationHandle uniqueidentifier NOT NULL,
    OriginServiceName nvarchar(255) NOT NULL,
    EnqueuedDateTimeUtc datetime2 NOT NULL,
    ServiceName nvarchar(255) NOT NULL,
    QueueName nvarchar(255) NOT NULL,
    MessageBody varbinary(max) NOT NULL,
    Retries tinyint NOT NULL,
    ErrorCode nvarchar(20) NOT NULL,
    ErrorMessage nvarchar(max) NOT NULL,
    CONSTRAINT [PK_PoisonMessage] PRIMARY KEY CLUSTERED 
    (
        [PoisonMessageId] ASC
    ),
);";
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                connection.Close();
            }
        }

        public static void WriteToPoisonMessageTable(SqlTransaction transaction
            , Guid conversationHandle
            , string originServiceName
            , DateTime enqueuedDateTimeUtc
            , string serviceBrokerService
            , string serviceBrokerQueue
            , byte[] messageBody
            , int retries
            , string errorCode
            , Exception exception
            )
        {
            // write to the message to the PosionMessage table
            using (var command = transaction.Connection.CreateCommand())
            {
                command.CommandText = @"
INSERT INTO[dbo].[PoisonMessage]
           ([InsertDateTimeUtc]
           ,[ConversationHandle]
           ,[OriginServiceName]
           ,[EnqueuedDateTimeUtc]
           ,[ServiceName]
           ,[QueueName]
           ,[MessageBody]
           ,[Retries]
           ,[ErrorCode]
           ,[ErrorMessage])
     VALUES
           (GETUTCDATE()
           ,@ConversationHandle
           ,@OriginServiceName
           ,@EnqueuedDateTimeUtc
           ,@ServiceName
           ,@QueueName
           ,@MessageBody
           ,@Retries
           ,@ErrorCode
           ,@ErrorMessage);";
                command.Parameters.AddWithValue("@ConversationHandle", conversationHandle);
                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@OriginServiceName",
                    Value = originServiceName,
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 255,
                });
                command.Parameters.AddWithValue("@EnqueuedDateTimeUtc", enqueuedDateTimeUtc);
                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@ServiceName",
                    Value = serviceBrokerService,
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 255,
                });
                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@QueueName",
                    Value = serviceBrokerQueue,
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 255,
                });
                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@MessageBody",
                    Value = messageBody,
                    SqlDbType = SqlDbType.VarBinary,
                    Size = -1,
                });
                command.Parameters.AddWithValue("@Retries", retries);
                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@ErrorCode",
                    Value = errorCode,
                    SqlDbType = SqlDbType.VarChar,
                    Size = 20,
                });
                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@ErrorMessage",
                    Value = exception != null ? (object)FormatErrorMessage(exception) : (object)DBNull.Value,
                    SqlDbType = SqlDbType.NVarChar,
                    Size = -1,
                });
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
        }

        private static string FormatErrorMessage(Exception e)
        {
            var message = e.GetType().ToString() + ": " + e.Message + Environment.NewLine + e.StackTrace;
            if (e.InnerException != null)
            {
                message = message + Environment.NewLine + Environment.NewLine + FormatErrorMessage(e.InnerException);
            }
            return message;
        }
    }
}
