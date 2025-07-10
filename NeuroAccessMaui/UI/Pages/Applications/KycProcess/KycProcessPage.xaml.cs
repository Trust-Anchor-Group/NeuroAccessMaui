using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI;
using NeuroAccessMaui.Services.Kyc.Models;

namespace NeuroAccessMaui.UI.Pages.Applications.KycProcess
{

	public partial class KycProcessPage : BaseContentPage
	{
		public KycProcessPage(KycProcessViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = ViewModel;

		/*	this.SectionsCollectionView.Filter = (item) =>
			{
				if (item is not KycSection Section) return false;
				return Section.AllFields.Any(f => f.IsVisible) && Section.IsVisible;
			};*/
		}
	}
}
