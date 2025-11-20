using System;

namespace NeuroAccessMaui.Animations
{
	/// <summary>
	/// Enumeration describing animation lifecycle stages.
	/// </summary>
	public enum AnimationLifecycleStage
	{
		/// <summary>
		/// Animation started running.
		/// </summary>
		Started,

		/// <summary>
		/// Animation completed successfully.
		/// </summary>
		Completed,

		/// <summary>
		/// Animation was cancelled before completion.
		/// </summary>
		Cancelled,

		/// <summary>
		/// Animation was skipped because no matching profile existed.
		/// </summary>
		Skipped,

		/// <summary>
		/// Animation failed due to an exception.
		/// </summary>
		Failed
	}

	/// <summary>
	/// Data raised when an animation lifecycle event occurs.
	/// </summary>
	public sealed record AnimationEvent(
		AnimationKey Key,
		AnimationLifecycleStage Stage,
		TimeSpan? Elapsed,
		bool WasCancelled,
		Exception? Exception);
}
