using EDaler;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Empty service provider. Used to show the caller the user wants to request a service from a user.
	/// </summary>
	internal class EmptyBuyEDalerServiceProvider : BuyEDalerServiceProvider
	{
		public EmptyBuyEDalerServiceProvider()
			: base(string.Empty, string.Empty, ServiceRef.Localizer[nameof(AppResources.FromUser)],
				  Constants.Images.Qr_Person, 230, 230, string.Empty)
		{
		}
	}
}
