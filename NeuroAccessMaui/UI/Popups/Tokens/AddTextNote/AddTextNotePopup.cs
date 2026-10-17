using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups.Tokens.AddTextNote
{
	/// <summary>
	/// Prompts the user for text to add as a note for a token.
	/// </summary>
	public partial class AddTextNotePopup : BasePopup
	{
		private readonly AddTextNoteViewModel viewModel;

		/// <summary>
		/// Creates a new instance of the <see cref="AddTextNotePopup"/> class.
		/// </summary>
		/// <param name="viewModel">View model of the popup.</param>
		public AddTextNotePopup(AddTextNoteViewModel viewModel)
		{
			this.InitializeComponent();
			this.viewModel = viewModel;
			this.BindingContext = viewModel;
		}

		/// <inheritdoc/>
		public override Task OnAppearingAsync()
		{
			this.NoteEditor.Focus();
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
