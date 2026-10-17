using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Wallet.EmbeddedLayout;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports;
using NeuroAccessMaui.UI.Pages.Wallet.MachineVariables;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels;
using NeuroAccessMaui.UI.Pages.Wallet.TokenEvents;
using NeuroAccessMaui.UI.Pages.Wallet.XmlViewer;
using NeuroAccessMaui.UI.Popups.Tokens.NoteCommand;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using NeuroFeatures.NoteCommands;
using NeuroFeatures.Tags;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Web;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Security;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of a token.
	/// </summary>
	/// <param name="Args">Navigation arguments.</param>
	public partial class TokenDetailsViewModel(TokenDetailsNavigationArgs? Args) : QrXmppViewModel()
	{
		/// <summary>
		/// The most state-machine variables shown inline. The rest are one tap away.
		/// </summary>
		private const int MaxInlineVariables = 5;

		private readonly TokenDetailsNavigationArgs? navigationArguments = Args;
		private readonly Dictionary<string, object> machineVariables = [];
		private string? machineId;
		private string? machineState;
		private int noteCommandsVersion;
		private int noteCommandCandidateCount;
		private bool noteCommandsLoaded;
		private bool machineStateFailed;
		private bool historyAvailable;
		private bool hasTransferTemplate;
		private bool hasConsignmentTemplate;
		private bool hasDonationTemplate;
		private bool hasDestructionTemplate;
		private NoteCommandItem[] noteCommands = [];

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (this.navigationArguments?.Token is TokenItem Item)
				await this.LoadTokenAsync(Item);

			ServiceRef.XmppService.NeuroFeatureAdded += this.Wallet_TokenAdded;
			ServiceRef.XmppService.NeuroFeatureRemoved += this.Wallet_TokenRemoved;
			ServiceRef.XmppService.NeuroFeatureStateUpdated += this.Wallet_StateUpdated;
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated += this.Wallet_VariablesUpdated;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			if (this.IsVerified && !string.IsNullOrEmpty(this.TokenId))
				await ServiceRef.TokenUnreadTracker.MarkTokenReadAsync(this.TokenId);
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			ServiceRef.XmppService.NeuroFeatureAdded -= this.Wallet_TokenAdded;
			ServiceRef.XmppService.NeuroFeatureRemoved -= this.Wallet_TokenRemoved;
			ServiceRef.XmppService.NeuroFeatureStateUpdated -= this.Wallet_StateUpdated;
			ServiceRef.XmppService.NeuroFeatureVariablesUpdated -= this.Wallet_VariablesUpdated;

			this.DescriptionTask.Cancel();
			this.MachineStateTask.Cancel();

			await base.OnDisposeAsync();
		}

		#region Loading

		/// <summary>
		/// Shows a token, and starts loading what is fetched separately: the glyph, the description, the state machine,
		/// and whether a token the user doesn't own has a visible history.
		/// </summary>
		/// <param name="Item">The token.</param>
		private async Task LoadTokenAsync(TokenItem Item)
		{
			this.Created = Item.Created;
			this.Updated = Item.Updated;
			this.Expires = Item.Expires;
			this.ArchiveRequired = Item.ArchiveRequired;
			this.ArchiveOptional = Item.ArchiveOptional;
			this.SignatureTimestamp = Item.SignatureTimestamp;
			this.Signature = Item.Signature;
			this.DefinitionSchemaDigest = Item.DefinitionSchemaDigest;
			this.DefinitionSchemaHashFunction = Item.DefinitionSchemaHashFunction;
			this.CreatorCanDestroy = Item.CreatorCanDestroy;
			this.OwnerCanDestroyBatch = Item.OwnerCanDestroyBatch;
			this.OwnerCanDestroyIndividual = Item.OwnerCanDestroyIndividual;
			this.CertifierCanDestroy = Item.CertifierCanDestroy;
			this.FriendlyName = Item.FriendlyName;
			this.Category = Item.Category;
			this.HasCategory = Item.HasCategory;
			this.GlyphContentType = Item.GlyphContentType;
			this.Ordinal = Item.Ordinal;
			this.Value = Item.Value;
			this.TokenIdMethod = Item.TokenIdMethod;
			this.TokenId = Item.TokenId;
			this.ShortTokenId = Item.ShortTokenId;
			this.Visibility = Item.Visibility;
			this.Creator = Item.Creator;
			this.CreatorFriendlyName = await ContactInfo.GetFriendlyName(Item.Creator);
			this.CreatorJid = Item.CreatorJid;
			this.Owner = Item.Owner;
			this.OwnerFriendlyName = await ContactInfo.GetFriendlyName(Item.Owner);
			this.OwnerJid = Item.OwnerJid;
			this.BatchSize = Item.BatchSize;
			this.TrustProvider = Item.TrustProvider;
			this.TrustProviderFriendlyName = await ContactInfo.GetFriendlyName(Item.TrustProvider);
			this.TrustProviderJid = Item.TrustProviderJid;
			this.Currency = Item.Currency;
			this.Reference = Item.Reference;
			this.Definition = Item.Definition;
			this.DefinitionNamespace = Item.DefinitionNamespace;
			this.CreationContract = Item.CreationContract;
			this.OwnershipContract = Item.OwnershipContract;
			this.GlyphWidth = TokenGlyph.DetailsBoxSize;
			this.GlyphHeight = TokenGlyph.DetailsBoxSize;
			this.TokenXml = Item.Token.ToXml();
			this.machineId = Item.Token.MachineId;

			// A shared copy that couldn't be checked with its Neuron is labeled, and offers nothing beyond copy and share.
			// Its embedded layout isn't rendered, since rendering runs the token's own script.
			this.IsUnverifiedCopy = this.navigationArguments?.Origin == TokenOrigin.UnverifiedCopy;
			this.IsVerified = !this.IsUnverifiedCopy;
			this.ChangedSinceShared = this.IsVerified && (this.navigationArguments?.ChangedSinceShared ?? false);
			this.IsMyToken = this.IsVerified && string.Equals(this.OwnerJid, ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase);
			this.IsExpired = Item.IsExpired;
			this.ExpiryText = Item.ExpiryText;
			this.HasExpiryText = Item.HasExpiryText;
			this.HasStateMachine = this.IsVerified && Item.HasStateMachine;
			this.HasEmbeddedLayout = this.IsVerified && Item.HasEmbeddedLayout;
			this.OrdinalText = ServiceRef.Localizer[nameof(AppResources.TokenOrdinalOfBatch), this.Ordinal ?? 0, this.BatchSize ?? 0];
			this.DefinitionSchemaUrl = GetSchemaUrl(this.TokenId, this.DefinitionNamespace, this.DefinitionSchemaDigest);
			this.WebUrl = this.HasStateMachine && TokenShareFormatter.IsPublic(Item.Token) ? GetWebUrl(this.TokenId) : null;
			this.HasWebUrl = this.WebUrl is not null;
			this.UpdateOwnershipText();

			string? RawDescription = Item.Description;
			this.HasDescription = !string.IsNullOrWhiteSpace(RawDescription) && RawDescription != this.FriendlyName;

			if (this.HasDescription)
				this.DescriptionTask.Load(this.LoadDescriptionAsync);

			_ = this.LoadGlyphAsync(Item.Token);

			if (this.HasEmbeddedLayout)
				_ = this.LoadEmbeddedLayoutAsync(Item);

			if (this.TokenId is not null)
				this.GenerateQrCode(Constants.UriSchemes.CreateTokenUri(this.TokenId));

			await this.BuildOwnershipPropertiesAsync(Item);
			this.BuildTags(Item);
			await this.BuildTechnicalPropertiesAsync(Item);

			// Until the templates are known, the buttons that need them keep their place, disabled.
			this.IsLoadingTemplates = this.IsVerified;
			this.UpdateActions();
			this.UpdateStateMachineSections();

			if (this.IsVerified)
				_ = this.LoadTemplateAvailabilityAsync();

			if (this.HasStateMachine)
			{
				this.MachineStateTask.Load(this.LoadMachineStateAsync);
				_ = this.LoadNoteCommandsAsync(Item.Token);
			}

			if (this.IsVerified && !this.IsMyToken)
				_ = this.ProbeHistoryAsync();
		}

		/// <summary>
		/// Loads the description as the token's Neuron renders it. When that fails, the description engraved in the token
		/// is shown instead.
		/// </summary>
		/// <param name="Context">Task context.</param>
		private async Task LoadDescriptionAsync(TaskContext<int> Context)
		{
			VerticalStackLayout? Content = null;

			if (this.IsVerified && !string.IsNullOrEmpty(this.TokenId))
			{
				try
				{
					Content = await ServiceRef.XmppService.GenerateNeuroFeatureDescriptionMaui(this.TokenId);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			Content ??= await (this.navigationArguments?.Token?.Description ?? string.Empty).MarkdownToMaui();

			await MainThread.InvokeOnMainThreadAsync(() => this.Description = Content);
		}

		/// <summary>
		/// Loads the glyph shown in token details.
		/// </summary>
		/// <param name="Token">The token.</param>
		private async Task LoadGlyphAsync(Token Token)
		{
			try
			{
				TokenGlyph Glyph = await TokenGlyph.CreateAsync(Token, TokenGlyph.DetailsBoxSize);

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.GlyphImage = Glyph.Image;
					this.HasGlyphImage = !Glyph.IsPlaceholder;
					this.ShowGlyphPlaceholder = Glyph.IsPlaceholder;
					this.GlyphWidth = Glyph.Width;
					this.GlyphHeight = Glyph.Height;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Renders the embedded layout, which replaces the glyph at the top of the page. The glyph stays when rendering fails.
		/// </summary>
		/// <param name="Item">The token.</param>
		private async Task LoadEmbeddedLayoutAsync(TokenItem Item)
		{
			try
			{
				byte[]? Png = await Item.RenderEmbeddedLayoutPngAsync();
				if (Png is null)
					return;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.EmbeddedLayoutImage = ImageSource.FromStream(() => new MemoryStream(Png));
					this.HasEmbeddedLayoutImage = true;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Checks if the token's Neuron lets the user see the history of a token the user doesn't own. History is offered
		/// only when it does.
		/// </summary>
		private async Task ProbeHistoryAsync()
		{
			string? TokenId = this.TokenId;
			if (string.IsNullOrEmpty(TokenId))
				return;

			try
			{
				await ServiceRef.XmppService.GetNeuroFeatureEvents(TokenId, 0, 1);

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.historyAvailable = true;
					this.UpdateActions();
				});
			}
			catch (Exception Ex)
			{
				// The history isn't offered.
				ServiceRef.LogService.LogDebug("Token history isn't available: " + Ex.Message);
			}
		}

		/// <summary>
		/// Builds the "Ownership and dates" rows.
		/// </summary>
		/// <param name="Item">The token.</param>
		private async Task BuildOwnershipPropertiesAsync(TokenItem Item)
		{
			List<TokenPropertyItem> Rows = [];

			AddParty(Rows, ServiceRef.Localizer[nameof(AppResources.Owner)], Item.Owner, this.OwnerFriendlyName);
			AddParty(Rows, ServiceRef.Localizer[nameof(AppResources.Creator)], Item.Creator, this.CreatorFriendlyName);
			AddParty(Rows, ServiceRef.Localizer[nameof(AppResources.TrustProvider)], Item.TrustProvider, this.TrustProviderFriendlyName);

			AddDate(Rows, ServiceRef.Localizer[nameof(AppResources.Created)], Item.Created);
			AddDate(Rows, ServiceRef.Localizer[nameof(AppResources.Updated)], Item.Updated);

			if (TokenExpiry.HasExpiry(Item.Expires))
				AddDate(Rows, ServiceRef.Localizer[nameof(AppResources.Expires)], Item.Expires);

			await AddPartiesAsync(Rows, ServiceRef.Localizer[nameof(AppResources.Witness)], Item.Witness);
			await AddPartiesAsync(Rows, ServiceRef.Localizer[nameof(AppResources.Certifier)], Item.Certifier);
			await AddPartiesAsync(Rows, ServiceRef.Localizer[nameof(AppResources.Valuator)], Item.Valuator);
			await AddPartiesAsync(Rows, ServiceRef.Localizer[nameof(AppResources.Assessor)], Item.Assessor);

			SetItems(this.OwnershipProperties, Rows);
		}

		/// <summary>
		/// Builds the tag rows: the creator's reference first, then the tags.
		/// </summary>
		/// <param name="Item">The token.</param>
		private void BuildTags(TokenItem Item)
		{
			List<TokenTagItem> Rows = [];

			if (!string.IsNullOrEmpty(Item.Reference))
				Rows.Add(new TokenTagItem(ServiceRef.Localizer[nameof(AppResources.Reference)], Item.Reference));

			if (Item.Tags is not null)
			{
				foreach (TokenTag Tag in Item.Tags)
					Rows.Add(new TokenTagItem(Tag.Name, Tag.Value?.ToString() ?? string.Empty));
			}

			SetItems(this.Tags, Rows);
			this.HasTags = Rows.Count > 0;
		}

		/// <summary>
		/// Builds the rows in "Technical details".
		/// </summary>
		/// <param name="Item">The token.</param>
		private async Task BuildTechnicalPropertiesAsync(TokenItem Item)
		{
			List<TokenPropertyItem> Rows =
			[
				new(ServiceRef.Localizer[nameof(AppResources.TokenId)], Item.TokenId, TokenPropertyKind.Text)
				{
					CopyText = Constants.UriSchemes.NeuroFeature + ":" + Item.TokenId,
					CopiedMessage = ServiceRef.Localizer[nameof(AppResources.IdCopiedSuccessfully)]
				}
			];

			if (!string.IsNullOrEmpty(Item.ShortTokenId))
				Rows.Add(new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.ShortId)], Item.ShortTokenId, TokenPropertyKind.Text));

			string Visibility = new LocalizedState().Convert(Item.Visibility, typeof(string), null, CultureInfo.CurrentCulture)?.ToString()
				?? Item.Visibility.ToString();

			Rows.Add(new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.TokenVisibility)], Visibility, TokenPropertyKind.Text));
			Rows.Add(new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.Ordinal)], this.OrdinalText ?? string.Empty, TokenPropertyKind.Text));
			Rows.Add(new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.TokenIdMethod)], Item.TokenIdMethod.ToString(), TokenPropertyKind.Text));

			if (this.HasStateMachine && !string.IsNullOrEmpty(this.machineId) &&
				!string.Equals(this.machineId, Item.TokenId, StringComparison.OrdinalIgnoreCase))
			{
				Rows.Add(new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.MachineId)], this.machineId, TokenPropertyKind.Text));
			}

			AddContract(Rows, ServiceRef.Localizer[nameof(AppResources.CreationContract)], Item.CreationContract);
			AddContract(Rows, ServiceRef.Localizer[nameof(AppResources.OwnershipContract)], Item.OwnershipContract);

			AddJid(Rows, ServiceRef.Localizer[nameof(AppResources.OwnerJid)], Item.OwnerJid, Item.Owner, this.OwnerFriendlyName);
			AddJid(Rows, ServiceRef.Localizer[nameof(AppResources.CreatorJid)], Item.CreatorJid, Item.Creator, this.CreatorFriendlyName);
			AddJid(Rows, ServiceRef.Localizer[nameof(AppResources.TrustProviderJid)], Item.TrustProviderJid, Item.TrustProvider, this.TrustProviderFriendlyName);

			int i, c = Item.CertifierJids?.Length ?? 0;
			int d = Item.Certifier?.Length ?? 0;

			for (i = 0; i < c; i++)
			{
				string? CertifierId = i < d ? Item.Certifier![i] : null;
				string? CertifierName = CertifierId is null ? null : await ContactInfo.GetFriendlyName(CertifierId);

				AddJid(Rows, ServiceRef.Localizer[nameof(AppResources.CertifierJid)], Item.CertifierJids![i], CertifierId, CertifierName);
			}

			if (Item.Signature is not null && Item.Signature.Length > 0)
			{
				Rows.Add(new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.Signature)],
					Convert.ToBase64String(Item.Signature), TokenPropertyKind.Text));
			}

			AddDate(Rows, ServiceRef.Localizer[nameof(AppResources.Timestamp)], Item.SignatureTimestamp);

			if (!string.IsNullOrEmpty(Item.DefinitionNamespace))
			{
				Rows.Add(new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.DefinitionNamespace)], Item.DefinitionNamespace,
					TokenTagItem.IsWebLink(Item.DefinitionNamespace) ? TokenPropertyKind.Link : TokenPropertyKind.Text));
			}

			if (Item.DefinitionSchemaDigest is not null && Item.DefinitionSchemaDigest.Length > 0)
			{
				string Digest = Convert.ToBase64String(Item.DefinitionSchemaDigest);

				// The digest links to the schema only when the schema's location is known.
				Rows.Add(this.DefinitionSchemaUrl is string SchemaUrl
					? new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.DefinitionSchemaDigest)], Digest, TokenPropertyKind.Link, SchemaUrl)
					{
						CopyText = Digest
					}
					: new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.DefinitionSchemaDigest)], Digest, TokenPropertyKind.Text));

				Rows.Add(new TokenPropertyItem(ServiceRef.Localizer[nameof(AppResources.HashFunction)],
					Item.DefinitionSchemaHashFunction.ToString(), TokenPropertyKind.Text));
			}

			AddYesNo(Rows, ServiceRef.Localizer[nameof(AppResources.CreatorCanDestroy)], Item.CreatorCanDestroy);
			AddYesNo(Rows, ServiceRef.Localizer[nameof(AppResources.OwnerCanDestroyBatch)], Item.OwnerCanDestroyBatch);
			AddYesNo(Rows, ServiceRef.Localizer[nameof(AppResources.OwnerCanDestroyIndividual)], Item.OwnerCanDestroyIndividual);
			AddYesNo(Rows, ServiceRef.Localizer[nameof(AppResources.CertifierCanDestroy)], Item.CertifierCanDestroy);

			SetItems(this.TechnicalProperties, Rows);
		}

		private static void AddParty(List<TokenPropertyItem> Rows, string Label, string? LegalId, string? FriendlyName)
		{
			if (string.IsNullOrEmpty(LegalId))
				return;

			Rows.Add(new TokenPropertyItem(Label, string.IsNullOrEmpty(FriendlyName) ? LegalId : FriendlyName, TokenPropertyKind.LegalId, LegalId));
		}

		private static async Task AddPartiesAsync(List<TokenPropertyItem> Rows, string Label, string[]? LegalIds)
		{
			if (LegalIds is null)
				return;

			foreach (string LegalId in LegalIds)
				AddParty(Rows, Label, LegalId, await ContactInfo.GetFriendlyName(LegalId));
		}

		private static void AddJid(List<TokenPropertyItem> Rows, string Label, string? Jid, string? LegalId, string? FriendlyName)
		{
			if (string.IsNullOrEmpty(Jid))
				return;

			Rows.Add(new TokenPropertyItem(Label, Jid, TokenPropertyKind.Jid)
			{
				LegalId = LegalId,
				FriendlyName = FriendlyName
			});
		}

		private static void AddContract(List<TokenPropertyItem> Rows, string Label, string? ContractId)
		{
			if (!string.IsNullOrEmpty(ContractId))
				Rows.Add(new TokenPropertyItem(Label, ContractId, TokenPropertyKind.Contract));
		}

		private static void AddDate(List<TokenPropertyItem> Rows, string Label, DateTime Value)
		{
			if (Value == DateTime.MinValue || Value == DateTime.MaxValue)
				return;

			DateTime Local = Value.Kind == DateTimeKind.Local ? Value : DateTime.SpecifyKind(Value, DateTimeKind.Utc).ToLocalTime();

			Rows.Add(new TokenPropertyItem(Label, Local.ToString("g", CultureInfo.CurrentCulture), TokenPropertyKind.Text));
		}

		private static void AddYesNo(List<TokenPropertyItem> Rows, string Label, bool Value)
		{
			Rows.Add(new TokenPropertyItem(Label,
				ServiceRef.Localizer[Value ? nameof(AppResources.Yes) : nameof(AppResources.No)], TokenPropertyKind.Text));
		}

		private static void SetItems<T>(ObservableCollection<T> Collection, IEnumerable<T> Items)
		{
			Collection.Clear();

			foreach (T Item in Items)
				Collection.Add(Item);
		}

		/// <summary>
		/// Gets the web address of the schema that validates the token's definition. The schema is hosted on the Neuron
		/// that hosts the token: the token ID's domain, without the label of the component hosting tokens.
		/// </summary>
		/// <param name="TokenId">Token ID.</param>
		/// <param name="Namespace">Namespace of the definition.</param>
		/// <param name="Digest">Digest of the schema.</param>
		/// <returns>The address, or null when it can't be known.</returns>
		private static string? GetSchemaUrl(string? TokenId, string? Namespace, byte[]? Digest)
		{
			if (string.IsNullOrEmpty(Namespace) || Digest is null || Digest.Length == 0)
				return null;

			string? Host = GetNeuronHost(TokenId);
			if (Host is null)
				return null;

			StringBuilder sb = new();

			sb.Append("https://");
			sb.Append(Host);
			sb.Append("/ValidationSchema.md?NS=");
			sb.Append(HttpUtility.UrlEncode(Namespace));
			sb.Append("&H=");
			sb.Append(HttpUtility.UrlEncode(Convert.ToBase64String(Digest)));
			sb.Append("&Download=1");

			return sb.ToString();
		}

		/// <summary>
		/// Gets the web address of the token's public page on the Neuron that hosts it. Only public tokens with a state
		/// machine have one.
		/// </summary>
		/// <param name="TokenId">Token ID.</param>
		/// <returns>The address, or null when the token ID doesn't have the expected form.</returns>
		private static string? GetWebUrl(string? TokenId)
		{
			string? Host = GetNeuronHost(TokenId);
			if (Host is null || TokenId is null)
				return null;

			int i = TokenId.IndexOf('@');
			if (!Guid.TryParse(TokenId[..i], out _))
				return null;

			return "https://" + Host + "/NF/" + TokenId;
		}

		/// <summary>
		/// Gets the host name of the Neuron that hosts a token: the token ID's domain, without the label of the component
		/// hosting tokens.
		/// </summary>
		/// <param name="TokenId">Token ID.</param>
		/// <returns>The host name, or null when it can't be known.</returns>
		private static string? GetNeuronHost(string? TokenId)
		{
			if (string.IsNullOrEmpty(TokenId))
				return null;

			int i = TokenId.IndexOf('@');
			if (i <= 0)
				return null;

			string Domain = TokenId[(i + 1)..];

			i = Domain.IndexOf('.');
			if (i <= 0 || i >= Domain.Length - 1)
				return null;

			string Host = Domain[(i + 1)..];

			return Uri.CheckHostName(Host) == UriHostNameType.Dns ? Host : null;
		}

		#endregion

		#region Ownership and actions

		/// <summary>
		/// Updates the ownership chip.
		/// </summary>
		private void UpdateOwnershipText()
		{
			this.OwnershipText = this.IsMyToken
				? ServiceRef.Localizer[nameof(AppResources.TokenYours)]
				: ServiceRef.Localizer[nameof(AppResources.TokenOwnedBy), this.OwnerFriendlyName ?? this.Owner ?? string.Empty];
		}

		/// <summary>
		/// Works out which actions are offered, from where the token comes from, who owns it, if it has expired, and which
		/// templates are available. Expired tokens can't be sold, given or destroyed.
		/// </summary>
		private void UpdateActions()
		{
			bool CanChangeOwner = this.IsVerified && !this.IsExpired && !this.HasLeftWallet;

			this.CanOfferToSell = CanChangeOwner && this.IsMyToken && this.hasTransferTemplate;
			this.CanConsign = CanChangeOwner && this.IsMyToken && this.hasConsignmentTemplate;
			this.CanOfferToBuy = CanChangeOwner && !this.IsMyToken && this.hasTransferTemplate;
			this.CanGive = CanChangeOwner && this.IsMyToken && this.hasDonationTemplate;
			this.CanDestroy = CanChangeOwner && this.IsMyToken && this.OwnerCanDestroyIndividual && this.hasDestructionTemplate;
			this.CanViewHistory = this.IsVerified && (this.IsMyToken || this.historyAvailable);
			this.HistoryLabel = this.IsMyToken
				? ServiceRef.Localizer[nameof(AppResources.NotesAndHistory)]
				: ServiceRef.Localizer[nameof(AppResources.History)];

			this.TransferBlockedText = this.IsVerified && this.IsMyToken && this.IsExpired
				? ServiceRef.Localizer[nameof(AppResources.TokenExpiredNoTransfer)]
				: null;
			this.HasTransferBlockedText = this.TransferBlockedText is not null;

			// The templates for selling, buying and giving are looked up in the background. Until they are known, More shows
			// a placeholder row, and Give only appears once its template is found, so a tile never disappears.
			this.IsLoadingTradeActions = CanChangeOwner && this.IsLoadingTemplates;

			this.UpdateNoteCommands();
		}

		/// <summary>
		/// Finds out which token actions have a contract template on the Neuron, and updates the actions.
		/// </summary>
		private async Task LoadTemplateAvailabilityAsync()
		{
			try
			{
				ITokenTemplateService Templates = ServiceRef.TokenTemplateService;

				Task<bool> Transfer = Templates.HasTemplateAsync(TokenAction.Sell);
				Task<bool> Consignment = Templates.HasTemplateAsync(TokenAction.Consign);
				Task<bool> Donation = Templates.HasTemplateAsync(TokenAction.Give);
				Task<bool> Destruction = Templates.HasTemplateAsync(TokenAction.Destroy);

				await Task.WhenAll(Transfer, Consignment, Donation, Destruction);

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.hasTransferTemplate = Transfer.Result;
					this.hasConsignmentTemplate = Consignment.Result;
					this.hasDonationTemplate = Donation.Result;
					this.hasDestructionTemplate = Destruction.Result;
					this.IsLoadingTemplates = false;
					this.UpdateActions();
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.IsLoadingTemplates = false;
					this.UpdateActions();
				});
			}
		}

		/// <summary>
		/// Fills in a contract template for a token action, and opens it for review.
		/// </summary>
		/// <param name="Action">The token action.</param>
		/// <param name="RecipientLegalId">Legal ID of the person a token is given to, if any.</param>
		private async Task PrepareContractAsync(TokenAction Action, string? RecipientLegalId)
		{
			try
			{
				Contract? Template = await ServiceRef.TokenTemplateService.GetTemplateAsync(Action);
				if (Template is null)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					return;
				}

				TokenContractContext Context = new()
				{
					TokenId = this.TokenId,
					OwnershipContract = this.OwnershipContract,
					OwnerLegalId = this.Owner,
					RecipientLegalId = RecipientLegalId,
					Category = this.Category,
					FriendlyName = this.FriendlyName
				};

				NewContractNavigationArgs Args = await ServiceRef.TokenTemplateService.PrepareAsync(Action, Template, Context);

				await ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), Args, BackMethod.CurrentPage);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await TokenErrors.ShowAsync(Ex);
			}
		}

		private bool IsThisToken(string? TokenId)
		{
			if (string.IsNullOrEmpty(TokenId))
				return false;

			return string.Equals(TokenId, this.TokenId, StringComparison.OrdinalIgnoreCase) ||
				(!string.IsNullOrEmpty(this.machineId) && string.Equals(TokenId, this.machineId, StringComparison.OrdinalIgnoreCase));
		}

		private Task Wallet_TokenRemoved(object? Sender, TokenEventArgs e)
		{
			if (!this.IsVerified || e.Token is null || !string.Equals(e.Token.TokenId, this.TokenId, StringComparison.OrdinalIgnoreCase))
				return Task.CompletedTask;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (!this.IsMyToken)
					return;

				this.IsMyToken = false;
				this.HasLeftWallet = true;
				this.UpdateOwnershipText();
				this.UpdateActions();
			});

			return Task.CompletedTask;
		}

		private Task Wallet_TokenAdded(object? Sender, TokenEventArgs e)
		{
			if (!this.IsVerified || e.Token is null || !string.Equals(e.Token.TokenId, this.TokenId, StringComparison.OrdinalIgnoreCase))
				return Task.CompletedTask;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (this.IsMyToken)
					return;

				this.IsMyToken = true;
				this.HasLeftWallet = false;
				this.UpdateOwnershipText();
				this.UpdateActions();
			});

			return Task.CompletedTask;
		}

		#endregion

		#region State machine

		/// <summary>
		/// Loads the current state and variables of the token's state machine.
		/// </summary>
		/// <param name="Context">Task context.</param>
		private async Task LoadMachineStateAsync(TaskContext<int> Context)
		{
			string? TokenId = this.TokenId;
			if (string.IsNullOrEmpty(TokenId))
				return;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.machineStateFailed = false;
				this.UpdateStateMachineSections();
			});

			try
			{
				CurrentStateEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCurrentState(TokenId);
				if (!e.Ok)
					throw e.StanzaError ?? new Exception(e.ErrorText);

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.machineVariables.Clear();
					this.Variables.Clear();

					if (e.Variables is not null)
					{
						foreach (Waher.Script.Variable Variable in e.Variables)
							this.SetVariable(Variable.Name, Variable.ValueObject, false);
					}

					this.SetMachineState(e.CurrentState, e.Ended);
				});
			}
			catch (Exception Ex)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.MachineStateErrorText = TokenErrors.ToMessage(Ex) ?? ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)];
					this.machineStateFailed = true;
					this.UpdateStateMachineSections();
				});
				throw;
			}
		}

		/// <summary>
		/// Shows the state of the state machine. Called on the main thread.
		/// </summary>
		/// <param name="State">The current state.</param>
		/// <param name="Ended">If the state machine has ended.</param>
		private void SetMachineState(string? State, bool Ended)
		{
			this.MachineEnded = Ended || string.IsNullOrEmpty(State);
			this.machineState = this.MachineEnded ? null : State;
			this.MachineStateText = this.MachineEnded
				? ServiceRef.Localizer[nameof(AppResources.StateEnded)]
				: State;
			this.MachineStateDescription = this.MachineEnded
				? ServiceRef.Localizer[nameof(AppResources.TokenStateEnded)]
				: ServiceRef.Localizer[nameof(AppResources.TokenStateBody), State ?? string.Empty];
			this.IsMachineStateKnown = true;

			this.UpdateNoteCommands();
		}

		/// <summary>
		/// Loads the note commands of the token's state machine, keeping those the app may run.
		/// </summary>
		/// <param name="Token">The token.</param>
		private async Task LoadNoteCommandsAsync(Token Token)
		{
			try
			{
				NoteCommand[] Commands = await Token.GetNoteCommands();
				List<NoteCommandItem> Items = [];

				foreach (NoteCommand Command in Commands)
				{
					if (!Command.OwnerNote && !Command.ExternalNote)
						continue;

					if (!NoteCommandRunner.CanRun(Command, out string[] AssignedNames))
					{
						ServiceRef.LogService.LogWarning("Note command " + Command.Id + " of token " + Token.TokenId +
							" was not shown, since its scripts aren't allowed in the app.");
						continue;
					}

					Items.Add(new NoteCommandItem(Command, AssignedNames, this.RunNoteCommandAsync, this.IsNoteCommandAvailableAsync));
				}

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.noteCommands = [.. Items];
					this.noteCommandsLoaded = true;
					this.UpdateNoteCommands();
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.noteCommandsLoaded = true;
					this.UpdateNoteCommands();
				});
			}
		}

		/// <summary>
		/// Shows the note commands the user can run: owner commands for the owner, and external commands for others, once
		/// the current state has loaded and while the state machine runs, and only those whose condition holds in the
		/// current state. The conditions run in the background, and the commands change when they are done. Called on the
		/// main thread.
		/// </summary>
		private void UpdateNoteCommands()
		{
			List<NoteCommandItem> Candidates = [];

			if (this.HasStateMachine && this.IsMachineStateKnown && !this.MachineEnded)
			{
				foreach (NoteCommandItem Item in this.noteCommands)
				{
					if (this.IsMyToken ? Item.Command.OwnerNote : Item.Command.ExternalNote)
						Candidates.Add(Item);
				}
			}

			int Version = ++this.noteCommandsVersion;
			_ = this.ShowAvailableNoteCommandsAsync(Candidates, this.machineState, new Dictionary<string, object>(this.machineVariables), Version);
		}

		/// <summary>
		/// Checks the conditions of note commands, and shows the commands that apply, unless a newer check has started.
		/// </summary>
		/// <param name="Candidates">The commands the user may run.</param>
		/// <param name="State">The current state, or null when it isn't known.</param>
		/// <param name="MachineVariables">A copy of the state machine's variables.</param>
		/// <param name="Version">Identifies the check.</param>
		private async Task ShowAvailableNoteCommandsAsync(List<NoteCommandItem> Candidates, string? State,
			Dictionary<string, object> MachineVariables, int Version)
		{
			List<NoteCommandItem> Visible = [];

			try
			{
				Task<bool>[] Checks = [.. Candidates.Select(Item => NoteCommandRunner.IsAvailableAsync(Item.Command, State, MachineVariables))];
				bool[] Applies = await Task.WhenAll(Checks);

				for (int i = 0; i < Candidates.Count; i++)
				{
					if (Applies[i])
						Visible.Add(Candidates[i]);
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (Version != this.noteCommandsVersion)
					return;

				// The rows are only rebuilt when the commands that apply change, so they don't flicker on every update.
				if (!this.NoteCommands.SequenceEqual(Visible))
					SetItems(this.NoteCommands, Visible);

				this.noteCommandCandidateCount = Candidates.Count;
				this.HasNoteCommands = this.NoteCommands.Count > 0;
				this.UpdateStateMachineSections();
			});
		}

		/// <summary>
		/// Works out which parts of the State and Actions sections are shown: placeholders while the state and the
		/// commands load, the commands that apply, or a note when none do. Called on the main thread.
		/// </summary>
		private void UpdateStateMachineSections()
		{
			bool Ready = this.noteCommandsLoaded && this.IsMachineStateKnown;

			this.ShowStateSection = this.HasStateMachine && (!this.IsMachineStateKnown || this.HasVariables);
			this.IsLoadingNoteCommands = this.HasStateMachine && !Ready && !this.machineStateFailed;
			this.ShowNoActions = this.HasStateMachine && Ready && !this.HasNoteCommands &&
				(this.MachineEnded || this.noteCommandCandidateCount > 0);
			this.NoActionsText = this.MachineEnded
				? ServiceRef.Localizer[nameof(AppResources.NoActionsEnded), this.FriendlyName ?? string.Empty]
				: ServiceRef.Localizer[nameof(AppResources.NoActionsInState)];
			this.ShowActionsSection = this.IsLoadingNoteCommands || this.HasNoteCommands || this.ShowNoActions;
		}

		/// <summary>
		/// Checks if a note command applies, with the latest state and variables of the token's state machine.
		/// </summary>
		/// <param name="Item">The note command.</param>
		/// <returns>If the command applies.</returns>
		private async Task<bool> IsNoteCommandAvailableAsync(NoteCommandItem Item)
		{
			try
			{
				(bool Running, string? State, Dictionary<string, object> MachineVariables) = await MainThread.InvokeOnMainThreadAsync(() =>
					(this.HasStateMachine && !this.MachineEnded, this.machineState, new Dictionary<string, object>(this.machineVariables)));

				return Running && await NoteCommandRunner.IsAvailableAsync(Item.Command, State, MachineVariables);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return false;
			}
		}

		/// <summary>
		/// Runs a note command. Commands with parameters ask for their values first.
		/// </summary>
		/// <param name="Item">The note command.</param>
		private async Task RunNoteCommandAsync(NoteCommandItem Item)
		{
			string? TokenId = this.TokenId;
			if (string.IsNullOrEmpty(TokenId))
				return;

			if (!Item.HasParameters)
			{
				await NoteCommandRunner.RunAsync(Item.Command, TokenId, Item.AssignedNames, Item.IsAvailableAsync);
				return;
			}

			NoteCommandViewModel ViewModel = new(Item, TokenId);
			await ViewModel.LoadAsync();
			await ServiceRef.PopupService.PushAsync(new NoteCommandPopup(ViewModel));
			await ViewModel.Result;
		}

		/// <summary>
		/// Sets a state-machine variable. The first few variables are shown inline. Called on the main thread.
		/// </summary>
		/// <param name="Name">Variable name.</param>
		/// <param name="Value">Variable value.</param>
		/// <param name="ShowChange">If a row whose value changes is tinted for a moment, with the value before.</param>
		private void SetVariable(string Name, object Value, bool ShowChange)
		{
			this.machineVariables[Name] = Value;

			bool Found = false;

			foreach (TokenVariableItem Model in this.Variables)
			{
				if (Model.Name == Name)
				{
					Model.ChangeValue(Value, ShowChange);
					Found = true;
					break;
				}
			}

			if (!Found && this.Variables.Count < MaxInlineVariables)
			{
				// The card always ends with the row that opens all variables, so the variables are its first and middle rows.
				this.Variables.Add(new TokenVariableItem(Name, Value)
				{
					CardPosition = this.Variables.Count == 0 ? ListCardPosition.First : ListCardPosition.Middle
				});
			}

			this.HasVariables = this.machineVariables.Count > 0;
			this.AllVariablesText = ServiceRef.Localizer[nameof(AppResources.AllVariables), this.machineVariables.Count];
		}

		private Task Wallet_StateUpdated(object? Sender, NewStateEventArgs e)
		{
			if (!this.HasStateMachine || !this.IsThisToken(e.TokenId))
				return Task.CompletedTask;

			MainThread.BeginInvokeOnMainThread(() => this.SetMachineState(e.NewState, string.IsNullOrEmpty(e.NewState)));

			return Task.CompletedTask;
		}

		private Task Wallet_VariablesUpdated(object? Sender, VariablesUpdatedEventArgs e)
		{
			if (!this.HasStateMachine || !this.IsThisToken(e.TokenId))
				return Task.CompletedTask;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				foreach (Waher.Script.Variable Variable in e.Variables)
					this.SetVariable(Variable.Name, Variable.ValueObject, true);

				this.UpdateNoteCommands();
			});

			return Task.CompletedTask;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Rows in "Ownership and dates".
		/// </summary>
		public ObservableCollection<TokenPropertyItem> OwnershipProperties { get; } = [];

		/// <summary>
		/// The note commands the user can run.
		/// </summary>
		public ObservableCollection<NoteCommandItem> NoteCommands { get; } = [];

		/// <summary>
		/// If there are note commands the user can run.
		/// </summary>
		[ObservableProperty]
		private bool hasNoteCommands;

		/// <summary>
		/// If the owner can give the token to someone.
		/// </summary>
		[ObservableProperty]
		private bool canGive;

		/// <summary>
		/// If the owner can destroy the token.
		/// </summary>
		[ObservableProperty]
		private bool canDestroy;

		/// <summary>
		/// The web address of the token's public page, if it has one.
		/// </summary>
		[ObservableProperty]
		private string? webUrl;

		/// <summary>
		/// If the token has a public web page.
		/// </summary>
		[ObservableProperty]
		private bool hasWebUrl;

		/// <summary>
		/// Rows in "Technical details".
		/// </summary>
		public ObservableCollection<TokenPropertyItem> TechnicalProperties { get; } = [];

		/// <summary>
		/// The creator's reference and tags.
		/// </summary>
		public ObservableCollection<TokenTagItem> Tags { get; } = [];

		/// <summary>
		/// The first state-machine variables, shown inline.
		/// </summary>
		public ObservableCollection<TokenVariableItem> Variables { get; } = [];

		/// <summary>
		/// Loads the description, and exposes its loading and loaded states.
		/// </summary>
		public ObservableTask DescriptionTask { get; } = new();

		/// <summary>
		/// Loads the state of the token's state machine, and exposes its loading, error and loaded states.
		/// </summary>
		public ObservableTask MachineStateTask { get; } = new();

		/// <summary>
		/// Web address of the schema that validates the token's definition, when it is known.
		/// </summary>
		[ObservableProperty]
		private string? definitionSchemaUrl;

		/// <summary>
		/// If the token has tags or a reference.
		/// </summary>
		[ObservableProperty]
		private bool hasTags;

		/// <summary>
		/// When token was created.
		/// </summary>
		[ObservableProperty]
		private DateTime? created;

		/// <summary>
		/// When token was last updated.
		/// </summary>
		[ObservableProperty]
		private DateTime? updated;

		/// <summary>
		/// When token expires.
		/// </summary>
		[ObservableProperty]
		private DateTime? expires;

		/// <summary>
		/// Required archiving time after token expires.
		/// </summary>
		[ObservableProperty]
		private Duration? archiveRequired;

		/// <summary>
		/// Optional archiving time after required archiving time.
		/// </summary>
		[ObservableProperty]
		private Duration? archiveOptional;

		/// <summary>
		/// Signature timestamp
		/// </summary>
		[ObservableProperty]
		private DateTime? signatureTimestamp;

		/// <summary>
		/// Token signature
		/// </summary>
		[ObservableProperty]
		private byte[]? signature;

		/// <summary>
		/// Digest of schema used to validate token definition XML.
		/// </summary>
		[ObservableProperty]
		private byte[]? definitionSchemaDigest;

		/// <summary>
		/// Hash function used to compute <see cref="DefinitionSchemaDigest"/>.
		/// </summary>
		[ObservableProperty]
		private HashFunction? definitionSchemaHashFunction;

		/// <summary>
		/// If the creator can destroy the token.
		/// </summary>
		[ObservableProperty]
		private bool creatorCanDestroy;

		/// <summary>
		/// If the owner can destroy the entire batch of tokens, if owner of every token in the batch.
		/// </summary>
		[ObservableProperty]
		private bool ownerCanDestroyBatch;

		/// <summary>
		/// If the owner can destroy an individual token.
		/// </summary>
		[ObservableProperty]
		private bool ownerCanDestroyIndividual;

		/// <summary>
		/// If a certifier can destroy the token.
		/// </summary>
		[ObservableProperty]
		private bool certifierCanDestroy;

		/// <summary>
		/// Friendly name of token.
		/// </summary>
		[ObservableProperty]
		private string? friendlyName;

		/// <summary>
		/// Category of token.
		/// </summary>
		[ObservableProperty]
		private string? category;

		/// <summary>
		/// If the token has a category.
		/// </summary>
		[ObservableProperty]
		private bool hasCategory;

		/// <summary>
		/// Description of token.
		/// </summary>
		[ObservableProperty]
		private object? description;

		/// <summary>
		/// Content-Type of glyph
		/// </summary>
		[ObservableProperty]
		private string? glyphContentType;

		/// <summary>
		/// Ordinal of token, within batch.
		/// </summary>
		[ObservableProperty]
		private int? ordinal;

		/// <summary>
		/// (Last) Value of token
		/// </summary>
		[ObservableProperty]
		private decimal? value;

		/// <summary>
		/// Method of assigning the Token ID.
		/// </summary>
		[ObservableProperty]
		private TokenIdMethod? tokenIdMethod;

		/// <summary>
		/// Token ID
		/// </summary>
		[ObservableProperty]
		private string? tokenId;

		/// <summary>
		/// ShortToken ID
		/// </summary>
		[ObservableProperty]
		private string? shortTokenId;

		/// <summary>
		/// Token XML
		/// </summary>
		[ObservableProperty]
		private string? tokenXml;

		/// <summary>
		/// Visibility of token
		/// </summary>
		[ObservableProperty]
		private ContractVisibility? visibility;

		/// <summary>
		/// Creator of token
		/// </summary>
		[ObservableProperty]
		private string? creator;

		/// <summary>
		/// CreatorFriendlyName of token
		/// </summary>
		[ObservableProperty]
		private string? creatorFriendlyName;

		/// <summary>
		/// JID of <see cref="Creator"/>.
		/// </summary>
		[ObservableProperty]
		private string? creatorJid;

		/// <summary>
		/// Current owner
		/// </summary>
		[ObservableProperty]
		private string? owner;

		/// <summary>
		/// Current owner
		/// </summary>
		[ObservableProperty]
		private string? ownerFriendlyName;

		/// <summary>
		/// JID of owner
		/// </summary>
		[ObservableProperty]
		private string? ownerJid;

		/// <summary>
		/// Number of tokens in batch being created.
		/// </summary>
		[ObservableProperty]
		private int? batchSize;

		/// <summary>
		/// Trust Provider asserting the validity of the token
		/// </summary>
		[ObservableProperty]
		private string? trustProvider;

		/// <summary>
		/// Trust Provider asserting the validity of the token
		/// </summary>
		[ObservableProperty]
		private string? trustProviderFriendlyName;

		/// <summary>
		/// JID of <see cref="TrustProvider"/>
		/// </summary>
		[ObservableProperty]
		private string? trustProviderJid;

		/// <summary>
		/// Currency of <see cref="Value"/>.
		/// </summary>
		[ObservableProperty]
		private string? currency;

		/// <summary>
		/// Any reference provided by the token creator.
		/// </summary>
		[ObservableProperty]
		private string? reference;

		/// <summary>
		/// XML Definition of token.
		/// </summary>
		[ObservableProperty]
		private string? definition;

		/// <summary>
		/// XML Namespace used in the <see cref="Definition"/>
		/// </summary>
		[ObservableProperty]
		private string? definitionNamespace;

		/// <summary>
		/// Contract used to create the contract.
		/// </summary>
		[ObservableProperty]
		private string? creationContract;

		/// <summary>
		/// Contract used to define the current ownership
		/// </summary>
		[ObservableProperty]
		private string? ownershipContract;

		/// <summary>
		/// Gets or sets the image representing the glyph.
		/// </summary>
		[ObservableProperty]
		private ImageSource? glyphImage;

		/// <summary>
		/// Gets or sets the value representing of a glyph is available or not.
		/// </summary>
		[ObservableProperty]
		private bool hasGlyphImage;

		/// <summary>
		/// If the token placeholder is shown, because the token has no glyph, or its glyph can't be read.
		/// </summary>
		[ObservableProperty]
		private bool showGlyphPlaceholder;

		/// <summary>
		/// Display width of the glyph.
		/// </summary>
		[ObservableProperty]
		private double glyphWidth;

		/// <summary>
		/// Display height of the glyph.
		/// </summary>
		[ObservableProperty]
		private double glyphHeight;

		/// <summary>
		/// The rendered embedded layout, shown instead of the glyph.
		/// </summary>
		[ObservableProperty]
		private ImageSource? embeddedLayoutImage;

		/// <summary>
		/// If the embedded layout has been rendered.
		/// </summary>
		[ObservableProperty]
		private bool hasEmbeddedLayoutImage;

		/// <summary>
		/// If the token belongs to the user.
		/// </summary>
		[ObservableProperty]
		private bool isMyToken;

		/// <summary>
		/// If the token left the user's wallet while the page was open.
		/// </summary>
		[ObservableProperty]
		private bool hasLeftWallet;

		/// <summary>
		/// Says who owns the token: the user, or someone else.
		/// </summary>
		[ObservableProperty]
		private string? ownershipText;

		/// <summary>
		/// If the token has expired.
		/// </summary>
		[ObservableProperty]
		private bool isExpired;

		/// <summary>
		/// Says that the token has expired or expires soon.
		/// </summary>
		[ObservableProperty]
		private string? expiryText;

		/// <summary>
		/// If <see cref="ExpiryText"/> has a text to show.
		/// </summary>
		[ObservableProperty]
		private bool hasExpiryText;

		/// <summary>
		/// If the token is associated with a state-machine.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasTechnicalLinks))]
		private bool hasStateMachine;

		/// <summary>
		/// If the token have an embedded layout.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasTechnicalLinks))]
		private bool hasEmbeddedLayout;

		/// <summary>
		/// If Technical details links to the profiling report or the embedded layout.
		/// </summary>
		public bool HasTechnicalLinks => this.HasStateMachine || this.HasEmbeddedLayout;

		/// <summary>
		/// If the token comes from the wallet or was fetched from its Neuron, and can be trusted.
		/// </summary>
		[ObservableProperty]
		private bool isVerified;

		/// <summary>
		/// If the token is a shared copy that couldn't be checked with its Neuron.
		/// </summary>
		[ObservableProperty]
		private bool isUnverifiedCopy;

		/// <summary>
		/// If the token has changed since a copy of it was shared.
		/// </summary>
		[ObservableProperty]
		private bool changedSinceShared;

		/// <summary>
		/// If the user can offer the token for sale to a buyer.
		/// </summary>
		[ObservableProperty]
		private bool canOfferToSell;

		/// <summary>
		/// If the user can consign the token to an auction.
		/// </summary>
		[ObservableProperty]
		private bool canConsign;

		/// <summary>
		/// If the user can offer to buy the token.
		/// </summary>
		[ObservableProperty]
		private bool canOfferToBuy;

		/// <summary>
		/// If the token's notes and history can be shown.
		/// </summary>
		[ObservableProperty]
		private bool canViewHistory;

		/// <summary>
		/// Label of the history action: "Notes and history" for the owner, "History" for others.
		/// </summary>
		[ObservableProperty]
		private string? historyLabel;

		/// <summary>
		/// Says why the token can't change owner, when it can't.
		/// </summary>
		[ObservableProperty]
		private string? transferBlockedText;

		/// <summary>
		/// If <see cref="TransferBlockedText"/> has a text to show.
		/// </summary>
		[ObservableProperty]
		private bool hasTransferBlockedText;

		/// <summary>
		/// If the token has a description worth showing: one that isn't empty, and isn't just the name.
		/// </summary>
		[ObservableProperty]
		private bool hasDescription;

		/// <summary>
		/// Position of the token within its batch, as text.
		/// </summary>
		[ObservableProperty]
		private string? ordinalText;

		/// <summary>
		/// If "Technical details" is expanded.
		/// </summary>
		[ObservableProperty]
		private bool isTechnicalDetailsExpanded;

		/// <summary>
		/// The state of the state machine, as text.
		/// </summary>
		[ObservableProperty]
		private string? machineStateText;

		/// <summary>
		/// If the state machine has ended.
		/// </summary>
		[ObservableProperty]
		private bool machineEnded;

		/// <summary>
		/// If the state machine has variables.
		/// </summary>
		[ObservableProperty]
		private bool hasVariables;

		/// <summary>
		/// Text shown when the state of the state machine couldn't be loaded.
		/// </summary>
		[ObservableProperty]
		private string? machineStateErrorText;

		/// <summary>
		/// What screen readers say about the state chip, such as "State: Calculating".
		/// </summary>
		[ObservableProperty]
		private string? machineStateDescription;

		/// <summary>
		/// If the current state of the state machine has loaded.
		/// </summary>
		[ObservableProperty]
		private bool isMachineStateKnown;

		/// <summary>
		/// The last row of the State card, such as "All 3 variables".
		/// </summary>
		[ObservableProperty]
		private string? allVariablesText;

		/// <summary>
		/// If the State section is shown: while the state loads, when it couldn't load, and when there are variables.
		/// </summary>
		[ObservableProperty]
		private bool showStateSection;

		/// <summary>
		/// If the Actions section is shown: while the commands load, when commands apply, or with a note saying why none do.
		/// </summary>
		[ObservableProperty]
		private bool showActionsSection;

		/// <summary>
		/// If the state and the note commands are still loading.
		/// </summary>
		[ObservableProperty]
		private bool isLoadingNoteCommands;

		/// <summary>
		/// If the Actions section says that nothing can be run now.
		/// </summary>
		[ObservableProperty]
		private bool showNoActions;

		/// <summary>
		/// Why nothing can be run now.
		/// </summary>
		[ObservableProperty]
		private string? noActionsText;

		/// <summary>
		/// If the contract templates for the token actions are still being looked up.
		/// </summary>
		[ObservableProperty]
		private bool isLoadingTemplates;

		/// <summary>
		/// If the templates for selling and buying are still being looked up, and More shows a placeholder row.
		/// </summary>
		[ObservableProperty]
		private bool isLoadingTradeActions;

		/// <summary>
		/// If the actions under More are shown: selling or offering to buy, and the actions used less often.
		/// </summary>
		[ObservableProperty]
		private bool isMoreVisible;

		#endregion

		#region Commands

		/// <summary>
		/// Expands or collapses "Technical details".
		/// </summary>
		[RelayCommand]
		private void ToggleTechnicalDetails()
		{
			this.IsTechnicalDetailsExpanded = !this.IsTechnicalDetailsExpanded;
		}

		/// <summary>
		/// Shows or hides the actions under More.
		/// </summary>
		[RelayCommand]
		private void ToggleMore()
		{
			this.IsMoreVisible = !this.IsMoreVisible;
		}

		/// <summary>
		/// Command to view the token's machine-readable definition.
		/// </summary>
		[RelayCommand]
		private async Task ViewDefinition()
		{
			if (string.IsNullOrEmpty(this.Definition))
				return;

			try
			{
				XmlViewerNavigationArgs Args = new(this.FriendlyName ?? ServiceRef.Localizer[nameof(AppResources.ViewDefinition)], this.Definition);
				await ServiceRef.NavigationService.GoToAsync(nameof(XmlViewerPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to send token to contact
		/// </summary>
		[RelayCommand]
		private async Task SendToContact()
		{
			Token? Token = this.navigationArguments?.Token?.Token;
			if (Token is null)
				return;

			TaskCompletionSource<ContactInfoModel?> Selected = new();
			ContactListNavigationArgs ContactListArgs = new(ServiceRef.Localizer[nameof(AppResources.SendInformationTo)], Selected)
			{
				CanScanQrCode = true,
			};

			await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), ContactListArgs, BackMethod.Pop);

			ContactInfoModel? Contact = await Selected.Task;
			if (Contact is null)
				return;

			string ContactName = Contact.FriendlyName ?? Contact.BareJid?.Value ?? string.Empty;
			if (!await TokenShareFormatter.ConfirmShareAsync(Token, ContactName))
				return;

			await ChatViewModel.ExecuteSendMessage(string.Empty, TokenShareFormatter.ToChatMarkdown(Token), Contact.BareJid);

			if (Contact.Contact is not null)
			{
				await Task.Delay(100);  // Otherwise, page doesn't show properly. (Underlying timing issue. TODO: Find better solution.)

				ChatNavigationArgs ChatArgs = new(Contact.Contact);
				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), ChatArgs, BackMethod.Inherited, Contact.BareJid);
			}
		}

		/// <summary>
		/// Command to share token with other applications
		/// </summary>
		[RelayCommand]
		private async Task Share()
		{
			try
			{
				if (this.QrCodeBin is null)
					return;

				await this.OpenQrPopup(this.FriendlyName);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to consign the token to an auction on the marketplace.
		/// </summary>
		[RelayCommand]
		private Task PublishMarketplace()
		{
			return this.PrepareContractAsync(TokenAction.Consign, null);
		}

		/// <summary>
		/// Command to offer the token for sale.
		/// </summary>
		[RelayCommand]
		private Task OfferToSell()
		{
			return this.PrepareContractAsync(TokenAction.Sell, null);
		}

		/// <summary>
		/// Command to offer to buy the token.
		/// </summary>
		[RelayCommand]
		private Task OfferToBuy()
		{
			return this.PrepareContractAsync(TokenAction.Buy, null);
		}

		/// <summary>
		/// Command to give the token to a contact, or to someone whose identity is scanned. The recipient must have a legal
		/// identity, since the donation contract names them.
		/// </summary>
		[RelayCommand]
		private async Task Give()
		{
			try
			{
				TaskCompletionSource<ContactInfoModel?> Selected = new();
				ContactListNavigationArgs ContactListArgs = new(ServiceRef.Localizer[nameof(AppResources.ChooseTokenRecipient)], Selected)
				{
					CanScanQrCode = true,
				};

				await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), ContactListArgs, BackMethod.Pop);

				ContactInfoModel? Contact = await Selected.Task;
				if (Contact is null)
					return;

				string? LegalId = Contact.LegalId?.Value;
				if (string.IsNullOrEmpty(LegalId))
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.Give)],
						ServiceRef.Localizer[nameof(AppResources.ContactHasNoLegalId)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					return;
				}

				await this.PrepareContractAsync(TokenAction.Give, LegalId);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Command to destroy the token. The user confirms first, and then reviews the destruction contract.
		/// </summary>
		[RelayCommand]
		private async Task Destroy()
		{
			string Name = string.IsNullOrWhiteSpace(this.FriendlyName) ? this.ShortTokenId ?? this.TokenId ?? string.Empty : this.FriendlyName;

			bool Confirmed = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.Destroy)],
				ServiceRef.Localizer[nameof(AppResources.DestroyTokenConfirm), Name],
				ServiceRef.Localizer[nameof(AppResources.Destroy)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);

			if (Confirmed)
				await this.PrepareContractAsync(TokenAction.Destroy, null);
		}

		/// <summary>
		/// Command to open the token's public page in the browser.
		/// </summary>
		[RelayCommand]
		private async Task OpenOnWeb()
		{
			if (this.WebUrl is null)
				return;

			try
			{
				await App.OpenUrlAsync(this.WebUrl);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Command to share the address of the token's public page with other apps.
		/// </summary>
		[RelayCommand]
		private async Task ShareLink()
		{
			if (this.WebUrl is null)
				return;

			try
			{
				await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new ShareTextRequest()
				{
					Title = this.FriendlyName,
					Uri = this.WebUrl
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Command to view the token's notes and history. The events page loads them itself, page by page.
		/// </summary>
		[RelayCommand]
		private async Task ViewEvents()
		{
			if (this.TokenId is null)
				return;

			try
			{
				TokenEventsNavigationArgs Args = new(this.TokenId, this.IsMyToken)
				{
					MachineId = this.machineId
				};

				await ServiceRef.NavigationService.GoToAsync(nameof(TokenEventsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to show the token's embedded layout on its own page.
		/// </summary>
		[RelayCommand]
		private async Task PresentEmbeddedLayout()
		{
			if (this.TokenId is null || this.navigationArguments?.Token is null)
				return;

			EmbeddedLayoutNavigationArgs Args = new(this.navigationArguments.Token);

			await ServiceRef.NavigationService.GoToAsync(nameof(EmbeddedLayoutPage), Args, BackMethod.Pop);
		}

		/// <summary>
		/// Command to display the present state report of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task PresentReport()
		{
			if (this.TokenId is null)
				return;

			await this.ShowReport(new TokenPresentReport(this.TokenId));
		}

		/// <summary>
		/// Command to display the historic states report of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task HistoryReport()
		{
			if (this.TokenId is null)
				return;

			await this.ShowReport(new TokenHistoryReport(this.TokenId));
		}

		/// <summary>
		/// Command to display the state diagram of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task StatesReport()
		{
			if (this.TokenId is null)
				return;

			await this.ShowReport(new TokenStateDiagramReport(this.TokenId));
		}

		/// <summary>
		/// Command to display a profiling report of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task ProfilingReport()
		{
			if (this.TokenId is null)
				return;

			await this.ShowReport(new TokenProfilingReport(this.TokenId));
		}

		/// <summary>
		/// Command to display the current states of variables of a state-machine.
		/// </summary>
		[RelayCommand]
		private async Task VariablesReport()
		{
			if (this.TokenId is null)
				return;

			try
			{
				CurrentStateEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCurrentState(this.TokenId);
				if (e.Ok)
				{
					MachineVariablesNavigationArgs Args = new(this.TokenId, e.Running, e.Ended, e.CurrentState, e.Variables);

					await ServiceRef.NavigationService.GoToAsync(nameof(MachineVariablesPage), Args, BackMethod.Pop);
				}
				else
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], e.ErrorText);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async Task ShowReport(TokenReport Report)
		{
			try
			{
				MachineReportNavigationArgs Args = new(Report);

				await ServiceRef.NavigationService.GoToAsync(nameof(MachineReportPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
			}
		}

		#endregion

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult<string>(this.FriendlyName ?? string.Empty);

		#endregion
	}
}
