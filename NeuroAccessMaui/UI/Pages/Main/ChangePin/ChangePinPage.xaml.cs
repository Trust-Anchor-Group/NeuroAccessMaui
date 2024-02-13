using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace NeuroAccessMaui.UI.Pages.Main.ChangePin
{
	/// <summary>
	/// Allows the user to change its PIN.
	/// </summary>
	public partial class ChangePinPage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChangePinPage"/> class.
		/// </summary>
		/// <param name="ViewModel">View model.</param>
		public ChangePinPage(ChangePinViewModel ViewModel)
			: base()
		{
			this.InitializeComponent();
			this.ContentPageModel = ViewModel;
		}

		/// <inheritdoc/>
		protected override void OnLoaded()
		{
			this.OldPinEntry.Focus();
			base.OnLoaded();
		}

		/// <inheritdoc/>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			WeakReferenceMessenger.Default.Register<KeyboardSizeMessage>(this, this.HandleKeyboardSizeMessage);
		}

		/// <inheritdoc/>
		protected override async Task OnDisappearingAsync()
		{
			WeakReferenceMessenger.Default.Unregister<KeyboardSizeMessage>(this);
			await base.OnDisappearingAsync();
		}

		private async void HandleKeyboardSizeMessage(object Recipient, KeyboardSizeMessage Message)
		{
			await this.Dispatcher.DispatchAsync(() =>
			{
				double Bottom = 0;
				if (DeviceInfo.Platform == DevicePlatform.iOS)
				{
					Thickness SafeInsets = this.On<iOS>().SafeAreaInsets();
					Bottom = SafeInsets.Bottom;
				}

				Thickness Margin = new(0, 0, 0, Message.KeyboardSize - Bottom);
				this.TheMainGrid.Margin = Margin;
			});
		}
	}
}
