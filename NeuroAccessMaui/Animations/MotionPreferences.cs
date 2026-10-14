using System;
using Microsoft.Extensions.DependencyInjection;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Gives views and controls one place to ask whether motion should be reduced.
	/// </summary>
	/// <remarks>
	/// Reads the registered <see cref="IMotionSettings"/>, which mirrors the operating system's accessibility setting.
	/// Animations played through <see cref="IAnimationCoordinator"/> already honor the setting. Use this for animations
	/// that a view runs directly, and show the settled end state instead when <see cref="ReduceMotion"/> is <c>true</c>.
	/// </remarks>
	public static class MotionPreferences
	{
		private static IMotionSettings? motionSettings;

		/// <summary>
		/// Gets the app-wide motion settings, or <c>null</c> when no service provider is available, such as in
		/// design-time or isolated rendering.
		/// </summary>
		/// <remarks>
		/// Views with looping animations can subscribe to <see cref="IMotionSettings.MotionSettingsChanged"/> while they
		/// are loaded, so they settle or resume when the preference changes.
		/// </remarks>
		public static IMotionSettings? Settings
		{
			get
			{
				try
				{
					motionSettings ??= ServiceRef.Provider?.GetService<IMotionSettings>();
					return motionSettings;
				}
				catch (Exception)
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether animations should be skipped in favor of their settled end state.
		/// </summary>
		public static bool ReduceMotion => Settings?.ReduceMotion == true;
	}
}
