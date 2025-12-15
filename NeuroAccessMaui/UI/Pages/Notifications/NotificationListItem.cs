using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;

namespace NeuroAccessMaui.UI.Pages.Notifications
{
	/// <summary>
	/// DTO for notification list display.
	/// </summary>
	public partial class NotificationListItem : ObservableObject
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationListItem"/> class.
		/// </summary>
		/// <param name="Id">Identifier.</param>
		/// <param name="Title">Title.</param>
		/// <param name="Body">Body.</param>
		/// <param name="Channel">Channel label used for icon selection.</param>
		/// <param name="DateText">Date text.</param>
		/// <param name="StateLabel">State label.</param>
		/// <param name="OccurrenceCount">Number of merged occurrences.</param>
		public NotificationListItem(string Id, string Title, string? Body, string? Channel, string DateText, string StateLabel, int OccurrenceCount)
		{
			this.Id = Id;
			this.Title = Title;
			this.Body = Body ?? string.Empty;
			this.Channel = Channel ?? string.Empty;
			this.DateText = DateText;
			this.StateLabel = StateLabel;
			this.OccurrenceCount = OccurrenceCount <= 0 ? 1 : OccurrenceCount;
			this.StateColor = StateLabel switch
			{
				"New" => AppColors.TnPWarningBg,
				"Read" => AppColors.TnPInfoBg,
				"Opened" => AppColors.TnPInfoBg,
				_ => Colors.Transparent
			};
			this.StateTextColor = StateLabel switch
			{
				"New" => AppColors.TnPWarningContent,
				"Read" => AppColors.TnPInfoContent,
				"Opened" => AppColors.TnPInfoContent,
				_ => Colors.Transparent
			};
		}

		/// <summary>
		/// Identifier.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Body.
		/// </summary>
		public string Body { get; }

		/// <summary>
		/// Channel label used for display and icon resolution.
		/// </summary>
		public string Channel { get; }

		/// <summary>
		/// Date text.
		/// </summary>
		public string DateText { get; }

		/// <summary>
		/// State label.
		/// </summary>
		public string StateLabel { get; }

		/// <summary>
		/// Number of occurrences merged into this record.
		/// </summary>
		public int OccurrenceCount { get; }

		/// <summary>
		/// State chip color.
		/// </summary>
		public Color StateColor { get; }

		/// <summary>
		/// State chip text color.
		/// </summary>
		public Color StateTextColor { get; }
	}
}
