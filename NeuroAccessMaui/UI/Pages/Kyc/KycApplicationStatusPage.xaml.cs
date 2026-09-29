namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Read-only page that shows the current KYC application status.
	/// </summary>
	/// <remarks>
	/// Code-behind only pauses the status animation while hidden and plays the celebration when the view model reports
	/// an approval.
	/// </remarks>
	public partial class KycApplicationStatusPage : BaseContentPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycApplicationStatusPage"/> class.
		/// </summary>
		/// <param name="ViewModel">Page view model.</param>
		public KycApplicationStatusPage(KycApplicationStatusViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
			ViewModel.ApplicationApproved += this.OnApplicationApproved;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			await this.Dispatcher.DispatchAsync(() => this.Visual.IsAnimationActive = true);
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			await this.Dispatcher.DispatchAsync(() => this.Visual.IsAnimationActive = false);
			await base.OnDisappearingAsync();
		}

		private void OnApplicationApproved(object? Sender, EventArgs E)
		{
			this.Dispatcher.Dispatch(this.Visual.Celebrate);
		}
	}
}
