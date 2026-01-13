using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Kyc
{

	public partial class KycProcessPage : BaseContentPage
	{
		public KycProcessPage(KycProcessViewModel Vm)
		{
			this.InitializeComponent();

			this.ContentPageModel = Vm;

			Vm.ScrollToTop += async (_, _) =>
			{
				await this.Dispatcher.DispatchAsync(() =>
				{
#warning Can still crash if the ItemSource is empty, but as of the moment it never is
					if (this.FormCollectionView is not null && this.FormCollectionView.ItemsSource != null)
					{
						this.FormCollectionView.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
					}
				});
			};
		}
	}
}
