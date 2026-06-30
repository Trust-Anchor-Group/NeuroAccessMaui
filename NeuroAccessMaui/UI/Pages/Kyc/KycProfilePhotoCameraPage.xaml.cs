using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// App-owned camera page for capturing KYC profile photos without cropping.
	/// </summary>
	public partial class KycProfilePhotoCameraPage
	{
		private bool cameraReleased;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycProfilePhotoCameraPage"/> class.
		/// </summary>
		public KycProfilePhotoCameraPage()
		{
			this.InitializeComponent();
			KycProfilePhotoCameraNavigationArgs? NavigationArgs = ServiceRef.NavigationService.PopLatestArgs<KycProfilePhotoCameraNavigationArgs>();
			KycProfilePhotoCameraViewModel ViewModel = new KycProfilePhotoCameraViewModel(NavigationArgs);
			this.BindingContext = ViewModel;
			ViewModel.CameraView = this.CameraView;
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			await this.ReleaseCameraAsync();
			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			await this.ReleaseCameraAsync();
			if (this.BindingContext is KycProfilePhotoCameraViewModel ViewModel)
			{
				ViewModel.CameraView = null;
			}

			await base.OnDisposeAsync();
		}

		private async Task ReleaseCameraAsync()
		{
			if (this.cameraReleased)
			{
				return;
			}

			this.cameraReleased = true;
			await this.CameraView.ReleaseAsync();
		}
	}
}
