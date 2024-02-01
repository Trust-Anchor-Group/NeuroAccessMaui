using EDaler;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Empty service provider. Used to show the caller the user wants to request a service from a user.
	/// </summary>
	internal class EmptySellEDalerServiceProvider : SellEDalerServiceProvider
	{
		public EmptySellEDalerServiceProvider()
			: base(string.Empty, string.Empty, ServiceRef.Localizer[nameof(AppResources.ToUser2)],
				  Constants.Images.Qr_Person, 230, 230, string.Empty)
		{
		}
	}
}
