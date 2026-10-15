namespace NeuroAccessMaui.UI.Controls
{
	/// <summary>
	/// Defines the display state of a step in an identity flow, such as a row in an application status timeline.
	/// </summary>
	public enum IdentityFlowPhaseState
	{
		/// <summary>
		/// The step has not started yet.
		/// </summary>
		Upcoming = 0,

		/// <summary>
		/// The step is in progress.
		/// </summary>
		Current = 1,

		/// <summary>
		/// The step is complete.
		/// </summary>
		Complete = 2
	}
}
