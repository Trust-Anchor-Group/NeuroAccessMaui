using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups.Xmpp.ReportOrBlock
{
	/// <summary>
	/// Prompts the user for a response of a presence subscription request.
	/// </summary>
	public partial class ReportOrBlockPopup : SimplePopup
	{
		private readonly ReportOrBlockViewModel viewModel;

		/// <summary>
		/// Prompts the user for a response of a presence subscription request.
		/// </summary>
		public ReportOrBlockPopup(ReportOrBlockViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = this.viewModel = ViewModel;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.viewModel.Close();
			return base.OnDisappearingAsync();
		}
	}
}
