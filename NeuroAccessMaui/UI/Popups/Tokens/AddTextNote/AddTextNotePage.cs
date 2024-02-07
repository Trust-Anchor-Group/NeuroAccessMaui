namespace NeuroAccessMaui.UI.Popups.Tokens.AddTextNote
{
	/// <summary>
	/// Prompts the user for text to add as a note for a token.
	/// </summary>
	public partial class AddTextNotePage
	{
		private readonly AddTextNoteViewModel viewModel;

		/// <summary>
		/// Prompts the user for text to add as a note for a token.
		/// </summary>
		/// <param name="ViewModel">View model</param>
		/// <param name="Background">Optional background</param>
		public AddTextNotePage(AddTextNoteViewModel ViewModel, ImageSource? Background = null)
			: base(Background)
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
