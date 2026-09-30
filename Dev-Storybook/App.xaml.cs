using DevStorybook.Pages;
using ProductionStyles = NeuroAccessMaui.Resources.Styles;

namespace DevStorybook
{
	/// <summary>
	/// Hosts the independent developer Storybook application.
	/// </summary>
	public partial class App : Application
	{
		private readonly StorybookHomePage homePage;

		/// <summary>
		/// Initializes a new instance of the <see cref="App"/> class.
		/// </summary>
		/// <param name="HomePage">The Storybook landing page.</param>
		public App(StorybookHomePage HomePage)
		{
			this.InitializeComponent();
			this.Resources.MergedDictionaries.Add(new ProductionStyles.Light());
			this.Resources.MergedDictionaries.Add(new ProductionStyles.Styles());
			this.Resources.MergedDictionaries.Add(new ProductionStyles.LabelStyles());
			this.Resources.MergedDictionaries.Add(new ProductionStyles.ButtonStyles());
			this.Resources.MergedDictionaries.Add(new ProductionStyles.EntryStyles());
			this.homePage = HomePage;
		}

		/// <inheritdoc/>
		protected override Window CreateWindow(IActivationState? ActivationState)
		{
			NavigationPage Navigation = new NavigationPage(this.homePage);
			return new Window(Navigation);
		}
	}
}
