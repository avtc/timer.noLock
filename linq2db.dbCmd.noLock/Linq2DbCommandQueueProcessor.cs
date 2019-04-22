using dbCmd.noLock;
using LinqToDB.Data.DbCommandProcessor;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace linq2db.dbCmd.noLock
{
    /// <summary>
    /// To enable - at start of app add: DbCommandProcessorExtensions.Instance = new Linq2DbCommandQueueProcessor();
    /// </summary>
    public class Linq2DbCommandQueueProcessor : IDbCommandProcessor
    {
        public int ExecuteNonQuery(DbCommand cmd) =>
            cmd.ExecuteNonQueryQueued();

        public Task<int> ExecuteNonQueryAsync(DbCommand cmd, CancellationToken ct) =>
            cmd.ExecuteNonQueryQueuedAsync(ct);

        public DbDataReader ExecuteReader(DbCommand cmd, CommandBehavior commandBehavior) =>
            cmd.ExecuteReaderQueued(commandBehavior);

        public Task<DbDataReader> ExecuteReaderAsync(DbCommand cmd, CommandBehavior commandBehavior, CancellationToken ct) =>
            cmd.ExecuteReaderQueuedAsync(commandBehavior, ct);

        public object ExecuteScalar(DbCommand cmd) =>
            cmd.ExecuteScalarQueued();

        public Task<object> ExecuteScalarAsync(DbCommand cmd, CancellationToken ct) =>
            cmd.ExecuteScalarQueuedAsync(ct);
    }
}
