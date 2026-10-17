namespace NeuroAccessMaui.UI.Pages.Contracts.MyContracts.Views
{
	/// <summary>
	/// The template picker: contract templates, or templates for creating tokens. It is the content of the
	/// <see cref="MyContractsPage"/> when the page lists templates, and binds to the page's <see cref="MyContractsViewModel"/>.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TemplatePickerView : ContentView
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TemplatePickerView"/> class.
		/// </summary>
		public TemplatePickerView()
		{
			this.InitializeComponent();
		}
	}
}
