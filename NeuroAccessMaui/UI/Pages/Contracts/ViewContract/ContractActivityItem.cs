using System;
using System.Globalization;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// Presents one reliable, locally available agreement-history entry.
	/// </summary>
	public sealed class ContractActivityItem
	{
		/// <summary>
		/// Initializes an agreement-history entry.
		/// </summary>
		/// <param name="Title">Localized activity title.</param>
		/// <param name="Description">Optional localized or participant description.</param>
		/// <param name="Timestamp">Timestamp associated with the activity.</param>
		public ContractActivityItem(string Title, string Description, DateTime Timestamp)
		{
			this.Title = Title;
			this.Description = Description;
			this.Timestamp = Timestamp;
		}

		/// <summary>
		/// Gets the localized activity title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Gets the optional activity description.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets whether an activity description is available.
		/// </summary>
		public bool HasDescription => !string.IsNullOrWhiteSpace(this.Description);

		/// <summary>
		/// Gets the activity time formatted using the current culture.
		/// </summary>
		public string TimestampText =>
			this.Timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

		/// <summary>
		/// Gets the timestamp used to order the history.
		/// </summary>
		internal DateTime Timestamp { get; }
	}
}
