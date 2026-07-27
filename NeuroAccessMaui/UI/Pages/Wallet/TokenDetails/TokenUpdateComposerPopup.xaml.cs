using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// Prompts the user for an explicit text or advanced XML token update.
	/// </summary>
	public partial class TokenUpdateComposerPopup : BasePopup
	{
		private readonly TokenUpdateComposerViewModel viewModel;

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenUpdateComposerPopup"/> class.
		/// </summary>
		/// <param name="ViewModel">Update composer view model.</param>
		public TokenUpdateComposerPopup(TokenUpdateComposerViewModel ViewModel)
		{
			this.InitializeComponent();
			this.viewModel = ViewModel;
			this.BindingContext = ViewModel;
		}

		/// <inheritdoc/>
		public override Task OnAppearingAsync()
		{
			this.TextEditor.Focus();
			return base.OnAppearingAsync();
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.viewModel.Close();
			return base.OnDisappearingAsync();
		}
	}
}
