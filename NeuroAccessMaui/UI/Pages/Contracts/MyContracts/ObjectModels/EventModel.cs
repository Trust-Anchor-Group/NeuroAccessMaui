using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;
using System.Windows.Input;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// The data model for a notification event that is not associate with a referenced contract.
	/// </summary>
	public class EventModel : ObservableObject, IUniqueItem
	{
		/// <summary>
		/// Creates an instance of the <see cref="EventModel"/> class.
		/// </summary>
		/// <param name="Received">When event was received.</param>
		/// <param name="Icon">Icon of event.</param>
		/// <param name="Description">Description of event.</param>
		/// <param name="Event">Notification event object.</param>
		public EventModel(DateTime Received, string Icon, string Description, NotificationEvent Event)
		{
			this.Received = Received;
			this.Icon = Icon;
			this.Description = Description;
			this.Event = Event;

			this.ClickedCommand = new Command(_ => this.Clicked());
		}

		/// <summary>
		/// When event was received.
		/// </summary>
		public DateTime Received { get; }

		/// <summary>
		/// Icon of event.
		/// </summary>
		public string Icon { get; }

		/// <summary>
		/// Description of event.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Notification event object.
		/// </summary>
		public NotificationEvent Event { get; }

		/// <inheritdoc/>
		public string UniqueName => this.Event.ObjectId ?? string.Empty;

		/// <summary>
		/// Command executed when the token has been clicked or tapped.
		/// </summary>
		public ICommand ClickedCommand { get; }

		/// <summary>
		/// Opens the notification event.
		/// </summary>
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
