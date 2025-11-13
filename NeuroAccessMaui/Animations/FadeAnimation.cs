using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Represents an opacity animation.
	/// </summary>
	public sealed class FadeAnimation : TimedViewAnimation
	{
		private readonly double? fromOpacity;
		private readonly double toOpacity;

		/// <summary>
		/// Initializes a new instance of the <see cref="FadeAnimation"/> class.
		/// </summary>
		/// <param name="Duration">Default duration.</param>
		/// <param name="Easing">Default easing.</param>
		/// <param name="FromOpacity">Optional starting opacity.</param>
		/// <param name="ToOpacity">Target opacity.</param>
		public FadeAnimation(TimeSpan Duration, Easing Easing, double? FromOpacity, double ToOpacity)
			: base(Duration, Easing)
		{
			if (ToOpacity < 0 || ToOpacity > 1)
				throw new ArgumentOutOfRangeException(nameof(ToOpacity), "Opacity must be between 0 and 1.");
			if (FromOpacity.HasValue && (FromOpacity.Value < 0 || FromOpacity.Value > 1))
				throw new ArgumentOutOfRangeException(nameof(FromOpacity), "Opacity must be between 0 and 1.");

			this.fromOpacity = FromOpacity;
			this.toOpacity = ToOpacity;
		}

		/// <inheritdoc/>
		public override async Task RunAsync(VisualElement Target, IAnimationContext Context, AnimationOptions? Options, CancellationToken Token)
		{
			ArgumentNullException.ThrowIfNull(Target);
			ArgumentNullException.ThrowIfNull(Context);

			if (Context.ReduceMotion && (Options?.ForceMotion != true))
			{
				Target.Opacity = this.toOpacity;
				return;
			}

			if (this.fromOpacity.HasValue)
				Target.Opacity = this.fromOpacity.Value;

			uint DurationMs = this.ResolveDurationMilliseconds(Context, Options);
			Func<CancellationToken, Task> Execute = _ => Target.FadeTo(this.toOpacity, DurationMs, this.Easing);
			ExceptionDispatchInfo? DispatchInfo = null;
			try
			{
				await AnimationRunner.RunAsync(Target, Execute, Token).ConfigureAwait(false);
			}
			catch (Exception Ex)
			{
				DispatchInfo = ExceptionDispatchInfo.Capture(Ex);
			}
			finally
			{
				await this.ApplyFinalOpacityAsync(Target).ConfigureAwait(false);
			}

			DispatchInfo?.Throw();
		}

		private Task ApplyFinalOpacityAsync(VisualElement Target)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				Target.Opacity = this.toOpacity;
			});
		}
	}
}
