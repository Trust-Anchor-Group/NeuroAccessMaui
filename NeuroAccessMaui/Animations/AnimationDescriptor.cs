using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Base class for animation descriptors.
	/// </summary>
	public abstract class AnimationDescriptor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AnimationDescriptor"/> class.
		/// </summary>
		/// <param name="Key">Descriptor key.</param>
		/// <param name="Duration">Base duration.</param>
		/// <param name="Easing">Default easing.</param>
		/// <param name="Metadata">Optional metadata dictionary.</param>
		protected AnimationDescriptor(AnimationKey Key, TimeSpan Duration, Easing Easing, IReadOnlyDictionary<string, object>? Metadata)
		{
			if (Duration <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(Duration), "Duration must be positive.");
			this.Key = Key;
			this.Duration = Duration;
			this.Easing = Easing ?? throw new ArgumentNullException(nameof(Easing));
			this.Metadata = Metadata ?? new Dictionary<string, object>();
		}

		/// <summary>
		/// Gets the descriptor key.
		/// </summary>
		public AnimationKey Key { get; }

		/// <summary>
		/// Gets the default duration.
		/// </summary>
		public TimeSpan Duration { get; }

		/// <summary>
		/// Gets the default easing.
		/// </summary>
		public Easing Easing { get; }

		/// <summary>
		/// Gets optional metadata describing the animation.
		/// </summary>
		public IReadOnlyDictionary<string, object> Metadata { get; }
	}

	/// <summary>
	/// Descriptor defining a reusable view animation profile.
	/// </summary>
	public sealed class ViewAnimationDescriptor : AnimationDescriptor
	{
		private readonly Func<IAnimationContext, IViewAnimation> factory;

		/// <summary>
		/// Initializes a new instance of the <see cref="ViewAnimationDescriptor"/> class.
		/// </summary>
		/// <param name="Key">Descriptor key.</param>
		/// <param name="Duration">Default duration.</param>
		/// <param name="Easing">Default easing.</param>
		/// <param name="Factory">Factory creating executors.</param>
		/// <param name="Metadata">Optional metadata dictionary.</param>
		public ViewAnimationDescriptor(AnimationKey Key, TimeSpan Duration, Easing Easing, Func<IAnimationContext, IViewAnimation> Factory, IReadOnlyDictionary<string, object>? Metadata = null)
			: base(Key, Duration, Easing, Metadata)
		{
			this.factory = Factory ?? throw new ArgumentNullException(nameof(Factory));
		}

		/// <summary>
		/// Creates an executor for the given context.
		/// </summary>
		/// <param name="Context">Active context.</param>
		/// <returns>Animation executor.</returns>
		public IViewAnimation CreateExecutor(IAnimationContext Context)
		{
			ArgumentNullException.ThrowIfNull(Context);
			return this.factory(Context);
		}
	}

	/// <summary>
	/// Descriptor defining a reusable transition animation profile.
	/// </summary>
	public sealed class TransitionAnimationDescriptor : AnimationDescriptor
	{
		private readonly Func<IAnimationContext, ITransitionAnimation> factory;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransitionAnimationDescriptor"/> class.
		/// </summary>
		/// <param name="Key">Descriptor key.</param>
		/// <param name="Duration">Default duration.</param>
		/// <param name="Easing">Default easing.</param>
		/// <param name="Factory">Factory creating transition executors.</param>
		/// <param name="Metadata">Optional metadata dictionary.</param>
		public TransitionAnimationDescriptor(AnimationKey Key, TimeSpan Duration, Easing Easing, Func<IAnimationContext, ITransitionAnimation> Factory, IReadOnlyDictionary<string, object>? Metadata = null)
			: base(Key, Duration, Easing, Metadata)
		{
			this.factory = Factory ?? throw new ArgumentNullException(nameof(Factory));
		}

		/// <summary>
		/// Creates an executor for the given context.
		/// </summary>
		/// <param name="Context">Active context.</param>
		/// <returns>Transition executor.</returns>
		public ITransitionAnimation CreateExecutor(IAnimationContext Context)
		{
			ArgumentNullException.ThrowIfNull(Context);
			return this.factory(Context);
		}
	}

	/// <summary>
	/// Defines how composite animations should execute child animations.
	/// </summary>
	public enum AnimationCompositionMode
	{
		/// <summary>
		/// Animations execute sequentially.
		/// </summary>
		Sequential,

		/// <summary>
		/// Animations execute in parallel.
		/// </summary>
		Parallel
	}
}
