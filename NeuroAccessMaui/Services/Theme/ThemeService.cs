using System.Text;
using System.Xml;
using System.Xml.Schema;
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
	public sealed class ThemeService : IThemeService
	{
		private const string providerFlagKey = "IsServerThemeDictionary";
		private const string localFlagKey = "IsLocalThemeDictionary";
		private static readonly TimeSpan ThemeExpiry = TimeSpan.FromDays(7);

		private static readonly Lazy<XmlSchema> BrandingSchemaV1Lazy = new(() =>
		{
			using Stream Stream = FileSystem.OpenAppPackageFileAsync("NeuroAccessBrandingV1.xsd").GetAwaiter().GetResult();
			return XSL.LoadSchema(Stream, "NeuroAccessBrandingV1.xsd");
		});
		private static readonly Lazy<XmlSchema> BrandingSchemaV2Lazy = new(() =>
		{
			using Stream Stream = FileSystem.OpenAppPackageFileAsync("NeuroAccessBrandingV2.xsd").GetAwaiter().GetResult();
			return XSL.LoadSchema(Stream, "NeuroAccessBrandingV2.xsd");
		});

		private readonly FileCacheManager CacheManager;
		private readonly Dictionary<string, Uri> ImageUrisMap;

		private ResourceDictionary? localLightDict = new Light();
		private ResourceDictionary? localDarkDict = new Dark();
		private AppTheme? LastAppliedLocalTheme;

		/// <summary>
		/// Initializes a new instance of <see cref="ThemeService"/>.
		/// </summary>
		public ThemeService()
		{
			this.CacheManager = new FileCacheManager("BrandingThemes", ThemeExpiry);
			this.ImageUrisMap = new(StringComparer.OrdinalIgnoreCase);
		}

		/// <inheritdoc />
		public IReadOnlyDictionary<string, Uri> ImageUris => new Dictionary<string, Uri>(this.ImageUrisMap);

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
					ServiceRef.LogService.LogException(Ex);
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

					// Remove old dictionaries if present
					if (this.localLightDict != null)
						MergedDictionaries.Remove(this.localLightDict);
					if (this.localDarkDict != null)
						MergedDictionaries.Remove(this.localDarkDict);

					this.localLightDict ??= new Light();
					this.localDarkDict ??= new Dark();

					// Add only the relevant dictionary
					if (Theme == AppTheme.Dark)
						MergedDictionaries.Add(this.localDarkDict);
					else
						MergedDictionaries.Add(this.localLightDict);

					this.LastAppliedLocalTheme = Theme;
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}

		/// <summary>
		/// Loads and applies the provider-supplied branding (v1 or v2).
		/// </summary>
		public async Task ApplyProviderTheme()
		{
			string? PubSubJid = ServiceRef.TagProfile.PubSubJid;
			if (string.IsNullOrEmpty(PubSubJid))
				return;

			Uri V2Uri = new Uri($"xmpp:NeuroAccessBranding@{PubSubJid}/BrandingV2");
			string V2Key = V2Uri.AbsoluteUri;
			byte[]? V2Bytes = await this.FetchOrGetCachedAsync(V2Uri, V2Key);

			if (V2Bytes != null)
			{
				try
				{
					XmlDocument Doc = new XmlDocument();
					Doc.LoadXml(Encoding.UTF8.GetString(V2Bytes));
					await this.ApplyV2Async(Doc);
					return;
				}
				catch
				{
					// Ignore and fallback to v1
				}
			}

			// Fallback to v1
			Uri V1Uri = new Uri($"xmpp:NeuroAccessBranding@{PubSubJid}/Branding");
			string V1Key = V1Uri.AbsoluteUri;
			byte[]? V1Bytes = await this.FetchOrGetCachedAsync(V1Uri, V1Key);

			if (V1Bytes != null)
			{
				XmlDocument Doc = new XmlDocument();
				Doc.LoadXml(Encoding.UTF8.GetString(V1Bytes));
				await this.ApplyV1Async(Doc);
			}
			// else: No branding found
		}

		/// <summary>
		/// Returns the image URI for the given ID, or empty string if not found.
		/// </summary>
		/// <param name="Id">The image identifier.</param>
		public string GetImageUri(string Id)
		{
			return this.ImageUrisMap.TryGetValue(Id, out Uri? UriVal)
				? UriVal.AbsoluteUri
				: string.Empty;
		}

		private void RemoveLocalDictionaries(ICollection<ResourceDictionary> MergedDictionaries)
		{
			foreach (ResourceDictionary Dict in MergedDictionaries.Where(D => D.ContainsKey(localFlagKey)).ToList())
				MergedDictionaries.Remove(Dict);
		}

		private void RemoveProviderDictionaries(ICollection<ResourceDictionary> MergedDictionaries)
		{
			foreach (ResourceDictionary Dict in MergedDictionaries.Where(D => D.ContainsKey(providerFlagKey)).ToList())
				MergedDictionaries.Remove(Dict);
		}

		private async Task ApplyV2Async(XmlDocument Doc)
		{
			XmlElement? Root = Doc.DocumentElement;
			if (Root is null || Root.NamespaceURI != "urn:neuroaccess:branding:2.0")
				return;

			this.ImageUrisMap.Clear();
			XmlNodeList? Images = Root.SelectNodes("//*[local-name()='ImageRef']");
			if (Images != null)
			{
				foreach (XmlElement Node in Images.OfType<XmlElement>())
				{
					string Id = Node.GetAttribute("id");
					string UriText = Node.GetAttribute("uri");
					if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(UriText) && Uri.TryCreate(UriText, UriKind.Absolute, out Uri? ImgUri))
						this.ImageUrisMap[Id] = ImgUri;
				}
			}

			// Find all ColorsUri for light/dark
			XmlNodeList? ColorUris = Root.SelectNodes("//*[local-name()='ColorsUri']");
			Uri? LightUri = null;
			Uri? DarkUri = null;
			foreach (XmlElement Node in ColorUris?.OfType<XmlElement>() ?? Enumerable.Empty<XmlElement>())
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
				ICollection<ResourceDictionary> MergedDictionaries = Application.Current?.Resources.MergedDictionaries;
				if (MergedDictionaries == null)
					return;

				// Remove both local from merged dicts
				if (this.localLightDict != null) MergedDictionaries.Remove(this.localLightDict);
				if (this.localDarkDict != null) MergedDictionaries.Remove(this.localDarkDict);

				this.localLightDict ??= new Light();
				this.localDarkDict ??= new Dark();
				this.localLightDict.Clear();
				this.localDarkDict.Clear();

				if (LightUri != null)
				{
					ResourceDictionary? ProviderLightDict = await this.LoadProviderDictionaryAsync(LightUri, "Light");
					if (ProviderLightDict != null)
					{
						foreach (string Key in ProviderLightDict.Keys.OfType<string>())
							this.localLightDict[Key] = ProviderLightDict[Key];
					}
				}
				if (DarkUri != null)
				{
					ResourceDictionary? ProviderDarkDict = await this.LoadProviderDictionaryAsync(DarkUri, "Dark");
					if (ProviderDarkDict != null)
					{
						foreach (string Key in ProviderDarkDict.Keys.OfType<string>())
							this.localDarkDict[Key] = ProviderDarkDict[Key];
					}
				}

				this.localLightDict[localFlagKey] = true;
				this.localDarkDict[localFlagKey] = true;

				AppTheme CurrentTheme = await this.GetTheme();
				if (CurrentTheme == AppTheme.Dark)
					MergedDictionaries.Add(this.localDarkDict);
				else
					MergedDictionaries.Add(this.localLightDict);

				this.LastAppliedLocalTheme = CurrentTheme;
			});
		}

		private async Task ApplyV1Async(XmlDocument Doc)
		{
			XmlElement? Root = Doc.DocumentElement;
			if (Root is null)
				return;

			this.ImageUrisMap.Clear();
			XmlNodeList? Images = Root.SelectNodes("//*[local-name()='ImageRef']");
			if (Images != null)
			{
				foreach (XmlElement Node in Images.OfType<XmlElement>())
				{
					string Id = Node.GetAttribute("id");
					string UriText = Node.GetAttribute("uri");
					if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(UriText) && Uri.TryCreate(UriText, UriKind.Absolute, out Uri? ImgUri))
						this.ImageUrisMap[Id] = ImgUri;
				}
			}

			XmlElement? ColorsNode = Root.SelectSingleNode("//*[local-name()='ColorsUri']") as XmlElement;
			if (ColorsNode is null)
				return;

			Uri ColorsUri = new Uri(ColorsNode.InnerText.Trim());

			ResourceDictionary? OrigDict = await this.LoadProviderDictionaryAsync(ColorsUri, "V1");
			if (OrigDict == null)
				return;

			Dictionary<string, object> LightDict = new Dictionary<string, object>();
			Dictionary<string, object> DarkDict = new Dictionary<string, object>();

			foreach (string Key in OrigDict.Keys.OfType<string>())
			{
				if (Key.EndsWith("Light", StringComparison.OrdinalIgnoreCase))
					LightDict[Key.Substring(0, Key.Length - 5)] = OrigDict[Key];
				else if (Key.EndsWith("Dark", StringComparison.OrdinalIgnoreCase))
					DarkDict[Key.Substring(0, Key.Length - 4)] = OrigDict[Key];
				else
				{
					LightDict[Key] = OrigDict[Key];
					DarkDict[Key] = OrigDict[Key];
				}
			}

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				ICollection<ResourceDictionary> MergedDictionaries = Application.Current?.Resources.MergedDictionaries;
				if (MergedDictionaries == null)
					return;

				// Remove both local from merged dicts
				if (this.localLightDict != null) MergedDictionaries.Remove(this.localLightDict);
				if (this.localDarkDict != null) MergedDictionaries.Remove(this.localDarkDict);

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
				if (CurrentTheme == AppTheme.Dark)
					MergedDictionaries.Add(this.localDarkDict);
				else
					MergedDictionaries.Add(this.localLightDict);

				this.LastAppliedLocalTheme = CurrentTheme;
			});
		}

		private async Task<ResourceDictionary?> LoadProviderDictionaryAsync(Uri Uri, string ThemeTag)
		{
			string Key = Uri.AbsoluteUri;
			byte[]? XamlBytes = await this.FetchOrGetCachedAsync(Uri, Key);
			if (XamlBytes == null)
				return null;

			try
			{
				ResourceDictionary Dict = new ResourceDictionary().LoadFromXaml(Encoding.UTF8.GetString(XamlBytes));
				Dict.Add(providerFlagKey, true);
				Dict.Add("Theme", ThemeTag); // Optional: for debugging
				return Dict;
			}
			catch
			{
				// Log or handle error loading provider dictionary
				return null;
			}
		}

		private async Task<byte[]?> FetchOrGetCachedAsync(Uri Uri, string Key)
		{
			(byte[]? Cached, string _) = await this.CacheManager.TryGet(Key);
			if (Cached != null)
				return Cached;

			(byte[]? Fetched, string _) = await ServiceRef.InternetCacheService.GetOrFetch(Uri, ServiceRef.TagProfile.PubSubJid!, true);
			if (Fetched != null)
			{
				await this.CacheManager.AddOrUpdate(Key, ServiceRef.TagProfile.PubSubJid!, true, Fetched, "application/xml");
			}

			return Fetched;
		}
	}
}
