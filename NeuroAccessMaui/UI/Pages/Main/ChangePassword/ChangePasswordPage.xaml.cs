using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace NeuroAccessMaui.UI.Pages.Main.ChangePassword
{
	/// <summary>
	/// Allows the user to change its password.
	/// </summary>
	public partial class ChangePasswordPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChangePasswordPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model.</param>
		public ChangePasswordPage(ChangePasswordViewModel ViewModel)
			: base()
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}


		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			this.OldPasswordEntry.Focus();
		}

		/// <inheritdoc/>
		public override async Task OnDisappearingAsync()
		{
			await base.OnDisappearingAsync();
		}
	}
}
