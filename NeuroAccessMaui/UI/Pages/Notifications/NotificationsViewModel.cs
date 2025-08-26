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
using NeuroAccessMaui.UI.Pages.Notifications.ObjectModel;

namespace NeuroAccessMaui.UI.Pages.Notifications
{
	public partial class NotificationsViewModel : BaseViewModel, IDisposable
	{

		public ObservableTask NotificationsLoader = new();

		[ObservableProperty]
		private ObservableCollection<ObservableNotification> notifications = new();


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
						this.Notifications.Add(new ObservableNotification(Event));
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
			//TODO: FIX this check
			if (!this.Notifications.Any(n => n.Notification == e.Event))
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					try
					{
						this.Notifications.Add(new ObservableNotification(e.Event));
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
					}
				});
			}
		}

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					this.NotificationsLoader.Dispose();
				}

				this.disposedValue = true;
			}
		}


		public void Dispose()
		{
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
