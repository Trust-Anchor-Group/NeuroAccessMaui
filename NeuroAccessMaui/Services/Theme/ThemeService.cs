using System.Text;
using System.Xml;
using Waher.Runtime.Inventory;
using NeuroAccessMaui.Services.Cache;
using System.Xml.Schema;
using Waher.Content.Xsl;
using Waher.Persistence;
using Waher.Persistence.Filters;

namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Default implementation of <see cref="IThemeService"/>.
	/// Loads branding over XMPP, caches it, and applies resource dictionaries and images.
	/// </summary>
	[Singleton]
	public sealed class ThemeService : IThemeService
	{
		private const string providerFlagKey = "IsServerThemeDictionary";
		private static readonly TimeSpan themeExpiry = TimeSpan.FromDays(7);
		private static readonly Lazy<XmlSchema> brandingSchemaLazy = new Lazy<XmlSchema>(() =>
		{
			using Stream SchemaStream = FileSystem.OpenAppPackageFileAsync("NeuroAccessBrandingV1.xsd").GetAwaiter().GetResult();
			return XSL.LoadSchema(SchemaStream, "NeuroAccessBrandingV1.xsd");
		});

		private readonly FileCacheManager cacheManager;
		private readonly Dictionary<string, ImageSource> imagesMap;

		/// <summary>
		/// Initializes a new instance of <see cref="ThemeService"/>.
		/// </summary>
		public ThemeService()
		{
			this.cacheManager = new FileCacheManager("BrandingThemes", themeExpiry);
			this.imagesMap = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
		}

		/// <inheritdoc/>
		public IReadOnlyDictionary<string, ImageSource> Images => new Dictionary<string, ImageSource>(this.imagesMap);

		/// <inheritdoc/>
		public async Task ApplyProviderTheme()
		{
			try
			{
				await Database.FindDelete<CacheEntry>(
	new FilterFieldGreaterOrEqualTo("Url", string.Empty));
				await Database.Provider.Flush();

				string? PubSubJid = ServiceRef.TagProfile.PubSubJid;
				if (string.IsNullOrEmpty(PubSubJid))
					return;

				Uri ThemeUri = new Uri($"xmpp:NeuroAccessBranding@{PubSubJid}/Branding");
				string CacheKey = ThemeUri.AbsoluteUri;

				// Attempt to load cached XML (never expires)
				(byte[]? XmlBytes, string XmlType) = await this.cacheManager.TryGet(CacheKey);

				if (XmlBytes is null)
				{
					// First load: fetch and cache as permanent
					(byte[]? FetchedBytes, string FetchedType) =
						await ServiceRef.InternetCacheService.GetOrFetch(ThemeUri, PubSubJid, true);

					if (FetchedBytes is null)
						return;

					XmlBytes = FetchedBytes;
					await this.cacheManager.AddOrUpdate(CacheKey, PubSubJid, true, XmlBytes, FetchedType);
				}
				else
				{
					// Already cached: check age and refresh if older than ThemeExpiry
					CacheEntry? Entry = await Database.FindFirstDeleteRest<CacheEntry>(
						new FilterFieldEqualTo("Url", CacheKey));

					if (Entry is not null)
					{
						DateTime LastWrite = File.GetLastWriteTimeUtc(Entry.LocalFileName);
						if (DateTime.UtcNow - LastWrite > themeExpiry)
						{
							try
							{
								(byte[]? FetchedBytes2, string FetchedType2) =
									await ServiceRef.InternetCacheService.GetOrFetch(ThemeUri, PubSubJid, true);

								if (FetchedBytes2 is not null)
								{
									XmlBytes = FetchedBytes2;
									await this.cacheManager.AddOrUpdate(CacheKey, PubSubJid, true, XmlBytes, FetchedType2);
								}
							}
							catch (Exception ExRefresh)
							{
								ServiceRef.LogService.LogException(ExRefresh);
								// Offline or fetch failure: continue with stale cache
							}
						}
					}
				}

				string XmlContent = Encoding.UTF8.GetString(XmlBytes);
				XmlDocument XmlDoc = new XmlDocument();

				try
				{
					XmlDoc.LoadXml(XmlContent);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					return;
				}

				// Validate against schema
				try
				{
					XmlSchema Schema = brandingSchemaLazy.Value;
					XSL.Validate("BrandingDescriptor", XmlDoc, Schema);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					return;
				}

				XmlElement? Root = XmlDoc.DocumentElement;
				if (Root is null || Root.LocalName != "BrandingDescriptor")
					return;

				// Find ColorsUri element ignoring namespace
				XmlNode? ColorsNode = Root.SelectSingleNode("//*[local-name()='ColorsUri']");
				if (ColorsNode is not XmlElement)
					return;

				Uri ColorsUri = new Uri(ColorsNode.InnerText.Trim());
				string ColorsKey = ColorsUri.AbsoluteUri;

				// Load or fetch colors XAML with same logic
				byte[]? XamlBytes;
				(XamlBytes, _) = await this.cacheManager.TryGet(ColorsKey);

				if (XamlBytes is null)
				{
					(byte[]? FetchedXaml, string FetchedXamlType) =
						await ServiceRef.InternetCacheService.GetOrFetch(ColorsUri, PubSubJid, true);

					if (FetchedXaml is null)
						return;

					XamlBytes = FetchedXaml;
					await this.cacheManager.AddOrUpdate(ColorsKey, PubSubJid, true, XamlBytes, FetchedXamlType);
				}
				else
				{
					CacheEntry? Entry = await Database.FindFirstDeleteRest<CacheEntry>(
						new FilterFieldEqualTo("Url", ColorsKey));
					if (Entry is not null)
					{
						DateTime LastWrite = File.GetLastWriteTimeUtc(Entry.LocalFileName);
						if (DateTime.UtcNow - LastWrite > themeExpiry)
						{
							try
							{
								(byte[]? FetchedXaml2, string FetchedXamlType2) =
									await ServiceRef.InternetCacheService.GetOrFetch(ColorsUri, PubSubJid, true);

								if (FetchedXaml2 is not null)
								{
									XamlBytes = FetchedXaml2;
									await this.cacheManager.AddOrUpdate(ColorsKey, PubSubJid, true, XamlBytes, FetchedXamlType2);
								}
							}
							catch (Exception ExRefresh)
							{
								ServiceRef.LogService.LogException(ExRefresh);
							}
						}
					}
				}

				string XamlContent = Encoding.UTF8.GetString(XamlBytes);
				ResourceDictionary Dict;

				try
				{
					Dict = new ResourceDictionary().LoadFromXaml(XamlContent);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					return;
				}

				// Merge ResourceDictionary
				ResourceDictionary? AppResources = Application.Current?.Resources;
				if (AppResources is not null)
				{
					ResourceDictionary? ExistingDict = AppResources.MergedDictionaries
						.FirstOrDefault(rd => rd.ContainsKey(providerFlagKey));

					if (ExistingDict is not null)
						AppResources.MergedDictionaries.Remove(ExistingDict);

					Dict.Add(providerFlagKey, true);
					await MainThread.InvokeOnMainThreadAsync(() =>
						AppResources.MergedDictionaries.Add(Dict));
				}

				// Process ImageRef entries
				this.imagesMap.Clear();
				XmlNodeList ImageRefs = Root.SelectNodes("//*[local-name()='ImageRef']")
					?? XmlDoc.CreateNode(XmlNodeType.Element, "tmp", "").ChildNodes;

				foreach (XmlNode Node in ImageRefs)
				{
					if (Node is not XmlElement ImageRef)
						continue;

					string Id = ImageRef.GetAttribute("id");
					string UriText = ImageRef.GetAttribute("uri");
					if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(UriText))
						continue;

					Uri ImgUri = new Uri(UriText);
					string FallbackType = ImageRef.GetAttribute("contentType");
					if (string.IsNullOrEmpty(FallbackType))
						FallbackType = "application/octet-stream";

					// Try cache
					(byte[]? ImgBytes, string CachedType) = await this.cacheManager.TryGet(ImgUri.AbsoluteUri);

					string UseType;
					if (ImgBytes is null)
					{
						// Fetch (permanent) and cache
						(byte[]? FetchedImage, string FetchedType) =
							await ServiceRef.InternetCacheService.GetOrFetch(ImgUri, PubSubJid, true);

						if (FetchedImage is null)
							continue;

						ImgBytes = FetchedImage;
						UseType = !string.IsNullOrEmpty(FetchedType) ? FetchedType : FallbackType;

						await this.cacheManager.AddOrUpdate(ImgUri.AbsoluteUri, PubSubJid, true, ImgBytes, UseType);
					}
					else
					{
						UseType = !string.IsNullOrEmpty(CachedType) ? CachedType : FallbackType;
					}

					ImageSource Source = ImageSource.FromStream(() => new MemoryStream(ImgBytes!));
					this.imagesMap[Id] = Source;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <inheritdoc/>
		public Task<AppTheme> GetTheme()
		{
			return Task.FromResult(App.Current?.UserAppTheme ?? AppTheme.Unspecified);
		}

		/// <inheritdoc/>
		public Task SetTheme(AppTheme Type)
		{
			if (App.Current is not null)
				App.Current.UserAppTheme = Type;

			return Task.CompletedTask;
		}
	}
}
