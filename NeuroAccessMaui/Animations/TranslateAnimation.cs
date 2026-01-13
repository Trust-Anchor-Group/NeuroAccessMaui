using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Represents a translation animation affecting the X and Y axes.
	/// </summary>
	public sealed class TranslateAnimation : TimedViewAnimation
	{
		private readonly double? fromX;
		private readonly double? fromY;
		private readonly double toX;
		private readonly double toY;

		/// <summary>
		/// Initializes a new instance of the <see cref="TranslateAnimation"/> class.
		/// </summary>
		/// <param name="Duration">Default duration.</param>
		/// <param name="Easing">Default easing.</param>
		/// <param name="FromX">Optional starting translation X.</param>
		/// <param name="FromY">Optional starting translation Y.</param>
		/// <param name="ToX">Target translation X.</param>
		/// <param name="ToY">Target translation Y.</param>
		public TranslateAnimation(TimeSpan Duration, Easing Easing, double? FromX, double? FromY, double ToX, double ToY)
			: base(Duration, Easing)
		{
			this.fromX = FromX;
			this.fromY = FromY;
			this.toX = ToX;
			this.toY = ToY;
		}

		/// <inheritdoc/>
		public override async Task RunAsync(VisualElement Target, IAnimationContext Context, AnimationOptions? Options, CancellationToken Token)
		{
			ArgumentNullException.ThrowIfNull(Target);
			ArgumentNullException.ThrowIfNull(Context);

			if (Context.ReduceMotion && (Options?.ForceMotion != true))
			{
				Target.TranslationX = this.toX;
				Target.TranslationY = this.toY;
				return;
			}

			if (this.fromX.HasValue)
				Target.TranslationX = this.fromX.Value;
			if (this.fromY.HasValue)
				Target.TranslationY = this.fromY.Value;

			uint DurationMs = this.ResolveDurationMilliseconds(Context, Options);
			Func<CancellationToken, Task> Execute = _ => Target.TranslateToAsync(this.toX, this.toY, DurationMs, this.Easing);
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
				await this.ApplyFinalTranslationAsync(Target);
			}

			DispatchInfo?.Throw();
		}

		private Task ApplyFinalTranslationAsync(VisualElement Target)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				Target.TranslationX = this.toX;
				Target.TranslationY = this.toY;
			});
		}
	}
}
