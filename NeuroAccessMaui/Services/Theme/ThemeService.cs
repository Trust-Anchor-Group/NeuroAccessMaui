using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using CommunityToolkit.Maui.Core;
using NeuroAccessMaui.Resources.Styles;
using NeuroAccessMaui.Services.Cache;
using NeuroAccessMaui.UI;
using Waher.Content.Xsl;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Manages application theming. Applies bundled (local) light/dark themes and, when a provider
	/// domain is available, downloads & applies remote branding descriptors (V2 preferred, V1 fallback).
	/// Performs XML schema validation, merges resource dictionaries, extracts image references, and
	/// implements a retry/backoff policy (0s,2s,5s) for transient network issues. Idempotent per domain:
	/// once Applied / NotSupported / FailedPermanent, subsequent calls are no-ops. Falls back to local
	/// theme on unsupported or permanent failures.
	/// </summary>
	[Singleton]
	public sealed class ThemeService : IThemeService, IDisposable
	{
		private const string providerFlagKey = "IsServerThemeDictionary";
		private const string localFlagKey = "IsLocalThemeDictionary";
		private static readonly TimeSpan themeExpiry = Constants.Cache.DefaultImageCache;

		private static readonly Lazy<Task<XmlSchema>> brandingSchemaV1Lazy = new(async () =>
		{
			using Stream Stream1 = await FileSystem.OpenAppPackageFileAsync("NeuroAccessBrandingV1.xsd");
			return XSL.LoadSchema(Stream1, "NeuroAccessBrandingV1.xsd");
		});
		private static readonly Lazy<Task<XmlSchema>> brandingSchemaV2Lazy = new(async () =>
		{
			using Stream Stream2 = await FileSystem.OpenAppPackageFileAsync("NeuroAccessBrandingV2.xsd");
			return XSL.LoadSchema(Stream2, "NeuroAccessBrandingV2.xsd");
		});

		// Provider theme application state & retry
		private enum ProviderThemeStatus { NotStarted, InProgress, Applied, NotSupported, FailedPermanent }
		private enum ProviderAttemptOutcome { AppliedV2, AppliedV1, Unsupported, TransientFailure, PermanentFailure }
		private enum BrandingFetchClassification { Success, NotFound, TransientFailure, PermanentFailure }
		private ProviderThemeStatus providerThemeState = ProviderThemeStatus.NotStarted;
		private string? lastDomainAttempted;
		private int providerThemeAttemptCount;
		private static readonly TimeSpan[] providerRetryDelays = { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) };
		private const int maxProviderThemeAttempts = 3;
		private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

		private readonly FileCacheManager cacheManager;
		private readonly Dictionary<string, Uri> imageUrisMap;
		private SemaphoreSlim? themeSemaphore;
		private ResourceDictionary? localLightDict = new Light();
		private ResourceDictionary? localDarkDict = new Dark();
		private AppTheme? lastAppliedLocalTheme;
		private bool disposedValue;

		/// <summary>
		/// Initializes a new <see cref="ThemeService"/> instance with cache manager and synchronization primitives.
		/// </summary>
		public ThemeService()
		{
			this.themeSemaphore = new SemaphoreSlim(1, 1);
			this.cacheManager = new FileCacheManager("BrandingThemes", themeExpiry);
			this.imageUrisMap = new(StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Current mapping of branding image identifiers to absolute URIs (empty if no provider theme applied).
		/// </summary>
		public IReadOnlyDictionary<string, Uri> ImageUris => new Dictionary<string, Uri>(this.imageUrisMap);

		/// <summary>
		/// Returns the current user application theme (may be <see cref="AppTheme.Unspecified"/> if unset).
		/// </summary>
		public Task<AppTheme> GetTheme() => Task.FromResult(Application.Current?.UserAppTheme ?? AppTheme.Unspecified);

		/// <summary>
		/// Completes when a provider theme application attempt (success, unsupported, or failure) finishes.
		/// </summary>
		public TaskCompletionSource ThemeLoaded { get; } = new();

		/// <summary>
		/// Sets the desired application theme and applies its local resource dictionary. If Unspecified,
		/// the system requested theme is used. Also updates status bar appearance when supported.
		/// </summary>
		/// <param name="Theme">Desired theme or <see cref="AppTheme.Unspecified"/>.</param>
		public void SetTheme(AppTheme Theme)
		{
			if (Theme is AppTheme.Unspecified)
				Theme = Application.Current!.RequestedTheme;
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					ServiceRef.TagProfile.Theme = Theme;
					Application.Current!.UserAppTheme = Theme;
					this.SetLocalTheme(Theme);
					// Platform specific status bar adjustments (iOS & MacCatalyst with supported versions)
#if IOS || MACCATALYST
					try
					{
						CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(AppColors.PrimaryBackground);
						CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(Theme == AppTheme.Dark ? StatusBarStyle.LightContent : StatusBarStyle.DarkContent);
					}
					catch (Exception)
					{
						// Non-fatal: ignore status bar styling issues on unsupported platforms/versions.
					}
#endif
				}
				catch (Exception Ex)
				{ ServiceRef.LogService.LogException(new Exception($"SetTheme failed for theme {Theme}.", Ex)); }
			});
		}

		/// <summary>
		/// Applies only bundled local theme dictionaries for the selected theme (no remote branding fetch).
		/// </summary>
		/// <param name="Theme">Theme to apply locally.</param>
		public void SetLocalTheme(AppTheme Theme)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					ICollection<ResourceDictionary> Merged = Application.Current!.Resources.MergedDictionaries;
					this.RemoveAllThemeDictionaries(Merged, this.localLightDict);
					this.RemoveAllThemeDictionaries(Merged, this.localDarkDict);
					this.localLightDict ??= new Light();
					this.localDarkDict ??= new Dark();
					if (Theme == AppTheme.Dark && !Merged.Contains(this.localDarkDict))
						Merged.Add(this.localDarkDict);
					else if (Theme != AppTheme.Dark && !Merged.Contains(this.localLightDict))
						Merged.Add(this.localLightDict);
					this.lastAppliedLocalTheme = Theme;
				}
				catch (Exception Ex)
				{ ServiceRef.LogService.LogException(new Exception($"SetLocalTheme failed for theme {Theme}.", Ex)); }
			});
		}

		/// <summary>
		/// Attempts to fetch and apply provider branding (V2 then V1) with retries for transient failures.
		/// Falls back to local theme for unsupported or permanent failures. Safe to call multiple times;
		/// after reaching a final state for a domain subsequent calls are ignored.
		/// </summary>
		public async Task ApplyProviderTheme()
		{
			if (this.themeSemaphore is null)
				return; // disposed
			await this.themeSemaphore.WaitAsync();
			try
			{
				string? Domain = ServiceRef.TagProfile.Domain;
				if (string.IsNullOrWhiteSpace(Domain))
				{
					ServiceRef.LogService.LogDebug("ApplyProviderTheme: Skipped (no domain). ");
					return;
				}
				if (this.lastDomainAttempted is not null && this.lastDomainAttempted.Equals(Domain, StringComparison.OrdinalIgnoreCase) &&
					this.providerThemeState is ProviderThemeStatus.Applied or ProviderThemeStatus.NotSupported or ProviderThemeStatus.FailedPermanent)
				{
					ServiceRef.LogService.LogDebug($"ApplyProviderTheme: Already finalized for {Domain} state={this.providerThemeState}.");
					return;
				}
				this.providerThemeState = ProviderThemeStatus.InProgress;
				this.lastDomainAttempted = Domain;
				this.providerThemeAttemptCount = 0;
				while (this.providerThemeAttemptCount < maxProviderThemeAttempts && this.providerThemeState == ProviderThemeStatus.InProgress)
				{
					int Attempt = ++this.providerThemeAttemptCount;
					TimeSpan Delay = providerRetryDelays[Math.Min(Attempt - 1, providerRetryDelays.Length - 1)];
					if (Delay > TimeSpan.Zero)
						await Task.Delay(Delay);
					ProviderAttemptOutcome Outcome = await this.TryFetchAndApplyProviderThemeAsync(Domain);
					switch (Outcome)
					{
						case ProviderAttemptOutcome.AppliedV2:
						case ProviderAttemptOutcome.AppliedV1:
							this.providerThemeState = ProviderThemeStatus.Applied;
							ServiceRef.LogService.LogDebug($"ApplyProviderTheme: Success {Outcome} attempt {Attempt} domain {Domain}.");
							break;
						case ProviderAttemptOutcome.Unsupported:
							this.providerThemeState = ProviderThemeStatus.NotSupported;
							ServiceRef.LogService.LogDebug($"ApplyProviderTheme: Unsupported domain {Domain}.");
							await this.SetLocalThemeFromBackgroundThread();
							break;
						case ProviderAttemptOutcome.TransientFailure:
							if (Attempt >= maxProviderThemeAttempts)
							{
								this.providerThemeState = ProviderThemeStatus.FailedPermanent;
								ServiceRef.LogService.LogDebug($"ApplyProviderTheme: Exhausted retries domain {Domain}.");
								await this.SetLocalThemeFromBackgroundThread();
							}
							else
								ServiceRef.LogService.LogDebug($"ApplyProviderTheme: Transient failure attempt {Attempt} domain {Domain} retrying.");
							break;
						case ProviderAttemptOutcome.PermanentFailure:
							this.providerThemeState = ProviderThemeStatus.FailedPermanent;
							ServiceRef.LogService.LogDebug($"ApplyProviderTheme: Permanent failure domain {Domain}.");
							await this.SetLocalThemeFromBackgroundThread();
							break;
					}
				}
			}
			finally
			{
				this.themeSemaphore?.Release();
				this.ThemeLoaded.TrySetResult();
			}
		}

		private async Task SetLocalThemeFromBackgroundThread()
		{
			try
			{
				this.SetLocalTheme(await this.GetTheme());
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(new Exception("SetLocalThemeFromBackgroundThread failed.", Ex));
			}
		}

		private async Task<ProviderAttemptOutcome> TryFetchAndApplyProviderThemeAsync(string Domain)
		{
			(bool Success, XmlDocument? Document, BrandingFetchClassification Classification) V2 = await this.TryGetBrandingXmlAsync(Domain, true);
			if (V2.Success && V2.Document is not null)
			{
				await this.ApplyV2Async(V2.Document);
				return ProviderAttemptOutcome.AppliedV2;
			}
			if (V2.Classification == BrandingFetchClassification.TransientFailure)
				return ProviderAttemptOutcome.TransientFailure;
			if (V2.Classification == BrandingFetchClassification.PermanentFailure)
				return ProviderAttemptOutcome.PermanentFailure;
			(bool Success, XmlDocument? Document, BrandingFetchClassification Classification) V1 = await this.TryGetBrandingXmlAsync(Domain, false);
			if (V1.Success && V1.Document is not null)
			{
				await this.ApplyV1Async(V1.Document);
				return ProviderAttemptOutcome.AppliedV1;
			}
			return V1.Classification switch
			{
				BrandingFetchClassification.NotFound => ProviderAttemptOutcome.Unsupported,
				BrandingFetchClassification.TransientFailure => ProviderAttemptOutcome.TransientFailure,
				_ => ProviderAttemptOutcome.PermanentFailure
			};
		}

		private async Task<(bool Success, XmlDocument? Document, BrandingFetchClassification Classification)> TryGetBrandingXmlAsync(string Domain, bool IsV2)
		{
			Uri Uri = BuildBrandingItemUrl(Domain, IsV2 ? "BrandingV2" : "Branding");
			string Key = Uri.AbsoluteUri;
			byte[]? Bytes = await this.FetchOrGetCachedAsync(Uri, Key);
			if (Bytes is not null)
			{
				try
				{
					XmlDocument Doc = new(); Doc.LoadXml(Encoding.UTF8.GetString(Bytes));
					bool Valid = await this.ValidateBrandingXmlAsync(Doc, IsV2);
					if (!Valid) return (false, null, BrandingFetchClassification.PermanentFailure);
					return (true, Doc, BrandingFetchClassification.Success);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(new Exception($"TryGetBrandingXmlAsync parse {(IsV2 ? "V2" : "V1")} {Uri}", Ex));
					return (false, null, BrandingFetchClassification.PermanentFailure);
				}
			}
			HttpStatusCode Probe = await ProbeUriAsync(Uri);
			return Probe switch
			{
				HttpStatusCode.NotFound => (false, null, BrandingFetchClassification.NotFound),
				HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout or HttpStatusCode.BadGateway => (false, null, BrandingFetchClassification.TransientFailure),
				HttpStatusCode.RequestTimeout => (false, null, BrandingFetchClassification.TransientFailure),
				0 => (false, null, BrandingFetchClassification.TransientFailure),
				_ => (false, null, BrandingFetchClassification.PermanentFailure)
			};
		}

		private static async Task<HttpStatusCode> ProbeUriAsync(Uri Uri)
		{
			try
			{
				using HttpRequestMessage Req = new HttpRequestMessage(HttpMethod.Head, Uri);
				using HttpResponseMessage Resp = await httpClient.SendAsync(Req); return Resp.StatusCode;
			}
			catch (HttpRequestException Ex) when (IsTransientNetwork(Ex))
			{
				return 0;
			}
			catch (TaskCanceledException)
			{
				return 0;
			}
			catch (Exception)
			{
				return 0;
			}
		}
		private static bool IsTransientNetwork(Exception Ex) => Ex is HttpRequestException or SocketException or TaskCanceledException or TimeoutException;

		private static Uri BuildBrandingItemUrl(string Domain, string ItemId) => new($"https://{Domain}/PubSub/NeuroAccessBranding/{ItemId}");
		/// <summary>
		/// Returns the absolute URI string for a branding image reference id, or empty string if not present.
		/// </summary>
		/// <param name="Id">Branding image identifier.</param>
		public string GetImageUri(string Id) => this.imageUrisMap.TryGetValue(Id, out Uri? ImageUri) ? ImageUri.AbsoluteUri : string.Empty;

		private async Task ApplyV2Async(XmlDocument Doc)
		{
			XmlElement? Root = Doc.DocumentElement; if (Root is null || Root.NamespaceURI != "urn:neuroaccess:branding:2.0") return;
			this.imageUrisMap.Clear();
			foreach (XmlElement Node in Root.SelectNodes("//*[local-name()='ImageRef']")?.OfType<XmlElement>() ?? [])
			{
				string Id = Node.GetAttribute("id");
				string UriText = Node.GetAttribute("uri");
				if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(UriText) && Uri.TryCreate(UriText, UriKind.Absolute, out Uri? ImgUri))
					this.imageUrisMap[Id] = ImgUri;
			}
			Uri? LightUri = null;
			Uri? DarkUri = null;
			foreach (XmlElement Node in Root.SelectNodes("//*[local-name()='ColorsUri']")?.OfType<XmlElement>() ?? [])
			{
				string ThemeName = Node.GetAttribute("theme").ToLowerInvariant();
				string UriText2 = Node.InnerText.Trim();
				if (ThemeName == "light")
					LightUri = new Uri(UriText2);
				else if (ThemeName == "dark")
					DarkUri = new Uri(UriText2);
			}
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				ICollection<ResourceDictionary>? Merged = Application.Current?.Resources.MergedDictionaries;
				if (Merged is null)
					return;
				this.RemoveAllThemeDictionaries(Merged, this.localLightDict);
				this.RemoveAllThemeDictionaries(Merged, this.localDarkDict);
				this.localLightDict ??= new Light();
				this.localDarkDict ??= new Dark();
				this.localLightDict.Clear();
				this.localDarkDict.Clear();
				if (LightUri is not null)
				{
					ResourceDictionary? LightDict = await this.LoadProviderDictionaryAsync(LightUri, "Light");
					if (LightDict is not null) foreach (string K in LightDict.Keys.OfType<string>()) this.localLightDict[K] = LightDict[K];
				}
				if (DarkUri is not null)
				{
					ResourceDictionary? DarkDict = await this.LoadProviderDictionaryAsync(DarkUri, "Dark");
					if (DarkDict is not null) foreach (string K in DarkDict.Keys.OfType<string>()) this.localDarkDict[K] = DarkDict[K];
				}
				this.localLightDict[localFlagKey] = true;
				this.localDarkDict[localFlagKey] = true;
				AppTheme Current = await this.GetTheme();
				if (Current == AppTheme.Dark && !Merged.Contains(this.localDarkDict))
					Merged.Add(this.localDarkDict);
				else if (Current != AppTheme.Dark && !Merged.Contains(this.localLightDict))
					Merged.Add(this.localLightDict);
				this.lastAppliedLocalTheme = Current;
			});
		}

		private async Task ApplyV1Async(XmlDocument Doc)
		{
			XmlElement? Root = Doc.DocumentElement;
			if (Root is null)
				return;
			this.imageUrisMap.Clear();
			foreach (XmlElement Node in Root.SelectNodes("//*[local-name()='ImageRef']")?.OfType<XmlElement>() ?? [])
			{
				string Id = Node.GetAttribute("id"); string UriText = Node.GetAttribute("uri");
				if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(UriText) && Uri.TryCreate(UriText, UriKind.Absolute, out Uri? ImgUri)) this.imageUrisMap[Id] = ImgUri;
			}
			if (Root.SelectSingleNode("//*[local-name()='ColorsUri']") is not XmlElement ColorsNode) 
				return;
			Uri ColorsUri = new(ColorsNode.InnerText.Trim());
			ResourceDictionary? Orig = await this.LoadProviderDictionaryAsync(ColorsUri, "V1");
			if (Orig is null)
				return;
			Dictionary<string, object> Light = [];
			 Dictionary<string, object> Dark = [];
			foreach (string K in Orig.Keys.OfType<string>())
			{
				if (K.EndsWith("Light", StringComparison.OrdinalIgnoreCase)) Light[K[..^5]] = Orig[K];
				else if (K.EndsWith("Dark", StringComparison.OrdinalIgnoreCase)) Dark[K[..^4]] = Orig[K];
				else { Light[K] = Orig[K]; Dark[K] = Orig[K]; }
			}
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				ICollection<ResourceDictionary>? Merged = Application.Current?.Resources.MergedDictionaries;
				if (Merged is null)
					return;
				this.RemoveAllThemeDictionaries(Merged, this.localLightDict);
				this.RemoveAllThemeDictionaries(Merged, this.localDarkDict);
				this.localLightDict ??= new Light(); this.localDarkDict ??= new Dark();
				this.localLightDict.Clear();
				this.localDarkDict.Clear();
				foreach (KeyValuePair<string, object> Kv in Light)
					this.localLightDict[Kv.Key] = Kv.Value;
				foreach (KeyValuePair<string, object> Kv in Dark)
					this.localDarkDict[Kv.Key] = Kv.Value;
				this.localLightDict[localFlagKey] = true;
				this.localDarkDict[localFlagKey] = true;
				AppTheme Current = await this.GetTheme();
				if (Current == AppTheme.Dark && !Merged.Contains(this.localDarkDict))
					Merged.Add(this.localDarkDict);
				else if (Current != AppTheme.Dark && !Merged.Contains(this.localLightDict))
					Merged.Add(this.localLightDict);
				this.lastAppliedLocalTheme = Current;
			});
		}

		private async Task<ResourceDictionary?> LoadProviderDictionaryAsync(Uri Uri, string Tag)
		{
			byte[]? Bytes = await this.FetchOrGetCachedAsync(Uri, Uri.AbsoluteUri);
			if (Bytes is null)
				return null;
			try
			{
				ResourceDictionary Dict = new ResourceDictionary().LoadFromXaml(Encoding.UTF8.GetString(Bytes));
				Dict.TryAdd(providerFlagKey, true); Dict.TryAdd("Theme", Tag);
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
			try
			{
				(byte[]? Cached, _) = await this.cacheManager.TryGet(Key); if (Cached is not null) return Cached;
				(byte[]? Fetched, _) = await ServiceRef.InternetCacheService.GetOrFetch(Uri, ServiceRef.TagProfile.PubSubJid!, true);
				if (Fetched is not null)
					await this.cacheManager.AddOrUpdate(Key, ServiceRef.TagProfile.PubSubJid!, true, Fetched, "application/xml");
				return Fetched;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(new Exception($"FetchOrGetCachedAsync failed for {Uri}", Ex));
				return null;
			}
		}

		private async Task<bool> ValidateBrandingXmlAsync(XmlDocument Doc, bool V2)
		{
			try
			{
				XmlSchema Schema = V2 ? await brandingSchemaV2Lazy.Value : await brandingSchemaV1Lazy.Value;
				XSL.Validate("BrandingDescriptor", Doc, Schema); return true;
			}
			catch (Exception Ex)
			{ ServiceRef.LogService.LogException(new Exception($"XSD validation failed for schema version {(V2 ? "V2" : "V1")}.", Ex)); return false; }
		}

		private void RemoveAllThemeDictionaries(ICollection<ResourceDictionary> Merged, ResourceDictionary? Instance)
		{
			if (Instance is not null) while (Merged.Contains(Instance)) Merged.Remove(Instance);
			foreach (ResourceDictionary? D in Merged.Where(D => D.ContainsKey(localFlagKey) || D.ContainsKey(providerFlagKey)).ToList()) Merged.Remove(D);
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					this.themeSemaphore?.Dispose();
					this.themeSemaphore = null;
				}
				this.disposedValue = true;
			}
		}
		/// <summary>
		/// Disposes the service, releasing synchronization resources.
		/// </summary>
		public void Dispose() { this.Dispose(true); GC.SuppressFinalize(this); }
	}
}