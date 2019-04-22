using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace dbCmd.noLock
{
	public static class DbCommandQueueExtensions
	{
        /// <summary>
        /// Single instance - to allow application to use sinigle thread to initiate all sql queries, to overcome lock contention in TimerQueue.Timer
        /// </summary>
        public static readonly DbCommandQueueProcessor Instance = new DbCommandQueueProcessor();

		public static object ExecuteScalarQueued(this IDbCommand cmd) =>
			Instance.ExecuteScalar((DbCommand)cmd);

		public static Task<object> ExecuteScalarQueuedAsync(this DbCommand cmd, CancellationToken ct) =>
			Instance.ExecuteScalarAsync(cmd, ct);

		public static int ExecuteNonQueryQueued(this IDbCommand cmd) =>
			Instance.ExecuteNonQuery((DbCommand)cmd);

		public static Task<int> ExecuteNonQueryQueuedAsync(this DbCommand cmd, CancellationToken ct) =>
			Instance.ExecuteNonQueryAsync(cmd, ct);

		public static DbDataReader ExecuteReaderQueued(this IDbCommand cmd, CommandBehavior commandBehavior) =>
			Instance.ExecuteReader((DbCommand)cmd, commandBehavior);

		public static Task<DbDataReader> ExecuteReaderQueuedAsync(this DbCommand cmd, CommandBehavior commandBehavior, CancellationToken ct) =>
			Instance.ExecuteReaderAsync(cmd, commandBehavior, ct);
	}
}
