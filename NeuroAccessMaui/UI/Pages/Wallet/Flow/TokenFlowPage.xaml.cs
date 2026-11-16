using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Wallet.Flow
{
	/// <summary>
	/// A page that shows a token's whole flow as a map, with what moves the token on from the tapped step.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TokenFlowPage
	{
		/// <summary>
		/// Creates the page for a token's flow.
		/// </summary>
		public TokenFlowPage()
		{
			this.ContentPageModel = new TokenFlowViewModel(ServiceRef.NavigationService.PopLatestArgs<TokenFlowNavigationArgs>());
			this.InitializeComponent();
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
		/// Fits the whole flow on the screen.
		/// </summary>
		private void Fit_Tapped(object? Sender, TappedEventArgs e)
		{
			this.FlowMap.Fit();
		}
	}
}
