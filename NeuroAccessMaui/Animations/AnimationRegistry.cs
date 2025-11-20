using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Registry abstraction for animation descriptors.
	/// </summary>
	public interface IAnimationRegistry
	{
		/// <summary>
		/// Attempts to create a view animation for the given key and context.
		/// </summary>
		/// <param name="Key">Animation key.</param>
		/// <param name="Context">Execution context.</param>
		/// <param name="Animation">Resulting animation when available.</param>
		/// <returns>True if an animation could be created; otherwise false.</returns>
		bool TryCreateViewAnimation(AnimationKey Key, IAnimationContext Context, out IViewAnimation Animation);

		/// <summary>
		/// Attempts to create a transition animation for the given key and context.
		/// </summary>
		/// <param name="Key">Animation key.</param>
		/// <param name="Context">Execution context.</param>
		/// <param name="Animation">Resulting animation when available.</param>
		/// <returns>True if an animation could be created; otherwise false.</returns>
		bool TryCreateTransition(AnimationKey Key, IAnimationContext Context, out ITransitionAnimation Animation);

		/// <summary>
		/// Registers a view animation descriptor.
		/// </summary>
		/// <param name="Descriptor">Descriptor to register.</param>
		/// <param name="Platform">Optional platform scope.</param>
		/// <param name="Theme">Optional theme scope.</param>
		void Register(ViewAnimationDescriptor Descriptor, string? Platform = null, string? Theme = null);

		/// <summary>
		/// Registers a transition animation descriptor.
		/// </summary>
		/// <param name="Descriptor">Descriptor to register.</param>
		/// <param name="Platform">Optional platform scope.</param>
		/// <param name="Theme">Optional theme scope.</param>
		void Register(TransitionAnimationDescriptor Descriptor, string? Platform = null, string? Theme = null);
	}

	/// <summary>
	/// Default implementation of <see cref="IAnimationRegistry"/>.
	/// </summary>
	public sealed class AnimationRegistry : IAnimationRegistry
	{
		private readonly ConcurrentDictionary<string, ViewAnimationDescriptor> viewDescriptors = new(StringComparer.Ordinal);
		private readonly ConcurrentDictionary<string, TransitionAnimationDescriptor> transitionDescriptors = new(StringComparer.Ordinal);

		/// <inheritdoc/>
		public bool TryCreateViewAnimation(AnimationKey Key, IAnimationContext Context, out IViewAnimation Animation)
		{
			ArgumentNullException.ThrowIfNull(Context);

			ViewAnimationDescriptor? Descriptor = ResolveDescriptor(this.viewDescriptors, Key, Context);
			if (Descriptor is null)
			{
				Animation = default!;
				return false;
			}

			Animation = Descriptor.CreateExecutor(Context);
			return true;
		}

		/// <inheritdoc/>
		public bool TryCreateTransition(AnimationKey Key, IAnimationContext Context, out ITransitionAnimation Animation)
		{
			ArgumentNullException.ThrowIfNull(Context);

			TransitionAnimationDescriptor? Descriptor = ResolveDescriptor(this.transitionDescriptors, Key, Context);
			if (Descriptor is null)
			{
				Animation = default!;
				return false;
			}

			Animation = Descriptor.CreateExecutor(Context);
			return true;
		}

		/// <inheritdoc/>
		public void Register(ViewAnimationDescriptor Descriptor, string? Platform = null, string? Theme = null)
		{
			ArgumentNullException.ThrowIfNull(Descriptor);
			string Key = ComposeKey(Descriptor.Key, Platform, Theme);
			this.viewDescriptors[Key] = Descriptor;
		}

		/// <inheritdoc/>
		public void Register(TransitionAnimationDescriptor Descriptor, string? Platform = null, string? Theme = null)
		{
			ArgumentNullException.ThrowIfNull(Descriptor);
			string Key = ComposeKey(Descriptor.Key, Platform, Theme);
			this.transitionDescriptors[Key] = Descriptor;
		}

		private static TDescriptor? ResolveDescriptor<TDescriptor>(ConcurrentDictionary<string, TDescriptor> Descriptors, AnimationKey Key, IAnimationContext Context)
			where TDescriptor : AnimationDescriptor
		{
			string BaseKey = Key.Value;
			string PlatformKey = string.IsNullOrWhiteSpace(Context.Platform) ? string.Empty : Context.Platform;
			string? ThemeKey = Context.Theme;

			List<string> Candidates = new List<string>();

			if (!string.IsNullOrWhiteSpace(PlatformKey) && !string.IsNullOrWhiteSpace(ThemeKey))
				Candidates.Add(ComposeKey(BaseKey, PlatformKey, ThemeKey));

			if (!string.IsNullOrWhiteSpace(PlatformKey))
				Candidates.Add(ComposeKey(BaseKey, PlatformKey, null));

			if (!string.IsNullOrWhiteSpace(ThemeKey))
				Candidates.Add(ComposeKey(BaseKey, null, ThemeKey));

			Candidates.Add(BaseKey);

			foreach (string Candidate in Candidates)
			{
				if (Descriptors.TryGetValue(Candidate, out TDescriptor Descriptor))
					return Descriptor;
			}

			return null;
		}

		private static string ComposeKey(AnimationKey Key, string? Platform, string? Theme)
		{
			return ComposeKey(Key.Value, Platform, Theme);
		}

		private static string ComposeKey(string BaseKey, string? Platform, string? Theme)
		{
			StringBuilder Builder = new StringBuilder(BaseKey);
			if (!string.IsNullOrWhiteSpace(Platform))
			{
				Builder.Append('.');
				Builder.Append(Platform);
			}

			if (!string.IsNullOrWhiteSpace(Theme))
			{
				Builder.Append('.');
				Builder.Append(Theme);
			}

			return Builder.ToString();
		}
	}
}
