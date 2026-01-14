using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using CommunityToolkit.Maui.Core;
using NeuroAccessMaui.Resources.Styles;
using NeuroAccessMaui.Services.Cache;
using NeuroAccessMaui.UI;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Theme
{
	/// <summary>
	/// Manages application theming. Applies bundled (local) light/dark themes and, when a provider
	/// domain is available, downloads &amp; applies remote branding descriptors (V2 preferred, V1 fallback).
	/// Performs XML schema validation, merges resource dictionaries, extracts image references, and
	/// supports background refresh with short timeouts. Idempotent per domain: once Applied /
	/// NotSupported / FailedPermanent, subsequent calls are no-ops. Falls back to local theme on
	/// unsupported or permanent failures.
	/// </summary>
	[Singleton]
	public sealed class ThemeService : IThemeService, IDisposable
	{
		private const string providerFlagKey = "IsServerThemeDictionary";
		private const string localFlagKey = "IsLocalThemeDictionary";
		private static readonly TimeSpan themeExpiry = Constants.Cache.DefaultImageCache;

		// Keys used for validation (registered in MauiProgram) now use the namespace URNs directly.
		private static readonly string brandingSchemaKeyV1 = Constants.Schemes.NeuroAccessBrandingV1;

		// Provider theme application state & refresh
		private enum ProviderThemeStatus { NotStarted, InProgress, Applied, NotSupported, FailedPermanent, FailedTransient }
		private enum BrandingFetchClassification { Success, NotFound, TransientFailure, PermanentFailure }
		private ProviderThemeStatus providerThemeState = ProviderThemeStatus.NotStarted;
		private string? lastDomainAttempted;
		private Task? backgroundRefreshTask;
		private const string unsupportedCachePrefix = "BrandingUnsupported:";
		private static readonly TimeSpan blockingFetchTimeout = TimeSpan.FromSeconds(6);
		private static readonly TimeSpan backgroundFetchTimeout = TimeSpan.FromSeconds(2);
		private static readonly TimeSpan manualFetchTimeout = TimeSpan.FromSeconds(8);
		private static readonly TimeSpan probeTimeout = TimeSpan.FromSeconds(2);
		private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

		private readonly FileCacheManager cacheManager;
		private readonly Dictionary<string, Uri> imageUrisMap;
		private ResourceDictionary? localLightDict = new Light();
		private ResourceDictionary? localDarkDict = new Dark();
		private AppTheme? lastAppliedLocalTheme;
		private bool disposedValue;

		/// <summary>
		/// Initializes a new <see cref="ThemeService"/> instance with cache manager and synchronization primitives.
		/// </summary>
		public ThemeService()
		{
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
		/// Attempts to fetch and apply provider branding using the background refresh policy.
		/// </summary>
		public async Task ApplyProviderThemeAsync()
		{
			await this.ApplyProviderThemeAsync(ThemeFetchPolicy.BackgroundRefresh, CancellationToken.None);
		}

		/// <summary>
		/// Attempts to fetch and apply provider branding according to the specified policy.
		/// </summary>
		/// <param name="Policy">The fetch policy to apply.</param>
		/// <param name="CancellationToken">A token that can be used to cancel the operation.</param>
		/// <returns>An outcome describing how the provider theme was resolved.</returns>
		public async Task<ThemeApplyOutcome> ApplyProviderThemeAsync(ThemeFetchPolicy Policy, CancellationToken CancellationToken = default)
		{
			string? Domain = ServiceRef.TagProfile.Domain;
			if (string.IsNullOrWhiteSpace(Domain))
			{
				ServiceRef.LogService.LogDebug("ApplyProviderThemeAsync: Skipped (no domain). ");
				this.ThemeLoaded.TrySetResult();
				return ThemeApplyOutcome.SkippedNoDomain;
			}

			if (this.lastDomainAttempted is not null && this.lastDomainAttempted.Equals(Domain, StringComparison.OrdinalIgnoreCase) &&
				this.providerThemeState is ProviderThemeStatus.Applied or ProviderThemeStatus.NotSupported or ProviderThemeStatus.FailedPermanent)
			{
				ServiceRef.LogService.LogDebug($"ApplyProviderThemeAsync: Already finalized for {Domain} state={this.providerThemeState}.");
				this.ThemeLoaded.TrySetResult();
				return this.providerThemeState switch
				{
					ProviderThemeStatus.NotSupported => ThemeApplyOutcome.NotSupported,
					ProviderThemeStatus.FailedPermanent => ThemeApplyOutcome.FailedPermanent,
					_ => ThemeApplyOutcome.AppliedFromCache
				};
			}

			this.providerThemeState = ProviderThemeStatus.InProgress;
			this.lastDomainAttempted = Domain;

			try
			{
				if (Policy != ThemeFetchPolicy.ManualRefresh && await this.IsBrandingUnsupportedAsync(Domain))
				{
					ServiceRef.LogService.LogDebug($"ApplyProviderThemeAsync: Unsupported cached for {Domain}.");
					this.providerThemeState = ProviderThemeStatus.NotSupported;
					await this.SetLocalThemeFromBackgroundThread();
					return ThemeApplyOutcome.NotSupported;
				}

				(bool AppliedFromCache, bool CacheExpired) = await this.TryApplyCachedProviderThemeAsync(Domain, Policy, CancellationToken);
				if (AppliedFromCache)
				{
					this.providerThemeState = ProviderThemeStatus.Applied;
					if (Policy == ThemeFetchPolicy.ManualRefresh)
					{
						ThemeApplyOutcome RefreshOutcome = await this.FetchAndApplyProviderThemeAsync(Domain, Policy, false, CancellationToken, true);
						return RefreshOutcome == ThemeApplyOutcome.Applied ? ThemeApplyOutcome.Applied : ThemeApplyOutcome.AppliedFromCache;
					}
					if (CacheExpired)
						this.StartBackgroundRefresh(Domain);
					return ThemeApplyOutcome.AppliedFromCache;
				}

				if (Policy == ThemeFetchPolicy.BackgroundRefresh)
				{
					this.StartBackgroundRefresh(Domain);
					this.providerThemeState = ProviderThemeStatus.FailedTransient;
					await this.SetLocalThemeFromBackgroundThread();
					return ThemeApplyOutcome.FailedTransient;
				}

				bool ForceNetwork = Policy == ThemeFetchPolicy.ManualRefresh;
				ThemeApplyOutcome Outcome = await this.FetchAndApplyProviderThemeAsync(Domain, Policy, true, CancellationToken, ForceNetwork);
				this.providerThemeState = Outcome switch
				{
					ThemeApplyOutcome.Applied => ProviderThemeStatus.Applied,
					ThemeApplyOutcome.AppliedFromCache => ProviderThemeStatus.Applied,
					ThemeApplyOutcome.NotSupported => ProviderThemeStatus.NotSupported,
					ThemeApplyOutcome.FailedPermanent => ProviderThemeStatus.FailedPermanent,
					ThemeApplyOutcome.FailedTransient => ProviderThemeStatus.FailedTransient,
					_ => this.providerThemeState
				};
				return Outcome;
			}
			finally
			{
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

		private void StartBackgroundRefresh(string Domain)
		{
			if (!ServiceRef.NetworkService.IsOnline)
				return;
			if (this.backgroundRefreshTask is not null && !this.backgroundRefreshTask.IsCompleted)
				return;
			this.backgroundRefreshTask = Task.Run(async () =>
			{
				try
				{
					ThemeApplyOutcome Outcome = await this.FetchAndApplyProviderThemeAsync(Domain, ThemeFetchPolicy.BackgroundRefresh, false, CancellationToken.None, true);
					if (Outcome == ThemeApplyOutcome.Applied)
						this.providerThemeState = ProviderThemeStatus.Applied;
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(new Exception($"Background provider theme refresh failed for {Domain}.", Ex));
				}
			});
		}

		private async Task<(bool Applied, bool CacheExpired)> TryApplyCachedProviderThemeAsync(string Domain, ThemeFetchPolicy Policy, CancellationToken CancellationToken)
		{
			(bool Applied, bool CacheExpired) = await this.TryApplyCachedBrandingAsync(Domain, true, Policy, CancellationToken);
			if (Applied)
				return (true, CacheExpired);
			return await this.TryApplyCachedBrandingAsync(Domain, false, Policy, CancellationToken);
		}

		private async Task<(bool Applied, bool CacheExpired)> TryApplyCachedBrandingAsync(string Domain, bool IsV2, ThemeFetchPolicy Policy, CancellationToken CancellationToken)
		{
			(XmlDocument? Document, bool IsExpired) = await this.TryGetCachedBrandingXmlAsync(Domain, IsV2);
			if (Document is null)
				return (false, false);

			if (IsV2)
				await this.ApplyV2Async(Document, Policy, CancellationToken, false);
			else
				await this.ApplyV1Async(Document, Policy, CancellationToken, false);

			return (true, IsExpired);
		}

		private async Task<ThemeApplyOutcome> FetchAndApplyProviderThemeAsync(string Domain, ThemeFetchPolicy Policy, bool ApplyFallbackOnFailure, CancellationToken CancellationToken, bool ForceNetwork)
		{
			if (!ServiceRef.NetworkService.IsOnline)
				return this.HandleFetchFailure(ThemeApplyOutcome.FailedTransient, ApplyFallbackOnFailure);

			(bool Success, XmlDocument? Document, BrandingFetchClassification Classification) V2 = await this.TryGetBrandingXmlAsync(Domain, true, Policy, ForceNetwork, CancellationToken);
			if (V2.Success && V2.Document is not null)
			{
				await this.ApplyV2Async(V2.Document, Policy, CancellationToken, ForceNetwork);
				await this.ClearBrandingUnsupportedAsync(Domain);
				return ThemeApplyOutcome.Applied;
			}
			if (V2.Classification == BrandingFetchClassification.TransientFailure)
				return this.HandleFetchFailure(ThemeApplyOutcome.FailedTransient, ApplyFallbackOnFailure);
			if (V2.Classification == BrandingFetchClassification.PermanentFailure)
				return this.HandleFetchFailure(ThemeApplyOutcome.FailedPermanent, ApplyFallbackOnFailure);

			(bool Success, XmlDocument? Document, BrandingFetchClassification Classification) V1 = await this.TryGetBrandingXmlAsync(Domain, false, Policy, ForceNetwork, CancellationToken);
			if (V1.Success && V1.Document is not null)
			{
				await this.ApplyV1Async(V1.Document, Policy, CancellationToken, ForceNetwork);
				await this.ClearBrandingUnsupportedAsync(Domain);
				return ThemeApplyOutcome.Applied;
			}

			return V1.Classification switch
			{
				BrandingFetchClassification.NotFound => await this.HandleNotSupportedAsync(Domain, ApplyFallbackOnFailure),
				BrandingFetchClassification.TransientFailure => this.HandleFetchFailure(ThemeApplyOutcome.FailedTransient, ApplyFallbackOnFailure),
				_ => this.HandleFetchFailure(ThemeApplyOutcome.FailedPermanent, ApplyFallbackOnFailure)
			};
		}

		private async Task<ThemeApplyOutcome> HandleNotSupportedAsync(string Domain, bool ApplyFallbackOnFailure)
		{
			await this.MarkBrandingUnsupportedAsync(Domain);
			if (ApplyFallbackOnFailure)
				await this.SetLocalThemeFromBackgroundThread();
			return ThemeApplyOutcome.NotSupported;
		}

		private ThemeApplyOutcome HandleFetchFailure(ThemeApplyOutcome Outcome, bool ApplyFallbackOnFailure)
		{
			if (ApplyFallbackOnFailure)
			{
				_ = this.SetLocalThemeFromBackgroundThread();
			}
			return Outcome;
		}

		private async Task<bool> IsBrandingUnsupportedAsync(string Domain)
		{
			string Key = GetUnsupportedCacheKey(Domain);
			(byte[]? Data, string _, bool IsExpired) = await this.cacheManager.TryGetWithExpiry(Key);
			return Data is not null && !IsExpired;
		}

		private async Task MarkBrandingUnsupportedAsync(string Domain)
		{
			string Key = GetUnsupportedCacheKey(Domain);
			byte[] Data = Encoding.ASCII.GetBytes("1");
			await this.cacheManager.AddOrUpdate(Key, Domain, false, Data, "text/plain");
		}

		private Task<bool> ClearBrandingUnsupportedAsync(string Domain)
		{
			string Key = GetUnsupportedCacheKey(Domain);
			return this.cacheManager.Remove(Key);
		}

		private static string GetUnsupportedCacheKey(string Domain)
		{
			string KeyDomain = Domain.ToLowerInvariant();
			return $"{unsupportedCachePrefix}{KeyDomain}";
		}

		private static TimeSpan GetFetchTimeout(ThemeFetchPolicy Policy)
		{
			return Policy switch
			{
				ThemeFetchPolicy.BlockingFirstRun => blockingFetchTimeout,
				ThemeFetchPolicy.ManualRefresh => manualFetchTimeout,
				_ => backgroundFetchTimeout
			};
		}

		private async Task<(XmlDocument? Document, bool IsExpired)> TryGetCachedBrandingXmlAsync(string Domain, bool IsV2)
		{
			Uri Uri = BuildBrandingItemUrl(Domain, IsV2 ? "BrandingV2" : "Branding");
			string Key = Uri.AbsoluteUri;
			(byte[]? Cached, string _, bool IsExpired) = await this.cacheManager.TryGetWithExpiry(Key);
			if (Cached is null)
				return (null, false);

			string XmlString = Encoding.UTF8.GetString(Cached);
			bool Valid = await this.ValidateBrandingXmlAsync(XmlString, IsV2, Domain);
			if (!Valid)
			{
				await this.cacheManager.Remove(Key);
				return (null, false);
			}
			try
			{
				XmlDocument Doc = new();
				Doc.LoadXml(XmlString);
				return (Doc, IsExpired);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(new Exception($"TryGetCachedBrandingXmlAsync parse {(IsV2 ? "V2" : "V1")} {Uri}", Ex));
				await this.cacheManager.Remove(Key);
				return (null, false);
			}
		}

		private async Task<(bool Success, XmlDocument? Document, BrandingFetchClassification Classification)> TryGetBrandingXmlAsync(string Domain, bool IsV2, ThemeFetchPolicy Policy, bool ForceNetwork, CancellationToken CancellationToken)
		{
			Uri Uri = BuildBrandingItemUrl(Domain, IsV2 ? "BrandingV2" : "Branding");
			string Key = Uri.AbsoluteUri;
			(byte[]? Bytes, bool _, bool AttemptedNetwork) = await this.FetchOrGetCachedAsync(Uri, Key, Policy, ForceNetwork, CancellationToken);
			if (Bytes is not null)
			{
				string XmlString = Encoding.UTF8.GetString(Bytes);
				bool Valid = await this.ValidateBrandingXmlAsync(XmlString, IsV2, Domain);
				if (!Valid)
				{
					await this.cacheManager.Remove(Key);
					return (false, null, BrandingFetchClassification.PermanentFailure);
				}
				try
				{
					XmlDocument Doc = new();
					Doc.LoadXml(XmlString);
					return (true, Doc, BrandingFetchClassification.Success);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(new Exception($"TryGetBrandingXmlAsync parse {(IsV2 ? "V2" : "V1")} {Uri}", Ex));
					await this.cacheManager.Remove(Key);
					return (false, null, BrandingFetchClassification.PermanentFailure);
				}
			}
			if (!AttemptedNetwork)
				return (false, null, BrandingFetchClassification.TransientFailure);

			TimeSpan ProbeTimeout = GetFetchTimeout(Policy);
			if (ProbeTimeout > probeTimeout)
				ProbeTimeout = probeTimeout;
			HttpStatusCode Probe = await ProbeUriAsync(Uri, ProbeTimeout, CancellationToken);
			return Probe switch
			{
				HttpStatusCode.NotFound => (false, null, BrandingFetchClassification.NotFound),
				HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout or HttpStatusCode.BadGateway => (false, null, BrandingFetchClassification.TransientFailure),
				HttpStatusCode.RequestTimeout => (false, null, BrandingFetchClassification.TransientFailure),
				0 => (false, null, BrandingFetchClassification.PermanentFailure),
				_ => (false, null, BrandingFetchClassification.PermanentFailure)
			};
		}

		private static async Task<HttpStatusCode> ProbeUriAsync(Uri Uri, TimeSpan Timeout, CancellationToken CancellationToken)
		{
			try
			{
				using CancellationTokenSource LinkedCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
				LinkedCts.CancelAfter(Timeout);
				using HttpRequestMessage Req = new HttpRequestMessage(HttpMethod.Head, Uri);
				using HttpResponseMessage Resp = await httpClient.SendAsync(Req, LinkedCts.Token);
				return Resp.StatusCode;
			}
			catch (HttpRequestException Ex) when (IsTransientNetwork(Ex))
			{
				return 0;
			}
			catch (OperationCanceledException)
			{
				return 0;
			}
			catch (Exception)
			{
				return 0;
			}
		}
		private static bool IsTransientNetwork(Exception Ex) => Ex is HttpRequestException or SocketException or TaskCanceledException or TimeoutException;

		private static Uri BuildBrandingItemUrl(string Domain, string ItemId) => ServiceRef.TagProfile.DomainIsLocal() ?
			new($"http://{Domain}/PubSub/NeuroAccessBranding/{ItemId}")
			:
			new($"https://{Domain}/PubSub/NeuroAccessBranding/{ItemId}");

		/// <summary>
		/// Returns the absolute URI string for a branding image reference id, or empty string if not present.
		/// </summary>
		/// <param name="Id">Branding image identifier.</param>
		public string GetImageUri(string Id) => this.imageUrisMap.TryGetValue(Id, out Uri? ImageUri) ? ImageUri.AbsoluteUri : string.Empty;

		private async Task ApplyV2Async(XmlDocument Doc, ThemeFetchPolicy Policy, CancellationToken CancellationToken, bool ForceNetwork)
		{
			XmlElement? Root = Doc.DocumentElement;
			if (Root is null)
				return;
			if (System.Array.IndexOf(Constants.Schemes.BrandingV2NamespaceKeys, Root.NamespaceURI) < 0)
				return;
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
					ResourceDictionary? LightDict = await this.LoadProviderDictionaryAsync(LightUri, "Light", Policy, CancellationToken, ForceNetwork);
					if (LightDict is not null) foreach (string K in LightDict.Keys.OfType<string>()) this.localLightDict[K] = LightDict[K];
				}
				if (DarkUri is not null)
				{
					ResourceDictionary? DarkDict = await this.LoadProviderDictionaryAsync(DarkUri, "Dark", Policy, CancellationToken, ForceNetwork);
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

		private async Task ApplyV1Async(XmlDocument Doc, ThemeFetchPolicy Policy, CancellationToken CancellationToken, bool ForceNetwork)
		{
			XmlElement? Root = Doc.DocumentElement;
			if (Root is null || Root.NamespaceURI != Constants.Schemes.NeuroAccessBrandingV1)
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
			ResourceDictionary? Orig = await this.LoadProviderDictionaryAsync(ColorsUri, "V1", Policy, CancellationToken, ForceNetwork);
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

		private async Task<ResourceDictionary?> LoadProviderDictionaryAsync(Uri Uri, string Tag, ThemeFetchPolicy Policy, CancellationToken CancellationToken, bool ForceNetwork)
		{
			(byte[]? Bytes, bool _, bool _) = await this.FetchOrGetCachedAsync(Uri, Uri.AbsoluteUri, Policy, ForceNetwork, CancellationToken);
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

		private async Task<(byte[]? Data, bool CacheExpired, bool AttemptedNetwork)> FetchOrGetCachedAsync(Uri Uri, string Key, ThemeFetchPolicy Policy, bool ForceNetwork, CancellationToken CancellationToken)
		{
			try
			{
				if (CancellationToken.IsCancellationRequested)
					return (null, false, false);
				(byte[]? Cached, string _, bool IsExpired) = await this.cacheManager.TryGetWithExpiry(Key);
				if (Cached is not null && !ForceNetwork)
					return (Cached, IsExpired, false);

				if (!ServiceRef.NetworkService.IsOnline)
					return (Cached, IsExpired, false);

				TimeSpan Timeout = GetFetchTimeout(Policy);
				(byte[]? Fetched, _) = await ServiceRef.InternetCacheService.GetOrFetch(Uri, ServiceRef.TagProfile.PubSubJid!, false, Timeout);
				if (Fetched is not null)
				{
					await this.cacheManager.AddOrUpdate(Key, ServiceRef.TagProfile.PubSubJid!, false, Fetched, "application/xml");
					return (Fetched, false, true);
				}
				return (Cached, IsExpired, true);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(new Exception($"FetchOrGetCachedAsync failed for {Uri}", Ex));
				return (null, false, false);
			}
		}

		private async Task<bool> ValidateBrandingXmlAsync(string Xml, bool V2, string Domain)
		{
			try
			{
				if (V2)
				{
					foreach (string SchemaKey in Constants.Schemes.BrandingV2NamespaceKeys)
					{
						bool Valid = await ServiceRef.XmlSchemaValidationService.ValidateAsync(SchemaKey, Xml).ConfigureAwait(false);
						if (!Valid)
							continue;
						if (string.Equals(SchemaKey, Constants.Schemes.NeuroAccessBrandingV2Url, StringComparison.Ordinal))
						{
							ServiceRef.LogService.LogDebug("BrandingXmlValidationPrimarySuccess", new KeyValuePair<string, object?>("Domain", Domain));
						}
						else
						{
							ServiceRef.LogService.LogInformational(
								"BrandingXmlValidationFallbackSuccess",
								new KeyValuePair<string, object?>("Domain", Domain),
								new KeyValuePair<string, object?>("LegacyKey", SchemaKey));
						}
						return true;
					}

					ServiceRef.LogService.LogWarning(
						"BrandingXmlValidationBothFailed",
						new KeyValuePair<string, object?>("Domain", Domain),
						new KeyValuePair<string, object?>("PrimaryKey", Constants.Schemes.NeuroAccessBrandingV2Url),
						new KeyValuePair<string, object?>("LegacyKey", Constants.Schemes.NeuroAccessBrandingV2));
					return false;
				}

				bool ValidV1 = await ServiceRef.XmlSchemaValidationService.ValidateAsync(brandingSchemaKeyV1, Xml).ConfigureAwait(false);
				if (!ValidV1)
				{
					ServiceRef.LogService.LogWarning(
						"BrandingXmlValidationV1Failed",
						new KeyValuePair<string, object?>("Domain", Domain),
						new KeyValuePair<string, object?>("Key", brandingSchemaKeyV1));
				}
				return ValidV1;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(new Exception($"Branding XML validation failed for schema version {(V2 ? "V2" : "V1")}.", Ex), new KeyValuePair<string, object?>("Domain", Domain));
				return false;
			}
		}

		private void RemoveAllThemeDictionaries(ICollection<ResourceDictionary> Merged, ResourceDictionary? Instance)
		{
			if (Instance is not null) while (Merged.Contains(Instance)) Merged.Remove(Instance);
			foreach (ResourceDictionary? D in Merged.Where(D => D.ContainsKey(localFlagKey) || D.ContainsKey(providerFlagKey)).ToList()) Merged.Remove(D);
		}

		/// <summary>
		/// Clears locally cached branding descriptors for the current provider domain.
		/// </summary>
		public async Task<int> ClearBrandingCacheForCurrentDomain()
		{
			string? ParentId = ServiceRef.TagProfile.PubSubJid;
			if (string.IsNullOrWhiteSpace(ParentId))
				return 0;

			int Removed = await this.cacheManager.RemoveByParentId(ParentId);
			string? Domain = ServiceRef.TagProfile.Domain;
			if (!string.IsNullOrWhiteSpace(Domain))
				await this.cacheManager.Remove(GetUnsupportedCacheKey(Domain));
			return Removed;
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
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
