namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class ValidateEmailView
	{
		public static ValidateEmailView Create()
		{
			return Create<ValidateEmailView>();
		}

		public ValidateEmailView(ValidateEmailViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;

			this.EmailEntry.Keyboard = Keyboard.Email;
			this.EmailEntry.IsSpellCheckEnabled = false;
			this.EmailEntry.IsTextPredictionEnabled = false;
		}
	}
}
