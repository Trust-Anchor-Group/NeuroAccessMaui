using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Kyc
{

	public partial class KycProcessPage : BaseContentPage
	{
		public KycProcessPage()
		{
			this.InitializeComponent();
			this.ContentPageModel = new KycProcessViewModel(ServiceRef.UiService.PopLatestArgs<KycProcessNavigationArgs>());

		/*	this.SectionsCollectionView.Filter = (item) =>
			{
				if (item is not KycSection Section) return false;
				return Section.AllFields.Any(f => f.IsVisible) && Section.IsVisible;
			};*/
		}
	}
}
