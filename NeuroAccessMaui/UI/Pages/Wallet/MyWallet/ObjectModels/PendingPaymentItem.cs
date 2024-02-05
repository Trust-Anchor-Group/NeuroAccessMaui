using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels
{
	/// <summary>
	/// Encapsulates a <see cref="PendingPayment"/> object.
	/// </summary>
	/// <param name="PendingPayment">Pending payment.</param>
	/// <param name="FriendlyName">Friendly name.</param>
	public class PendingPaymentItem(EDaler.PendingPayment PendingPayment, string FriendlyName) : IUniqueItem
	{
		private readonly EDaler.PendingPayment pendingPayment = PendingPayment;
		private readonly string friendlyName = FriendlyName;

		/// <summary>
		/// Associated transaction ID
		/// </summary>
		public Guid Id => this.pendingPayment.Id;

		/// <inheritdoc/>
		public string UniqueName => this.Id.ToString();

		/// <summary>
		/// When pending payment expires
		/// </summary>
		public DateTime Expires => this.pendingPayment.Expires;

		/// <summary>
		/// String representation of <see cref="Expires"/>
		/// </summary>
		public string ExpiresStr => ServiceRef.Localizer[nameof(AppResources.ExpiresAt), this.Expires.ToShortDateString()];

		/// <summary>
		/// Currency of pending payment
		/// </summary>
		public string Currency => this.pendingPayment.Currency.Value;

		/// <summary>
		/// Amount pending
		/// </summary>
		public decimal Amount => this.pendingPayment.Amount;

		/// <summary>
		/// Sender of payment
		/// </summary>
		public string From => this.pendingPayment.From;

		/// <summary>
		/// Recipient of payment
		/// </summary>
		public string To => this.pendingPayment.To;

		/// <summary>
		/// Corresponding eDaler URI.
		/// </summary>
		public string Uri => this.pendingPayment.Uri;

		/// <summary>
		/// Friendly name of recipient.
		/// </summary>
		public string FriendlyName => this.friendlyName;

	}
}
