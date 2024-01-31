using IdApp.Helpers;
using IdApp.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.Xaml;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// A page that displays a list of the current user's contacts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ChatPageIos
	{
		/// <inheritdoc/>
		public override string UniqueId
		{
			get => (this.ViewModel as ChatViewModel).UniqueId;
			set => (this.ViewModel as ChatViewModel).UniqueId = value;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ChatPageIos"/> class.
		/// </summary>
		public ChatPageIos()
		{
			this.ViewModel = new ChatViewModel();

			this.InitializeComponent();
		}

		/// <inheritdoc/>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			MessagingCenter.Subscribe<object, KeyboardAppearEventArgs>(this, Constants.MessagingCenter.KeyboardAppears, (sender, eargs) =>
			{
				if (this.ContainerView.TranslationY == 0)
				{
					double Bottom = 0;
					if (Device.RuntimePlatform == Device.iOS)
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
				{
					this.EditorControl.Focus();
				}
			});

			this.ContainerView.ResolveLayoutChanges();  // Strange Xamarin issue: https://github.com/xamarin/Xamarin.Forms/issues/15066
		}

		/// <inheritdoc/>
		protected override async Task OnDisappearingAsync()
		{
			MessagingCenter.Unsubscribe<object, KeyboardAppearEventArgs>(this, Constants.MessagingCenter.KeyboardAppears);
			MessagingCenter.Unsubscribe<object>(this, Constants.MessagingCenter.KeyboardDisappears);
			MessagingCenter.Unsubscribe<object>(this, Constants.MessagingCenter.ChatEditorFocus);

			await base.OnDisappearingAsync();
		}

		private void OnEditorControlUnfocused(object Sender, FocusEventArgs e)
		{
			this.CollectionView.SelectedItem = null;
		}

		private void ViewCell_Appearing(object Sender, EventArgs EventArgs)
		{
			// This is a one-time Cell.Appearing event handler to work around an iOS issue whereby an image inside a ListView
			// does not update its size when fully loaded.

			ViewCell ViewCell = (ViewCell)Sender;
			ViewCell.Appearing -= this.ViewCell_Appearing;

			FFImageLoading.Forms.CachedImage CachedImage = ViewCell.View.Descendants().OfType<FFImageLoading.Forms.CachedImage>().FirstOrDefault();
			if (CachedImage is not null)
			{
				ImageSizeChangedHandler SizeChangedHandler = new(new WeakReference<ViewCell>(ViewCell));
				CachedImage.SizeChanged += SizeChangedHandler.HandleSizeChanged;
			}
			else
			{
				Image Image = ViewCell.View.Descendants().OfType<Image>().FirstOrDefault();
				if (Image is not null)
				{
					ImageSizeChangedHandler SizeChangedHandler = new(new WeakReference<ViewCell>(ViewCell));
					Image.SizeChanged += SizeChangedHandler.HandleSizeChanged;
				}
			}
		}

		private class ImageSizeChangedHandler(WeakReference<ViewCell> WeakViewCell)
		{
			private readonly WeakReference<ViewCell> weakViewCell = WeakViewCell;

			public void HandleSizeChanged(object Sender, EventArgs EventArgs)
			{
				if (this.weakViewCell.TryGetTarget(out ViewCell? ViewCell))
					ViewCell.ForceUpdateSize();
			}
		}
	}
}
