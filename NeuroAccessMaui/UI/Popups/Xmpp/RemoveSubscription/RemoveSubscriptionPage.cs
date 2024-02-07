namespace NeuroAccessMaui.UI.Popups.Xmpp.RemoveSubscription
{
	/// <summary>
	/// Asks the user if it wants to remove an existing presence subscription request as well.
	/// </summary>
	public partial class RemoveSubscriptionPage
	{
		private readonly RemoveSubscriptionViewModel viewModel;

		/// <summary>
		/// Asks the user if it wants to remove an existing presence subscription request as well.
		/// </summary>
		/// <param name="ViewModel">View model</param>
		/// <param name="Background">Optional background</param>
		public RemoveSubscriptionPage(RemoveSubscriptionViewModel ViewModel, ImageSource? Background = null)
			: base(Background)
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
