using System.Collections.Specialized;
using System.Web;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Persistence;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens Smart Contract links.
	/// </summary>
	public class SmartContractLink : ILinkOpener
	{
		/// <summary>
		/// Opens Smart Contract links.
		/// </summary>
		public SmartContractLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return string.Compare(Link.Scheme, Constants.UriSchemes.IotSc, StringComparison.OrdinalIgnoreCase) == 0 ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <param name="ShowErrorIfUnable">If an error message should be displayed, in case the URI could not be opened.</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link, bool ShowErrorIfUnable)
		{
			Dictionary<CaseInsensitiveString, object> Parameters = [];
			string s = Link.OriginalString.Replace("%40", "@"); // Android has problems with URIs containg @

			string? ContractId = Constants.UriSchemes.RemoveScheme(s) ?? string.Empty;
			if (ContractId is null)
				return false;

			int i = ContractId.IndexOf('?');

			if (i > 0)
			{
				NameValueCollection QueryParameters = HttpUtility.ParseQueryString(ContractId[i..]);

				foreach (string? Key in QueryParameters.AllKeys)
				{
					if (Key is null)
						continue;

					string? Value = QueryParameters[Key];
					if (Value is null)
						continue;

					Parameters[Key] = Value;
				}

				ContractId = ContractId[..i];
			}

			await ServiceRef.ContractOrchestratorService.OpenContract(ContractId,
				ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)], Parameters);

			return true;
		}
	}
}
