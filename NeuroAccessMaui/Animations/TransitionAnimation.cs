using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Represents a pair of animations used for transitions.
	/// </summary>
	public sealed class TransitionAnimation : ITransitionAnimation
	{
		private readonly IViewAnimation? enterAnimation;
		private readonly IViewAnimation? exitAnimation;
		private readonly IViewAnimation? overlayAnimation;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransitionAnimation"/> class.
		/// </summary>
		/// <param name="EnterAnimation">Animation applied to the entering view.</param>
		/// <param name="ExitAnimation">Animation applied to the exiting view.</param>
		/// <param name="OverlayAnimation">Optional animation applied to a shared view.</param>
		public TransitionAnimation(IViewAnimation? EnterAnimation, IViewAnimation? ExitAnimation, IViewAnimation? OverlayAnimation = null)
		{
			this.enterAnimation = EnterAnimation;
			this.exitAnimation = ExitAnimation;
			this.overlayAnimation = OverlayAnimation;
		}

		/// <inheritdoc/>
		public async Task RunAsync(VisualElement? Entering, VisualElement? Exiting, IAnimationContext Context, AnimationOptions? Options, CancellationToken Token)
		{
			ArgumentNullException.ThrowIfNull(Context);
			List<Task> Tasks = new List<Task>(3);

			if (this.enterAnimation is not null && Entering is not null)
				Tasks.Add(this.enterAnimation.RunAsync(Entering, Context, Options, Token));

			if (this.exitAnimation is not null && Exiting is not null)
				Tasks.Add(this.exitAnimation.RunAsync(Exiting, Context, Options, Token));

			if (this.overlayAnimation is not null)
			{
				VisualElement Anchor = Entering ?? Exiting ?? throw new InvalidOperationException("Overlay animations require at least one visual element.");
				Tasks.Add(this.overlayAnimation.RunAsync(Anchor, Context, Options, Token));
			}

			if (Tasks.Count == 0)
				return;

			await Task.WhenAll(Tasks).ConfigureAwait(false);
		}
	}
}
