using System;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Popups.Password
{
	/// <summary>
	/// Popup view for entering a password that will be validated against the stored credentials.
	/// </summary>
	public partial class CheckPasswordPopup : BasePopup
	{
		public CheckPasswordPopup()
		{
			this.InitializeComponent();
			this.Loaded += this.OnLoaded;
		}

		private void OnLoaded(object? sender, EventArgs args)
		{
			this.Loaded -= this.OnLoaded;
			this.PasswordEntry.Keyboard = ServiceRef.TagProfile.IsNumericPassword ? Keyboard.Numeric : Keyboard.Default;
			this.PasswordEntry.Focus();
		}
	}
}
