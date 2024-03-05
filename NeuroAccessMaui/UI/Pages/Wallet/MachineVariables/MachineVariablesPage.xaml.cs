using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineVariables
{
	/// <summary>
	/// A page that allows the user to view information about the current state of a state-machine.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MachineVariablesPage
	{
		/// <summary>
		/// A page that allows the user to view information about the current state of a state-machine.
		/// </summary>
		public MachineVariablesPage()
		{
			this.ContentPageModel = new MachineVariablesViewModel(ServiceRef.UiService.PopLatestArgs<MachineVariablesNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
