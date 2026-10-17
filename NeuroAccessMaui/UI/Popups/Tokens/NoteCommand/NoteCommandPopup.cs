namespace NeuroAccessMaui.UI.Popups.Tokens.NoteCommand
{
	/// <summary>
	/// Asks for the values of a note command's parameters, and then runs the command.
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
