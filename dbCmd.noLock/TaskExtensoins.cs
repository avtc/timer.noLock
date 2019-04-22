using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace dbCmd.noLock
{
	internal static class TaskExtensoins
	{
		public struct VoidTypeStruct { }

		internal class SimpleSynchronizationContext : SynchronizationContext
		{
			internal static readonly SimpleSynchronizationContext Instance = new SimpleSynchronizationContext();
			internal static void Enqueue(Action action)
			{
				var sc = Current;
				SetSynchronizationContext(Instance);
				try
				{
					action();
				}
				finally
				{
					SetSynchronizationContext(sc);
				}
			}
			internal static T Enqueue<T>(Func<T> func)
			{
				var sc = Current;
				SetSynchronizationContext(Instance);
				try
				{
					return func();
				}
				finally
				{
					SetSynchronizationContext(sc);
				}
			}

		};

		public static void MarshalTaskResults<TResult>(this Task source, TaskCompletionSource<TResult> proxy)
		{
			switch (source.Status)
			{
				case TaskStatus.Faulted:
					proxy.TrySetExceptionAsync(source.Exception);
					break;
				case TaskStatus.Canceled:
					proxy.TrySetCanceledAsync();
					break;
				case TaskStatus.RanToCompletion:
					Task<TResult> castedSource = source as Task<TResult>;
					proxy.TrySetResultAsync(
						castedSource == null ? default(TResult) : // source is a Task
							castedSource.Result); // source is a Task<TResult>
					break;
			}
		}

		/// <summary>
		/// Emulates RunContinuationsAsynchronously from NetFramework 4.6 for async/await continuations
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="tcs"></param>
		/// <param name="result"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TrySetResultAsync<T>(this TaskCompletionSource<T> tcs, T result)
		{
#if NET45
			return SimpleSynchronizationContext.Enqueue(() => tcs.TrySetResult(result));
#else
			return tcs.TrySetResult(result);
#endif
		}
		/// <summary>
		/// Emulates RunContinuationsAsynchronously from NetFramework 4.6 for async/await continuations
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="tcs"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TrySetCanceledAsync<T>(this TaskCompletionSource<T> tcs)
		{
#if NET45
			return SimpleSynchronizationContext.Enqueue(() => tcs.TrySetCanceled());
#else
			return tcs.TrySetCanceled();
#endif
		}
		/// <summary>
		/// Emulates RunContinuationsAsynchronously from NetFramework 4.6 for async/await continuations
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="tcs"></param>
		/// <param name="ex"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TrySetExceptionAsync<T>(this TaskCompletionSource<T> tcs, Exception ex)
		{
#if NET45
			return SimpleSynchronizationContext.Enqueue(() => tcs.TrySetException(ex));
#else
			return tcs.TrySetException(ex);
#endif
		}
	}
}
