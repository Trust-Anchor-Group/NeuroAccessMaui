using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Navigation;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport;
using NeuroAccessMaui.UI.Pages.Wallet.MachineReport.Reports;
using NeuroAccessMaui.UI.Pages.Wallet.MachineVariables;
using NeuroAccessMaui.UI.Pages.Wallet.TokenEvents;
using NeuroFeatures;
using NeuroFeatures.Events;
using NeuroFeatures.Tags;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Web;
using System.Windows.Input;
using System.Xml;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Persistence;
using Waher.Security;

namespace NeuroAccessMaui.UI.Pages.Wallet.TokenDetails
{
	/// <summary>
	/// The view model to bind to for when displaying the contents of a token.
	/// </summary>
	public partial class TokenDetailsViewModel : QrXmppViewModel
	{
		private readonly TokenDetailsPage page;

		/// <summary>
		/// Creates an instance of the <see cref="TokenDetailsViewModel"/> class.
		/// </summary>
		/// <param name="Page">Page hosting details.</param>
		public TokenDetailsViewModel(TokenDetailsPage Page)
			: base()
		{
			this.page = Page;

			this.Certifiers = [];
			this.Valuators = [];
			this.Assessors = [];
			this.Witnesses = [];
			this.Tags = [];

			this.CopyToClipboardCommand = new Command(async P => await this.CopyToClipboard(P));
			this.ViewIdCommand = new Command(async P => await this.ViewId(P));
			this.ViewContractCommand = new Command(async P => await this.ViewContract(P));
			this.OpenChatCommand = new Command(async P => await this.OpenChat(P));
			this.OpenLinkCommand = new Command(async P => await this.OpenLink(P));
			this.ShowM2mInfoCommand = new Command(async _ => await this.ShowM2mInfo());
			this.SendToContactCommand = new Command(async _ => await this.SendToContact());
			this.ShareCommand = new Command(async _ => await this.Share());
			this.OfferToSellCommand = new Command(async _ => await this.OfferToSell());
			this.PublishMarketplaceCommand = new Command(async _ => await this.PublishMarketplace());
			this.OfferToBuyCommand = new Command(async _ => await this.OfferToBuy());
			this.ViewEventsCommand = new Command(async _ => await this.ViewEvents());
			this.PresentReportCommand = new Command(async _ => await this.PresentReport());
			this.HistoryReportCommand = new Command(async _ => await this.HistoryReport());
			this.VariablesReportCommand = new Command(async _ => await this.VariablesReport());
			this.StatesReportCommand = new Command(async _ => await this.StatesReport());
			this.ProfilingReportCommand = new Command(async _ => await this.ProfilingReport());
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.NavigationService.TryGetArgs(out TokenDetailsNavigationArgs? args))
			{
				this.Created = args.Token?.Created;
				this.Updated = args.Token?.Updated;
				this.Expires = args.Token?.Expires;
				this.ArchiveRequired = args.Token?.ArchiveRequired;
				this.ArchiveOptional = args.Token?.ArchiveOptional;
				this.SignatureTimestamp = args.Token?.SignatureTimestamp;
				this.Signature = args.Token?.Signature;
				this.DefinitionSchemaDigest = args.Token?.DefinitionSchemaDigest;
				this.DefinitionSchemaHashFunction = args.Token?.DefinitionSchemaHashFunction;
				this.CreatorCanDestroy = args.Token?.CreatorCanDestroy ?? false;
				this.OwnerCanDestroyBatch = args.Token?.OwnerCanDestroyBatch ?? false;
				this.OwnerCanDestroyIndividual = args.Token?.OwnerCanDestroyIndividual ?? false;
				this.CertifierCanDestroy = args.Token?.CertifierCanDestroy ?? false;
				this.FriendlyName = args.Token?.FriendlyName;
				this.Category = args.Token?.Category;
				this.Description = args.Token?.Description is null ? null : await args.Token.Description.MarkdownToXaml();
				this.GlyphContentType = args.Token?.GlyphContentType;
				this.Ordinal = args.Token?.Ordinal;
				this.Value = args.Token?.Value;
				this.TokenIdMethod = args.Token?.TokenIdMethod;
				this.TokenId = args.Token?.TokenId;
				this.ShortTokenId = args.Token?.ShortTokenId;
				this.Visibility = args.Token?.Visibility;
				this.Creator = args.Token?.Creator;
				this.CreatorFriendlyName = await ContactInfo.GetFriendlyName(args.Token?.Creator);
				this.CreatorJid = args.Token?.CreatorJid;
				this.Owner = args.Token?.Owner;
				this.OwnerFriendlyName = await ContactInfo.GetFriendlyName(args.Token?.Owner);
				this.OwnerJid = args.Token?.OwnerJid;
				this.BatchSize = args.Token?.BatchSize;
				this.TrustProvider = args.Token?.TrustProvider;
				this.TrustProviderFriendlyName = await ContactInfo.GetFriendlyName(args.Token?.TrustProvider);
				this.TrustProviderJid = args.Token?.TrustProviderJid;
				this.Currency = args.Token?.Currency;
				this.Reference = args.Token?.Reference;
				this.Definition = args.Token?.Definition;
				this.DefinitionNamespace = args.Token?.DefinitionNamespace;
				this.CreationContract = args.Token?.CreationContract;
				this.OwnershipContract = args.Token?.OwnershipContract;
				this.GlyphImage = args.Token?.GlyphImage;
				this.HasGlyphImage = args.Token?.HasGlyphImage ?? false;
				this.GlyphWidth = args.Token?.GlyphWidth;
				this.GlyphHeight = args.Token?.GlyphHeight;
				this.TokenXml = args.Token?.Token.ToXml();
				this.IsMyToken = string.Equals(this.OwnerJid, ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase);
				this.HasStateMachine = args.Token?.HasStateMachine ?? false;

				if (!string.IsNullOrEmpty(args.Token?.Reference))
				{
					if (Uri.TryCreate(args.Token?.Reference, UriKind.Absolute, out Uri? RefUri) &&
						RefUri.Scheme.ToLower(CultureInfo.InvariantCulture) is string s &&
						(s == "http" || s == "https"))
					{
						this.page.AddLink(this, ServiceRef.Localizer[nameof(AppResources.Reference)], s);   // TODO: Replace with grouped collection, when this works in Xamarin.
					}
				}

				if (args.Token?.Tags is not null)
				{
					foreach (TokenTag Tag in args.Token.Tags)
						this.page.AddLink(this, Tag.Name, Tag.Value?.ToString() ?? string.Empty);   // TODO: Replace with grouped collection, when this works in Xamarin.
				}

				if (this.TokenId is not null)
					this.GenerateQrCode(Constants.UriSchemes.CreateTokenUri(this.TokenId));

				await this.Populate(ServiceRef.Localizer[nameof(AppResources.Witness)], string.Empty, args.Token.Witness, null, this.Witnesses);
				await this.Populate(ServiceRef.Localizer[nameof(AppResources.Certifier)], ServiceRef.Localizer[nameof(AppResources.CertifierJid)], args.Token.Certifier, args.Token.CertifierJids, this.Certifiers);
				await this.Populate(ServiceRef.Localizer[nameof(AppResources.Valuator)], string.Empty, args.Token.Valuator, null, this.Valuators);
				await this.Populate(ServiceRef.Localizer[nameof(AppResources.Assessor)], string.Empty, args.Token.Assessor, null, this.Assessors);

				this.Tags.Clear();

				if (args.Token.Tags is not null)
				{
					foreach (TokenTag Tag in args.Token.Tags)
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

		private async Task Populate(string LegalIdLabel, string JidLabel, string[] LegalIds, string[] Jids, ObservableCollection<PartItem> Parts)
		{
			int i, c = LegalIds.Length;
			int d = Jids?.Length ?? 0;
			string FriendlyName;
			string Jid;

			Parts.Clear();

			for (i = 0; i < c; i++)
			{
				FriendlyName = await ContactInfo.GetFriendlyName(LegalIds[i]);
				Jid = i < d ? (Jids![i] ?? string.Empty) : string.Empty;

				Parts.Add(new PartItem(LegalIds[i], Jid, FriendlyName));

				this.page.AddLegalId(this, LegalIdLabel, FriendlyName, LegalIds[i]);    // TODO: Replace with grouped collection, when this works in Xamarin.

				if (!string.IsNullOrEmpty(Jid))
					this.page.AddJid(this, JidLabel, Jid, LegalIds[i], FriendlyName);   // TODO: Replace with grouped collection, when this works in Xamarin.
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
		public ObservableCollection<PartItem> Certifiers { get; }

		/// <summary>
		/// Valuators
		/// </summary>
		public ObservableCollection<PartItem> Valuators { get; }

		/// <summary>
		/// Assessors
		/// </summary>
		public ObservableCollection<PartItem> Assessors { get; }

		/// <summary>
		/// Witnesses
		/// </summary>
		public ObservableCollection<PartItem> Witnesses { get; }

		/// <summary>
		/// Witnesses
		/// </summary>
		public ObservableCollection<TokenTag> Tags { get; }

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
		/// Gets or sets the value representing the width of the glyph.
		/// </summary>
		[ObservableProperty]
		private int? glyphWidth;

		/// <summary>
		/// Gets or sets the value representing the height of the glyph.
		/// </summary>
		[ObservableProperty]
		private int? glyphHeight;

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

		#endregion

		#region Commands

		/// <summary>
		/// Command to copy a value to the clipboard.
		/// </summary>
		public ICommand CopyToClipboardCommand { get; }

		/// <summary>
		/// Command to view a Legal ID.
		/// </summary>
		public ICommand ViewIdCommand { get; }

		/// <summary>
		/// Command to view a smart contract.
		/// </summary>
		public ICommand ViewContractCommand { get; }

		/// <summary>
		/// Command to open a chat page.
		/// </summary>
		public ICommand OpenChatCommand { get; }

		/// <summary>
		/// Command to open a link.
		/// </summary>
		public ICommand OpenLinkCommand { get; }

		/// <summary>
		/// Command to show machine-readable details of token.
		/// </summary>
		public ICommand ShowM2mInfoCommand { get; }

		/// <summary>
		/// Command to send token to contact
		/// </summary>
		public ICommand SendToContactCommand { get; }

		/// <summary>
		/// Command to share token with other applications
		/// </summary>
		public ICommand ShareCommand { get; }

		/// <summary>
		/// Command to publish the token on the marketplace, for sale.
		/// </summary>
		public ICommand PublishMarketplaceCommand { get; }

		/// <summary>
		/// Command to offer the token for sale.
		/// </summary>
		public ICommand OfferToSellCommand { get; }

		/// <summary>
		/// Command to offer to buy the token.
		/// </summary>
		public ICommand OfferToBuyCommand { get; }

		/// <summary>
		/// Command to view events related to token.
		/// </summary>
		public ICommand ViewEventsCommand { get; }

		/// <summary>
		/// Command to display the present state report of a state-machine.
		/// </summary>
		public ICommand PresentReportCommand { get; }

		/// <summary>
		/// Command to display the historic states report of a state-machine.
		/// </summary>
		public ICommand HistoryReportCommand { get; }

		/// <summary>
		/// Command to display the current states of variables of a state-machine.
		/// </summary>
		public ICommand VariablesReportCommand { get; }

		/// <summary>
		/// Command to display the state diagram of a state-machine.
		/// </summary>
		public ICommand StatesReportCommand { get; }

		/// <summary>
		/// Command to display a profiling report of a state-machine.
		/// </summary>
		public ICommand ProfilingReportCommand { get; }

		private async Task CopyToClipboard(object Parameter)
		{
			try
			{
				string s = Parameter?.ToString() ?? string.Empty;
				int i = s.IndexOf('@');

				if (i > 0 && Guid.TryParse(s[..i], out _))
				{
					await Clipboard.SetTextAsync(Constants.UriSchemes.NeuroFeature + ":" + s);
					await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.IdCopiedSuccessfully)]);
				}
				else
				{
					await Clipboard.SetTextAsync(s);
					await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private async Task ViewId(object Parameter)
		{
			try
			{
				await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(Parameter.ToString(),
					ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)]);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private async Task ViewContract(object Parameter)
		{
			try
			{
				await ServiceRef.ContractOrchestratorService.OpenContract(Parameter.ToString(),
					ServiceRef.Localizer[nameof(AppResources.PurposeReviewToken)], null);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private async Task OpenChat(object Parameter)
		{
			string? s = Parameter?.ToString();
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
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private async Task OpenLink(object Parameter)
		{
			if (Parameter is not null)
				await App.OpenUrlAsync(Parameter.ToString()!);
		}

		private async Task ShowM2mInfo()
		{
			if (this.Definition is null)
				return;

			try
			{
				byte[] Bin = Encoding.UTF8.GetBytes(this.Definition);
				HttpFileUploadEventArgs e = await ServiceRef.XmppService.RequestUploadSlotAsync(this.TokenId + ".xml", "text/xml; charset=utf-8", Bin.Length);

				if (e.Ok)
				{
					await e.PUT(Bin, "text/xml", (int)Constants.Timeouts.UploadFile.TotalMilliseconds);
					await App.OpenUrlAsync(e.GetUrl);
				}
				else
					await ServiceRef.UiSerializer.DisplayException(e.StanzaError ?? new Exception(e.ErrorText));
			}
			catch (Exception ex)
			{
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private async Task SendToContact()
		{
			TaskCompletionSource<ContactInfoModel?> Selected = new();
			ContactListNavigationArgs ContactListArgs = new(ServiceRef.Localizer[nameof(AppResources.SendInformationTo)], Selected)
			{
				CanScanQrCode = true,
			};

			await ServiceRef.NavigationService.GoToAsync(nameof(MyContactsPage), ContactListArgs, BackMethod.Pop);

			ContactInfoModel? Contact = await Selected.Task;
			if (Contact is null)
				return;

			StringBuilder Markdown = new();

			Markdown.Append("```");
			Markdown.AppendLine(Constants.UriSchemes.NeuroFeature);
			Markdown.AppendLine(this.TokenXml);
			Markdown.AppendLine("```");

			await ChatViewModel.ExecuteSendMessage(string.Empty, Markdown.ToString(), Contact.BareJid);

			if (Contact.Contact is not null)
			{
				await Task.Delay(100);  // Otherwise, page doesn't show properly. (Underlying timing issue. TODO: Find better solution.)

				ChatNavigationArgs ChatArgs = new(Contact.Contact);

				await ServiceRef.NavigationService.GoToAsync(nameof(ChatPage), ChatArgs, BackMethod.Inherited, Contact.BareJid);
			}
		}

		private async Task Share()
		{
			try
			{
				if (this.QrCodeBin is null)
					return;

				string FileName = "Token.QR." + InternetContent.GetFileExtension(this.QrCodeContentType);

				ServiceRef.PlatformSpecific.ShareImage(this.QrCodeBin, this.FriendlyName ?? string.Empty,
					ServiceRef.Localizer[nameof(AppResources.Share)], FileName);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

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

				Template.Parts = new Part[]
				{
					new Part()
					{
						Role = "Seller",
						LegalId = ServiceRef.TagProfile.LegalIdentity?.Id
					},
					new Part()
					{
						Role = "Auctioneer",
						LegalId = e.TrustProviderId
					}
				};

				NewContractArgs.SuppressProposal(e.TrustProviderId);

				await ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), NewContractArgs, BackMethod.CurrentPage);
			}
			catch (Exception ex)
			{
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private async Task OfferToSell()
		{
			try
			{
				Dictionary<CaseInsensitiveString, object> Parameters = new();
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
						List<Part> Parts = new();

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

						Template.Parts = Parts.ToArray();
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
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private async Task OfferToBuy()
		{
			try
			{
				Dictionary<CaseInsensitiveString, object> Parameters = new();
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
						List<Part> Parts = new();

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

						Template.Parts = Parts.ToArray();
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
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		private async Task ViewEvents()
		{
			try
			{
				TokenEvent[] Events = this.TokenId is null ? [] : await ServiceRef.XmppService.GetNeuroFeatureEvents(this.TokenId);
				TokenEventsNavigationArgs Args = new(this.TokenId, Events);

				await ServiceRef.NavigationService.GoToAsync(nameof(TokenEventsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task PresentReport()
		{
			await this.ShowReport(new TokenPresentReport(this.XmppService, this.TokenId));
		}

		private async Task HistoryReport()
		{
			await this.ShowReport(new TokenHistoryReport(this.XmppService, this.TokenId));
		}

		private async Task StatesReport()
		{
			await this.ShowReport(new TokenStateDiagramReport(this.XmppService, this.TokenId));
		}

		private async Task ProfilingReport()
		{
			await this.ShowReport(new TokenProfilingReport(this.XmppService, this.TokenId));
		}

		private async Task VariablesReport()
		{
			try
			{
				CurrentStateEventArgs e = await this.XmppService.GetNeuroFeatureCurrentState(this.TokenId);
				if (e.Ok)
				{
					MachineVariablesNavigationArgs Args = new(e.Running, e.Ended, e.CurrentState, e.Variables);

					await ServiceRef.NavigationService.GoToAsync(nameof(MachineVariablesPage), Args, BackMethod.Pop);
				}
				else
					await this.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], e.ErrorText);
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
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
				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
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
