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

		private void OnEditorControlUnfocused(object Sender, FocusEventArgs e)
		{
			this.CollectionView.SelectedItem = null;
		}

		private void ViewCell_Appearing(object? Sender, EventArgs EventArgs)
		{
			// This is a one-time Cell.Appearing event handler to work around an iOS issue whereby an image inside a ListView
			// does not update its size when fully loaded.

			if (Sender is not ViewCell ViewCell)
				return;

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

			public void HandleSizeChanged(object? Sender, EventArgs EventArgs)
			{
				if (this.weakViewCell.TryGetTarget(out ViewCell? ViewCell))
					ViewCell.ForceUpdateSize();
			}
		}
	}
}
