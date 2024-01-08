using CommunityToolkit.Mvvm.Input;
using IServiceProvider = Waher.Networking.XMPP.Contracts.IServiceProvider;

namespace NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Service Provider information model, including related notification information.
	/// </summary>
	/// <param name="ServiceProvider">Contact information.</param>
	public partial class ServiceProviderViewModel(IServiceProvider ServiceProvider) : XmppViewModel
	{
		private readonly IServiceProvider serviceProvider = ServiceProvider;
		private ImageSource? iconSource;

		/// <summary>
		/// Underlying service provider
		/// </summary>
		public IServiceProvider ServiceProvider => this.serviceProvider;

		/// <summary>
		/// Service ID
		/// </summary>
		public string Id => this.serviceProvider.Id;

		/// <summary>
		/// Displayable name
		/// </summary>
		public string Name => this.serviceProvider.Name;

		/// <summary>
		/// If service provider has an icon
		/// </summary>
		public bool HasIcon => !string.IsNullOrEmpty(this.serviceProvider.IconUrl);

		/// <summary>
		/// Icon URL
		/// </summary>
		public string IconUrl => this.serviceProvider.IconUrl;

		/// <summary>
		/// If an image should be displayed.
		/// </summary>
		public bool ShowImage => this.HasIcon;

		/// <summary>
		/// If text should be displayed.
		/// </summary>
		public bool ShowText => !this.HasIcon || this.IconWidth <= 250 || this.serviceProvider.GetType().Assembly == typeof(App).Assembly;

		/// <summary>
		/// Icon URL Source
		/// </summary>
		public ImageSource? IconUrlSource
		{
			get
			{
				if (this.iconSource is null)
				{
					if (this.IconUrl.StartsWith("resource://", StringComparison.Ordinal))
						this.iconSource = ImageSource.FromResource(this.IconUrl[11..]);
					else if (Uri.TryCreate(this.IconUrl, UriKind.Absolute, out Uri? ParsedUri))
						this.iconSource = ImageSource.FromUri(ParsedUri);
				}

				return this.iconSource;
			}
		}

		/// <summary>
		/// Icon Width
		/// </summary>
		public int IconWidth => this.serviceProvider.IconWidth;

		/// <summary>
		/// Icon Height
		/// </summary>
		public int IconHeight => this.serviceProvider.IconHeight;

		/// <summary>
		/// Requests a review from the service provider.
		/// </summary>
		[RelayCommand]
		private async Task RequestReview()
		{
		}
	}
}
