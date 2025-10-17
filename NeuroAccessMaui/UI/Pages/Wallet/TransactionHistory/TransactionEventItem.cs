using NeuroAccessMaui.Services;
using EDaler;

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
	public class TransactionEventItem(AccountEventModel accountEvent, string friendlyName, string currency)
	{
		private readonly AccountEventModel accountEvent = accountEvent;
		private readonly string friendlyName = friendlyName;
		private readonly string currency = currency;

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
		public Color ChangeColor => this.Change >= 0 ? AppColors.ContentAccess : AppColors.ContentPrimary;

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
