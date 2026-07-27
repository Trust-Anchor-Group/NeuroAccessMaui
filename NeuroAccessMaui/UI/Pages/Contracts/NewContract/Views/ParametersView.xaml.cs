using System.ComponentModel;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract.Views
{
	/// <summary>
	/// Displays the template-defined parameters required to create a contract.
	/// </summary>
	public partial class ParametersView : BaseNewContractView
	{
		private NewContractViewModel? viewModel;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParametersView"/> class.
		/// </summary>
		public ParametersView()
		{
			this.InitializeComponent();
		}

		/// <inheritdoc/>
		protected override void OnBindingContextChanged()
		{
			if (this.viewModel is not null)
				this.viewModel.PropertyChanged -= this.ViewModel_PropertyChanged;

			base.OnBindingContextChanged();

			this.viewModel = this.BindingContext as NewContractViewModel;
			if (this.viewModel is not null)
			{
				this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
				this.ScrollToParameter(this.viewModel.FirstInvalidParameter);
			}
		}

		private void ViewModel_PropertyChanged(object? Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(NewContractViewModel.FirstInvalidParameter))
				this.ScrollToParameter(this.viewModel?.FirstInvalidParameter);
		}

		private void ScrollToParameter(ObservableParameter? Parameter)
		{
			if (Parameter is null)
				return;

			// Scrolling is view-only behavior; validation and target selection remain in the view model.
			MainThread.BeginInvokeOnMainThread(() =>
				this.ParameterCollection.ScrollTo(Parameter, position: ScrollToPosition.Start, animate: true));
		}
	}
}
