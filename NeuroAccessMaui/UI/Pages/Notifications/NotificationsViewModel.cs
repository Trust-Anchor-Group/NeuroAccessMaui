using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.MVVM;

namespace NeuroAccessMaui.UI.Pages.Notifications
{
	public partial class NotificationsViewModel : BaseViewModel
	{

		public ObservableTask NotificationsLoader = new();

		[ObservableProperty]
		private ObservableCollection<NotificationEvent> notifications = new();


		protected override Task OnAppearing()
		{
			ServiceRef.NotificationService.OnNewNotification += this.OnNotificationEvent;
			//Load notifications
			this.NotificationsLoader.Load(async () =>
			{
				NotificationEvent[] NotificationEvents = ServiceRef.NotificationService.GetAllEvents();
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Notifications.Clear();
					foreach (NotificationEvent Event in NotificationEvents)
						this.Notifications.Add(Event);
				});
			});
			return base.OnAppearing();
		}

		protected override Task OnDisappearing()
		{
			ServiceRef.NotificationService.OnNewNotification -= this.OnNotificationEvent;

			return base.OnDisappearing();
		}

		//On notification event
		public async Task OnNotificationEvent(object sender, NotificationEventArgs e)
		{
			//Wait for notification to load first
			await this.NotificationsLoader.WaitAllAsync(true);

			//if notification is not already in the list, add it
			///TODO: FIX this check
			if (!this.Notifications.Any(n => n.ObjectId == e.Event.ObjectId))
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Notifications.Add(e.Event);
				});
			}
		}

	}
}
