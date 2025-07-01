using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Threading;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Resources.Styles;
using NeuroAccessMaui.Services.Cache;
using NeuroAccessMaui.UI;
using Waher.Content.Xsl;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Service for loading, applying, and retrieving themes and branding in the application.
	/// Handles dynamic swapping of local and provider color dictionaries (supports v1 and v2 branding schemas).
	/// </summary>
	[Singleton]
	public sealed class ThemeService : IThemeService, IDisposable
	{
		private const string providerFlagKey = "IsServerThemeDictionary";
		private const string localFlagKey = "IsLocalThemeDictionary";
		private static readonly TimeSpan themeExpiry = TimeSpan.FromDays(7);

		private static readonly Lazy<Task<XmlSchema>> brandingSchemaV1Lazy = new(async () =>
		{
			using Stream Stream = await FileSystem.OpenAppPackageFileAsync("NeuroAccessBrandingV1.xsd");
			return XSL.LoadSchema(Stream, "NeuroAccessBrandingV1.xsd");
		});
		private static readonly Lazy<Task<XmlSchema>> brandingSchemaV2Lazy = new(async () =>
		{
			using Stream Stream = await FileSystem.OpenAppPackageFileAsync("NeuroAccessBrandingV2.xsd");
			return XSL.LoadSchema(Stream, "NeuroAccessBrandingV2.xsd");
		});

		private readonly FileCacheManager cacheManager;
		private readonly Dictionary<string, Uri> imageUrisMap;
		private readonly SemaphoreSlim themeSemaphore = new(1, 1); // Prevent race conditions on theme switching

		private ResourceDictionary? localLightDict = new Light();
		private ResourceDictionary? localDarkDict = new Dark();
		private AppTheme? lastAppliedLocalTheme;
		private bool disposedValue;

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
		public Task<AppTheme> GetTheme()
			=> Task.FromResult(Application.Current?.UserAppTheme ?? AppTheme.Unspecified);

		/// <inheritdoc />
		public void SetTheme(AppTheme Theme)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					ServiceRef.TagProfile.Theme = Theme;
					Application.Current!.UserAppTheme = Theme;
					this.SetLocalTheme(Theme);
					CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(AppColors.PrimaryBackground);
					CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(Theme == AppTheme.Dark ? StatusBarStyle.LightContent : StatusBarStyle.DarkContent);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(new Exception($"SetTheme failed for theme {Theme}.", Ex));
				}
			});
		}

		/// <summary>
		/// Loads and applies the local theme resource dictionary for light or dark theme.
		/// Call this at startup and on theme change.
		/// </summary>
		/// <param name="Theme">Theme to apply (light/dark).</param>
		public void SetLocalTheme(AppTheme Theme)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					ICollection<ResourceDictionary> MergedDictionaries = Application.Current!.Resources.MergedDictionaries;

					// Remove ALL old theme dictionaries of the same type, just to be safe.
					this.RemoveAllThemeDictionaries(MergedDictionaries, this.localLightDict);
					this.RemoveAllThemeDictionaries(MergedDictionaries, this.localDarkDict);

					this.localLightDict ??= new Light();
					this.localDarkDict ??= new Dark();

					// Only add if not already present (avoid double add, rare Maui bug).
					if (Theme == AppTheme.Dark && !MergedDictionaries.Contains(this.localDarkDict))
						MergedDictionaries.Add(this.localDarkDict);
					else if (Theme != AppTheme.Dark && !MergedDictionaries.Contains(this.localLightDict))
						MergedDictionaries.Add(this.localLightDict);

					this.lastAppliedLocalTheme = Theme;
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(new Exception($"SetLocalTheme failed for theme {Theme}.", Ex));
				}
			});
		}

		/// <summary>
		/// Loads and applies the provider-supplied branding (v1 or v2), with XSD validation.
		/// Uses a semaphore to prevent race conditions on concurrent applies.
		/// </summary>
		public async Task ApplyProviderTheme()
		{
			await this.themeSemaphore.WaitAsync();
			try
			{
				string? PubSubJid = ServiceRef.TagProfile.PubSubJid;
				if (string.IsNullOrEmpty(PubSubJid))
					return;

				Uri V2Uri = new($"xmpp:NeuroAccessBranding@{PubSubJid}/BrandingV2");
				string V2Key = V2Uri.AbsoluteUri;
				byte[]? V2Bytes = await this.FetchOrGetCachedAsync(V2Uri, V2Key);

				if (V2Bytes is not null)
				{
					try
					{
						XmlDocument Doc = new();
						Doc.LoadXml(Encoding.UTF8.GetString(V2Bytes));
						bool IsValid = await this.ValidateBrandingXmlAsync(Doc, true);
						if (!IsValid)
							throw new XmlSchemaValidationException("BrandingV2.xml failed schema validation.");
						await this.ApplyV2Async(Doc);
						return;
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(new Exception($"ApplyProviderTheme: V2 load/validation failed. Uri={V2Uri}", Ex));
						// Ignore and fallback to v1
					}
				}

				// Fallback to v1
				Uri V1Uri = new($"xmpp:NeuroAccessBranding@{PubSubJid}/Branding");
				string V1Key = V1Uri.AbsoluteUri;
				byte[]? V1Bytes = await this.FetchOrGetCachedAsync(V1Uri, V1Key);

				if (V1Bytes is not null)
				{
					try
					{
						XmlDocument Doc = new();
						Doc.LoadXml(Encoding.UTF8.GetString(V1Bytes));
						bool IsValid = await this.ValidateBrandingXmlAsync(Doc, false);
						if (!IsValid)
							throw new XmlSchemaValidationException("BrandingV1.xml failed schema validation.");
						await this.ApplyV1Async(Doc);
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(new Exception($"ApplyProviderTheme: V1 load/validation failed. Uri={V1Uri}", Ex));
						// Try fallback to local theme as last resort
						this.SetLocalTheme(await this.GetTheme());
					}
				}
				else
				{
					// No branding found, fallback to local theme
					this.SetLocalTheme(await this.GetTheme());
				}
			}
			finally
			{
				this.themeSemaphore.Release();
			}
		}

		/// <summary>
		/// Returns the image URI for the given ID, or empty string if not found.
		/// </summary>
		/// <param name="Id">The image identifier.</param>
		public string GetImageUri(string Id)
		{
			return this.imageUrisMap.TryGetValue(Id, out Uri? UriVal)
				? UriVal.AbsoluteUri
				: string.Empty;
		}

		private async Task ApplyV2Async(XmlDocument Doc)
		{
			XmlElement? Root = Doc.DocumentElement;
			if (Root is null || Root.NamespaceURI != "urn:neuroaccess:branding:2.0")
				return;

			this.imageUrisMap.Clear();
			XmlNodeList? Images = Root.SelectNodes("//*[local-name()='ImageRef']");
			if (Images is not null)
			{
				foreach (XmlElement Node in Images.OfType<XmlElement>())
				{
					string Id = Node.GetAttribute("id");
					string UriText = Node.GetAttribute("uri");
					if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(UriText) && Uri.TryCreate(UriText, UriKind.Absolute, out Uri? ImgUri))
						this.imageUrisMap[Id] = ImgUri;
				}
			}

			// Find all ColorsUri for light/dark
			XmlNodeList? ColorUris = Root.SelectNodes("//*[local-name()='ColorsUri']");
			Uri? LightUri = null;
			Uri? DarkUri = null;
			foreach (XmlElement Node in ColorUris?.OfType<XmlElement>() ?? [])
			{
				string Theme = Node.GetAttribute("theme").ToLowerInvariant();
				string UriText = Node.InnerText.Trim();

				if (Theme == "light")
					LightUri = new Uri(UriText);
				else if (Theme == "dark")
					DarkUri = new Uri(UriText);
			}

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				ICollection<ResourceDictionary>? MergedDictionaries = Application.Current?.Resources.MergedDictionaries;
				if (MergedDictionaries is null)
					return;

				// Remove ALL old theme dictionaries of the same type, just to be safe.
				this.RemoveAllThemeDictionaries(MergedDictionaries, this.localLightDict);
				this.RemoveAllThemeDictionaries(MergedDictionaries, this.localDarkDict);

				this.localLightDict ??= new Light();
				this.localDarkDict ??= new Dark();
				this.localLightDict.Clear();
				this.localDarkDict.Clear();

				if (LightUri is not null)
				{
					ResourceDictionary? ProviderLightDict = await this.LoadProviderDictionaryAsync(LightUri, "Light");
					if (ProviderLightDict is not null)
					{
						foreach (string Key in ProviderLightDict.Keys.OfType<string>())
							this.localLightDict[Key] = ProviderLightDict[Key];
					}
				}
				if (DarkUri is not null)
				{
					ResourceDictionary? ProviderDarkDict = await this.LoadProviderDictionaryAsync(DarkUri, "Dark");
					if (ProviderDarkDict is not null)
					{
						foreach (string Key in ProviderDarkDict.Keys.OfType<string>())
							this.localDarkDict[Key] = ProviderDarkDict[Key];
					}
				}

				this.localLightDict[localFlagKey] = true;
				this.localDarkDict[localFlagKey] = true;

				AppTheme CurrentTheme = await this.GetTheme();
				if (CurrentTheme == AppTheme.Dark && !MergedDictionaries.Contains(this.localDarkDict))
					MergedDictionaries.Add(this.localDarkDict);
				else if (CurrentTheme != AppTheme.Dark && !MergedDictionaries.Contains(this.localLightDict))
					MergedDictionaries.Add(this.localLightDict);

				this.lastAppliedLocalTheme = CurrentTheme;
			});
		}

		private async Task ApplyV1Async(XmlDocument Doc)
		{
			XmlElement? Root = Doc.DocumentElement;
			if (Root is null)
				return;

			this.imageUrisMap.Clear();
			XmlNodeList? Images = Root.SelectNodes("//*[local-name()='ImageRef']");
			if (Images is not null)
			{
				foreach (XmlElement Node in Images.OfType<XmlElement>())
				{
					string Id = Node.GetAttribute("id");
					string UriText = Node.GetAttribute("uri");
					if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(UriText) && Uri.TryCreate(UriText, UriKind.Absolute, out Uri? ImgUri))
						this.imageUrisMap[Id] = ImgUri;
				}
			}

			if (Root.SelectSingleNode("//*[local-name()='ColorsUri']") is not XmlElement ColorsNode)
				return;

			Uri ColorsUri = new(ColorsNode.InnerText.Trim());

			ResourceDictionary? OrigDict = await this.LoadProviderDictionaryAsync(ColorsUri, "V1");
			if (OrigDict is null)
				return;

			Dictionary<string, object> LightDict = [];
			Dictionary<string, object> DarkDict = [];

			foreach (string Key in OrigDict.Keys.OfType<string>())
			{
				if (Key.EndsWith("Light", StringComparison.OrdinalIgnoreCase))
					LightDict[Key[..^5]] = OrigDict[Key];
				else if (Key.EndsWith("Dark", StringComparison.OrdinalIgnoreCase))
					DarkDict[Key[..^4]] = OrigDict[Key];
				else
				{
					LightDict[Key] = OrigDict[Key];
					DarkDict[Key] = OrigDict[Key];
				}
			}

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				ICollection<ResourceDictionary>? MergedDictionaries = Application.Current?.Resources.MergedDictionaries;
				if (MergedDictionaries is null)
					return;

				this.RemoveAllThemeDictionaries(MergedDictionaries, this.localLightDict);
				this.RemoveAllThemeDictionaries(MergedDictionaries, this.localDarkDict);

				this.localLightDict ??= new Light();
				this.localDarkDict ??= new Dark();
				this.localLightDict.Clear();
				this.localDarkDict.Clear();

				foreach (KeyValuePair<string, object> Kv in LightDict)
					this.localLightDict[Kv.Key] = Kv.Value;
				foreach (KeyValuePair<string, object> Kv in DarkDict)
					this.localDarkDict[Kv.Key] = Kv.Value;

				this.localLightDict[localFlagKey] = true;
				this.localDarkDict[localFlagKey] = true;

				AppTheme CurrentTheme = await this.GetTheme();
				if (CurrentTheme == AppTheme.Dark && !MergedDictionaries.Contains(this.localDarkDict))
					MergedDictionaries.Add(this.localDarkDict);
				else if (CurrentTheme != AppTheme.Dark && !MergedDictionaries.Contains(this.localLightDict))
					MergedDictionaries.Add(this.localLightDict);

				this.lastAppliedLocalTheme = CurrentTheme;
			});
		}

		private async Task<ResourceDictionary?> LoadProviderDictionaryAsync(Uri Uri, string ThemeTag)
		{
			string Key = Uri.AbsoluteUri;
			byte[]? XamlBytes = await this.FetchOrGetCachedAsync(Uri, Key);
			if (XamlBytes is null)
				return null;

			try
			{
				ResourceDictionary Dict = new ResourceDictionary().LoadFromXaml(Encoding.UTF8.GetString(XamlBytes));
				Dict.TryAdd(providerFlagKey, true);
				Dict.TryAdd("Theme", ThemeTag); // Optional: for debugging
				return Dict;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(new Exception($"Failed to load provider ResourceDictionary from {Uri}", Ex));
				return null;
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

		private async Task<bool> ValidateBrandingXmlAsync(XmlDocument Doc, bool UseV2)
		{
			try
			{
				XmlSchema Schema = UseV2 ? await brandingSchemaV2Lazy.Value : await brandingSchemaV1Lazy.Value;
				XSL.Validate("BrandingDescriptor", Doc, Schema);
				return true;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(new Exception($"XSD validation failed for schema version {(UseV2 ? "V2" : "V1")}.", Ex));
				return false;
			}
		}

		/// <summary>
		/// Removes all dictionaries matching the specified instance or with the theme flag key.
		/// </summary>
		private void RemoveAllThemeDictionaries(ICollection<ResourceDictionary> MergedDictionaries, ResourceDictionary? DictInstance)
		{
			if (DictInstance != null)
			{
				while (MergedDictionaries.Contains(DictInstance))
					MergedDictionaries.Remove(DictInstance);
			}
			// Additionally, ensure no duplicates of our custom theme flag
			foreach (ResourceDictionary Dict in MergedDictionaries.Where(d => d.ContainsKey(localFlagKey) || d.ContainsKey(providerFlagKey)).ToList())
			{
				MergedDictionaries.Remove(Dict);
			}
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					this.themeSemaphore?.Dispose();
				}

				this.disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
