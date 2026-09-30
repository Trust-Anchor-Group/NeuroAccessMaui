using DevStorybook.ViewModels;

namespace DevStorybook.Pages
{
	/// <summary>
	/// Displays grouped screens or reusable shared components.
	/// </summary>
	public partial class StoryCategoryPage : ContentPage
	{
		private readonly StoryCategoryViewModel viewModel;

		/// <summary>
		/// Initializes a new instance of the <see cref="StoryCategoryPage"/> class.
		/// </summary>
		/// <param name="ViewModel">The category ViewModel.</param>
		public StoryCategoryPage(StoryCategoryViewModel ViewModel)
		{
			this.InitializeComponent();
			this.viewModel = ViewModel;
			this.BindingContext = ViewModel;
		}

		/// <summary>
		/// Loads a top-level catalog category.
		/// </summary>
		/// <param name="Category">The category name.</param>
		public void LoadCategory(string Category)
		{
			this.viewModel.LoadCategory(Category);
		}
	}
}
