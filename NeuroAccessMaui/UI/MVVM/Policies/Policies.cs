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
	}
}
