using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeuroAccessMaui.UI.MVVM.Policies;

namespace NeuroAccessMaui.Services.Resilience
{
    /// <summary>
    /// Executes async operations through a pipeline of policies (timeout, retry, bulkhead, etc.).
    /// Service-safe: no UI dependencies.
    /// </summary>
    public static class PolicyRunner
    {
        public static Task RunAsync(
            Func<CancellationToken, Task> action,
            CancellationToken ct,
            params IAsyncPolicy[] policies)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            Func<CancellationToken, Task> pipeline = action;
            if (policies is { Length: > 0 })
            {
				policies.Reverse();

				foreach (IAsyncPolicy pol in policies)
                {
                    var next = pipeline;
                    pipeline = c => pol.ExecuteAsync(next, c);
                }
            }
            return pipeline(ct);
        }

        public static async Task<T> RunAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken ct,
            params IAsyncPolicy[] policies)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            // Build a non-generic pipeline by capturing the result.
            T? result = default;
            await RunAsync(async c => { result = await action(c).ConfigureAwait(false); }, ct, policies).ConfigureAwait(false);
            return result!;
        }
    }
}

