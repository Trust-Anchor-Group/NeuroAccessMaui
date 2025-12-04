namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Represents a filter decision for notification handling.
	/// </summary>
	public sealed class NotificationFilterDecision
	{
		/// <summary>
		/// Gets a decision that does not ignore any operation.
		/// </summary>
		public static NotificationFilterDecision None { get; } = new NotificationFilterDecision(false, false, false);

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationFilterDecision"/> class.
		/// </summary>
		/// <param name="IgnoreRender">Whether to suppress OS rendering.</param>
		/// <param name="IgnoreStore">Whether to skip persistence.</param>
		/// <param name="IgnoreRoute">Whether to skip routing/navigation.</param>
		public NotificationFilterDecision(bool IgnoreRender, bool IgnoreStore, bool IgnoreRoute)
		{
			this.IgnoreRender = IgnoreRender;
			this.IgnoreStore = IgnoreStore;
			this.IgnoreRoute = IgnoreRoute;
		}

		/// <summary>
		/// Gets a value indicating whether rendering should be suppressed.
		/// </summary>
		public bool IgnoreRender { get; }

		/// <summary>
		/// Gets a value indicating whether storage should be suppressed.
		/// </summary>
		public bool IgnoreStore { get; }

		/// <summary>
		/// Gets a value indicating whether routing should be suppressed.
		/// </summary>
		public bool IgnoreRoute { get; }

		/// <summary>
		/// Merges this decision with another, OR-ing the ignore flags.
		/// </summary>
		/// <param name="Other">Other decision.</param>
		/// <returns>Merged decision.</returns>
		public NotificationFilterDecision Merge(NotificationFilterDecision Other)
		{
			return new NotificationFilterDecision(
				this.IgnoreRender || Other.IgnoreRender,
				this.IgnoreStore || Other.IgnoreStore,
				this.IgnoreRoute || Other.IgnoreRoute);
		}
	}
}
