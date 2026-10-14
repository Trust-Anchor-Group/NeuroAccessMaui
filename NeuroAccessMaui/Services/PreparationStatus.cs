namespace NeuroAccessMaui.Services
{
	/// <summary>
	/// Describes whether input is ready for its intended action.
	/// </summary>
	public enum PreparationStatus
	{
		/// <summary>
		/// The input is ready to be used.
		/// </summary>
		Ready,

		/// <summary>
		/// The input is not supported in this context.
		/// </summary>
		Unsupported,

		/// <summary>
		/// The input is malformed, expired, or refers to something that does not exist.
		/// </summary>
		Invalid,

		/// <summary>
		/// A temporary failure, such as a missing connection or a timeout, prevented preparation.
		/// </summary>
		Unavailable,

		/// <summary>
		/// The operation was cancelled.
		/// </summary>
		Cancelled
	}
}
