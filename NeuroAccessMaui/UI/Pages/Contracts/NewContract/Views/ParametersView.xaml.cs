using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.Views
{
	/// <summary>
	/// The step of the new-contract wizard where the user fills in the contract's values.
	/// </summary>
	public partial class ParametersView : BaseNewContractView
	{
		/// <summary>
		/// Creates the step where the user fills in the contract's values.
		/// </summary>
		public ParametersView()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Scrolls to the first value that needs a fix.
		/// </summary>
		private void ShowFirstFix_Tapped(object? Sender, TappedEventArgs e)
		{
			if (this.BindingContext is NewContractViewModel ViewModel &&
				ViewModel.FirstParameterToFix is ObservableParameter Parameter)
			{
				this.ParametersList.ScrollTo(Parameter, position: ScrollToPosition.Start, animate: true);
			}
		}
	}
}
