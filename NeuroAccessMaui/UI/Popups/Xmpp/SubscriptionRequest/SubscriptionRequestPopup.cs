namespace NeuroAccessMaui.UI.Popups.Xmpp.SubscriptionRequest
{
	/// <summary>
	/// Prompts the user for a response of a presence subscription request.
	/// </summary>
	public partial class SubscriptionRequestPopup
	{
		private readonly SubscriptionRequestViewModel viewModel;

		/// <summary>
		/// Prompts the user for a response of a presence subscription request.
		/// </summary>
		/// <param name="ViewModel">View model</param>
		public SubscriptionRequestPopup(SubscriptionRequestViewModel ViewModel)
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
