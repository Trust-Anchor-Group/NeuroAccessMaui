using System.Text;
using System.Xml;
using System.Xml.Schema;
using NeuroAccessMaui.Services.Cache;
using Waher.Content.Xsl;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Default implementation of <see cref="IThemeService"/>.
	/// Loads branding over XMPP, caches it, and exposes image URIs and resource dictionaries.
	/// </summary>
	[Singleton]
	public sealed class ThemeService : IThemeService
	{
		private const string providerFlagKey = "IsServerThemeDictionary";
		private static readonly TimeSpan themeExpiry = TimeSpan.FromDays(7);
		private static readonly Lazy<XmlSchema> brandingSchemaLazy = new(() =>
		{
			using Stream SchemaStream = FileSystem.OpenAppPackageFileAsync("NeuroAccessBrandingV1.xsd").GetAwaiter().GetResult();
			return XSL.LoadSchema(SchemaStream, "NeuroAccessBrandingV1.xsd");
		});

		private readonly FileCacheManager cacheManager;
		private readonly Dictionary<string, Uri> imageUrisMap;

		/// <summary>
		/// Initializes a new instance of <see cref="ThemeService"/>.
		/// </summary>
		public ThemeService()
		{
			this.cacheManager = new FileCacheManager("BrandingThemes", themeExpiry);
			this.imageUrisMap = new(StringComparer.OrdinalIgnoreCase);
		}

		/// <inheritdoc />
		public IReadOnlyDictionary<string, Uri> ImageUris => new Dictionary<string, Uri>(this.imageUrisMap);

		/// <inheritdoc />
		public async Task ApplyProviderTheme()
		{
				string? PubSubJid = ServiceRef.TagProfile.PubSubJid;
			if (string.IsNullOrEmpty(PubSubJid))
				return;

			// Load branding descriptor XML
			Uri BrandingUri = new($"xmpp:NeuroAccessBranding@{PubSubJid}/Branding");
			string BrandingKey = BrandingUri.AbsoluteUri;

			byte[]? XmlBytes = await this.FetchOrGetCachedAsync(BrandingUri, BrandingKey);
			if (XmlBytes is null)
				return;

			XmlDocument XmlDoc = new();
			try
			{
				XmlDoc.LoadXml(Encoding.UTF8.GetString(XmlBytes));
				XSL.Validate("BrandingDescriptor", XmlDoc, brandingSchemaLazy.Value);
			}
			catch
			{
				return;
			}

			XmlElement? Root = XmlDoc.DocumentElement;
			if (Root?.LocalName != "BrandingDescriptor")
				return;

			// Merge resource dictionary
			XmlElement? ColorsNode = Root.SelectSingleNode("//*[local-name()='ColorsUri']") as XmlElement;
			if (ColorsNode is not null)
			{
				Uri ColorsUri = new(ColorsNode.InnerText.Trim());
				await this.LoadAndMergeDictionaryAsync(ColorsUri, PubSubJid);
			}

			// Expose image URIs
			this.imageUrisMap.Clear();

			XmlNodeList? ImageRefNodes = Root.SelectNodes("//*[local-name()='ImageRef']");
			if (ImageRefNodes is null)
				return;

			foreach (XmlElement Node in ImageRefNodes.OfType<XmlElement>())
			{
				string Id = Node.GetAttribute("id");
				string UriText = Node.GetAttribute("uri");
				if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(UriText))
					continue;

				if (Uri.TryCreate(UriText, UriKind.Absolute, out Uri? ImgUri))
					this.imageUrisMap[Id] = ImgUri;
			}
		}

		private async Task<byte[]?> FetchOrGetCachedAsync(Uri Uri, string Key)
		{
			(byte[]? Cached, string _) = await this.cacheManager.TryGet(Key);
			if (Cached is not null)
				return Cached;

			(byte[]? Fetched, string _) = await ServiceRef.InternetCacheService.GetOrFetch(Uri, ServiceRef.TagProfile.PubSubJid!, true);
			if (Fetched is not null)
			{
				await this.cacheManager.AddOrUpdate(Key, ServiceRef.TagProfile.PubSubJid!, true, Fetched, "application/xml");
			}

			return Fetched;
		}

		private async Task LoadAndMergeDictionaryAsync(Uri Uri, string ParentId)
		{
			string Key = Uri.AbsoluteUri;
			byte[]? XamlBytes = await this.FetchOrGetCachedAsync(Uri, Key);
			if (XamlBytes is null)
				return;

			ResourceDictionary Dict;
			try
			{
				Dict = new ResourceDictionary().LoadFromXaml(Encoding.UTF8.GetString(XamlBytes));
			}
			catch
			{
				return;
			}

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				ResourceDictionary? AppRes = Application.Current?.Resources;
				if (AppRes is null)
					return;

				ResourceDictionary? Existing = AppRes.MergedDictionaries.FirstOrDefault(rd => rd.ContainsKey(providerFlagKey));
				if (Existing is not null)
					AppRes.MergedDictionaries.Remove(Existing);

				Dict.Add(providerFlagKey, true);
				AppRes.MergedDictionaries.Add(Dict);
			});
		}

		/// <inheritdoc />
		public Task<AppTheme> GetTheme()
			=> Task.FromResult(Application.Current?.UserAppTheme ?? AppTheme.Unspecified);

		/// <inheritdoc />
		public Task SetTheme(AppTheme Theme)
		{
			if (Application.Current is not null)
				Application.Current.UserAppTheme = Theme;
			return Task.CompletedTask;
		}

		/// <summary>
		/// Returns the image URI for the given ID, or empty string if not found.
		/// </summary>
		/// <param name="Id">The image identifier.</param>
		public string GetImageUri(string Id)
		{
			return this.imageUrisMap.TryGetValue(Id, out Uri? UriVal)
				? UriVal!.AbsoluteUri
				: string.Empty;
		}
	}
}
