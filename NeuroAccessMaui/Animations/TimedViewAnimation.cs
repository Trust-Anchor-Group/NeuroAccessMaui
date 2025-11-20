using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Base class providing timing helpers for view animations.
	/// </summary>
	public abstract class TimedViewAnimation : IViewAnimation
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TimedViewAnimation"/> class.
		/// </summary>
		/// <param name="Duration">Default duration.</param>
		/// <param name="Easing">Default easing.</param>
		protected TimedViewAnimation(TimeSpan Duration, Easing Easing)
		{
			if (Duration < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(Duration), "Duration cannot be negative.");
			this.BaseDuration = Duration;
			this.Easing = Easing ?? throw new ArgumentNullException(nameof(Easing));
		}

		/// <summary>
		/// Gets the base duration.
		/// </summary>
		protected TimeSpan BaseDuration { get; }

		/// <summary>
		/// Gets the base easing.
		/// </summary>
		protected Easing Easing { get; }

		/// <inheritdoc/>
		public abstract Task RunAsync(VisualElement Target, IAnimationContext Context, AnimationOptions? Options, CancellationToken Token);

		/// <summary>
		/// Resolves the effective duration, respecting overrides and duration scale.
		/// </summary>
		/// <param name="Context">Animation context.</param>
		/// <param name="Options">Execution options.</param>
		/// <returns>Effective duration.</returns>
		protected TimeSpan ResolveDuration(IAnimationContext Context, AnimationOptions? Options)
		{
			if (Options?.DurationOverride is TimeSpan Override)
				return Override;

			double Scale = Context.DurationScale <= 0 ? 1 : Context.DurationScale;
			double Milliseconds = this.BaseDuration.TotalMilliseconds * Scale;
			if (Milliseconds < 0)
				Milliseconds = 0;
			return TimeSpan.FromMilliseconds(Milliseconds);
		}

		/// <summary>
		/// Resolves the duration expressed in milliseconds.
		/// </summary>
		/// <param name="Context">Animation context.</param>
		/// <param name="Options">Execution options.</param>
		/// <returns>Duration in milliseconds.</returns>
		protected uint ResolveDurationMilliseconds(IAnimationContext Context, AnimationOptions? Options)
		{
			TimeSpan Duration = this.ResolveDuration(Context, Options);
			double Milliseconds = Math.Round(Duration.TotalMilliseconds);
			if (Milliseconds < 0)
				Milliseconds = 0;
			return (uint)Milliseconds;
		}
	}
}
