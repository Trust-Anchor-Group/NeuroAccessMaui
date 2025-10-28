using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.UI.MVVM;
using Microsoft.Maui.Graphics; // Added for Colors

// Alias to avoid conflict with NeuroAccessMaui.UI.Pages.Wallet.AccountEvent namespace
using AccountEventModel = EDaler.AccountEvent;
using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory
{
	/// <summary>
	/// View model for the Transaction History page. Loads and pages through eDaler account events.
	/// </summary>
	public partial class TransactionHistoryViewModel : XmppViewModel
	{
		private DateTime? lastLoadedOldestTimestamp;
		private string? currency; // Currency for events (taken from balance)

		/// <summary>
		/// Backing store for all events (unfiltered)
		/// </summary>
		private readonly List<TransactionEventItem> allEvents = new();

		/// <summary>
		/// Creates a new instance of the <see cref="TransactionHistoryViewModel"/> class.
		/// </summary>
		public TransactionHistoryViewModel()
		{
			this.Events = new ObservableCollection<TransactionEventItem>();
		}

		/// <summary>
		/// Collection of transaction events (filtered).
		/// </summary>
		public ObservableCollection<TransactionEventItem> Events { get; }

		[ObservableProperty]
		private bool isLoading;

		[ObservableProperty]
		private bool hasMore;

		[ObservableProperty]
		private string? searchText;

		partial void OnSearchTextChanged(string? value)
		{
			this.ApplyFilter();
		}


		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			await this.LoadInitialAsync();
		}

		private async Task EnsureCurrencyAsync()
		{
			if (!string.IsNullOrEmpty(this.currency))
				return;

			try
			{
				Balance Balance = await ServiceRef.XmppService.GetEDalerBalance();
				this.currency = Balance.Currency;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				this.currency = string.Empty;
			}
		}

		private async Task LoadInitialAsync()
		{
			if (this.IsLoading)
				return;

			this.IsLoading = true;
			try
			{
				await this.EnsureCurrencyAsync();
				this.Events.Clear();
				this.allEvents.Clear();
				this.lastLoadedOldestTimestamp = null;

				await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect);
				(AccountEventModel[] Events, bool More) = await ServiceRef.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize);
				await this.AddEventsAsync(Events);
				this.HasMore = More;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.IsLoading = false;
			}
		}

		private async Task AddEventsAsync(AccountEventModel[] eventsBatch)
		{
			if (eventsBatch is null || eventsBatch.Length == 0)
				return;

			Dictionary<string, string> FriendlyNames = new();
			string CurrencyLocal = this.currency ?? string.Empty;

			// Events are expected newest->oldest. Maintain order in UI newest first.
			foreach (AccountEventModel Evt in eventsBatch)
			{
				string? Type = null;
				string Remote = Evt.Remote;
				if (!FriendlyNames.TryGetValue(Remote, out string? Friendly))
				{
					Friendly = await ContactInfo.GetFriendlyName(Remote);

					string[] FriendlyParts = Friendly.Split("@");

					Type = ServiceRef.Localizer[nameof(AppResources.Transfer)].Value;

					Friendly = FriendlyParts[0];
					FriendlyNames[Remote] = Friendly;
				}

				TransactionEventItem Item = new(Evt, Friendly, CurrencyLocal, Type);
				// Insert at end since service provides descending blocks already preserving global order.
				this.Events.Add(Item);
				// Store in backing collection
				this.allEvents.Add(Item);

				if (!this.lastLoadedOldestTimestamp.HasValue || Evt.Timestamp < this.lastLoadedOldestTimestamp.Value)
					this.lastLoadedOldestTimestamp = Evt.Timestamp;
			}
		}

		private void ApplyFilter()
		{
			string? Filter = this.SearchText;
			IEnumerable<TransactionEventItem> Query = this.allEvents;

			if (!string.IsNullOrWhiteSpace(Filter))
			{
				string FilterLower = Filter.Trim().ToLowerInvariant();
				Query = Query.Where(e => (e.FriendlyName ?? string.Empty).ToLowerInvariant().Contains(FilterLower));
			}

			// Update observable collection efficiently
			List<TransactionEventItem> NewItems = Query.ToList();
			// Simple refresh strategy (dataset relatively small per page)
			this.Events.Clear();
			foreach (TransactionEventItem Item in NewItems)
				this.Events.Add(Item);
		}

		[RelayCommand]
		private async Task Refresh()
		{
			await this.LoadInitialAsync();
		}

		[RelayCommand]
		private async Task LoadMore()
		{
			if (this.IsLoading || !this.HasMore || !this.lastLoadedOldestTimestamp.HasValue)
				return;

			this.IsLoading = true;
			try
			{
				(AccountEventModel[] Events, bool More) = await ServiceRef.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize, this.lastLoadedOldestTimestamp.Value);
				await this.AddEventsAsync(Events);
				this.HasMore = More;
				this.ApplyFilter();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.IsLoading = false;
			}
		}

		/// <summary>
		/// Command to go back to previous page.
		/// </summary>
		[RelayCommand]
		private Task Back()
		{
			return this.GoBack();
		}
	}
}
