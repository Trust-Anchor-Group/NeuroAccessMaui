using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// A page that displays a list of the current user's contacts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ChatPage
	{
		/// <inheritdoc/>
		public override string UniqueId
		{
			get => (this.ViewModel as ChatViewModel).UniqueId;
			set => (this.ViewModel as ChatViewModel).UniqueId = value;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ChatPage"/> class.
		/// </summary>
		public ChatPage()
		{
			this.ViewModel = new ChatViewModel();

			this.InitializeComponent();
		}

		/// <inheritdoc/>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (DeviceInfo.Platform == DevicePlatform.Android)
				App.Current?.On<Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);

			MessagingCenter.Subscribe<object, KeyboardAppearEventArgs>(this, Constants.MessagingCenter.KeyboardAppears, (sender, eargs) =>
			{
				if (this.ContainerView.TranslationY == 0)
				{
					double Bottom = 0;
					if (DeviceInfo.Platform == DevicePlatform.iOS)
					{
						Thickness SafeInsets = this.On<iOS>().SafeAreaInsets();
						Bottom = SafeInsets.Bottom;
					}

					this.ContainerView.TranslationY -= eargs.KeyboardSize - Bottom;
				}
			});

			MessagingCenter.Subscribe<object>(this, Constants.MessagingCenter.KeyboardDisappears, (sender) =>
			{
				this.ContainerView.TranslationY = 0;
			});

			MessagingCenter.Subscribe<object>(this, Constants.MessagingCenter.ChatEditorFocus, (sender) =>
			{
				if (!this.EditorControl.IsFocused)
					this.EditorControl.Focus();
			});

			this.ContainerView.ResolveLayoutChanges();  // Strange Xamarin issue: https://github.com/xamarin/Xamarin.Forms/issues/15066
		}

		/// <inheritdoc/>
		protected override async Task OnDisappearingAsync()
		{
			MessagingCenter.Unsubscribe<object, KeyboardAppearEventArgs>(this, Constants.MessagingCenter.KeyboardAppears);
			MessagingCenter.Unsubscribe<object>(this, Constants.MessagingCenter.KeyboardDisappears);
			MessagingCenter.Unsubscribe<object>(this, Constants.MessagingCenter.ChatEditorFocus);

			if (DeviceInfo.Platform == DevicePlatform.Android)
				App.Current?.On<Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Pan);

			await base.OnDisappearingAsync();
			
		}

		private void OnEditorControlUnfocused(object Sender, FocusEventArgs e)
		{
			this.CollectionView.SelectedItem = null;
		}
	}
}
