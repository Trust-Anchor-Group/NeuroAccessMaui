using EDaler;
using NeuroAccessMaui.Services.Navigation;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// Holds navigation parameters specific to the eDaler wallet.
	/// </summary>
	public class WalletNavigationArgs : NavigationArgs
	{
		private readonly Balance? balance;
		private readonly decimal pendingAmount;
		private readonly string? pendingCurrency;
		private readonly EDaler.PendingPayment[]? pendingPayments;
		private readonly EDaler.AccountEvent[]? events;

		private readonly bool more;

		/// <summary>
		/// Creates a new instance of the <see cref="WalletNavigationArgs"/> class.
		/// </summary>
		public WalletNavigationArgs()
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="WalletNavigationArgs"/> class.
		/// </summary>
		/// <param name="Balance">Balance information.</param>
		/// <param name="PendingAmount">Total amount of pending payments.</param>
		/// <param name="PendingCurrency">Currency of pending payments.</param>
		/// <param name="PendingPayments">Details about pending payments.</param>
		/// <param name="Events">Wallet events.</param>
		/// <param name="More">If more events are available.</param>
		public WalletNavigationArgs(Balance Balance, decimal PendingAmount, string PendingCurrency, EDaler.PendingPayment[] PendingPayments,
			EDaler.AccountEvent[] Events, bool More)
		{
			this.balance = Balance;
			this.pendingAmount = PendingAmount;
			this.pendingCurrency = PendingCurrency;
			this.pendingPayments = PendingPayments;
			this.events = Events;
			this.more = More;
		}

		/// <summary>
		/// Balance information.
		/// </summary>
		public Balance? Balance => this.balance;

		/// <summary>
		/// Total amount of pending payments.
		/// </summary>
		public decimal PendingAmount => this.pendingAmount;

		/// <summary>
		/// Currency of pending payments.
		/// </summary>
		public string? PendingCurrency => this.pendingCurrency;

		/// <summary>
		/// Details about pending payments.
		/// </summary>
		public EDaler.PendingPayment[]? PendingPayments => this.pendingPayments;

		/// <summary>
		/// Wallet events.
		/// </summary>
		public EDaler.AccountEvent[]? Events => this.events;

		/// <summary>
		/// If more events are available.
		/// </summary>
		public bool More => this.more;
	}
}
