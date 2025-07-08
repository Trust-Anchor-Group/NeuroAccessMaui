using Microsoft.Maui.Controls;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI;

namespace NeuroAccessMaui.UI.Pages.Applications.KycProcess
{

	public partial class KycProcessPage : BaseContentPage
	{
		public KycProcessPage(KycProcessViewModel ViewModel)
		{
			InitializeComponent();
			this.BindingContext = ViewModel;
		}
	}
}
