using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;

namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels
{
	/// <summary>
	/// Represents a notification event displayed outside a referenced contract.
	/// </summary>
	/// <param name="Received">When the event was received.</param>
	/// <param name="Icon">The icon representing the event.</param>
	/// <param name="Description">The event description.</param>
	/// <param name="Event">The underlying notification event.</param>
	public partial class EventModel(DateTime Received, Geometry Icon, string Description, NotificationEvent Event) : ObservableObject, IUniqueItem
	{
		/// <summary>
		/// Gets when the event was received.
		/// </summary>
		public DateTime Received { get; } = Received;

		/// <summary>
		/// Gets the icon representing the event.
		/// </summary>
		public Geometry Icon { get; } = Icon;

		/// <summary>
		/// Gets the event description.
		/// </summary>
		public string Description { get; } = Description;

		/// <summary>
		/// Gets the underlying notification event.
		/// </summary>
		public NotificationEvent Event { get; } = Event;

		/// <inheritdoc/>
		public string UniqueName => this.Event.ObjectId ?? string.Empty;

		/// <summary>
		/// Opens the notification and removes it when configured to be deleted after opening.
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
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}
	}
}
