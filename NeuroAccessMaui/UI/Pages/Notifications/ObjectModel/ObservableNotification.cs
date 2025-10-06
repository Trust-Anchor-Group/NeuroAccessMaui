using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.MVVM;

namespace NeuroAccessMaui.UI.Pages.Notifications.ObjectModel
{
	public partial class ObservableNotification : ObservableObject, IDisposable
	{
		private readonly NotificationEvent notification;

		private readonly ObservableTask loadTask;

		public ObservableNotification(NotificationEvent Notification)
		{
			this.notification = Notification;
			this.loadTask = new ObservableTask();
			this.loadTask.Load(async () =>
			{
				string Description = await this.notification.GetDescription();
				Geometry Icon = await this.notification.GetCategoryIcon();

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.Description = Description;
					this.Icon = Icon;
				});
			});
		}

		public NotificationEvent Notification => this.notification;

		public DateTime Received => this.notification.Received;

		public ObservableTask LoadTask => this.loadTask;

		[ObservableProperty]
		Geometry? icon = null;

		[ObservableProperty]
		string? description = null;


		public NotificationEventType? Type => this.notification.Type;

		public string? Category => this.notification.Category;
		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					this.loadTask.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				this.disposedValue = true;
			}
		}


		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

	}
}
