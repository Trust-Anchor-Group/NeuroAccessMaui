using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Immutable context supplied to <see cref="IViewTransition"/> implementations.
	/// </summary>
	public sealed class ViewSwitcherTransitionRequest
	{
		public ViewSwitcherTransitionRequest(
			ViewSwitcher host,
			View? oldView,
			View? newView,
			bool isInitial,
			bool animate,
			uint duration,
			Easing? easing)
		{
			this.Host = host;
			this.OldView = oldView;
			this.NewView = newView;
			this.IsInitial = isInitial;
			this.Animate = animate;
			this.Duration = duration;
			this.Easing = easing;
		}

		public ViewSwitcher Host { get; }

		public View? OldView { get; }

		public View? NewView { get; }

		public bool IsInitial { get; }

		public bool Animate { get; }

		public uint Duration { get; }

		public Easing? Easing { get; }
	}
}
