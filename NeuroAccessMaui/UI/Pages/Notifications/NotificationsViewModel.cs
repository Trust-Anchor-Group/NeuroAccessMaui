using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.MVVM;

namespace NeuroAccessMaui.UI.Pages.Notifications
{
	/// <summary>
	/// View model for notifications page.
	/// </summary>
	public partial class NotificationsViewModel : BaseViewModel
	{
		private readonly INotificationServiceV2 notificationService;
		private IReadOnlyList<NotificationRecord> loadedRecords = [];

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationsViewModel"/> class.
		/// </summary>
		/// <param name="NotificationService">Notification service.</param>
		public NotificationsViewModel(INotificationServiceV2 NotificationService)
		{
			this.notificationService = NotificationService;
			this.Items = new ObservableCollection<NotificationListItem>();
		}

		/// <summary>
		/// Notifications to display.
		/// </summary>
		public ObservableCollection<NotificationListItem> Items { get; }

		/// <summary>
		/// Search text.
		/// </summary>
		[ObservableProperty]
		private string searchText = string.Empty;

		/// <summary>
		/// Show unread filter.
		/// </summary>
		[ObservableProperty]
		private bool showUnreadOnly;

		/// <summary>
		/// Command to set unread filter.
		/// </summary>
		[RelayCommand]
		private void SetUnread()
		{
			this.ShowUnreadOnly = true;
			this.ApplyFilters();
		}

		/// <summary>
		/// Command to set all filter.
		/// </summary>
		[RelayCommand]
		private void SetAll()
		{
			this.ShowUnreadOnly = false;
			this.ApplyFilters();
		}

		/// <summary>
		/// Command to toggle unread filter.
		/// </summary>
		[RelayCommand]
		private void ToggleUnreadFilter()
		{
			this.ShowUnreadOnly = !this.ShowUnreadOnly;
			this.ApplyFilters();
		}

		/// <summary>
		/// Command to clear all notifications in current view.
		/// </summary>
		[RelayCommand]
		private async Task ClearAllAsync()
		{
			IEnumerable<string> ids = this.Items.Select(x => x.Id).ToArray();
			await this.notificationService.DeleteAsync(ids, CancellationToken.None);
			await this.LoadAsync();
		}

		/// <summary>
		/// Command to open and consume a notification.
		/// </summary>
		/// <param name="Item">Notification item.</param>
		[RelayCommand]
		private async Task OpenNotificationAsync(NotificationListItem Item)
		{
			await this.notificationService.ConsumeAsync(Item.Id, CancellationToken.None);
			await this.LoadAsync();
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			this.notificationService.OnNotificationAdded += this.OnNotificationAddedAsync;
			await this.LoadAsync();
			await base.OnAppearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			this.notificationService.OnNotificationAdded -= this.OnNotificationAddedAsync;
			await base.OnDisappearingAsync();
		}

		partial void OnSearchTextChanged(string value)
		{
			this.ApplyFilters();
		}

		private async Task LoadAsync()
		{
			NotificationQuery query = new NotificationQuery
			{
				States = this.ShowUnreadOnly ? new[] { NotificationState.New, NotificationState.Delivered } : null
			};

			this.loadedRecords = await this.notificationService.GetAsync(query, CancellationToken.None);
			this.ApplyFilters();
		}

		private void ApplyFilters()
		{
			IEnumerable<NotificationRecord> query = this.loadedRecords;

			if (this.ShowUnreadOnly)
			{
				query = query.Where(r => r.State == NotificationState.New || r.State == NotificationState.Delivered);
			}

			if (!string.IsNullOrWhiteSpace(this.SearchText))
			{
				string term = this.SearchText.Trim();
				query = query.Where(r =>
					(r.Title?.Contains(term, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
					(r.Body?.Contains(term, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
					(r.Channel?.Contains(term, System.StringComparison.OrdinalIgnoreCase) ?? false));
			}

			List<NotificationListItem> filtered = query
				.OrderByDescending(r => r.TimestampCreated)
				.Select(this.ToListItem)
				.ToList();

			this.Items.Clear();
			foreach (NotificationListItem item in filtered)
			{
				this.Items.Add(item);
			}
		}

		private async Task OnNotificationAddedAsync(object? Sender, NotificationRecordEventArgs Args)
		{
			await this.LoadAsync();
		}

		private NotificationListItem ToListItem(NotificationRecord record)
		{
			string channelShort = string.IsNullOrEmpty(record.Channel) ? "N" : record.Channel.Substring(0, 1).ToUpperInvariant();
			string dateText = record.TimestampCreated.ToLocalTime().ToString("MMM d", CultureInfo.CurrentCulture);
			string stateLabel = record.State is NotificationState.New or NotificationState.Delivered ? "New" : string.Empty;

			return new NotificationListItem(record.Id, record.Title, record.Body, record.Channel, channelShort, dateText, stateLabel);
		}
	}
}
