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
		/// <param name="Channel">Channel.</param>
		/// <param name="ChannelShort">Channel short label.</param>
		/// <param name="DateText">Date text.</param>
		/// <param name="StateLabel">State label.</param>
		public NotificationListItem(string Id, string Title, string? Body, string? Channel, string ChannelShort, string DateText, string StateLabel)
		{
			this.Id = Id;
			this.Title = Title;
			this.Body = Body ?? string.Empty;
			this.Channel = Channel ?? string.Empty;
			this.ChannelShort = ChannelShort;
			this.DateText = DateText;
			this.StateLabel = StateLabel;
			this.StateColor = string.IsNullOrEmpty(StateLabel) ? Colors.Transparent : AppColors.TnPSuccessBg;
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
		/// Channel.
		/// </summary>
		public string Channel { get; }

		/// <summary>
		/// Short channel label.
		/// </summary>
		public string ChannelShort { get; }

		/// <summary>
		/// Date text.
		/// </summary>
		public string DateText { get; }

		/// <summary>
		/// State label.
		/// </summary>
		public string StateLabel { get; }

		/// <summary>
		/// State chip color.
		/// </summary>
		public Color StateColor { get; }
	}
}
