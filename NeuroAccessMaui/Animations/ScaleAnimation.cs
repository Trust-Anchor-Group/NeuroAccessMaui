using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Represents a scale animation.
	/// </summary>
	public sealed class ScaleAnimation : TimedViewAnimation
	{
		private readonly double? fromScale;
		private readonly double toScale;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScaleAnimation"/> class.
		/// </summary>
		/// <param name="Duration">Default duration.</param>
		/// <param name="Easing">Default easing.</param>
		/// <param name="FromScale">Optional starting scale.</param>
		/// <param name="ToScale">Target scale.</param>
		public ScaleAnimation(TimeSpan Duration, Easing Easing, double? FromScale, double ToScale)
			: base(Duration, Easing)
		{
			if (ToScale < 0)
				throw new ArgumentOutOfRangeException(nameof(ToScale), "Scale cannot be negative.");
			if (FromScale.HasValue && FromScale.Value < 0)
				throw new ArgumentOutOfRangeException(nameof(FromScale), "Scale cannot be negative.");

			this.fromScale = FromScale;
			this.toScale = ToScale;
		}

		/// <inheritdoc/>
		public override async Task RunAsync(VisualElement Target, IAnimationContext Context, AnimationOptions? Options, CancellationToken Token)
		{
			ArgumentNullException.ThrowIfNull(Target);
			ArgumentNullException.ThrowIfNull(Context);

			if (Context.ReduceMotion && (Options?.ForceMotion != true))
			{
				Target.Scale = this.toScale;
				return;
			}

			if (this.fromScale.HasValue)
				Target.Scale = this.fromScale.Value;

			uint DurationMs = this.ResolveDurationMilliseconds(Context, Options);
			Func<CancellationToken, Task> Execute = _ => Target.ScaleToAsync(this.toScale, DurationMs, this.Easing);
			ExceptionDispatchInfo? DispatchInfo = null;
			try
			{
				await AnimationRunner.RunAsync(Target, Execute, Token);
			}
			catch (Exception Ex)
			{
				DispatchInfo = ExceptionDispatchInfo.Capture(Ex);
			}
			finally
			{
				await this.ApplyFinalScaleAsync(Target);
			}

			DispatchInfo?.Throw();
		}

		private Task ApplyFinalScaleAsync(VisualElement Target)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				Target.Scale = this.toScale;
			});
		}
	}
}
