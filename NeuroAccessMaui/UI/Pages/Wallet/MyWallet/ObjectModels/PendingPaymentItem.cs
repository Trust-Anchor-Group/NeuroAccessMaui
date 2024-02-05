using System;
using Xamarin.CommunityToolkit.Helpers;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels
{
	/// <summary>
	/// Encapsulates a <see cref="PendingPayment"/> object.
	/// </summary>
	public class PendingPaymentItem : IUniqueItem
	{
		private readonly EDaler.PendingPayment pendingPayment;
		private readonly string friendlyName;

		/// <summary>
		/// Encapsulates a <see cref="PendingPayment"/> object.
		/// </summary>
		/// <param name="PendingPayment">Pending payment.</param>
		/// <param name="FriendlyName">Friendly name.</param>
		public PendingPaymentItem(EDaler.PendingPayment PendingPayment, string FriendlyName)
		{
			this.pendingPayment = PendingPayment;
			this.friendlyName = FriendlyName;
		}

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
		public string ExpiresStr => string.Format(LocalizationResourceManager.Current["ExpiresAt"], this.Expires.ToShortDateString());

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
