using System.Xml;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Networking.XMPP.Contracts.Search;
using Waher.Persistence;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// Finds the contract templates for token actions on the user's Neuron, and prepares contracts from them. Templates
	/// are looked for in this order: approved public templates found by searching the Neuron, then templates the user has
	/// saved (for creating tokens only), and last the templates known for the Neuron's domain.
	/// </summary>
	public sealed class TokenTemplateService : ITokenTemplateService
	{
		/// <summary>
		/// Namespace of the marketplace's machine-readable contract elements.
		/// </summary>
		public const string MarketplaceNamespace = "https://paiwise.tagroot.io/Schema/Marketplace.xsd";

		private const int SearchBatchSize = 25;
		private const int MaxSearchHits = 50;
		private const int MaxSavedCreationTemplates = 50;
		private static readonly TimeSpan cacheDuration = TimeSpan.FromHours(1);

		private readonly Dictionary<string, CachedSearch> searches = new(StringComparer.Ordinal);
		private readonly object synchObject = new();
		private string? cacheDomain;

		/// <summary>
		/// Finds the contract templates for token actions on the user's Neuron, and prepares contracts from them.
		/// </summary>
		public TokenTemplateService()
		{
		}

		/// <inheritdoc/>
		public async Task<bool> HasTemplateAsync(TokenAction Action)
		{
			string[] Ids = await this.GetTemplateIdsAsync(Action, 1);
			return Ids.Length > 0;
		}

		/// <inheritdoc/>
		public async Task<Contract?> GetTemplateAsync(TokenAction Action)
		{
			string Namespace = GetNamespace(Action);
			string LocalName = GetLocalName(Action);
			Exception? LastError = null;

			foreach (string Id in await this.GetTemplateIdsAsync(Action, MaxSavedCreationTemplates))
			{
				try
				{
					Contract Template = await ServiceRef.XmppService.GetContract(Id);

					if (IsUsableTemplate(Template, Namespace, LocalName))
						return Template;
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					LastError = Ex;
				}
			}

			if (LastError is not null)
				throw LastError;

			return null;
		}

		/// <inheritdoc/>
		public Task<string[]> GetCreationTemplatesAsync()
		{
			return this.GetTemplateIdsAsync(TokenAction.Create, MaxSavedCreationTemplates);
		}

		/// <inheritdoc/>
		public async Task<NewContractNavigationArgs> PrepareAsync(TokenAction Action, Contract Template, TokenContractContext Context)
		{
			CreationAttributesEventArgs Attributes = Context.Attributes ?? await GetAttributesAsync();
			string? MyLegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
			string? TrustProviderId = string.IsNullOrEmpty(Attributes.TrustProviderId) ? null : Attributes.TrustProviderId;

			TemplateMapping Mapping = new();
			XmlElement? Root = Template.ForMachines;

			if (Root is not null && Root.NamespaceURI == GetNamespace(Action) && Root.LocalName == GetLocalName(Action))
				MapForMachines(Action, Root, MyLegalId, TrustProviderId, Attributes, Context, Mapping);

			ApplyParts(Template, Mapping.Roles);
			Template.Visibility = ContractVisibility.Public;

			NewContractNavigationArgs Args = new(Template, true, Mapping.Parameters);

			if (TrustProviderId is not null)
				Args.SuppressProposal(TrustProviderId);

			return Args;
		}

		#region Lookup

		/// <summary>
		/// Gets the namespace of the machine-readable element of contracts for an action.
		/// </summary>
		/// <param name="Action">The token action.</param>
		/// <returns>The namespace.</returns>
		public static string GetNamespace(TokenAction Action)
		{
			return Action == TokenAction.Consign ? MarketplaceNamespace : NeuroFeaturesClient.NamespaceNeuroFeatures;
		}

		/// <summary>
		/// Gets the local name of the machine-readable element of contracts for an action. Selling and buying both use a
		/// transfer.
		/// </summary>
		/// <param name="Action">The token action.</param>
		/// <returns>The local name.</returns>
		public static string GetLocalName(TokenAction Action)
		{
			return Action switch
			{
				TokenAction.Create => "Create",
				TokenAction.Sell => "Transfer",
				TokenAction.Buy => "Transfer",
				TokenAction.Consign => "ForSale",
				TokenAction.Give => "Donate",
				TokenAction.Destroy => "Destroy",
				_ => throw new ArgumentOutOfRangeException(nameof(Action))
			};
		}

		/// <summary>
		/// Checks if a contract can be used as a template for an action: it is approved, can act as a template, and has the
		/// action's machine-readable element. A consignment must also name the token it sells.
		/// </summary>
		/// <param name="Template">The contract.</param>
		/// <param name="Namespace">Namespace of the machine-readable element.</param>
		/// <param name="LocalName">Local name of the machine-readable element.</param>
		/// <returns>If the contract can be used.</returns>
		private static bool IsUsableTemplate(Contract? Template, string Namespace, string LocalName)
		{
			if (Template is null || Template.State != ContractState.Approved || !Template.CanActAsTemplate)
				return false;

			if (Template.ForMachinesNamespace != Namespace || Template.ForMachinesLocalName != LocalName)
				return false;

			if (Namespace == MarketplaceNamespace)
				return GetTag(GetChild(Template.ForMachines, "Description"), "TokenID") is not null;

			return true;
		}

		/// <summary>
		/// Gets the IDs of the templates for an action, in the order they should be tried.
		/// </summary>
		/// <param name="Action">The token action.</param>
		/// <param name="MaxSaved">The most saved creation templates to include.</param>
		/// <returns>Template IDs, without duplicates.</returns>
		private async Task<string[]> GetTemplateIdsAsync(TokenAction Action, int MaxSaved)
		{
			string LocalName = GetLocalName(Action);
			List<string> Result = [];
			HashSet<string> Seen = new(StringComparer.OrdinalIgnoreCase);

			foreach (string Id in await this.GetSearchedTemplateIdsAsync(GetNamespace(Action), LocalName))
			{
				if (Seen.Add(Id))
					Result.Add(Id);
			}

			if (Action == TokenAction.Create)
			{
				foreach (string Id in await GetSavedCreationTemplateIdsAsync(MaxSaved))
				{
					if (Seen.Add(Id))
						Result.Add(Id);
				}
			}

			foreach (string Id in GetKnownTemplateIds(LocalName))
			{
				if (Seen.Add(Id))
					Result.Add(Id);
			}

			return [.. Result];
		}

		/// <summary>
		/// Gets the IDs of the approved public templates on the Neuron with a given machine-readable element. Results are
		/// kept for an hour per Neuron. A search that fails isn't kept, so it is tried again the next time.
		/// </summary>
		/// <param name="Namespace">Namespace of the machine-readable element.</param>
		/// <param name="LocalName">Local name of the machine-readable element.</param>
		/// <returns>Template IDs.</returns>
		private async Task<string[]> GetSearchedTemplateIdsAsync(string Namespace, string LocalName)
		{
			string? Domain = ServiceRef.TagProfile.Domain;
			Task<string[]?> Search;

			lock (this.synchObject)
			{
				if (!string.Equals(Domain, this.cacheDomain, StringComparison.OrdinalIgnoreCase))
				{
					this.searches.Clear();
					this.cacheDomain = Domain;
				}

				if (!this.searches.TryGetValue(LocalName, out CachedSearch? Cached) || Cached.Expires <= DateTime.UtcNow)
				{
					Cached = new CachedSearch(SearchTemplatesAsync(Namespace, LocalName), DateTime.UtcNow.Add(cacheDuration));
					this.searches[LocalName] = Cached;
				}

				Search = Cached.Search;
			}

			string[]? Ids = await Search;

			if (Ids is null)
			{
				lock (this.synchObject)
				{
					if (this.searches.TryGetValue(LocalName, out CachedSearch? Cached) && Cached.Search == Search)
						this.searches.Remove(LocalName);
				}

				return [];
			}

			return Ids;
		}

		/// <summary>
		/// Searches the Neuron for approved public templates with a given machine-readable element.
		/// </summary>
		/// <param name="Namespace">Namespace of the machine-readable element.</param>
		/// <param name="LocalName">Local name of the machine-readable element.</param>
		/// <returns>Template IDs, or null when the search failed or can't be made yet.</returns>
		private static async Task<string[]?> SearchTemplatesAsync(string Namespace, string LocalName)
		{
			try
			{
				// Without the address of the legal service, the search is made again the next time.
				if (string.IsNullOrEmpty(ServiceRef.TagProfile.LegalJid))
					return null;

				SearchFilter[] Filter =
				[
					new FilterMachineContentNamespaceEquality(Namespace),
					new FilterMachineContentLocalNameEquality(LocalName)
				];

				List<string> Result = [];
				int Offset = 0;

				while (Result.Count < MaxSearchHits)
				{
					SearchResultEventArgs e = await ServiceRef.XmppService.SearchPublicContracts(Filter, Offset, SearchBatchSize);
					string[] References = e.References ?? [];

					if (References.Length > 0)
					{
						ContractsEventArgs e2 = await ServiceRef.XmppService.ContractsClient.GetContractsAsync(References);
						if (!e2.Ok)
							throw e2.StanzaError ?? new Exception(e2.ErrorText);

						foreach (Contract Contract in e2.Contracts ?? [])
						{
							if (IsUsableTemplate(Contract, Namespace, LocalName) && !Result.Contains(Contract.ContractId))
								Result.Add(Contract.ContractId);
						}
					}

					if (!e.More || References.Length == 0)
						break;

					Offset += References.Length;
				}

				return [.. Result];
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return null;
			}
		}

		/// <summary>
		/// Gets the IDs of the token creation templates the user has saved.
		/// </summary>
		/// <param name="MaxCount">The most IDs to return.</param>
		/// <returns>Template IDs, most recently changed first.</returns>
		private static async Task<string[]> GetSavedCreationTemplateIdsAsync(int MaxCount)
		{
			try
			{
				IReadOnlyList<ContractReference> Rows = await ServiceRef.ContractStore.QueryAsync(new ContractListQuery()
				{
					IsTemplate = true,
					IsTokenCreationTemplate = true,
					MaxCount = MaxCount
				});

				List<string> Result = [];

				foreach (ContractReference Row in Rows)
				{
					string? Id = Row.ContractId?.Value;
					if (!string.IsNullOrEmpty(Id))
						Result.Add(Id);
				}

				return [.. Result];
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return [];
			}
		}

		/// <summary>
		/// Gets the IDs of the templates known for the Neuron's domain.
		/// </summary>
		/// <param name="LocalName">Local name of the machine-readable element.</param>
		/// <returns>Template IDs.</returns>
		private static string[] GetKnownTemplateIds(string LocalName)
		{
			string? Domain = ServiceRef.TagProfile.Domain;

			if (string.IsNullOrEmpty(Domain) ||
				!Constants.ContractTemplates.TokenTemplatesByDomain.TryGetValue(Domain, out IReadOnlyDictionary<string, string[]>? ByElement) ||
				!ByElement.TryGetValue(LocalName, out string[]? Ids))
			{
				return [];
			}

			return Ids;
		}

		/// <summary>
		/// Gets the token service's trust provider, currency and commission.
		/// </summary>
		/// <returns>The attributes.</returns>
		private static async Task<CreationAttributesEventArgs> GetAttributesAsync()
		{
			CreationAttributesEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
			if (!e.Ok)
				throw e.StanzaError ?? new Exception(e.ErrorText);

			return e;
		}

		#endregion

		#region Mapping

		/// <summary>
		/// Works out the legal IDs of the roles, and the values of the parameters, that the machine-readable element of a
		/// template refers to.
		/// </summary>
		/// <param name="Action">The token action.</param>
		/// <param name="Root">The machine-readable element.</param>
		/// <param name="MyLegalId">The user's legal ID.</param>
		/// <param name="TrustProviderId">Legal ID of the token service's trust provider.</param>
		/// <param name="Attributes">The token service's currency and commission.</param>
		/// <param name="Context">What is known about the action.</param>
		/// <param name="Mapping">Where roles and parameters are collected.</param>
		private static void MapForMachines(TokenAction Action, XmlElement Root, string? MyLegalId, string? TrustProviderId,
			CreationAttributesEventArgs Attributes, TokenContractContext Context, TemplateMapping Mapping)
		{
			switch (Action)
			{
				case TokenAction.Create:
					Mapping.SetRole(GetChild(Root, "Creator"), MyLegalId);
					Mapping.SetRole(GetChild(Root, "Owner"), MyLegalId);
					Mapping.SetRole(GetChild(Root, "TrustProvider"), TrustProviderId);
					Mapping.SetParameter(GetChild(Root, "Currency"), Attributes.Currency);
					Mapping.SetParameter(GetChild(Root, "CommissionPercent"), Attributes.Commission);
					break;

				case TokenAction.Sell:
				case TokenAction.Buy:
					if (Action == TokenAction.Sell)
						Mapping.SetRole(GetChild(Root, "Seller"), MyLegalId);
					else
					{
						Mapping.SetRole(GetChild(Root, "Buyer"), MyLegalId);
						Mapping.SetRole(GetChild(Root, "Seller"), Context.OwnerLegalId);
					}

					Mapping.SetRole(GetChild(Root, "TrustProvider"), TrustProviderId);
					Mapping.SetParameter(GetChild(Root, "TokenID"), Context.TokenId);
					Mapping.SetParameter(GetChild(Root, "Currency"), Attributes.Currency);
					Mapping.SetParameter(GetChild(Root, "CommissionPercent"), Attributes.Commission);
					Mapping.SetParameter(GetChild(Root, "OwnershipContract"), Context.OwnershipContract);
					break;

				case TokenAction.Give:
					Mapping.SetRole(GetChild(Root, "Donor"), MyLegalId);
					Mapping.SetRole(GetChild(Root, "Recipient"), Context.RecipientLegalId);
					Mapping.SetRole(GetChild(Root, "TrustProvider"), TrustProviderId);
					Mapping.SetParameter(GetChild(Root, "TokenID"), Context.TokenId);
					Mapping.SetParameter(GetChild(Root, "OwnershipContract"), Context.OwnershipContract);
					break;

				case TokenAction.Destroy:
					Mapping.SetRole(GetChild(Root, "Owner"), MyLegalId);
					Mapping.SetRole(GetChild(Root, "TrustProvider"), TrustProviderId);
					Mapping.SetParameter(GetChild(Root, "TokenID"), Context.TokenId);
					break;

				case TokenAction.Consign:
					// The marketplace names the roles of a consignment directly, and has no elements referring to them.
					Mapping.SetRoleByName("Seller", MyLegalId);
					Mapping.SetRoleByName("Auctioneer", TrustProviderId);

					// Templates usually name their parameters like this. Elements that refer to parameters are used too.
					Mapping.SetParameterByName("TokenID", Context.TokenId);
					Mapping.SetParameterByName("Category", Context.Category);
					Mapping.SetParameterByName("FriendlyName", Context.FriendlyName);
					Mapping.SetParameterByName("CommissionPercent", Attributes.Commission);
					Mapping.SetParameterByName("Currency", Attributes.Currency);

					XmlElement? Price = GetChild(Root, "Price");

					Mapping.SetParameter(GetTag(GetChild(Root, "Description"), "TokenID"), Context.TokenId);
					Mapping.SetParameter(GetChild(Price, "CommissionPercent"), Attributes.Commission);
					Mapping.SetParameter(GetChild(Price, "Currency"), Attributes.Currency);
					break;
			}
		}

		/// <summary>
		/// Assigns the mapped legal IDs to the template's parts. When the template has no parts, parts are created for the
		/// mapped roles the template defines, and they become the template's named parts.
		/// </summary>
		/// <param name="Template">The template.</param>
		/// <param name="Roles">Legal IDs, by role name.</param>
		private static void ApplyParts(Contract Template, Dictionary<string, string> Roles)
		{
			if (Roles.Count == 0)
				return;

			if (Template.Parts is null || Template.Parts.Length == 0)
			{
				List<Part> Parts = [];

				foreach (KeyValuePair<string, string> P in Roles)
				{
					if (Template.Roles is not null && Template.Roles.Any(R => R.Name == P.Key))
					{
						Parts.Add(new Part()
						{
							Role = P.Key,
							LegalId = P.Value
						});
					}
				}

				if (Parts.Count > 0)
				{
					Template.Parts = [.. Parts];
					Template.PartsMode = ContractParts.ExplicitlyDefined;
				}
			}
			else
			{
				foreach (Part Part in Template.Parts)
				{
					if (Part.Role is not null && Roles.TryGetValue(Part.Role, out string? LegalId))
						Part.LegalId = LegalId;
				}
			}
		}

		/// <summary>
		/// Gets the first child element with a given local name, in the namespace of its parent.
		/// </summary>
		/// <param name="Parent">The parent element.</param>
		/// <param name="LocalName">Local name of the child element.</param>
		/// <returns>The child element, or null when there is none.</returns>
		private static XmlElement? GetChild(XmlElement? Parent, string LocalName)
		{
			if (Parent is null)
				return null;

			foreach (XmlNode N in Parent.ChildNodes)
			{
				if (N is XmlElement E && E.LocalName == LocalName && E.NamespaceURI == Parent.NamespaceURI)
					return E;
			}

			return null;
		}

		/// <summary>
		/// Gets a named tag of a marketplace item description.
		/// </summary>
		/// <param name="Description">The description element.</param>
		/// <param name="Name">Name of the tag.</param>
		/// <returns>The tag element, or null when there is none.</returns>
		private static XmlElement? GetTag(XmlElement? Description, string Name)
		{
			if (Description is null)
				return null;

			foreach (XmlNode N in Description.ChildNodes)
			{
				if (N is XmlElement E && E.LocalName == "Tag" && E.NamespaceURI == Description.NamespaceURI &&
					E.GetAttribute("name") == Name)
				{
					return E;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the value of an attribute of a child element of a given local name.
		/// </summary>
		/// <param name="Element">The element.</param>
		/// <param name="ChildName">Local name of the child element.</param>
		/// <param name="AttributeName">Name of the attribute.</param>
		/// <returns>The attribute value, or null when there is none.</returns>
		private static string? GetChildAttribute(XmlElement? Element, string ChildName, string AttributeName)
		{
			XmlElement? Child = GetChild(Element, ChildName);
			if (Child is null)
				return null;

			string Value = Child.GetAttribute(AttributeName);
			return string.IsNullOrEmpty(Value) ? null : Value;
		}

		#endregion

		/// <summary>
		/// A search for templates, kept until it expires.
		/// </summary>
		/// <param name="Search">The search. It returns null when it fails.</param>
		/// <param name="Expires">When the result should no longer be used.</param>
		private sealed class CachedSearch(Task<string[]?> Search, DateTime Expires)
		{
			/// <summary>
			/// The search. It returns null when it fails.
			/// </summary>
			public Task<string[]?> Search { get; } = Search;

			/// <summary>
			/// When the result should no longer be used.
			/// </summary>
			public DateTime Expires { get; } = Expires;
		}

		/// <summary>
		/// The legal IDs of roles, and the values of parameters, worked out from a template.
		/// </summary>
		private sealed class TemplateMapping
		{
			/// <summary>
			/// Legal IDs, by role name.
			/// </summary>
			public Dictionary<string, string> Roles { get; } = new(StringComparer.Ordinal);

			/// <summary>
			/// Parameter values, by parameter name.
			/// </summary>
			public Dictionary<CaseInsensitiveString, object> Parameters { get; } = [];

			/// <summary>
			/// Sets the legal ID of the role an element refers to.
			/// </summary>
			/// <param name="Element">An element that refers to a role.</param>
			/// <param name="LegalId">The legal ID, if known.</param>
			public void SetRole(XmlElement? Element, string? LegalId)
			{
				this.SetRoleByName(GetChildAttribute(Element, "RoleReference", "role"), LegalId);
			}

			/// <summary>
			/// Sets the legal ID of a role.
			/// </summary>
			/// <param name="Role">Role name, if known.</param>
			/// <param name="LegalId">The legal ID, if known.</param>
			public void SetRoleByName(string? Role, string? LegalId)
			{
				if (!string.IsNullOrEmpty(Role) && !string.IsNullOrEmpty(LegalId))
					this.Roles[Role] = LegalId;
			}

			/// <summary>
			/// Sets the value of the parameter an element refers to.
			/// </summary>
			/// <param name="Element">An element that refers to a parameter.</param>
			/// <param name="Value">The value, if known.</param>
			public void SetParameter(XmlElement? Element, object? Value)
			{
				this.SetParameterByName(GetChildAttribute(Element, "ParameterReference", "parameter"), Value);
			}

			/// <summary>
			/// Sets the value of a parameter.
			/// </summary>
			/// <param name="Name">Parameter name, if known.</param>
			/// <param name="Value">The value, if known.</param>
			public void SetParameterByName(string? Name, object? Value)
			{
				if (string.IsNullOrEmpty(Name) || Value is null || (Value is string Text && string.IsNullOrEmpty(Text)))
					return;

				this.Parameters[Name] = Value;
			}
		}
	}
}
