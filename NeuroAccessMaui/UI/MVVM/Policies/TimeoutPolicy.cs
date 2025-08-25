using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.MVVM.Policies
{
	sealed class TimeoutPolicy : IAsyncPolicy
	{
		private readonly TimeSpan timeout;
		public TimeoutPolicy(TimeSpan t) => this.timeout = t;

		public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
		{
			using CancellationTokenSource Linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
			Linked.CancelAfter(this.timeout);
			await action(Linked.Token);
		}
	}
}
