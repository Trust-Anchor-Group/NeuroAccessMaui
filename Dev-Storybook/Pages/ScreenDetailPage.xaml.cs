using DevStorybook.ViewModels;

namespace DevStorybook.Pages
{
	/// <summary>
	/// Displays the full preview, states, and components associated with one screen.
	/// </summary>
	public partial class ScreenDetailPage : ContentPage
	{
		private readonly ScreenDetailViewModel viewModel;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScreenDetailPage"/> class.
		/// </summary>
		/// <param name="ViewModel">The screen-detail ViewModel.</param>
		public ScreenDetailPage(ScreenDetailViewModel ViewModel)
		{
			this.InitializeComponent();
			this.viewModel = ViewModel;
			this.BindingContext = ViewModel;
		}

		/// <summary>
		/// Loads a production screen's catalog entries.
		/// </summary>
		/// <param name="ScreenKey">The stable screen key.</param>
		public void LoadScreen(string ScreenKey)
		{
			this.viewModel.LoadScreen(ScreenKey);
		}
	}
}
