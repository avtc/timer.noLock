using System.Threading;

namespace dbCmd.noLock
{
	public class DbCommandQueueStats
	{
		public static readonly DbCommandQueueStats Instance = new DbCommandQueueStats();

		private volatile int _queuedCommands;
		public int QueuedCommands => _queuedCommands;

		public void IncQueuedCommands()
		{
			Interlocked.Increment(ref _queuedCommands);
		}

		public void DecQueuedCommands()
		{
			Interlocked.Decrement(ref _queuedCommands);
		}
	}
}
