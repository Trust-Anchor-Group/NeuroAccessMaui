using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Wallet.Tokens;
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
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using NeuroFeatures.Events;
using NeuroFeatures.Tags;
using ShimSkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Web;
using System.Xml;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Security;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of a token.
	/// </summary>
	/// <param name="Page">Page hosting details.</param>
	/// <param name="Args">Navigation arguments.</param>
	public partial class TokenDetailsViewModel(TokenDetailsPage Page, TokenDetailsNavigationArgs? Args) : QrXmppViewModel()
	{
		private readonly TokenDetailsNavigationArgs? navigationArguments = Args;
		private readonly TokenDetailsPage page = Page;

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (this.navigationArguments is not null)
			{
				this.Created = this.navigationArguments.Token?.Created;
				this.Updated = this.navigationArguments.Token?.Updated;
				this.Expires = this.navigationArguments.Token?.Expires;
				this.ArchiveRequired = this.navigationArguments.Token?.ArchiveRequired;
				this.ArchiveOptional = this.navigationArguments.Token?.ArchiveOptional;
				this.SignatureTimestamp = this.navigationArguments.Token?.SignatureTimestamp;
				this.Signature = this.navigationArguments.Token?.Signature;
				this.DefinitionSchemaDigest = this.navigationArguments.Token?.DefinitionSchemaDigest;
				this.DefinitionSchemaHashFunction = this.navigationArguments.Token?.DefinitionSchemaHashFunction;
				this.CreatorCanDestroy = this.navigationArguments.Token?.CreatorCanDestroy ?? false;
				this.OwnerCanDestroyBatch = this.navigationArguments.Token?.OwnerCanDestroyBatch ?? false;
				this.OwnerCanDestroyIndividual = this.navigationArguments.Token?.OwnerCanDestroyIndividual ?? false;
				this.CertifierCanDestroy = this.navigationArguments.Token?.CertifierCanDestroy ?? false;
				this.FriendlyName = this.navigationArguments.Token?.FriendlyName;
				this.Category = this.navigationArguments.Token?.Category;
				this.GlyphContentType = this.navigationArguments.Token?.GlyphContentType;
				this.Ordinal = this.navigationArguments.Token?.Ordinal;
				this.Value = this.navigationArguments.Token?.Value;
				this.TokenIdMethod = this.navigationArguments.Token?.TokenIdMethod;
				this.TokenId = this.navigationArguments.Token?.TokenId;
				this.ShortTokenId = this.navigationArguments.Token?.ShortTokenId;
				this.Visibility = this.navigationArguments.Token?.Visibility;
				this.Creator = this.navigationArguments.Token?.Creator;
				this.CreatorFriendlyName = await ContactInfo.GetFriendlyName(this.navigationArguments.Token?.Creator);
				this.CreatorJid = this.navigationArguments.Token?.CreatorJid;
				this.Owner = this.navigationArguments.Token?.Owner;
				this.OwnerFriendlyName = await ContactInfo.GetFriendlyName(this.navigationArguments.Token?.Owner);
				this.OwnerJid = this.navigationArguments.Token?.OwnerJid;
				this.BatchSize = this.navigationArguments.Token?.BatchSize;
				this.TrustProvider = this.navigationArguments.Token?.TrustProvider;
				this.TrustProviderFriendlyName = await ContactInfo.GetFriendlyName(this.navigationArguments.Token?.TrustProvider);
				this.TrustProviderJid = this.navigationArguments.Token?.TrustProviderJid;
				this.Currency = this.navigationArguments.Token?.Currency;
				this.Reference = this.navigationArguments.Token?.Reference;
				this.Definition = this.navigationArguments.Token?.Definition;
				this.DefinitionNamespace = this.navigationArguments.Token?.DefinitionNamespace;
				this.CreationContract = this.navigationArguments.Token?.CreationContract;
				this.OwnershipContract = this.navigationArguments.Token?.OwnershipContract;
				this.GlyphWidth = TokenGlyph.DetailsBoxSize;
				this.GlyphHeight = TokenGlyph.DetailsBoxSize;
				this.TokenXml = this.navigationArguments.Token?.Token.ToXml();

				// A shared copy that couldn't be checked with its Neuron is labeled, and offers nothing beyond copy and share.
				// Its embedded layout isn't rendered, since rendering runs the token's own script.
				this.IsUnverifiedCopy = this.navigationArguments.Origin == TokenOrigin.UnverifiedCopy;
				this.IsVerified = !this.IsUnverifiedCopy;
				this.ChangedSinceShared = this.IsVerified && this.navigationArguments.ChangedSinceShared;
				this.IsMyToken = this.IsVerified && string.Equals(this.OwnerJid, ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase);
				this.CanOfferToBuy = this.IsVerified && !this.IsMyToken;
				this.HasStateMachine = this.IsVerified && (this.navigationArguments.Token?.HasStateMachine ?? false);
				this.HasEmbeddedLayout = this.IsVerified && (this.navigationArguments.Token?.HasEmbeddedLayout ?? false);
				this.OrdinalText = ServiceRef.Localizer[nameof(AppResources.TokenOrdinalOfBatch), this.Ordinal ?? 0, this.BatchSize ?? 0];

				string? RawDescription = this.navigationArguments.Token?.Description;
				this.HasDescription = !string.IsNullOrWhiteSpace(RawDescription) && RawDescription != this.FriendlyName;

				if (this.HasDescription)
					this.DescriptionTask.Load(this.LoadDescriptionAsync);

				if (this.navigationArguments.Token is not null)
					_ = this.LoadGlyphAsync(this.navigationArguments.Token.Token);

				// Only http(s) values are shown as links.
				if (!string.IsNullOrEmpty(this.navigationArguments.Token?.Reference))
					this.page.AddTag(this, ServiceRef.Localizer[nameof(AppResources.Reference)], this.navigationArguments.Token.Reference);   // TODO: Replace with grouped collection, when this works in Maui.

				if (this.navigationArguments.Token?.Tags is not null)
				{
					foreach (TokenTag Tag in this.navigationArguments.Token.Tags)
						this.page.AddTag(this, Tag.Name, Tag.Value?.ToString() ?? string.Empty);   // TODO: Replace with grouped collection, when this works in Maui.
				}

				if (this.TokenId is not null)
					this.GenerateQrCode(Constants.UriSchemes.CreateTokenUri(this.TokenId));

				await this.Populate(ServiceRef.Localizer[nameof(AppResources.Witness)], string.Empty, this.navigationArguments.Token?.Witness, null, this.Witnesses);
				await this.Populate(ServiceRef.Localizer[nameof(AppResources.Certifier)], ServiceRef.Localizer[nameof(AppResources.CertifierJid)], this.navigationArguments.Token?.Certifier, this.navigationArguments.Token?.CertifierJids, this.Certifiers);
				await this.Populate(ServiceRef.Localizer[nameof(AppResources.Valuator)], string.Empty, this.navigationArguments.Token?.Valuator, null, this.Valuators);
				await this.Populate(ServiceRef.Localizer[nameof(AppResources.Assessor)], string.Empty, this.navigationArguments.Token?.Assessor, null, this.Assessors);

				this.Tags.Clear();

				if (this.navigationArguments.Token?.Tags is not null)
				{
					foreach (TokenTag Tag in this.navigationArguments.Token.Tags)
						this.Tags.Add(Tag);
				}

				StringBuilder sb = new();
				string Domain = this.TokenId?.After("@") ?? string.Empty;
				Domain = Domain.After("."); // Remove last sub-domain, corresponding to the component hosting the token.

				sb.Append("https://");
				sb.Append(Domain);
				sb.Append("/ValidationSchema.md?NS=");
				sb.Append(HttpUtility.UrlEncode(this.DefinitionNamespace));
				sb.Append("&H=");

				if (this.DefinitionSchemaDigest is not null)
					sb.Append(HttpUtility.UrlEncode(Convert.ToBase64String(this.DefinitionSchemaDigest)));

				sb.Append("&Download=1");

				this.DefinitionSchemaUrl = sb.ToString();   // TODO: The above assume contract hosted by the TAG Neuron. URL should be retrieved using API, or be standardized.
			}
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
			this.DescriptionTask.Cancel();

			await base.OnDisposeAsync();
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

		private async Task Populate(string LegalIdLabel, string JidLabel, string[]? LegalIds, string[]? Jids, ObservableCollection<PartItem> Parts)
		{
			int i, c = LegalIds?.Length ?? 0;
			int d = Jids?.Length ?? 0;
			string FriendlyName;
			string Jid;

			Parts.Clear();

			for (i = 0; i < c; i++)
			{
				FriendlyName = await ContactInfo.GetFriendlyName(LegalIds![i]);
				Jid = i < d ? (Jids![i] ?? string.Empty) : string.Empty;

				Parts.Add(new PartItem(LegalIds[i], Jid, FriendlyName));

				this.page.AddLegalId(this, LegalIdLabel, FriendlyName, LegalIds[i]);    // TODO: Replace with grouped collection, when this works in Maui.

				if (!string.IsNullOrEmpty(Jid))
					this.page.AddJid(this, JidLabel, Jid, LegalIds[i], FriendlyName);   // TODO: Replace with grouped collection, when this works in Maui.
			}
		}

		/// <summary>
		/// Token ID
		/// </summary>
		[ObservableProperty]
		private string? definitionSchemaUrl;

		#region Properties

		/// <summary>
		/// Certifiers
		/// </summary>
		public ObservableCollection<PartItem> Certifiers { get; } = [];

		/// <summary>
		/// Valuators
		/// </summary>
		public ObservableCollection<PartItem> Valuators { get; } = [];

		/// <summary>
		/// Assessors
		/// </summary>
		public ObservableCollection<PartItem> Assessors { get; } = [];

		/// <summary>
		/// Witnesses
		/// </summary>
		public ObservableCollection<PartItem> Witnesses { get; } = [];

		/// <summary>
		/// Witnesses
		/// </summary>
		public ObservableCollection<TokenTag> Tags { get; } = [];

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
		/// Friendly name of token.
		/// </summary>
		[ObservableProperty]
		private string? category;

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

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.ShortTokenId):
					this.HasShortTokenId = !string.IsNullOrEmpty(this.ShortTokenId);
					break;
			}
		}

		/// <summary>
		/// If the token has a Short ID
		/// </summary>
		[ObservableProperty]
		private bool hasShortTokenId;

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
		/// If the token belongs to the user.
		/// </summary>
		[ObservableProperty]
		private bool isMyToken;

		/// <summary>
		/// If the token is associated with a state-machine.
		/// </summary>
		[ObservableProperty]
		private bool hasStateMachine;

		/// <summary>
		/// If the token have an embedded layout.
		/// </summary>
		[ObservableProperty]
		private bool hasEmbeddedLayout;

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
		/// If the user can offer to buy the token.
		/// </summary>
		[ObservableProperty]
		private bool canOfferToBuy;

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
		/// Loads the description, and exposes its loading and loaded states.
		/// </summary>
		public ObservableTask DescriptionTask { get; } = new();


		#endregion

		#region Commands

		/// <summary>
		/// Command to copy a value to the clipboard.
		/// </summary>
		[RelayCommand]
		public static async Task CopyToClipboard(object Parameter)
		{
			try
			{
				string s = Parameter?.ToString() ?? string.Empty;
				int i = s.IndexOf('@');

				if (i > 0 && Guid.TryParse(s[..i], out _))
				{
					await Clipboard.SetTextAsync(Constants.UriSchemes.NeuroFeature + ":" + s);
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
						ServiceRef.Localizer[nameof(AppResources.IdCopiedSuccessfully)]);
				}
				else
				{
					await Clipboard.SetTextAsync(s);
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
						ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to view a Legal ID.
		/// </summary>
		[RelayCommand]
		private static async Task ViewId(object Parameter)
		{
			string? LegalId = Parameter?.ToString();
			if (string.IsNullOrEmpty(LegalId))
				return;

			try
			{
				await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(LegalId, ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)]);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to view a smart contract.
		/// </summary>
		[RelayCommand]
		private static async Task ViewContract(object Parameter)
		{
			string? ContractId = Parameter?.ToString();
			if (string.IsNullOrEmpty(ContractId))
				return;

			try
			{
				await ServiceRef.ContractOrchestratorService.OpenContract(ContractId, ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)], null);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to open a chat page.
		/// </summary>
		[RelayCommand]
		private async Task OpenChat(object Parameter)
		{
			string? s = Parameter?.ToString();
			if (string.IsNullOrEmpty(s))
				return;

			string? BareJid;
			string? LegalId;
			string? FriendlyName;

			switch (s)
			{
				case "Owner":
					BareJid = this.OwnerJid;
					LegalId = this.Owner;
					FriendlyName = this.OwnerFriendlyName;
					break;

				case "Creator":
					BareJid = this.CreatorJid;
					LegalId = this.Creator;
					FriendlyName = this.CreatorFriendlyName;
					break;

				case "TrustProvider":
					BareJid = this.TrustProviderJid;
					LegalId = this.TrustProvider;
					FriendlyName = this.TrustProviderFriendlyName;
					break;

				default:
					string[] Parts = s.Split(" | ");
					if (Parts.Length != 3)
						return;

					BareJid = Parts[0];
					LegalId = Parts[1];
					FriendlyName = Parts[2];
					break;
			}

			try
			{
				ChatNavigationArgs Args = new(LegalId, BareJid, FriendlyName);
				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), Args, BackMethod.Inherited, BareJid);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to open a link.
		/// </summary>
		[RelayCommand]
		private static async Task OpenLink(object Parameter)
		{
			if (Parameter is not null)
				await App.OpenUrlAsync(Parameter.ToString()!);
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

				string FileName = "Token.QR." + InternetContent.GetFileExtension(this.QrCodeContentType);

				await this.OpenQrPopup(this.FriendlyName);

				//ServiceRef.PlatformSpecific.ShareImage(this.QrCodeBin, this.FriendlyName ?? string.Empty,
				//	ServiceRef.Localizer[nameof(AppResources.Share)], FileName);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to publish the token on the marketplace, for sale.
		/// </summary>
		[RelayCommand]
		private async Task PublishMarketplace()
		{
			try
			{
				CreationAttributesEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
				Contract Template = await ServiceRef.XmppService.GetContract(Constants.ContractTemplates.TokenConsignmentTemplate);
				Template.Visibility = ContractVisibility.Public;
				NewContractNavigationArgs NewContractArgs = new(Template, true,
					new Dictionary<CaseInsensitiveString, object>()
					{
						{ "TokenID", this.TokenId ?? string.Empty },
						{ "Category", this.Category ?? string.Empty },
						{ "FriendlyName", this.FriendlyName ?? string.Empty },
						{ "CommissionPercent", e.Commission },
						{ "Currency", e.Currency }
					});

				Template.Parts = 
				[
					new()
					{
						Role = "Seller",
						LegalId = ServiceRef.TagProfile.LegalIdentity?.Id
					},
					new()
					{
						Role = "Auctioneer",
						LegalId = e.TrustProviderId
					}
				];

				NewContractArgs.SuppressProposal(e.TrustProviderId);

				await ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs, BackMethod.CurrentPage);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to offer the token for sale.
		/// </summary>
		[RelayCommand]
		private async Task OfferToSell()
		{
			try
			{
				Dictionary<CaseInsensitiveString, object> Parameters = [];
				string? TrustProviderId = null;
				Contract Template = await ServiceRef.XmppService.GetContract(Constants.ContractTemplates.TransferTokenTemplate);
				Template.Visibility = ContractVisibility.Public;

				if (Template.ForMachinesLocalName == "Transfer" && Template.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures)
				{
					CreationAttributesEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
					Doc.LoadXml(Template.ForMachines.OuterXml);

					TrustProviderId = e.TrustProviderId;

					XmlNamespaceManager NamespaceManager = new(Doc.NameTable);
					NamespaceManager.AddNamespace("nft", NeuroFeaturesClient.NamespaceNeuroFeatures);

					string? SellerRole = Doc.SelectSingleNode("/nft:Transfer/nft:Seller/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? TrustProviderRole = Doc.SelectSingleNode("/nft:Transfer/nft:TrustProvider/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? TokenIdParameter = Doc.SelectSingleNode("/nft:Transfer/nft:TokenID/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? CurrencyParameter = Doc.SelectSingleNode("/nft:Transfer/nft:Currency/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? CommissionParameter = Doc.SelectSingleNode("/nft:Transfer/nft:CommissionPercent/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? OwnershipContractParameter = Doc.SelectSingleNode("/nft:Transfer/nft:OwnershipContract/nft:ParameterReference/@parameter", NamespaceManager)?.Value;

					if (Template.Parts is null)
					{
						List<Part> Parts = [];

						if (!string.IsNullOrEmpty(SellerRole))
						{
							Parts.Add(new Part()
							{
								LegalId = ServiceRef.TagProfile.LegalIdentity?.Id,
								Role = SellerRole
							});
						}

						if (!string.IsNullOrEmpty(TrustProviderRole))
						{
							Parts.Add(new Part()
							{
								LegalId = e.TrustProviderId,
								Role = TrustProviderRole
							});
						}

						Template.Parts = [.. Parts];
						Template.PartsMode = ContractParts.ExplicitlyDefined;
					}
					else
					{
						foreach (Part Part in Template.Parts)
						{
							if (Part.Role == SellerRole)
								Part.LegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
							else if (Part.Role == TrustProviderRole)
								Part.LegalId = e.TrustProviderId;
						}
					}

					if (!string.IsNullOrEmpty(TokenIdParameter))
						Parameters[TokenIdParameter] = this.TokenId ?? string.Empty;

					if (!string.IsNullOrEmpty(CurrencyParameter))
						Parameters[CurrencyParameter] = e.Currency;

					if (!string.IsNullOrEmpty(CommissionParameter))
						Parameters[CommissionParameter] = e.Commission;

					if (!string.IsNullOrEmpty(OwnershipContractParameter))
						Parameters[OwnershipContractParameter] = this.OwnershipContract ?? string.Empty;
				}

				NewContractNavigationArgs NewContractArgs = new(Template, true, Parameters);

				if (!string.IsNullOrEmpty(TrustProviderId))
					NewContractArgs.SuppressProposal(TrustProviderId);

				await ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs, BackMethod.CurrentPage);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to offer to buy the token.
		/// </summary>
		[RelayCommand]
		private async Task OfferToBuy()
		{
			try
			{
				Dictionary<CaseInsensitiveString, object> Parameters = [];
				string? TrustProviderId = null;
				Contract Template = await ServiceRef.XmppService.GetContract(Constants.ContractTemplates.TransferTokenTemplate);
				Template.Visibility = ContractVisibility.Public;

				if (Template.ForMachinesLocalName == "Transfer" && Template.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures)
				{
					CreationAttributesEventArgs e = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
					Doc.LoadXml(Template.ForMachines.OuterXml);

					TrustProviderId = e.TrustProviderId;

					XmlNamespaceManager NamespaceManager = new(Doc.NameTable);
					NamespaceManager.AddNamespace("nft", NeuroFeaturesClient.NamespaceNeuroFeatures);

					string? BuyerRole = Doc.SelectSingleNode("/nft:Transfer/nft:Buyer/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? SellerRole = Doc.SelectSingleNode("/nft:Transfer/nft:Seller/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? TrustProviderRole = Doc.SelectSingleNode("/nft:Transfer/nft:TrustProvider/nft:RoleReference/@role", NamespaceManager)?.Value;
					string? TokenIdParameter = Doc.SelectSingleNode("/nft:Transfer/nft:TokenID/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? CurrencyParameter = Doc.SelectSingleNode("/nft:Transfer/nft:Currency/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? CommissionParameter = Doc.SelectSingleNode("/nft:Transfer/nft:CommissionPercent/nft:ParameterReference/@parameter", NamespaceManager)?.Value;
					string? OwnershipContractParameter = Doc.SelectSingleNode("/nft:Transfer/nft:OwnershipContract/nft:ParameterReference/@parameter", NamespaceManager)?.Value;

					if (Template.Parts is null)
					{
						List<Part> Parts = [];

						if (!string.IsNullOrEmpty(BuyerRole))
						{
							Parts.Add(new Part()
							{
								LegalId = ServiceRef.TagProfile.LegalIdentity?.Id,
								Role = BuyerRole
							});
						}

						if (!string.IsNullOrEmpty(SellerRole))
						{
							Parts.Add(new Part()
							{
								LegalId = this.Owner,
								Role = SellerRole
							});
						}

						if (!string.IsNullOrEmpty(TrustProviderRole))
						{
							Parts.Add(new Part()
							{
								LegalId = e.TrustProviderId,
								Role = TrustProviderRole
							});
						}

						Template.Parts = [.. Parts];
						Template.PartsMode = ContractParts.ExplicitlyDefined;
					}
					else
					{
						foreach (Part Part in Template.Parts)
						{
							if (Part.Role == BuyerRole)
								Part.LegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
							else if (Part.Role == SellerRole)
								Part.LegalId = this.Owner;
							else if (Part.Role == TrustProviderRole)
								Part.LegalId = e.TrustProviderId;
						}
					}

					if (!string.IsNullOrEmpty(TokenIdParameter))
						Parameters[TokenIdParameter] = this.TokenId ?? string.Empty;

					if (!string.IsNullOrEmpty(CurrencyParameter))
						Parameters[CurrencyParameter] = e.Currency;

					if (!string.IsNullOrEmpty(CommissionParameter))
						Parameters[CommissionParameter] = e.Commission;

					if (!string.IsNullOrEmpty(OwnershipContractParameter))
						Parameters[OwnershipContractParameter] = this.OwnershipContract ?? string.Empty;
				}

				NewContractNavigationArgs NewContractArgs = new(Template, true, Parameters);

				if (!string.IsNullOrEmpty(TrustProviderId))
					NewContractArgs.SuppressProposal(TrustProviderId);

				await ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs, BackMethod.CurrentPage);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to view events related to token.
		/// </summary>
		[RelayCommand]
		private async Task ViewEvents()
		{
			if (this.TokenId is null)
				return;

			try
			{
				TokenEvent[] Events = this.TokenId is null ? [] : await ServiceRef.XmppService.GetNeuroFeatureEvents(this.TokenId);
				TokenEventsNavigationArgs Args = new(this.TokenId!, Events, this.IsMyToken);

				await ServiceRef.NavigationService.GoToAsync(nameof(TokenEventsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand]
		private async Task PresentEmbeddedLayout()
		{
			if (this.TokenId is null)
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
