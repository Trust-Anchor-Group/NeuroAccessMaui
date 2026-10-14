using System.Collections.Specialized;
using System.Web;
using NeuroAccessMaui.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.StanzaErrors;
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

		/// <inheritdoc/>
		public bool RequiresXmppConnection => true;

		/// <inheritdoc/>
		public async Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			ContractLinkData? Data = await this.ReadAsync(Link, CancellationToken);
			if (Data is null)
				return null;

			// The contract read during preparation is opened as it is, so it is not downloaded again. A protected contract
			// could not be read, so opening it sends the access petition instead.
			return new PreparedLink(Link, () => Data.Contract is not null
				? ServiceRef.ContractOrchestratorService.OpenContract(Data.Contract, Data.Parameters)
				: ServiceRef.ContractOrchestratorService.PetitionContract(Data.Identifier, ServiceRef.Localizer[nameof(AppResources.ScannedQrCode)]));
		}

		/// <summary>
		/// Reads a smart contract link without opening it or requesting access.
		/// </summary>
		/// <param name="Link">The link to read.</param>
		/// <param name="CancellationToken">Cancellation for the lookup.</param>
		/// <returns>The contract data, or <c>null</c> when the link is malformed.</returns>
		public async Task<ContractLinkData?> ReadAsync(Uri Link, CancellationToken CancellationToken)
		{
			string Payload = Constants.UriSchemes.RemoveScheme(Link.OriginalString.Replace("%40", "@")) ?? string.Empty; // Android has problems with URIs containg @
			int QueryStart = Payload.IndexOf('?');
			string Identifier = QueryStart < 0 ? Payload : Payload[..QueryStart];
			if (string.IsNullOrWhiteSpace(Identifier) || Identifier.Any(char.IsWhiteSpace))
				return null;

			Dictionary<CaseInsensitiveString, object> Parameters = [];
			if (QueryStart > 0)
			{
				NameValueCollection QueryParameters = HttpUtility.ParseQueryString(Payload[QueryStart..]);

				foreach (string? Key in QueryParameters.AllKeys)
				{
					if (Key is not null && QueryParameters[Key] is string Value)
						Parameters[Key] = Value;
				}
			}

			Contract? Contract;
			try
			{
				Contract = await ServiceRef.XmppService.GetContract(Identifier).WaitAsync(CancellationToken);
			}
			catch (ForbiddenException)
			{
				// Protected contracts are opened through an access petition by the open action.
				Contract = null;
			}

			return new ContractLinkData(Identifier, Contract, Parameters);
		}
	}
}
