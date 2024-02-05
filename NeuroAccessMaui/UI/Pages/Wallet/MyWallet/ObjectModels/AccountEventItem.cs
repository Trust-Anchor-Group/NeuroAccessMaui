using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.Converters;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels
{
	/// <summary>
	/// Encapsulates a <see cref="AccountEvent"/> object.
	/// </summary>
	/// <param name="AccountEvent">Account event.</param>
	/// <param name="ViewModel">Current view model</param>
	/// <param name="FriendlyName">Friendly name of remote entity.</param>
	/// <param name="NotificationEvents">Notification events.</param>
	public class AccountEventItem(EDaler.AccountEvent AccountEvent, MyWalletViewModel ViewModel, string FriendlyName, NotificationEvent[] NotificationEvents) : IUniqueItem
	{
		private readonly EDaler.AccountEvent accountEvent = AccountEvent;
		private readonly MyWalletViewModel viewModel = ViewModel;
		private readonly string friendlyName = FriendlyName;
		private bool? @new;
		private NotificationEvent[] notificationEvents = NotificationEvents;

		/// <summary>
		/// Balance after event.
		/// </summary>
		public decimal Balance => this.accountEvent.Balance;

		/// <summary>
		/// Reserved after event.
		/// </summary>
		public decimal Reserved => this.accountEvent.Reserved;

		/// <summary>
		/// Balance change
		/// </summary>
		public decimal Change => this.accountEvent.Change;

		/// <summary>
		/// Timestamp of event
		/// </summary>
		public DateTime Timestamp => this.accountEvent.Timestamp;

		/// <summary>
		/// Transaction ID corresponding to event.
		/// </summary>
		public Guid TransactionId => this.accountEvent.TransactionId;

		/// <inheritdoc/>
		public string UniqueName => this.TransactionId.ToString();

		/// <summary>
		/// Remote endpoint in transaction.
		/// </summary>
		public string Remote => this.accountEvent.Remote;

		/// <summary>
		/// Friendly name of remote entity.
		/// </summary>
		public string FriendlyName => this.friendlyName;

		/// <summary>
		/// Any message associated with event.
		/// </summary>
		public string Message => this.accountEvent.Message;

		/// <summary>
		/// If the event has a message.
		/// </summary>
		public bool HasMessage => !string.IsNullOrEmpty(this.accountEvent.Message);

		/// <summary>
		/// Currency used for event.
		/// </summary>
		public string Currency => this.viewModel.Currency;

		/// <summary>
		/// Associated notification events
		/// </summary>
		public NotificationEvent[] NotificationEvents => this.notificationEvents;

		/// <summary>
		/// If the event item is new or not.
		/// </summary>
		public bool New
		{
			get
			{
				if (!this.@new.HasValue)
				{
					this.@new = this.notificationEvents.Length > 0;
					if (this.@new.Value)
					{
						NotificationEvent[] ToDelete = this.notificationEvents;

						this.notificationEvents = [];

						Task.Run(() => ServiceRef.NotificationService.DeleteEvents(ToDelete));
					}
				}

				return this.@new.Value;
			}
		}

		/// <summary>
		/// Color to use when displaying change.
		/// </summary>
		public Color? TextColor
		{
			get
			{
				if (this.Change >= 0)
				{
					if (Application.Current?.RequestedTheme == AppTheme.Dark)
						return (Color?)Application.Current?.Resources["PrimaryForegroundDark"];
					else
						return (Color?)Application.Current?.Resources["PrimaryForegroundLight"];
				}
				else
				{
					if (Application.Current?.RequestedTheme == AppTheme.Dark)
						return (Color?)Application.Current?.Resources["AlertColorDarkTheme"];
					else
						return (Color?)Application.Current?.Resources["AlertColor"];
				}
			}
		}

		/// <summary>
		/// String representation of timestamp
		/// </summary>
		public string TimestampStr
		{
			get
			{
				DateTime Today = DateTime.Today;

				if (this.Timestamp.Date == Today)
					return this.Timestamp.ToLongTimeString();
				else if (this.Timestamp.Date == Today.AddDays(-1))
					return ServiceRef.Localizer[nameof(AppResources.Yesterday)] + ", " + this.Timestamp.ToLongTimeString();
				else
					return this.Timestamp.ToShortDateString() + ", " + this.Timestamp.ToLongTimeString();
			}
		}

		/// <summary>
		/// Formatted string of any amount being reserved.
		/// </summary>
		public string ReservedSuffix
		{
			get
			{
				if (this.accountEvent.Reserved == 0)
					return string.Empty;

				return "+" + MoneyToString.ToString(this.accountEvent.Reserved);
			}
		}
	}
}
