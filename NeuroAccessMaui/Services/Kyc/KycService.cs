using System.Reflection;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Kyc
{
	/// <summary>
	/// Default implementation of <see cref="IKycService"/>.
	/// </summary>
	[Singleton]
	public class KycService : IKycService
	{
		/// <inheritdoc/>
		public async Task<KycReference> LoadKycReferenceAsync(string Resource, string? Lang = null)
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

			if (Reference is null) // Set line to commented out code to enable storage
			{
				Reference = new KycReference
				{
					Created = DateTime.UtcNow
				};
				try
				{
					await ServiceRef.StorageService.Insert(Reference);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				}

				// TODO: Replace this with fetching pub/sub element on neuron
				string FileName = GetFileName(Resource);
				using Stream Stream = await FileSystem.OpenAppPackageFileAsync(FileName);
				using StreamReader Reader = new(Stream);
				string Xml = await Reader.ReadToEndAsync().ConfigureAwait(false);
				Reference.KycXml = Xml;
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
	}
}
