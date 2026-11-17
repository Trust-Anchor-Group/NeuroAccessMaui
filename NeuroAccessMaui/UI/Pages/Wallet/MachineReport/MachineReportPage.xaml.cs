using System.ComponentModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport
{
	/// <summary>
	/// A page that allows the user to view a state-machine report.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MachineReportPage
	{
		private bool? isFullScreen;

		/// <summary>
		/// A page that allows the user to view a state-machine report.
		/// </summary>
		public MachineReportPage()
		{
			MachineReportViewModel ViewModel = new(ServiceRef.NavigationService.PopLatestArgs<MachineReportNavigationArgs>());
			this.ContentPageModel = ViewModel;
			this.InitializeComponent();

			ViewModel.PropertyChanged += this.ViewModel_PropertyChanged;
		}

		/// <summary>
		/// Lets the screen turn to landscape while the page is shown, for a wider view.
		/// </summary>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			ServiceRef.PlatformSpecific.SetScreenRotation(true);
		}

		/// <summary>
		/// Keeps the screen upright again when the page is left.
		/// </summary>
		public override async Task OnDisappearingAsync()
		{
			ServiceRef.PlatformSpecific.SetScreenRotation(false);
			await base.OnDisappearingAsync();
		}

		/// <inheritdoc/>
		protected override void OnSizeAllocated(double Width, double Height)
		{
			base.OnSizeAllocated(Width, Height);
			this.Arrange();
		}

		/// <summary>
		/// Shows an image report on the whole screen in landscape, under the system bars, with the top bar and the zoom bar
		/// over it. In portrait, and for text reports, the page keeps clear of the system bars as usual.
		/// </summary>
		private void Arrange()
		{
			if (this.Width <= 0 || this.Height <= 0 || this.BindingContext is not MachineReportViewModel ViewModel)
				return;

			bool FullScreen = this.Width > this.Height && ViewModel.IsImageReport;
			if (this.isFullScreen == FullScreen)
				return;

			this.isFullScreen = FullScreen;

			SafeArea.SetMode(this, FullScreen ? SafeAreaMode.Ignore : SafeAreaMode.Default);
			UiPageResolver.GetRequiredShellPresenter().UpdateBars(this);

			// The page is now drawn under the system bars, so the parts over the image keep clear of them.
			Thickness Insets = FullScreen ? ServiceRef.PlatformSpecific.GetInsets() : new Thickness(0);

			Grid.SetRow(this.Body, FullScreen ? 0 : 1);
			Grid.SetRowSpan(this.Body, FullScreen ? 2 : 1);

			this.Header.Margin = new Thickness(16 + Insets.Left, 8 + Insets.Top, 16 + Insets.Right, 8);
			this.TitleBox.Padding = FullScreen ? new Thickness(14, 6) : new Thickness(0);
			if (FullScreen)
				this.TitleBox.SetDynamicResource(BackgroundColorProperty, "SurfaceElevation1WL");
			else
				this.TitleBox.BackgroundColor = Colors.Transparent;

			if (this.ZoomView is not null)
			{
				this.ZoomView.Margin = FullScreen ? new Thickness(0) : new Thickness(12, 0, 12, 20);
				this.ZoomView.OverlayPadding = Insets;
				this.ZoomView.IsFullScreen = FullScreen;
			}
		}

		/// <summary>
		/// Arranges the page again when the report turns into an image, or back into text.
		/// </summary>
		private void ViewModel_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(MachineReportViewModel.IsImageReport))
				this.Arrange();
		}
	}
}
