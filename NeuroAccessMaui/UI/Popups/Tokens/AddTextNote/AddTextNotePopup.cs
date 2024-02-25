namespace NeuroAccessMaui.UI.Popups.Tokens.AddTextNote
{
	/// <summary>
	/// Prompts the user for text to add as a note for a token.
	/// </summary>
	public partial class AddTextNotePopup
	{
		private readonly AddTextNoteViewModel viewModel;

		/// <summary>
		/// Prompts the user for text to add as a note for a token.
		/// </summary>
		/// <param name="ViewModel">View model</param>
		public AddTextNotePopup(AddTextNoteViewModel ViewModel)
		{
			this.InitializeComponent();
			this.BindingContext = this.viewModel = ViewModel;
		}

		/// <inheritdoc/>
		protected override void OnAppearing()
		{
			this.NoteEntry.Focus();
			base.OnAppearing();
		}

		/// <inheritdoc/>
		protected override void OnDisappearing()
		{
			this.viewModel.Close();
			base.OnDisappearing();
		}
	}
}
