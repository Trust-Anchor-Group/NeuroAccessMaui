using System;
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
using NeuroAccessMaui.UI.MVVM.Building;

namespace NeuroAccessMaui.UI.Pages.Notifications
{
	/// <summary>
	/// View model for notifications page.
	/// </summary>
	public partial class NotificationsViewModel : BaseViewModel
	{
		private readonly INotificationServiceV2 notificationService;
		private readonly ObservableTask<int> notificationsLoader;
		private readonly List<NotificationRecord> loadedRecords = [];
		private readonly int batchSize = 15;
		private int loadedCount;
		private bool suppressNotificationReload;

		/// <summary>
		/// Initializes a new instance of the <see cref="NotificationsViewModel"/> class.
		/// </summary>
		/// <param name="NotificationService">Notification service.</param>
		public NotificationsViewModel(INotificationServiceV2 NotificationService)
		{
			this.notificationService = NotificationService;
			this.Items = new ObservableCollection<NotificationListItem>();
			this.HasMore = 0;
			this.notificationsLoader = new ObservableTaskBuilder<int>()
				.Named("Notifications Loader")
				.AutoStart(false)
				.UseTaskRun(false)
				.Run(async ctx => await this.LoadBatchAsync(ctx.IsRefreshing, ctx.CancellationToken))
				.Build();
		}

		/// <summary>
		/// Notifications to display.
		/// </summary>
		public ObservableCollection<NotificationListItem> Items { get; }

		/// <summary>
		/// Loader task for batched notifications retrieval.
		/// </summary>
		public ObservableTask<int> NotificationsLoader => this.notificationsLoader;

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
		/// Remaining items threshold toggle for incremental loading.
		/// </summary>
		[ObservableProperty]
		private int hasMore;

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
		/// Command to load the next batch of notifications.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private Task LoadMoreNotifications()
		{
			if (this.HasMore == -1)
				return Task.CompletedTask;

			this.notificationsLoader.Refresh();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Command to clear all notifications in current view.
		/// </summary>
		[RelayCommand]
		private async Task ClearAllAsync()
		{
			this.suppressNotificationReload = true;
			try
			{
				List<string> Ids = this.Items.Select(Item => Item.Id).ToList();
				await this.notificationService.DeleteAsync(Ids, CancellationToken.None);
				this.RemoveRecords(Ids);
				this.loadedCount = this.loadedRecords.Count;
				this.HasMore = 0;
				this.ApplyFilters();
				this.notificationsLoader.Run();
				await this.notificationsLoader.WaitAllAsync();
			}
			finally
			{
				this.suppressNotificationReload = false;
			}
		}

		/// <summary>
		/// Command to open and consume a notification.
		/// </summary>
		/// <param name="Item">Notification item.</param>
		[RelayCommand]
		private async Task OpenNotificationAsync(NotificationListItem Item)
		{
			try
			{
				this.suppressNotificationReload = true;
				await this.notificationService.ConsumeAsync(Item.Id, CancellationToken.None);
				this.UpdateRecordState(Item.Id, NotificationState.Consumed, true);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				this.suppressNotificationReload = false;
			}
			this.ApplyFilters();
		}

		/// <summary>
		/// Command to mark a notification as read without consuming it.
		/// </summary>
		/// <param name="Item">Notification item.</param>
		[RelayCommand]
		private async Task MarkReadAsync(NotificationListItem Item)
		{
			try
			{
				this.suppressNotificationReload = true;
				await this.notificationService.MarkReadAsync(Item.Id, CancellationToken.None);
				this.UpdateRecordState(Item.Id, NotificationState.Read, false);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				this.suppressNotificationReload = false;
			}
			this.ApplyFilters();
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			this.notificationService.OnNotificationAdded += this.OnNotificationAddedAsync;
			this.notificationsLoader.Run();
			await this.notificationsLoader.WaitAllAsync();
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

		private async Task LoadBatchAsync(bool isRefresh, CancellationToken cancellationToken)
		{
			if (!isRefresh)
			{
				this.loadedRecords.Clear();
				this.loadedCount = 0;
			}

			NotificationQuery Query = new NotificationQuery
			{
				States = this.ShowUnreadOnly ? new[] { NotificationState.New, NotificationState.Delivered } : null,
				Limit = this.batchSize,
				Skip = isRefresh ? this.loadedCount : 0
			};

			IReadOnlyList<NotificationRecord> Records = await this.notificationService.GetAsync(Query, cancellationToken);

			if (!isRefresh)
				this.loadedRecords.Clear();

			this.UpsertRecords(Records);

			int FetchedCount = Records.Count;
			this.HasMore = FetchedCount < this.batchSize ? -1 : 0;
			this.ApplyFilters();
		}

		private void UpsertRecords(IEnumerable<NotificationRecord> records)
		{
			foreach (NotificationRecord Record in records)
			{
				int Index = this.loadedRecords.FindIndex(r => string.Equals(r.Id, Record.Id, StringComparison.Ordinal));
				if (Index >= 0)
				{
					this.loadedRecords[Index] = Record;
				}
				else
				{
					this.loadedRecords.Add(Record);
				}
			}

			this.loadedCount = this.loadedRecords.Count;
		}

		private void RemoveRecords(IEnumerable<string> ids)
		{
			HashSet<string> IdSet = new HashSet<string>(ids);
			this.loadedRecords.RemoveAll(Record => IdSet.Contains(Record.Id));
			this.loadedCount = this.loadedRecords.Count;
		}

		private void UpdateRecordState(string id, NotificationState state, bool markConsumed)
		{
			NotificationRecord? Record = this.loadedRecords.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.Ordinal));
			if (Record is null)
				return;

			Record.State = state;

			if (state == NotificationState.Read)
			{
				Record.ReadAt = DateTime.UtcNow;
			}

			if (markConsumed || state == NotificationState.Consumed)
			{
				Record.ConsumedAt = DateTime.UtcNow;
				Record.OccurrenceCount = 1;
			}

			this.loadedCount = this.loadedRecords.Count;
		}

