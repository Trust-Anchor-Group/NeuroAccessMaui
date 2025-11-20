using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat
{
	/// <summary>
	/// A page that displays a list of the current user's contacts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class ChatPage
	{
		/// <inheritdoc/>
		public override string? UniqueId
		{
			get => (this.ContentPageModel as ChatViewModel)?.UniqueId;
			set
			{
				if (this.ContentPageModel is ChatViewModel ChatViewModel)
					ChatViewModel.UniqueId = value;
			}
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ChatPage"/> class.
		/// </summary>
		public ChatPage()
		{
			this.ContentPageModel = new ChatViewModel(this, ServiceRef.NavigationService.PopLatestArgs<ChatNavigationArgs>());
			this.InitializeComponent();
		}
	}
}
