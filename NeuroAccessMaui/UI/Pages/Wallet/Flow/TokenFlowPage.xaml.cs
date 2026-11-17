using Microsoft.Maui.Controls.Shapes;
using NeuroAccessMaui.Resources.Languages;
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

		/// <summary>
		/// The tallest the list of ways on gets in portrait. More ways scroll.
		/// </summary>
		private const double maxSheetListHeight = 240;

		/// <summary>
		/// The largest share of the page's height the list of ways on takes in portrait, so on a small phone the sheet
		/// never covers most of the map.
		/// </summary>
		private const double maxSheetListShare = 0.28;

		private bool? isLandscape;
		private bool isSheetFolded;

		/// <summary>
		/// Creates the page for a token's flow.
		/// </summary>
		public TokenFlowPage()
		{
			this.ContentPageModel = new TokenFlowViewModel(ServiceRef.NavigationService.PopLatestArgs<TokenFlowNavigationArgs>());
			this.InitializeComponent();

			this.FlowMap.SelectionTapped += this.FlowMap_SelectionTapped;
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
				this.Arrange(Width > Height, Width, Height);
		}

		/// <summary>
		/// In portrait the sheet sits under the map. In landscape, where height is short, the map takes the whole height
		/// with the top bar over it, and the sheet moves to the right side.
		/// </summary>
		/// <param name="Landscape">If the page is wider than it is tall.</param>
		/// <param name="Width">The page's width.</param>
		/// <param name="Height">The page's height.</param>
		private void Arrange(bool Landscape, double Width, double Height)
		{
			if (Landscape)
				this.Sheet.WidthRequest = Math.Min(maxSideSheetWidth, Width * 0.4);

			// Beside the map the sheet can take the whole height. Under it, the list is kept short and scrolls.
			this.SheetScroll.MaximumHeightRequest = Landscape
				? double.PositiveInfinity
				: Math.Min(maxSheetListHeight, Height * maxSheetListShare);

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
			this.Sheet.Padding = Landscape ? new Thickness(10, 16) : new Thickness(10, 10, 10, 24);
			if (!Landscape)
				this.Sheet.WidthRequest = -1;

			// Beside the map the sheet doesn't fold, since it covers none of it.
			this.SheetHandle.IsVisible = !Landscape;
			this.SheetToggle.IsVisible = !Landscape;
			if (Landscape)
				this.FoldSheet(false);

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

		/// <summary>
		/// Folds or unfolds the sheet when its top is tapped.
		/// </summary>
		private void SheetHeader_Tapped(object? Sender, TappedEventArgs e)
		{
			this.FoldSheet(!this.isSheetFolded);
		}

		/// <summary>
		/// Folds the sheet when its top is swiped down.
		/// </summary>
		private void SheetHeader_SwipedDown(object? Sender, SwipedEventArgs e)
		{
			this.FoldSheet(true);
		}

		/// <summary>
		/// Unfolds the sheet when its top is swiped up.
		/// </summary>
		private void SheetHeader_SwipedUp(object? Sender, SwipedEventArgs e)
		{
			this.FoldSheet(false);
		}

		/// <summary>
		/// Folds the sheet down to the step's name, or shows its ways on again. Only the sheet under the map folds.
		/// </summary>
		/// <param name="Fold">If the sheet is folded.</param>
		private void FoldSheet(bool Fold)
		{
			if (this.isLandscape == true)
				Fold = false;

			this.isSheetFolded = Fold;
			this.SheetScroll.IsVisible = !Fold;
			this.SheetToggleArrow.Rotation = Fold ? 180 : 0;
			SemanticProperties.SetDescription(this.SheetToggle,
				ServiceRef.Localizer[Fold ? nameof(AppResources.ShowMore) : nameof(AppResources.ShowLess)]);
		}

		/// <summary>
		/// Shows the ways on of a step whose card or arrow label was tapped, with a tapped way scrolled into view.
		/// </summary>
		private void FlowMap_SelectionTapped(object? Sender, EventArgs e)
		{
			this.FoldSheet(false);

			// The lines are listed again first, so the marked one is looked for once they are laid out.
			this.Dispatcher.Dispatch(async () =>
			{
				try
				{
					foreach (IView Line in this.SheetLinesView.Children)
					{
						if (Line is VisualElement Element && Element.BindingContext is TokenFlowSheetLine { IsMarked: true })
						{
							await this.SheetScroll.ScrollToAsync(Element, ScrollToPosition.MakeVisible, true);
							break;
						}
					}
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}
	}
}
