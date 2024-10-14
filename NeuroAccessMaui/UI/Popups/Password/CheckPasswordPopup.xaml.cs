using NeuroAccessMaui.Services;

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
			this.PasswordEntry.Keyboard = ServiceRef.TagProfile.IsNumericPassword ? Keyboard.Numeric : Keyboard.Default;

		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			this.PasswordEntry.Focus();
		}


	}
}
