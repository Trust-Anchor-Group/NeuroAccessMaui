using System.Threading;
using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Defines a strategy used by <see cref="ViewSwitcher"/> to animate between two views.
	/// </summary>
	public interface IViewTransition
	{
		Task RunAsync(ViewSwitcherTransitionRequest request, CancellationToken cancellationToken);
	}
}
