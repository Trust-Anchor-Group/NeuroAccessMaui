using NeuroAccessMaui.Services.Kyc.Models;
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
		public async Task<KycProcess> LoadProcessAsync(string Resource, string? Lang = null)
		{
			string FileName = GetFileName(Resource);
			using Stream Stream = await FileSystem.OpenAppPackageFileAsync(FileName);
			using StreamReader Reader = new(Stream);
			string Xml = await Reader.ReadToEndAsync().ConfigureAwait(false);
			return await KycProcessParser.LoadProcessAsync(Xml, Lang).ConfigureAwait(false);
		}

		private static string GetFileName(string Resource)
		{
			int Index = Resource.LastIndexOf("Raw.", StringComparison.OrdinalIgnoreCase);
			return Index >= 0 ? Resource[(Index + 4)..] : Resource;
		}
	}
}
