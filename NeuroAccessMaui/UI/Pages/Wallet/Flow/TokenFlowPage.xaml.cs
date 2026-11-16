using Microsoft.Maui.Controls.Shapes;
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
		/// The widest the side sheet gets in landscape.
		/// </summary>
		private const double maxSideSheetWidth = 360;

		private bool? isLandscape;

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

		/// <inheritdoc/>
		protected override void OnSizeAllocated(double Width, double Height)
		{
			base.OnSizeAllocated(Width, Height);

			if (Width > 0 && Height > 0)
				this.Arrange(Width > Height, Width);
		}

		/// <summary>
		/// In portrait the sheet sits under the map. In landscape, where height is short, the map takes the whole height
		/// with the top bar over it, and the sheet moves to the right side.
		/// </summary>
		/// <param name="Landscape">If the page is wider than it is tall.</param>
		/// <param name="Width">The page's width.</param>
		private void Arrange(bool Landscape, double Width)
		{
			if (Landscape)
				this.Sheet.WidthRequest = Math.Min(maxSideSheetWidth, Width * 0.4);

			if (this.isLandscape == Landscape)
				return;

			this.isLandscape = Landscape;

			Grid.SetRow(this.FlowMap, Landscape ? 0 : 1);
			Grid.SetRowSpan(this.FlowMap, Landscape ? 3 : 1);
			Grid.SetColumnSpan(this.FlowMap, Landscape ? 1 : 2);

			Grid.SetColumnSpan(this.Header, Landscape ? 1 : 2);
			// Over the map the title is smaller. In portrait it takes its size from its style again.
			if (Landscape)
				this.TitleLabel.FontSize = 20;
			else
				this.TitleLabel.ClearValue(Label.FontSizeProperty);

			Grid.SetRow(this.Sheet, Landscape ? 0 : 2);
			Grid.SetRowSpan(this.Sheet, Landscape ? 3 : 1);
			Grid.SetColumn(this.Sheet, Landscape ? 1 : 0);
			Grid.SetColumnSpan(this.Sheet, Landscape ? 1 : 2);
			this.Sheet.StrokeShape = new RoundRectangle { CornerRadius = Landscape ? new CornerRadius(24, 0, 24, 0) : new CornerRadius(24, 24, 0, 0) };
			this.Sheet.Padding = Landscape ? new Thickness(20, 16) : new Thickness(20, 10, 20, 24);
			if (!Landscape)
				this.Sheet.WidthRequest = -1;

			this.SheetHandle.IsVisible = !Landscape;
			this.SheetScroll.MaximumHeightRequest = Landscape ? double.PositiveInfinity : 240;

			// The map gets a new shape, so it is fitted to it again once it has its new size.
			this.FlowMap.RefitOnResize();
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
