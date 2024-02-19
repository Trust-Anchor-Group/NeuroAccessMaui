namespace NeuroAccessMaui.UI.Popups.Xmpp.ReportType
{
	/// <summary>
	/// Prompts the user for a response of a presence subscription request.
	/// </summary>
	public partial class ReportTypePopup
	{
		private readonly ReportTypeViewModel viewModel;

		/// <summary>
		/// Prompts the user for a response of a presence subscription request.
		/// </summary>
		public ReportTypePopup(ReportTypeViewModel ViewModel)
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
