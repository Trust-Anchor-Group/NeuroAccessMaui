using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Animations;

namespace NeuroAccessMaui.UI.Controls
{
	public sealed class CrossFadeViewTransition : IViewTransition
	{
		public async Task RunAsync(ViewSwitcherTransitionRequest request, CancellationToken cancellationToken)
		{
			if (request is null)
				return;

			if (!request.Animate)
				return;

			if (request.IsInitial)
				return;

			VisualElement? oldElement = request.OldView as VisualElement;
			VisualElement? newElement = request.NewView as VisualElement;

			if (oldElement is null && newElement is null)
				return;

			cancellationToken.ThrowIfCancellationRequested();

			if (MotionPreferences.ReduceMotion)
			{
				// A reused view may still be transparent from when it was faded out.
				if (newElement is not null)
					newElement.Opacity = 1.0;

				return;
			}

			Task? fadeOutTask = null;
			if (oldElement is not null)
			{
				fadeOutTask = oldElement.FadeToAsync(0.0, request.Duration / 2, request.Easing);
			}

			Task? fadeInTask = null;
			if (newElement is not null)
			{
				newElement.Opacity = 0.0;
				fadeInTask = newElement.FadeToAsync(1.0, request.Duration / 2, request.Easing);
			}

			if (fadeOutTask is not null && fadeInTask is not null)
			{
				await Task.WhenAll(fadeOutTask, fadeInTask);
			}
			else if (fadeOutTask is not null)
			{
				await fadeOutTask;
			}
			else if (fadeInTask is not null)
			{
				await fadeInTask;
			}
		}
	}
}
