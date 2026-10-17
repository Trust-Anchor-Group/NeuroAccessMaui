using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Popups.Tokens.NoteCommand
{
	/// <summary>
	/// A sheet named after a note command, that asks for the values of the command's parameters, and then runs the
	/// command. It rises from the bottom edge, and stays above the keyboard.
	/// </summary>
	public partial class NoteCommandPopup : BasePopup, IKeyboardInsetAware
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

		/// <summary>
		/// Moves the sheet up while the keyboard is shown, so the field and the button stay in view.
		/// </summary>
		/// <param name="Args">The keyboard's height, and if it is shown.</param>
		public void OnKeyboardInsetChanged(KeyboardInsetChangedEventArgs Args)
		{
			double Bottom = Args.IsVisible ? Math.Max(0, Args.KeyboardHeight) : 0;

			MainThread.BeginInvokeOnMainThread(() => this.PopupMargin = new Thickness(0, 0, 0, Bottom));
		}
	}
}
