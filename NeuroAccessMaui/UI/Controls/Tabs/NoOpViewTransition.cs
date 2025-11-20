using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Controls
{
	public sealed class NoOpViewTransition : IViewTransition
	{
		public Task RunAsync(ViewSwitcherTransitionRequest request, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
