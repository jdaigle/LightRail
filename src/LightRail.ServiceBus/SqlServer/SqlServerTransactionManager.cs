using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace LightRail.ServiceBus.SqlServer
{
    internal static class SqlServerTransactionManager
    {
        private static readonly Dictionary<int, SqlTransaction> asyncTaskTransactionStorage = new Dictionary<int, SqlTransaction>();

        public static void SaveTransactionForCurrentTask(SqlTransaction transaction)
        {
            var currentTaskId = Task.CurrentId;
            if (currentTaskId == null)
            {
                throw new InvalidOperationException("Task.CurrentId is null. Only call this method from a running task.");
            }
            asyncTaskTransactionStorage[currentTaskId.Value] = transaction;
        }

        public static void ClearTransactionForCurrentTask()
        {
            var currentTaskId = Task.CurrentId;
            if (currentTaskId == null)
            {
                throw new InvalidOperationException("Task.CurrentId is null. Only call this method from a running task.");
            }
            asyncTaskTransactionStorage.Remove(currentTaskId.Value);
        }

        public static SqlTransaction TryGetTransactionForCurrentTask()
        {
            var currentTaskId = Task.CurrentId;
            if (currentTaskId == null || !asyncTaskTransactionStorage.ContainsKey(currentTaskId.Value))
            {
                return null;
            }
            return asyncTaskTransactionStorage[currentTaskId.Value];
        }

        public static SqlConnection OpenConnection(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public static SqlTransaction BeginTransaction(string connectionString)
        {
            return OpenConnection(connectionString).BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public static void CommitTransactionAndDisposeConnection(SqlTransaction transaction)
        {
            var connection = transaction.Connection;
            using (connection)
            {
                using (transaction)
                {
                    transaction.Commit();
                }
                try
                {
                    connection.Close();
                }
                catch (Exception) { }
            }
        }

        public static void TryRollbackTransaction(SqlTransaction transaction)
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception) { }
        }

        public static void TryForceDisposeTransactionAndConnection(SqlTransaction transaction)
        {
            if (transaction != null)
            {
                SqlConnection connection = null;
                try
                {
                    connection = transaction.Connection;
                    transaction.Dispose();
                }
                catch (Exception) { }
                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                    }
                    catch (Exception) { }
                    try
                    {
                        connection.Dispose();
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
