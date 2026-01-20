using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Wallet.TokenDetails;
using NeuroFeatures;
using NeuroFeatures.Tags;
using SkiaSharp;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Security;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyWallet.ObjectModels
{
	/// <summary>
	/// Encapsulates a <see cref="Token"/> object.
	/// </summary>
	public partial class TokenItem : ObservableObject, IUniqueItem
	{
		private readonly Token token;
		private readonly TaskCompletionSource<TokenItem?>? selected;
		private bool? @new;
		private int nrEvents;
		private NotificationEvent[] notificationEvents;

		/// <summary>
		/// Encapsulates a <see cref="Token"/> object.
		/// </summary>
		/// <param name="Token">Token</param>
		/// <param name="NotificationEvents">Notification events.</param>
		public TokenItem(Token Token, NotificationEvent[] NotificationEvents)
			: this(Token, null, NotificationEvents)
		{
		}

		/// <summary>
		/// Encapsulates a <see cref="Token"/> object.
		/// </summary>
		/// <param name="Token">Token</param>
		/// <param name="Selected">Asynchronous task completion source, waiting for the user to select a token.</param>
		/// <param name="NotificationEvents">Notification events.</param>
		public TokenItem(Token Token, TaskCompletionSource<TokenItem?>? Selected, NotificationEvent[] NotificationEvents)
		{
			this.token = Token;
			this.selected = Selected;
			this.notificationEvents = NotificationEvents;

			if (this.Glyph is not null && this.GlyphContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			{
				this.GlyphImage = ImageSource.FromStream(() => new MemoryStream(this.Glyph));
				this.HasGlyphImage = true;

				double s = 32.0 / this.token.GlyphWidth;
				double s2 = 32.0 / this.token.GlyphHeight;

				if (s2 < s)
					s = s2;
				else if (s > 1)
					s = 1;

				this.GlyphWidth = (int)(this.token.GlyphWidth * s + 0.5);
				this.GlyphHeight = (int)(this.token.GlyphHeight * s + 0.5);
			}
			else
			{
				this.GlyphImage = null;
				this.HasGlyphImage = false;
				this.GlyphWidth = 0;
				this.GlyphHeight = 0;
			}
		}

		/// <summary>
		/// Token object.
		/// </summary>
		public Token Token => this.token;

		/// <inheritdoc/>
		public string UniqueName => this.token.TokenId;

		/// <summary>
		/// When token was created.
		/// </summary>
		public DateTime Created => this.token.Created;

		/// <summary>
		/// When token was last updated.
		/// </summary>
		public DateTime Updated => this.token.Updated;

		/// <summary>
		/// When token expires.
		/// </summary>
		public DateTime Expires => this.token.Expires;

		/// <summary>
		/// Required archiving time after token expires.
		/// </summary>
		public Duration? ArchiveRequired => this.token.ArchiveRequired;

		/// <summary>
		/// Optional archiving time after required archiving time.
		/// </summary>
		public Duration? ArchiveOptional => this.token.ArchiveOptional;

		/// <summary>
		/// Signature timestamp
		/// </summary>
		public DateTime SignatureTimestamp => this.token.SignatureTimestamp;

		/// <summary>
		/// Token signature
		/// </summary>
		public byte[] Signature => this.token.Signature;

		/// <summary>
		/// Digest of schema used to validate token definition XML.
		/// </summary>
		public byte[] DefinitionSchemaDigest => this.token.DefinitionSchemaDigest;

		/// <summary>
		/// Hash function used to compute <see cref="DefinitionSchemaDigest"/>.
		/// </summary>
		public HashFunction DefinitionSchemaHashFunction => this.token.DefinitionSchemaHashFunction;

		/// <summary>
		/// If the creator can destroy the token.
		/// </summary>
		public bool CreatorCanDestroy => this.token.CreatorCanDestroy;

		/// <summary>
		/// If the owner can destroy the entire batch of tokens, if owner of every token in the batch.
		/// </summary>
		public bool OwnerCanDestroyBatch => this.token.OwnerCanDestroyBatch;

		/// <summary>
		/// If the owner can destroy an individual token.
		/// </summary>
		public bool OwnerCanDestroyIndividual => this.token.OwnerCanDestroyIndividual;

		/// <summary>
		/// If a certifier can destroy the token.
		/// </summary>
		public bool CertifierCanDestroy => this.token.CertifierCanDestroy;

		/// <summary>
		/// Friendly name of token.
		/// </summary>
		public string FriendlyName => this.token.FriendlyName;

		/// <summary>
		/// Category of token.
		/// </summary>
		public string Category => this.token.Category;

		/// <summary>
		/// Description engraved into the token.
		/// </summary>
		public string Description => this.token.Description;

		/// <summary>
		/// Glyph of token.
		/// </summary>
		public byte[] Glyph => this.token.Glyph;

		/// <summary>
		/// Content-Type of glyph
		/// </summary>
		public string GlyphContentType => this.token.GlyphContentType;

		/// <summary>
		/// Ordinal of token, within batch.
		/// </summary>
		public int Ordinal => this.token.Ordinal;

		/// <summary>
		/// (Last) Value of token
		/// </summary>
		public decimal Value => this.token.Value;

		/// <summary>
		/// Witnesses
		/// </summary>
		public string[] Witness => this.token.Witness;

		/// <summary>
		/// JIDs of certifiers
		/// </summary>
		public string[] CertifierJids => this.token.CertifierJids;

		/// <summary>
		/// Certifiers
		/// </summary>
		public string[] Certifier => this.token.Certifier;

		/// <summary>
		/// Method of assigning the Token ID.
		/// </summary>
		public TokenIdMethod TokenIdMethod => this.token.TokenIdMethod;

		/// <summary>
		/// Token ID
		/// </summary>
		public string TokenId => this.token.TokenId;

		/// <summary>
		/// ShortToken ID
		/// </summary>
		public string ShortTokenId => this.token.ShortId;

		/// <summary>
		/// Visibility of token
		/// </summary>
		public ContractVisibility Visibility => this.token.Visibility;

		/// <summary>
		/// Creator of token
		/// </summary>
		public string Creator => this.token.Creator;

		/// <summary>
		/// JID of <see cref="Creator"/>.
		/// </summary>
		public string CreatorJid => this.token.CreatorJid;

		/// <summary>
		/// Current owner
		/// </summary>
		public string Owner => this.token.Owner;

		/// <summary>
		/// Number of tokens in batch being created.
		/// </summary>
		public int BatchSize => this.token.BatchSize;

		/// <summary>
		/// Trust Provider asserting the validity of the token
		/// </summary>
		public string TrustProvider => this.token.TrustProvider;

		/// <summary>
		/// JID of owner
		/// </summary>
		public string OwnerJid => this.token.OwnerJid;

		/// <summary>
		/// Currency of <see cref="Value"/>.
		/// </summary>
		public string Currency => this.token.Currency;

		/// <summary>
		/// Any reference provided by the token creator.
		/// </summary>
		public string Reference => this.token.Reference;

		/// <summary>
		/// XML Definition of token.
		/// </summary>
		public string Definition => this.token.Definition;

		/// <summary>
		/// XML Namespace used in the <see cref="Definition"/>
		/// </summary>
		public string DefinitionNamespace => this.token.DefinitionNamespace;

		/// <summary>
		/// Contract used to create the contract.
		/// </summary>
		public string CreationContract => this.token.CreationContract;

		/// <summary>
		/// Contract used to define the current ownership
		/// </summary>
		public string OwnershipContract => this.token.OwnershipContract;

		/// <summary>
		/// Valuators
		/// </summary>
		public string[] Valuator => this.token.Valuator;

		/// <summary>
		/// Assessors
		/// </summary>
		public string[] Assessor => this.token.Assessor;

		/// <summary>
		/// JID of <see cref="TrustProvider"/>
		/// </summary>
		public string TrustProviderJid => this.token.TrustProviderJid;

		/// <summary>
		/// Any custom Token Tags provided during creation of the token.
		/// </summary>
		public TokenTag[] Tags => this.token.Tags;

		/// <summary>
		/// If the token is associated with a state-machine.
		/// </summary>
		public bool HasStateMachine => this.token.HasStateMachine;

		/// <summary>
		/// If the token have an embedded layout.
		/// </summary>
		public bool HasEmbeddedLayout => this.token.HasEmbeddedLayout;

		/// <summary>
		/// The embedded layout of the token.
		/// </summary>
		public XmlElement EmbeddedLayoutDefinition => this.token.EmbeddedLayoutDefinition;

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
		private int glyphWidth;

		/// <summary>
		/// Gets or sets the value representing the height of the glyph.
		/// </summary>
		[ObservableProperty]
		private int glyphHeight;

		/// <summary>
		/// Associated notification events
		/// </summary>
		public NotificationEvent[] NotificationEvents => this.notificationEvents;

		/// <summary>
		/// Number of notification events recorded for the item.
		/// </summary>
		public int NrEvents
		{
			get
			{
				this.CheckEvents();

				return this.nrEvents;
			}
		}

		/// <summary>
		/// If the event item is new or not.
		/// </summary>
		public bool New
		{
			get
			{
				this.CheckEvents();

				return this.@new ?? false;
			}
		}

		private void CheckEvents()
		{
			if (!this.@new.HasValue)
			{
				this.nrEvents = this.notificationEvents.Length;
				this.@new = this.nrEvents > 0;

				if (this.@new.Value)
				{
					NotificationEvent[] ToDelete = this.notificationEvents;

					this.notificationEvents = [];

					Task.Run(() => ServiceRef.NotificationService.DeleteEvents(ToDelete));
				}
			}
		}

		public async Task<ImageSource?> RenderEmbeddedLayout()
		{
			if (!this.Token.HasEmbeddedLayout)
				return null;

			SkiaSharp.SKImage Img = await this.Token?.RenderEmbeddedLayout();


			return ImageSource.FromStream(() =>
			{
				SKData Data = Img.Encode(SKEncodedImageFormat.Png, 100);
				return Data.AsStream();
			});
		}

		/// <summary>
		/// Command executed when the token has been clicked or tapped.
		/// </summary>
		[RelayCommand]
		private async Task Clicked()
		{
			if (this.selected is null)
			{
				TokenDetailsNavigationArgs Args = new(this);

				await ServiceRef.NavigationService.GoToAsync(nameof(TokenDetailsPage), Args, BackMethod.Pop);
			}
			else
			{
				this.selected.TrySetResult(this);
				await ServiceRef.UiService.GoBackAsync();
			}
		}
	}
}
