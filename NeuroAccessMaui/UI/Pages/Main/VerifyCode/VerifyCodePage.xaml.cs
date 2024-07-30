using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Main.VerifyCode
{
	public partial class VerifyCodePage
	{
		private readonly List<Label> innerLabels;

		/// <summary>
		/// Creates a new instance of the <see cref="VerifyCodePage"/> class.
		/// </summary>
		public VerifyCodePage()
		{
			this.InitializeComponent();

			VerifyCodeViewModel ViewModel = new(ServiceRef.UiService.PopLatestArgs<VerifyCodeNavigationArgs>());
			this.ContentPageModel = ViewModel;

			this.innerLabels = [
				this.InnerCode1,
				this.InnerCode2,
				this.InnerCode3,
				this.InnerCode4,
				this.InnerCode5,
				this.InnerCode6
				];

			this.InnerCodeEntry.Text = string.Empty;
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
					Thickness Margin = new(0, 0, 0, Message.KeyboardSize - Bottom);
					this.TheMainGrid.Margin = Margin;
				}


			});
		}

		private async void InnerCodeEntry_TextChanged(object? Sender, TextChangedEventArgs e)
		{
			string NewText = e.NewTextValue;
			int NewLength = NewText.Length;

			bool IsValid = (NewLength <= this.innerLabels.Count) || NewText.ToCharArray().All(ch => !"0123456789".Contains(ch));

			if (!IsValid)
			{
				this.InnerCodeEntry.Text = e.OldTextValue;
				return;
			}

			for (int i = 0; i < this.innerLabels.Count; i++)
			{
				if (NewLength > i)
				{
					this.innerLabels[i].Text = NewText[i..(i + 1)];
					VisualStateManager.GoToState(this.innerLabels[i], VisualStateManager.CommonStates.Normal);
				}
				else
				{
					this.innerLabels[i].Text = "0\u2060"; // Added a "zero width no-break space" to make the disabled state to work right :)
					VisualStateManager.GoToState(this.innerLabels[i], VisualStateManager.CommonStates.Disabled);
				}
			}

			if (NewLength == this.innerLabels.Count)
				await this.InnerCodeEntry.HideKeyboardAsync();
		}

		private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
		{
			this.InnerCodeEntry.Focus();
		}
	}
}
