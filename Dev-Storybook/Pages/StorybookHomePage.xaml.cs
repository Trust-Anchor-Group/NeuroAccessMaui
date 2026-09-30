using DevStorybook.ViewModels;

namespace DevStorybook.Pages
{
	/// <summary>
	/// Displays Storybook categories and searchable direct story links.
	/// </summary>
	public partial class StorybookHomePage : ContentPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StorybookHomePage"/> class.
		/// </summary>
		/// <param name="ViewModel">The landing-page ViewModel.</param>
		public StorybookHomePage(StorybookHomeViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = ViewModel;
		}
	}
}
