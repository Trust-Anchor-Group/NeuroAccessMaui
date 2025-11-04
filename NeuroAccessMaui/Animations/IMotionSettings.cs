using System;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Represents motion preferences that affect animation execution.
	/// </summary>
	public interface IMotionSettings
	{
		/// <summary>
		/// Event raised when settings change.
		/// </summary>
		event EventHandler? MotionSettingsChanged;

		/// <summary>
		/// Gets a value indicating whether reduce-motion is enabled.
		/// </summary>
		bool ReduceMotion { get; }

		/// <summary>
		/// Gets the multiplier applied to animation durations.
		/// </summary>
		double DurationScale { get; }

		/// <summary>
		/// Updates the motion settings.
		/// </summary>
		/// <param name="ReduceMotion">New reduce-motion value.</param>
		/// <param name="DurationScale">New duration multiplier.</param>
		void Update(bool ReduceMotion, double DurationScale);
	}

	/// <summary>
	/// Default implementation of <see cref="IMotionSettings"/>.
	/// </summary>
	public sealed class MotionSettings : IMotionSettings
	{
		private bool reduceMotion;
		private double durationScale = 1.0;

		/// <inheritdoc/>
		public event EventHandler? MotionSettingsChanged;

		/// <inheritdoc/>
		public bool ReduceMotion => this.reduceMotion;

		/// <inheritdoc/>
		public double DurationScale => this.durationScale;

		/// <inheritdoc/>
		public void Update(bool ReduceMotion, double DurationScale)
		{
			if (DurationScale <= 0)
				throw new ArgumentOutOfRangeException(nameof(DurationScale), "Duration scale must be greater than zero.");

			bool ReduceMotionChanged = this.reduceMotion != ReduceMotion;
			bool DurationChanged = Math.Abs(this.durationScale - DurationScale) > double.Epsilon;

			if (!ReduceMotionChanged && !DurationChanged)
				return;

			this.reduceMotion = ReduceMotion;
			this.durationScale = DurationScale;

			this.MotionSettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
