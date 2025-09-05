using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM.Policies
{
    public static class Policies
    {
        public static IAsyncPolicy Timeout(TimeSpan timeout) => new TimeoutPolicy(timeout);
        public static IAsyncPolicy Retry(
            int maxAttempts,
            Func<int, Exception, TimeSpan> delayProvider,
            Action<int, Exception, TimeSpan>? onRetry = null) => new RetryPolicy(maxAttempts, delayProvider, onRetry);

        public static IAsyncPolicy Retry(
            int maxAttempts,
            Func<int, Exception, TimeSpan> delayProvider,
            Func<Exception, bool> shouldRetry,
            Action<int, Exception, TimeSpan>? onRetry = null) => new RetryPolicy(maxAttempts, delayProvider, onRetry, shouldRetry);

		// Convenience creators for additional policies
		public static IAsyncPolicy Debounce(TimeSpan delay) => new DebouncePolicy(delay);
		public static IAsyncPolicy Bulkhead(int maxParallel, int maxQueue = 0, Action? onRejected = null) => new BulkheadPolicy(maxParallel, maxQueue, onRejected);
		public static IAsyncPolicy CircuitBreaker(int failureThreshold, TimeSpan breakDuration, Action<string>? onStateChange = null)
			=> new CircuitBreakerPolicy(failureThreshold, breakDuration, onStateChange);
    }
}
