namespace NeuroAccessMaui.UI.Popups.Xmpp.SubscribeTo
{
	/// <summary>
	/// Asks the user if it wants to remove an existing presence subscription request as well.
	/// </summary>
	public partial class SubscribeToPage
	{
		private readonly SubscribeToViewModel viewModel;

		/// <summary>
		/// Asks the user if it wants to remove an existing presence subscription request as well.
		/// </summary>
		/// <param name="ViewModel">View model</param>
		/// <param name="Background">Optional background</param>
		public SubscribeToPage(SubscribeToViewModel ViewModel, ImageSource? Background = null)
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
