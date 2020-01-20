using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace dbCmd.noLock
{
	/// <summary>
	/// This class exists to overcome lock contention with TimerQueue inside Sql Command Pipeline
	/// </summary>
	public class DbCommandQueueProcessor: IDisposable
	{
		private const TaskContinuationOptions _taskContinuationOptions
#if NET45
		= TaskContinuationOptions.None;
#else
		= TaskContinuationOptions.RunContinuationsAsynchronously;
#endif

		private const TaskCreationOptions _taskCreationOptions
#if NET45
		= TaskCreationOptions.None;
#else
		= TaskCreationOptions.RunContinuationsAsynchronously;
#endif

		private enum QueryKind
		{
			Unknown,
			ExecuteScalarAsync,
			ExecuteNonQueryAsync,
			ExecuteReaderAsync,
		}

		private class QueryDto
		{
			public TaskCompletionSource<object> TcsObject;
			public TaskCompletionSource<int> TcsInt;
			public TaskCompletionSource<DbDataReader> TcsReader;
			public DbCommand Cmd;
			public QueryKind Kind;
			public CancellationToken CancellationToken;
		}

		private readonly DbCommandQueueStats _stats;
		private readonly Thread _thread;
		private readonly ConcurrentQueue<QueryDto> _queue;
		private volatile bool _disposed;

		public DbCommandQueueProcessor()
		{
			_stats = DbCommandQueueStats.Instance;
			_queue = new ConcurrentQueue<QueryDto>();
			_thread = new Thread(() =>
			{
				using (var waitObject = new ManualResetEventSlim(false))
				{
					while (!_disposed)
					{
						// idle for 1 ms until new items appear in queue
						waitObject.Wait(1);
						ProcessQueue();
					}
				}
			})
			{
				IsBackground = true
			};
			_thread.Start();
		}

		private void ProcessQueue()
		{
			if (_queue.Count == 0)
				return;
			while (_queue.TryDequeue(out var dto))
			{
				try
				{
					switch (dto.Kind)
					{
						case QueryKind.ExecuteScalarAsync:
							dto.Cmd.ExecuteScalarAsync(dto.CancellationToken)
								.ContinueWith((t, obj) => t.MarshalTaskResults((TaskCompletionSource<object>)obj),
								dto.TcsObject,
								CancellationToken.None,
								_taskContinuationOptions,
								TaskScheduler.Default);
							break;
						case QueryKind.ExecuteNonQueryAsync:
							dto.Cmd.ExecuteNonQueryAsync(dto.CancellationToken)
								.ContinueWith((t, obj) => t.MarshalTaskResults((TaskCompletionSource<int>)obj),
								dto.TcsInt,
                                CancellationToken.None,
								_taskContinuationOptions,
								TaskScheduler.Default);
							break;
						case QueryKind.ExecuteReaderAsync:
							dto.Cmd.ExecuteReaderAsync(dto.CancellationToken)
								.ContinueWith((t, obj) => t.MarshalTaskResults((TaskCompletionSource<DbDataReader>)obj),
								dto.TcsReader,
                                CancellationToken.None,
								_taskContinuationOptions,
								TaskScheduler.Default);
							break;
						default:
							dto.TcsObject?.TrySetException(new ArgumentNullException($"Query kind {dto.Kind} not implemented"));
							dto.TcsInt?.TrySetException(new ArgumentNullException($"Query kind {dto.Kind} not implemented"));
							dto.TcsReader?.TrySetException(new ArgumentNullException($"Query kind {dto.Kind} not implemented"));
							break;
					}
				}
				catch (Exception ex)
				{
					dto.TcsObject?.TrySetException(ex);
					dto.TcsInt?.TrySetException(ex);
					dto.TcsReader?.TrySetException(ex);
				}
				finally
				{
					_stats.DecQueuedCommands();
				}
			}
		}

		public object ExecuteScalar(DbCommand cmd)
		{
			return Task.Run(async () => await ExecuteScalarAsync(cmd, CancellationToken.None).ConfigureAwait(false))
				.ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public Task<object> ExecuteScalarAsync(DbCommand cmd, CancellationToken ct)
		{
			var dto = new QueryDto
			{
				Cmd = cmd,
				Kind = QueryKind.ExecuteScalarAsync,
				TcsObject = new TaskCompletionSource<object>(_taskCreationOptions),
				CancellationToken = ct,
			};
			_stats.IncQueuedCommands();
			_queue.Enqueue(dto);
			return dto.TcsObject.Task;
		}

		public int ExecuteNonQuery(DbCommand cmd)
		{
			return Task.Run(async () => await ExecuteNonQueryAsync(cmd, CancellationToken.None).ConfigureAwait(false))
				.ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public Task<int> ExecuteNonQueryAsync(DbCommand cmd, CancellationToken ct)
		{
			var dto = new QueryDto
			{
				Cmd = cmd,
				Kind = QueryKind.ExecuteNonQueryAsync,
				TcsInt = new TaskCompletionSource<int>(_taskCreationOptions),
				CancellationToken = ct,
			};
			_stats.IncQueuedCommands();
			_queue.Enqueue(dto);
			return dto.TcsInt.Task;
		}

		public DbDataReader ExecuteReader(DbCommand cmd, CommandBehavior commandBehavior)
		{
			return Task.Run(async () => await ExecuteReaderAsync(cmd, commandBehavior, CancellationToken.None).ConfigureAwait(false))
				.GetAwaiter().GetResult();
		}

		public Task<DbDataReader> ExecuteReaderAsync(DbCommand cmd, CommandBehavior commandBehavior, CancellationToken ct)
		{
			var dto = new QueryDto
			{
				Cmd = cmd,
				Kind = QueryKind.ExecuteReaderAsync,
				TcsReader = new TaskCompletionSource<DbDataReader>(_taskCreationOptions),
				CancellationToken = ct,
			};
			_stats.IncQueuedCommands();
			_queue.Enqueue(dto);
			return dto.TcsReader.Task;
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			ProcessQueue();
		}
	}
}
