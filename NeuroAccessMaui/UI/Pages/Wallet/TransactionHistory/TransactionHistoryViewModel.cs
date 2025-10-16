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

namespace NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory
{
	/// <summary>
	/// View model for the Transaction History page. Loads and pages through eDaler account events.
	/// </summary>
	public partial class TransactionHistoryViewModel : XmppViewModel
	{
		private bool isLoading;
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
		/// Collection of transaction events.
		/// Collection of transaction events (filtered).
		/// </summary>
		public ObservableCollection<TransactionEventItem> Events { get; }

		[ObservableProperty]
		private decimal totalIn;

		[ObservableProperty]
		private decimal totalOut;

		[ObservableProperty]
		private bool hasMore;

		[ObservableProperty]
		private string? searchText;

		partial void OnSearchTextChanged(string? value)
		{
			this.ApplyFilter();
		}

		/// <summary>
		/// Total incoming amount formatted.
		/// </summary>
		public string TotalInString => this.TotalIn.ToString("F2") + " NC";

		/// <summary>
		/// Total outgoing amount formatted.
		/// </summary>
		public string TotalOutString => this.TotalOut.ToString("F2") + " NC";

		/// <summary>
		/// Net change formatted.
		/// </summary>
		public string NetChangeString => (this.TotalIn - this.TotalOut).ToString("F2") + " NC";

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
			if (this.isLoading)
				return;

			this.isLoading = true;
			try
			{
				await this.EnsureCurrencyAsync();
				this.Events.Clear();
				this.allEvents.Clear();
				this.TotalIn = 0M;
				this.TotalOut = 0M;
				this.lastLoadedOldestTimestamp = null;

				(AccountEventModel[] Events, bool More) = await ServiceRef.XmppService.GetEDalerAccountEvents(Constants.BatchSizes.AccountEventBatchSize);
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
				this.isLoading = false;
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
				string Remote = Evt.Remote;
				if (!FriendlyNames.TryGetValue(Remote, out string? Friendly))
				{
					Friendly = await ContactInfo.GetFriendlyName(Remote);
					FriendlyNames[Remote] = Friendly;
				}

				TransactionEventItem item = new(Evt, Friendly, CurrencyLocal);
				// Insert at end since service provides descending blocks already preserving global order.
				this.Events.Add(item);
				// Store in backing collection
				this.allEvents.Add(item);

				if (!this.lastLoadedOldestTimestamp.HasValue || Evt.Timestamp < this.lastLoadedOldestTimestamp.Value)
					this.lastLoadedOldestTimestamp = Evt.Timestamp;

				if (Evt.Change >= 0)
					this.TotalIn += Evt.Change;
				else
					this.TotalOut += -Evt.Change; // store absolute outgoing
			}

			this.OnPropertyChanged(nameof(this.TotalInString));
			this.OnPropertyChanged(nameof(this.TotalOutString));
			this.OnPropertyChanged(nameof(this.NetChangeString));
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
			if (this.isLoading || !this.HasMore || !this.lastLoadedOldestTimestamp.HasValue)
				return;

			this.isLoading = true;
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
				this.isLoading = false;
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

	/// <summary>
	/// Presentation model for a transaction event in history (no notification support).
	/// </summary>
	public class TransactionEventItem
	{
		private readonly AccountEventModel accountEvent;
		private readonly string friendlyName;
		private readonly string currency;

		/// <summary>
		/// Creates a transaction event presentation model.
		/// </summary>
		/// <param name="accountEvent">Underlying account event.</param>
		/// <param name="friendlyName">Friendly name for remote party.</param>
		/// <param name="currency">Currency code.</param>
		public TransactionEventItem(AccountEventModel accountEvent, string friendlyName, string currency)
		{
			this.accountEvent = accountEvent;
			this.friendlyName = friendlyName;
			this.currency = currency;
		}

		public decimal Balance => this.accountEvent.Balance;
		public decimal Reserved => this.accountEvent.Reserved;
		public decimal Change => this.accountEvent.Change;
		public DateTime Timestamp => this.accountEvent.Timestamp;
		public Guid TransactionId => this.accountEvent.TransactionId;
		public string Remote => this.accountEvent.Remote;
		public string FriendlyName => this.friendlyName;
		public string Message => this.accountEvent.Message;
		public bool HasMessage => !string.IsNullOrEmpty(this.accountEvent.Message);
		public string Currency => this.currency;

		/// <summary>
		/// Gets a color representing the type of transaction. Incoming (Change &gt;= 0) is green, outgoing is black.
		/// </summary>
		public Color ChangeColor => this.Change >= 0 ? Colors.Green : Colors.Black;

		public string TimestampStr
		{
			get
			{
				DateTime Today = DateTime.Today;
				if (this.Timestamp.Date == Today)
					return this.Timestamp.ToLongTimeString();
				else if (this.Timestamp.Date == Today.AddDays(-1))
					return ServiceRef.Localizer[nameof(Resources.Languages.AppResources.Yesterday)] + ", " + this.Timestamp.ToLongTimeString();
				else
					return this.Timestamp.ToShortDateString() + ", " + this.Timestamp.ToLongTimeString();
			}
		}

		public string ReservedSuffix => this.accountEvent.Reserved == 0 ? string.Empty : "+" + NeuroAccessMaui.UI.Converters.MoneyToString.ToString(this.accountEvent.Reserved);
	}
}
