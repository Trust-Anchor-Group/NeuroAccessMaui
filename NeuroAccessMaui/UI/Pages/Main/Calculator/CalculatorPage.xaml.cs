namespace NeuroAccessMaui.UI.Pages.Main.Calculator
{
	/// <summary>
	/// A page that allows the user to calculate the value of a numerical input field.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CalculatorPage
	{
		/// <summary>
		/// A page that allows the user to calculate the value of a numerical input field.
		/// </summary>
		public CalculatorPage()
		{
			this.ViewModel = new CalculatorViewModel();

			this.InitializeComponent();
		}
	}
}
