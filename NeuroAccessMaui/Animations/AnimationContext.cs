using System;
using Microsoft.Maui;
using Microsoft.Maui.Devices;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Represents contextual information required when executing an animation.
	/// </summary>
	public interface IAnimationContext
	{
		/// <summary>
		/// Gets the platform identifier (e.g., Android, iOS, WinUI).
		/// </summary>
		string Platform { get; }

		/// <summary>
		/// Gets the optional theme key associated with the animation request.
		/// </summary>
		string? Theme { get; }

		/// <summary>
		/// Gets the safe-area insets active for the request.
		/// </summary>
		Thickness SafeAreaInsets { get; }

		/// <summary>
		/// Gets the active keyboard inset height in device-independent units.
		/// </summary>
		double KeyboardInset { get; }

		/// <summary>
		/// Gets the available viewport width for the animation.
		/// </summary>
		double ViewportWidth { get; }

		/// <summary>
		/// Gets the available viewport height for the animation.
		/// </summary>
		double ViewportHeight { get; }

		/// <summary>
		/// Gets a value indicating whether the caller requests reduced motion.
		/// </summary>
		bool ReduceMotion { get; }

		/// <summary>
		/// Gets the multiplier applied to animation durations.
		/// </summary>
		double DurationScale { get; }
	}

	/// <summary>
	/// Data structure used when building animation contexts.
	/// </summary>
	public sealed class AnimationContextOptions
	{
		/// <summary>
		/// Gets a default instance with no overrides.
		/// </summary>
		public static AnimationContextOptions Default { get; } = new AnimationContextOptions();

		/// <summary>
		/// Gets or sets an optional platform override.
		/// </summary>
		public string? Platform { get; init; }

		/// <summary>
		/// Gets or sets an optional theme identifier.
		/// </summary>
		public string? Theme { get; init; }

		/// <summary>
		/// Gets or sets an optional safe-area inset value.
		/// </summary>
		public Thickness? SafeAreaInsets { get; init; }

		/// <summary>
		/// Gets or sets an optional keyboard inset height.
		/// </summary>
		public double? KeyboardInset { get; init; }

		/// <summary>
		/// Gets or sets an optional viewport width value.
		/// </summary>
		public double? ViewportWidth { get; init; }

		/// <summary>
		/// Gets or sets an optional viewport height value.
		/// </summary>
		public double? ViewportHeight { get; init; }
	}

	/// <summary>
	/// Concrete snapshot of contextual data used during animation execution.
	/// </summary>
	public sealed class AnimationContext : IAnimationContext
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AnimationContext"/> class.
		/// </summary>
		/// <param name="Platform">Active platform.</param>
		/// <param name="Theme">Active theme.</param>
		/// <param name="SafeAreaInsets">Safe-area insets.</param>
		/// <param name="KeyboardInset">Keyboard inset height.</param>
		/// <param name="ViewportWidth">Viewport width available to the animation.</param>
		/// <param name="ViewportHeight">Viewport height available to the animation.</param>
		/// <param name="ReduceMotion">True to prefer reduced motion.</param>
		/// <param name="DurationScale">Duration multiplier applied to animations.</param>
		public AnimationContext(string Platform, string? Theme, Thickness SafeAreaInsets, double KeyboardInset, double ViewportWidth, double ViewportHeight, bool ReduceMotion, double DurationScale)
		{
			if (string.IsNullOrWhiteSpace(Platform))
				throw new ArgumentException("Platform cannot be null or whitespace.", nameof(Platform));

			this.Platform = Platform;
			this.Theme = Theme;
			this.SafeAreaInsets = SafeAreaInsets;
			this.KeyboardInset = KeyboardInset;
			this.ViewportWidth = ViewportWidth;
			this.ViewportHeight = ViewportHeight;
			this.ReduceMotion = ReduceMotion;
			this.DurationScale = DurationScale;
		}

		/// <inheritdoc/>
		public string Platform { get; }

		/// <inheritdoc/>
		public string? Theme { get; }

		/// <inheritdoc/>
		public Thickness SafeAreaInsets { get; }

		/// <inheritdoc/>
		public double KeyboardInset { get; }

		/// <inheritdoc/>
		public double ViewportWidth { get; }

		/// <inheritdoc/>
		public double ViewportHeight { get; }

		/// <inheritdoc/>
		public bool ReduceMotion { get; }

		/// <inheritdoc/>
		public double DurationScale { get; }
	}

	/// <summary>
	/// Factory abstraction responsible for constructing animation contexts.
	/// </summary>
	public interface IAnimationContextProvider
	{
		/// <summary>
		/// Builds a context instance using provided options.
		/// </summary>
		/// <param name="Options">Options controlling the created context.</param>
		/// <returns>Created context instance.</returns>
		IAnimationContext CreateContext(AnimationContextOptions? Options);
	}

	/// <summary>
	/// Default implementation of <see cref="IAnimationContextProvider"/>.
	/// </summary>
	public sealed class AnimationContextProvider : IAnimationContextProvider
	{
		private readonly IMotionSettings motionSettings;

		/// <summary>
		/// Initializes a new instance of the <see cref="AnimationContextProvider"/> class.
		/// </summary>
		/// <param name="MotionSettings">Motion settings service.</param>
		public AnimationContextProvider(IMotionSettings MotionSettings)
		{
			this.motionSettings = MotionSettings ?? throw new ArgumentNullException(nameof(MotionSettings));
		}

		/// <inheritdoc/>
		public IAnimationContext CreateContext(AnimationContextOptions? Options)
		{
			AnimationContextOptions EffectiveOptions = Options ?? AnimationContextOptions.Default;
			string Platform = EffectiveOptions.Platform ?? DeviceInfo.Platform.ToString();
			string? Theme = EffectiveOptions.Theme;
			Thickness SafeArea = EffectiveOptions.SafeAreaInsets ?? Thickness.Zero;
			double KeyboardInset = EffectiveOptions.KeyboardInset ?? 0;
			double ViewportWidth = EffectiveOptions.ViewportWidth ?? 0;
			double ViewportHeight = EffectiveOptions.ViewportHeight ?? 0;
			bool ReduceMotion = this.motionSettings.ReduceMotion;
			double DurationScale = this.motionSettings.DurationScale;
			return new AnimationContext(Platform, Theme, SafeArea, KeyboardInset, ViewportWidth, ViewportHeight, ReduceMotion, DurationScale);
		}
	}
}
