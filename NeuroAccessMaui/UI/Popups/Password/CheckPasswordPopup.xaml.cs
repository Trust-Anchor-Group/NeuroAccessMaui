namespace NeuroAccessMaui.UI.Popups.Password
{
	/// <summary>
	/// A Popup letting the user enter a password to be verified with the password defined by the user earlier.
	/// </summary>
	public partial class CheckPasswordPopup
	{
		public CheckPasswordPopup()
		{
			this.InitializeComponent();
		}

		protected override void OnAppearing()
		{
			this.PasswordEntry.Focus();
			base.OnAppearing();
		}
	}
}
