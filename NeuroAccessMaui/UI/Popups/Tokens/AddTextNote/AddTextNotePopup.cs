using System.Threading.Tasks;

namespace NeuroAccessMaui.UI.Popups.Tokens.AddTextNote
{
	/// <summary>
	/// Prompts the user for text to add as a note for a token.
	/// </summary>
	public partial class AddTextNotePopup : BasePopup
	{
		private readonly AddTextNoteViewModel viewModel;

		public AddTextNotePopup(AddTextNoteViewModel viewModel)
		{
			this.InitializeComponent();
			this.viewModel = viewModel;
			this.BindingContext = viewModel;
		}

		public override Task OnAppearingAsync()
		{
			this.NoteEntry.Focus();
			return base.OnAppearingAsync();
		}

		public override Task OnDisappearingAsync()
		{
			this.viewModel.Close();
			return base.OnDisappearingAsync();
		}
	}
}
