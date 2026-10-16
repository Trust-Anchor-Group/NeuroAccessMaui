using NeuroAccessMaui.UI.Controls;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Read-only page that shows the current KYC application status.
	/// </summary>
	/// <remarks>
	/// Code-behind only pauses the status badge animation while the page is hidden.
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
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			await this.Dispatcher.DispatchAsync(() => this.SetStatusAnimationActive(true));
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			await this.Dispatcher.DispatchAsync(() => this.SetStatusAnimationActive(false));
			await base.OnDisappearingAsync();
		}

		/// <summary>
		/// Lets the status badge loop its animation, or pauses it.
		/// </summary>
		/// <param name="IsActive">Whether the looping animation may run.</param>
		private void SetStatusAnimationActive(bool IsActive)
		{
			this.StatusBadge.IsAnimationActive = IsActive;
		}
	}
}
