using System.Reflection;
using System.Net;
using System.Text;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using Waher.Runtime.Inventory;
using Waher.Persistence;
using NeuroAccessMaui.Services.Fetch;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Default implementation of <see cref="IKycService"/>.
	/// </summary>
	[Singleton]
	public class KycService : IKycService
	{
		private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
		private static readonly string backupKyc = "TestKYCK.xml";

		/// <inheritdoc/>
		public async Task<KycReference> LoadKycReferenceAsync(string? Lang = null)
		{
			KycReference? Reference;

			try
			{
				Reference = await ServiceRef.StorageService.FindFirstDeleteRest<KycReference>();
			}
			catch (Exception FindEx)
			{
				ServiceRef.LogService.LogException(FindEx, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				Reference = null;
			}

			if (Reference is null || string.IsNullOrEmpty(Reference.ObjectId))
			{
				Reference = new KycReference
				{
					CreatedUtc = DateTime.UtcNow
				};
				try
				{
					await ServiceRef.StorageService.Insert(Reference);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}

				// Try to fetch KYC XML from provider first, fallback to embedded resource
				string? Xml = await this.TryFetchKycXmlFromProvider();
				if (string.IsNullOrEmpty(Xml))
				{
					using Stream Stream = await FileSystem.OpenAppPackageFileAsync(backupKyc);
					using StreamReader Reader = new(Stream);
					Xml = await Reader.ReadToEndAsync().ConfigureAwait(false);
				}
				Reference.KycXml = Xml;
				Reference.UpdatedUtc = DateTime.UtcNow;
				Reference.FetchedUtc = DateTime.UtcNow;
			}

			// Populate localized friendly name from process, if available
			try
			{
				KycProcess? Process = await Reference.GetProcess(Lang).ConfigureAwait(false);
				if (Process?.Name is not null)
				{
					string NewName = Process.Name.Text;
					if (!string.Equals(Reference.FriendlyName, NewName, StringComparison.Ordinal))
					{
						Reference.FriendlyName = NewName;
						// Persist update non-critically
						try { await ServiceRef.StorageService.Update(Reference); } catch { /* ignore */ }
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			}

			// 3) Load default if not available.
			return Reference;
		}

		public async Task SaveKycReferenceAsync(KycReference Reference)
		{
			try
			{
				if (string.IsNullOrEmpty(Reference.ObjectId))
					await ServiceRef.StorageService.Insert(Reference);
				else
					await ServiceRef.StorageService.Update(Reference);
			}
			catch (KeyNotFoundException)
			{
				await ServiceRef.StorageService.Insert(Reference);
			}
		}

		/// <summary>
		/// Loads available KYC process references from the provider, falling back to the bundled test KYC when unavailable.
		/// </summary>
		/// <param name="Lang">Optional language code used for resolving localized process name.</param>
		/// <returns>A read-only list of <see cref="KycReference"/> items representing available processes.</returns>
		public async Task<IReadOnlyList<KycReference>> LoadAvailableKycReferencesAsync(string? Lang = null)
		{
			try
			{
				string? xml = await this.TryFetchKycXmlFromProvider();
				if (string.IsNullOrEmpty(xml))
				{
					using Stream stream = await FileSystem.OpenAppPackageFileAsync(backupKyc);
					using StreamReader reader = new(stream);
					xml = await reader.ReadToEndAsync().ConfigureAwait(false);
				}

				KycProcess process = await KycProcessParser.LoadProcessAsync(xml, Lang).ConfigureAwait(false);
				KycReference reference = KycReference.FromProcess(process, xml, process.Name?.Text);
				return new List<KycReference> { reference };
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return Array.Empty<KycReference>();
			}
		}

		private static string GetFileName(string Resource)
		{
			int Index = Resource.LastIndexOf("Raw.", StringComparison.OrdinalIgnoreCase);
			return Index >= 0 ? Resource[(Index + 4)..] : Resource;
		}

		private async Task<string?> TryFetchKycXmlFromProvider()
		{
			try
			{
				string? Domain = ServiceRef.TagProfile.Domain;
				if (string.IsNullOrWhiteSpace(Domain))
					return null;

				Uri uri = new($"https://{Domain}/PubSub/NeuroAccessKyc/KycProcess");
				IResourceFetcher fetcher = new ResourceFetcher();
				ResourceFetchOptions opts = new() { ParentId = $"KycProcess:{Domain}", Permanent = false };
				ResourceResult<byte[]> result = await fetcher.GetBytesAsync(uri, opts).ConfigureAwait(false);
				byte[]? bytes = result.Value;
				if (bytes is null || bytes.Length == 0)
					return null;
				return Encoding.UTF8.GetString(bytes);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return null;
			}
		}

	}
}
