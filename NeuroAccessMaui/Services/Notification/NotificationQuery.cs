using System.Collections.Generic;

namespace NeuroAccessMaui.Services.Notification
{
	/// <summary>
	/// Query options for retrieving notifications.
	/// </summary>
	public sealed class NotificationQuery
	{
		/// <summary>
		/// Gets channels to include. Empty/null means all.
		/// </summary>
		public IReadOnlyList<string>? Channels { get; set; }

		/// <summary>
		/// Gets states to include. Empty/null means all.
		/// </summary>
		public IReadOnlyList<NotificationState>? States { get; set; }

		/// <summary>
		/// Gets the maximum number of notifications to return.
		/// </summary>
		public int? Limit { get; set; }

		/// <summary>
		/// Gets the number of notifications to skip before returning results.
		/// </summary>
		public int? Skip { get; set; }
	}
}
