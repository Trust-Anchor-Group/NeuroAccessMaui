using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups.Transaction;
using Waher.Networking.XMPP;
using AccountEventModel = EDaler.AccountEvent;

namespace NeuroAccessMaui.UI.Pages.Wallet.TransactionHistory
{
	/// <summary>
	/// Presentation model for a transaction event in history (no notification support).
	/// </summary>
	/// <remarks>
	/// Creates a transaction event presentation model.
	/// </remarks>
	/// <param name="accountEvent">Underlying account event.</param>
	/// <param name="friendlyName">Friendly name for remote party.</param>
	/// <param name="currency">Currency code.</param>
	public partial class TransactionEventItem(AccountEventModel accountEvent, string friendlyName, string currency, string? transactionType)
	{
		private readonly AccountEventModel accountEvent = accountEvent;
		private readonly string friendlyName = friendlyName;
		private readonly string currency = currency;
		private readonly string? transactionType = transactionType;

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
		public string? TransactionType => this.transactionType ?? ServiceRef.Localizer[nameof(AppResources.CreditTransfer)].Value;

		/// <summary>
		/// Gets a color representing the type of transaction. Incoming (Change &gt;= 0) is green, outgoing is black.
		/// </summary>
		public Color ChangeColor => this.Change >= 0 ? AppColors.TnPSuccessContent : AppColors.ContentPrimary;

		public string TimestampStr
		{
			get
			{
				DateTime Dt = this.Timestamp.Date.ToLocalTime();
				DateTime Now = DateTime.Now.ToLocalTime();
				TimeSpan Span = Now - Dt;

				string TimeString;

				if (Span.TotalMinutes < 1)
					TimeString = ServiceRef.Localizer[nameof(AppResources.Now)].Value;
				else if (Span.TotalHours < 1)
					TimeString = ServiceRef.Localizer[nameof(AppResources.MinutesAgoFormat), false, (int)Span.TotalMinutes];
				else if (Span.TotalDays < 1)
					TimeString = ServiceRef.Localizer[nameof(AppResources.HoursAgoFormat), false, (int)Span.TotalHours];
				else
					TimeString = Dt.ToString("m", CultureInfo.CurrentCulture);
				return TimeString;
				}
				}

		public string ReservedSuffix => this.accountEvent.Reserved == 0 ? string.Empty : "+" + NeuroAccessMaui.UI.Converters.MoneyToString.ToString(this.accountEvent.Reserved);
		/// <summary>If any amount is reserved.</summary>
		public bool IsReserved => this.accountEvent.Reserved != 0;
		/// <summary>True if remote party appears in roster with subscription state To or Both.</summary>
		/// 
		public bool IsContact
		{
			get
			{
				if (string.IsNullOrEmpty(this.Remote))
					return false;
				try
				{
					RosterItem? Item = ServiceRef.XmppService.GetRosterItem(this.Remote);
					return Item is not null;
				}
				catch
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Opens transaction details popup.
		/// </summary>
		[RelayCommand]
		public async Task OpenDetailsAsync()
		{
			TransactionPopup Popup = new(this);
			await ServiceRef.UiService.PushAsync(Popup);
		}
	}
}
