using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// The data model for a notification event that is not associate with a referenced contract.
	/// </summary>
	/// <param name="Received">When event was received.</param>
	/// <param name="Icon">Icon of event.</param>
	/// <param name="Description">Description of event.</param>
	/// <param name="Event">Notification event object.</param>
	public partial class EventModel(DateTime Received, string Icon, string Description, NotificationEvent Event) : ObservableObject, IUniqueItem
	{
		/// <summary>
		/// When event was received.
		/// </summary>
		public DateTime Received { get; } = Received;

		/// <summary>
		/// Icon of event.
		/// </summary>
		public string Icon { get; } = Icon;

		/// <summary>
		/// Description of event.
		/// </summary>
		public string Description { get; } = Description;

		/// <summary>
		/// Notification event object.
		/// </summary>
		public NotificationEvent Event { get; } = Event;

		/// <inheritdoc/>
		public string UniqueName => this.Event.ObjectId ?? string.Empty;

		/// <summary>
		/// Command executed when the token has been clicked or tapped.
		/// </summary>
		[RelayCommand]
		public void Clicked()
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					await this.Event.Open();

					if (this.Event.DeleteWhenOpened)
						await ServiceRef.NotificationService.DeleteEvents(this.Event);
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}
			});
		}
	}
}
