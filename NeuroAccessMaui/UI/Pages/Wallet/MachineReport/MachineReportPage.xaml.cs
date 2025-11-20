using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport
{
	/// <summary>
	/// A page that allows the user to view a state-machine report.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MachineReportPage
	{
		/// <summary>
		/// A page that allows the user to view a state-machine report.
		/// </summary>
		public MachineReportPage()
		{
			this.ContentPageModel = new MachineReportViewModel(ServiceRef.NavigationService.PopLatestArgs<MachineReportNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
