using System;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Options applied for an individual animation execution.
	/// </summary>
	public sealed class AnimationOptions
	{
		/// <summary>
		/// Gets or sets an optional duration override applied to the target animation.
		/// </summary>
		public TimeSpan? DurationOverride { get; init; }

		/// <summary>
		/// Gets or sets a value indicating whether motion should run even when global settings request reduced motion.
		/// </summary>
		public bool ForceMotion { get; init; }

		/// <summary>
		/// Gets or sets an optional callback invoked when the animation completes successfully.
		/// </summary>
		public Action? Completed { get; init; }
	}
}
