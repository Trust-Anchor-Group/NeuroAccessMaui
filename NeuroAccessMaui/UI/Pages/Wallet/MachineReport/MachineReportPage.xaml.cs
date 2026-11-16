using System.ComponentModel;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.MachineReport
{
	/// <summary>
	/// A page that allows the user to view a state-machine report.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MachineReportPage
	{
		private double scrollY;

		/// <summary>
		/// A page that allows the user to view a state-machine report.
		/// </summary>
		public MachineReportPage()
		{
			MachineReportViewModel ViewModel = new(ServiceRef.NavigationService.PopLatestArgs<MachineReportNavigationArgs>());
			this.ContentPageModel = ViewModel;
			this.InitializeComponent();

			ViewModel.PropertyChanging += this.ViewModel_PropertyChanging;
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

		/// <summary>
		/// Remembers where the user had scrolled, before a refreshed report replaces the shown one.
		/// </summary>
		private void ViewModel_PropertyChanging(object? Sender, System.ComponentModel.PropertyChangingEventArgs e)
		{
			if (e.PropertyName == nameof(MachineReportViewModel.Report))
				this.scrollY = this.ReportScrollView?.ScrollY ?? 0;
		}

		/// <summary>
		/// Scrolls back to where the user was, once a refreshed report is shown.
		/// </summary>
		private void ViewModel_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(MachineReportViewModel.Report) || this.scrollY <= 0 || this.ReportScrollView is null)
				return;

			double Y = this.scrollY;

			this.Dispatcher.Dispatch(async () =>
			{
				try
				{
					await this.ReportScrollView.ScrollToAsync(0, Y, false);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}
	}
}
