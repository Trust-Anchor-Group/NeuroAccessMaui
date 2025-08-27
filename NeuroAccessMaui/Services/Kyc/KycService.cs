using System.Reflection;
using System.Net;
using System.Text;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using Waher.Runtime.Inventory;
using Waher.Persistence;

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

				// Heuristic endpoint, similar to ThemeService PubSub scheme
				Uri Uri = new($"https://{Domain}/PubSub/NeuroAccessKyc/KycProcess");
				HttpStatusCode Probe = await ProbeUriAsync(Uri);
				if (Probe != HttpStatusCode.OK)
					return null;

				using HttpRequestMessage Req = new(HttpMethod.Get, Uri);
				using HttpResponseMessage Resp = await httpClient.SendAsync(Req);
				if (!Resp.IsSuccessStatusCode)
					return null;
				byte[] Bytes = await Resp.Content.ReadAsByteArrayAsync();
				if (Bytes is null || Bytes.Length == 0)
					return null;
				return Encoding.UTF8.GetString(Bytes);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return null;
			}
		}

		private static async Task<HttpStatusCode> ProbeUriAsync(Uri Uri)
		{
			try
			{
				using HttpRequestMessage Req = new(HttpMethod.Head, Uri);
				using HttpResponseMessage Resp = await httpClient.SendAsync(Req);
				return Resp.StatusCode;
			}
			catch
			{
				return 0;
			}
		}
	}
}
