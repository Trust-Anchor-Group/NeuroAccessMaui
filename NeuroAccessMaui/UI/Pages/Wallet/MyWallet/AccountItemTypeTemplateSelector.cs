using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using Xamarin.Forms;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet
{
	/// <summary>
	/// Data Template Selector, based on Account Item Type.
	/// </summary>
	public class AccountItemTypeTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template to use for pending payments
		/// </summary>
		public DataTemplate PendingPaymentTemplate { get; set; }

		/// <summary>
		/// Template to use for account events.
		/// </summary>
		public DataTemplate AccountEventTemplate { get; set; }

		/// <summary>
		/// Template to use for other items.
		/// </summary>
		public DataTemplate DefaultTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			if (item is PendingPaymentItem)
				return this.PendingPaymentTemplate ?? this.DefaultTemplate;
			else if (item is AccountEventItem)
				return this.AccountEventTemplate ?? this.DefaultTemplate;
			else
				return this.DefaultTemplate;
		}
	}
}
