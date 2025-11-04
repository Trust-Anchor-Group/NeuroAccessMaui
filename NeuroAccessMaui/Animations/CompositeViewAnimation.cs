using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Represents an animation composed of child animations.
	/// </summary>
	public sealed class CompositeViewAnimation : IViewAnimation
	{
		private readonly IReadOnlyList<IViewAnimation> children;
		private readonly AnimationCompositionMode mode;

		/// <summary>
		/// Initializes a new instance of the <see cref="CompositeViewAnimation"/> class.
		/// </summary>
		/// <param name="Mode">Composition mode.</param>
		/// <param name="Children">Child animations.</param>
		public CompositeViewAnimation(AnimationCompositionMode Mode, IReadOnlyList<IViewAnimation> Children)
		{
			this.mode = Mode;
			this.children = Children ?? throw new ArgumentNullException(nameof(Children));
		}

		/// <inheritdoc/>
		public async Task RunAsync(VisualElement Target, IAnimationContext Context, AnimationOptions? Options, CancellationToken Token)
		{
			ArgumentNullException.ThrowIfNull(Target);
			ArgumentNullException.ThrowIfNull(Context);

			if (this.children.Count == 0)
				return;

			if (this.mode == AnimationCompositionMode.Parallel)
			{
				List<Task> Tasks = new List<Task>(this.children.Count);
				foreach (IViewAnimation Child in this.children)
				{
					Task Execution = Child.RunAsync(Target, Context, Options, Token);
					Tasks.Add(Execution);
				}
				await Task.WhenAll(Tasks).ConfigureAwait(false);
			}
			else
			{
				foreach (IViewAnimation Child in this.children)
				{
					await Child.RunAsync(Target, Context, Options, Token).ConfigureAwait(false);
				}
			}
		}
	}
}
