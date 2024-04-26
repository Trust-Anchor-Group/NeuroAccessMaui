using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using IServiceProvider = Waher.Networking.XMPP.Contracts.IServiceProvider;

namespace NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Service Provider information model, including related notification information.
	/// </summary>
	/// <param name="ServiceProvider">Contact information.</param>
	/// <param name="IconHeight">Desired Icon Height</param>
	/// <param name="Parent">Parent view.</param>
	public partial class ServiceProviderViewModel(IServiceProvider ServiceProvider, int IconHeight, ServiceProvidersViewModel Parent)
		: XmppViewModel
	{
		private readonly IServiceProvider serviceProvider = ServiceProvider;
		private readonly ServiceProvidersViewModel parent = Parent;
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
		/// Icon Height
		/// </summary>
		public int IconHeight { get; } = IconHeight;

		/// <summary>
		/// Icon Width
		/// </summary>
		public int IconWidth
		{
			get
			{
				if (!this.HasIcon || this.serviceProvider.IconHeight == 0)
					return 0;

				double s = ((double)this.IconHeight) / this.serviceProvider.IconHeight;
				
				return (int)(this.serviceProvider.IconWidth * s + 0.5);
			}
		}

		private bool hasRun = false;

		/// <summary>
		/// Icon URL Source
		/// </summary>
		public ImageSource? IconUrlSource
		{
			get
			{
				if (this.iconSource == null && !this.hasRun)
				{
					//Load the icon
					Task.Run(this.UpdateIconUrlSourceAsync);
					this.hasRun = true;
				}
				return this.iconSource;
			}
		}

		public async Task UpdateIconUrlSourceAsync()
		{
			if (this.iconSource != null) return; // Already loaded or loading

			try
			{
				//Check if URI is SVG
				bool isSvg = this.IconUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);

				if (this.IconUrl.StartsWith("resource://", StringComparison.Ordinal))
					this.iconSource = ImageSource.FromResource(this.IconUrl[11..]);
				else if (this.IconUrl.StartsWith("file://", StringComparison.Ordinal))
				{
					if(isSvg)
						this.iconSource = this.IconUrl[7..^4];
					else
						this.iconSource = this.IconUrl[7..];
				}
				else if (Uri.TryCreate(this.IconUrl, UriKind.Absolute, out Uri? ParsedUri))
				{
					if(isSvg)
						this.iconSource = await ServiceRef.UiService.ConvertSvgUriToImageSource(this.IconUrl);
					else
						this.iconSource = ImageSource.FromUri(ParsedUri);
				}
				else
				{
					//if it is an SVG, remove the extension
					//Maui does not inheritly support SVGs, but can implicitly convert them to png
					if(isSvg)
						this.iconSource = ImageSource.FromFile(this.IconUrl[..^4]);
					else
						this.iconSource = ImageSource.FromFile(this.IconUrl);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			MainThread.BeginInvokeOnMainThread(() => this.OnPropertyChanged(nameof(this.IconUrlSource)));
		}

		/// <summary>
		/// Selects a service provider.
		/// </summary>
		[RelayCommand]
		private Task SelectServiceProvider()
		{
			return this.parent.SelectServiceProvider(this);
		}
	}
}
