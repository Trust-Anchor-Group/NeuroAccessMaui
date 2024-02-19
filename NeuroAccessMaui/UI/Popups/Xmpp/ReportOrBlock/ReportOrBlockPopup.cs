namespace NeuroAccessMaui.UI.Popups.Xmpp.ReportOrBlock
{
	/// <summary>
	/// Prompts the user for a response of a presence subscription request.
	/// </summary>
	public partial class ReportOrBlockPopup
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
		protected override void OnDisappearing()
		{
			this.viewModel.Close();
			base.OnDisappearing();
		}
	}
}
