namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// A page that displays a specific contract.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ViewContractPage"/> class.
		/// </summary>
		public ViewContractPage()
		{
			this.ContentPageModel = new ViewContractViewModel();
			this.InitializeComponent();
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			return base.OnDisappearingAsync();
		}
	}
}
