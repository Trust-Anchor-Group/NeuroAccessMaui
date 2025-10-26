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
		/// Threshold for considering an icon to be a wide (horizontal) rectangle.
		/// Icons at or above this aspect ratio are treated as hero/banners and shown without text.
		/// </summary>
		private const double WideAspectThreshold = 800/200; 

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
		/// True if icon has valid dimensions.
		/// </summary>
		private bool HasValidIconDimensions => this.serviceProvider.IconWidth > 0 && this.serviceProvider.IconHeight > 0;

		/// <summary>
		/// Aspect ratio of icon (Width/Height). Returns 1 if not available.
		/// </summary>
		private double IconAspectRatio
		{
			get
			{
				if (!this.HasValidIconDimensions)
					return 1.0;

				return (double)this.serviceProvider.IconWidth / this.serviceProvider.IconHeight;
			}
		}

		/// <summary>
		/// True if icon is considered a wide (horizontal) rectangle suited for image-only display.
		/// </summary>
		public bool IsWideHero => this.HasIcon && this.HasValidIconDimensions && this.IconAspectRatio >= WideAspectThreshold;

		/// <summary>
		/// If an image should be displayed.
		/// </summary>
		public bool ShowImage => this.HasIcon;

		/// <summary>
		/// If text should be displayed.
		/// 
		/// New rule:
		/// - If icon is a wide horizontal rectangle (hero) -> show image alone (no text), unless icon is from the app assembly.
		/// - Otherwise -> show text (with or without image).
		/// </summary>
		public bool ShowText =>
			!this.HasIcon ||
			!this.IsWideHero ||
			this.serviceProvider.GetType().Assembly == typeof(App).Assembly;

		/// <summary>
		/// Icon Height (requested)
		/// </summary>
		public int IconHeight { get; } = IconHeight;

		/// <summary>
		/// Icon Width (scaled to requested height).
		/// 
		/// Note: This retains the original proportional scaling behavior.
		/// When used together with text, prefer <see cref="DisplayIconWidth"/> for a uniform presentation.
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

		/// <summary>
		/// Display Icon Height, taking layout rules into account.
		/// - If showing image with text: Use a uniform square based on requested IconHeight.
		/// - If showing image alone (wide hero): Use requested IconHeight (original proportional width applies).
		/// - If no image: 0.
		/// </summary>
		public int DisplayIconHeight
		{
			get
			{
				if (!this.ShowImage)
					return 0;

				// With text -> uniform square (height == width == IconHeight)
				// Image-only -> use the requested height (width is proportional)
				return this.IconHeight;
			}
		}

		/// <summary>
		/// Display Icon Width, taking layout rules into account.
		/// - If showing image with text: Use a uniform square based on requested IconHeight.
		/// - If showing image alone (wide hero): Proportional width based on requested IconHeight (same as IconWidth).
		/// - If no image: 0.
		/// </summary>
		public int DisplayIconWidth
		{
			get
			{
				if (!this.ShowImage)
					return 0;

				// If text is visible, we present the icon as a square for uniformity.
				if (this.ShowText)
					return this.IconHeight;

				// Image-only (wide hero) preserves proportional width.
				return this.IconWidth;
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

		/// <summary>
		/// Loads the icon source asynchronously.
		/// Handles SVG by converting or removing extension depending on source kind.
		/// </summary>
		public async Task UpdateIconUrlSourceAsync()
		{
			if (this.iconSource != null)
				return; // Already loaded or loading

			try
			{
				//Check if URI is SVG
				bool isSvg = this.IconUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);

				if (this.IconUrl.StartsWith("resource://", StringComparison.Ordinal))
					this.iconSource = ImageSource.FromResource(this.IconUrl[11..]);
				else if (this.IconUrl.StartsWith("file://", StringComparison.Ordinal))
				{
					if (isSvg)
						this.iconSource = this.IconUrl[7..^4];
					else
						this.iconSource = this.IconUrl[7..];
				}
				else if (Uri.TryCreate(this.IconUrl, UriKind.Absolute, out Uri? ParsedUri))
				{
					if (isSvg)
						this.iconSource = await ServiceRef.UiService.ConvertSvgUriToImageSource(this.IconUrl);
					else
						this.iconSource = ImageSource.FromUri(ParsedUri);
				}
				else
				{
					//if it is an SVG, remove the extension
					//Maui does not inheritly support SVGs, but can implicitly convert them to png
					if (isSvg)
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