		private void ApplyFilters()
		{
			IEnumerable<NotificationRecord> Query = this.loadedRecords;

			if (this.ShowUnreadOnly)
			{
				Query = Query.Where(Record => Record.State == NotificationState.New || Record.State == NotificationState.Delivered);
			}

			if (!string.IsNullOrWhiteSpace(this.SearchText))
			{
				string Term = this.SearchText.Trim();
				Query = Query.Where(Record =>
					(Record.Title?.Contains(Term, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(Record.Body?.Contains(Term, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(Record.Channel?.Contains(Term, StringComparison.OrdinalIgnoreCase) ?? false));
			}

			List<NotificationListItem> Filtered = Query
				.OrderByDescending(Record => Record.TimestampCreated)
				.Select(this.ToListItem)
				.ToList();

			this.Items.Clear();
			foreach (NotificationListItem Item in Filtered)
			{
				this.Items.Add(Item);
			}
		}

		private async Task OnNotificationAddedAsync(object? Sender, NotificationRecordEventArgs Args)
		{
			if (this.suppressNotificationReload)
				return;

			await MainThread.InvokeOnMainThreadAsync(() => this.notificationsLoader.Run());
		}

		/// <summary>
		/// Command to delete a single notification.
		/// </summary>
		/// <param name="Item">Notification item.</param>
		[RelayCommand]
		private async Task DeleteNotificationAsync(NotificationListItem Item)
		{
			try
			{
				this.suppressNotificationReload = true;
				await this.notificationService.DeleteAsync(new[] { Item.Id }, CancellationToken.None);
				this.RemoveRecords(new[] { Item.Id });
				this.loadedCount = this.loadedRecords.Count;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
			finally
			{
				this.suppressNotificationReload = false;
			}

			this.ApplyFilters();

			if (this.HasMore == 0)
				this.notificationsLoader.Refresh();
		}

		private NotificationListItem ToListItem(NotificationRecord record)
		{
			string ChannelShort = string.IsNullOrEmpty(record.Channel) ? "N" : record.Channel.Substring(0, 1).ToUpperInvariant();
			string DateText = record.TimestampCreated.ToLocalTime().ToString("MMM d", CultureInfo.CurrentCulture);
			string StateLabel = record.State switch
			{
				NotificationState.New or NotificationState.Delivered => "New",
				NotificationState.Read => "Read",
				NotificationState.Consumed => "Opened",
				_ => string.Empty
			};

			return new NotificationListItem(record.Id, record.Title, record.Body, record.Channel, ChannelShort, DateText, StateLabel, record.OccurrenceCount);
		}

	}
}
