namespace NeuroAccessMaui.UI.Popups.Tokens.NoteCommand
{
	/// <summary>
	/// A sheet named after a note command, that asks for the values of the command's parameters, and then runs the
	/// command. It rises from the bottom edge, and the shell keeps it above the keyboard.
	/// </summary>
	public partial class NoteCommandPopup : BasePopup
	{
		private readonly NoteCommandViewModel viewModel;

		/// <summary>
		/// Creates a new instance of the <see cref="NoteCommandPopup"/> class.
		/// </summary>
		/// <param name="ViewModel">View model of the popup.</param>
		public NoteCommandPopup(NoteCommandViewModel ViewModel)
		{
			this.InitializeComponent();
			this.viewModel = ViewModel;
			this.BindingContext = ViewModel;
		}

		/// <inheritdoc/>
		public override Task OnDisappearingAsync()
		{
			this.viewModel.Close();
			return base.OnDisappearingAsync();
		}
	}
}
