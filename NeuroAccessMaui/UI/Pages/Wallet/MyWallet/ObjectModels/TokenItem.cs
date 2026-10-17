using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Wallet.Tokens;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Converters;
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
	public partial class TokenItem : ObservableObject, IUniqueItem, IListCardItem
	{
		private readonly Token token;
		private readonly TaskCompletionSource<TokenItem?>? selected;
		private readonly TokenExpiryStatus expiryStatus;
		private readonly int expiresInDays;

		/// <summary>
		/// Encapsulates a <see cref="Token"/> object.
		/// </summary>
		/// <param name="Token">Token</param>
		public TokenItem(Token Token)
			: this(Token, null)
		{
		}

		/// <summary>
		/// Encapsulates a <see cref="Token"/> object.
		/// </summary>
		/// <param name="Token">Token</param>
		/// <param name="Selected">Asynchronous task completion source, waiting for the user to select a token.</param>
		public TokenItem(Token Token, TaskCompletionSource<TokenItem?>? Selected)
		{
			this.token = Token;
			this.selected = Selected;
			this.GlyphWidth = TokenGlyph.ListBoxSize;
			this.GlyphHeight = TokenGlyph.ListBoxSize;
			this.expiryStatus = TokenExpiry.GetStatus(Token.Expires, DateTime.UtcNow, out this.expiresInDays);

			_ = this.LoadGlyphAsync();
		}

		/// <summary>
		/// Token object.
		/// </summary>
		public Token Token => this.token;

		/// <inheritdoc/>
		public string UniqueName => this.token.TokenId;

		/// <summary>
		/// Opens the token when it is tapped. When null, a tap opens token details with this copy of the token, or
		/// completes the selection the item was created for.
		/// </summary>
		public Func<TokenItem, Task>? Open { get; init; }

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
		/// How soon the token expires, as of when the item was created.
		/// </summary>
		public TokenExpiryStatus ExpiryStatus => this.expiryStatus;

		/// <summary>
		/// If the token has expired.
		/// </summary>
		public bool IsExpired => this.expiryStatus == TokenExpiryStatus.Expired;

		/// <summary>
		/// If the token expires today or within the next few days.
		/// </summary>
		public bool IsExpiringSoon => this.expiryStatus == TokenExpiryStatus.Today || this.expiryStatus == TokenExpiryStatus.Soon;

		/// <summary>
		/// Calendar days left until the token expires, or null when it has expired or doesn't expire.
		/// </summary>
		public int? ExpiresInDays => this.expiryStatus switch
		{
			TokenExpiryStatus.Later or TokenExpiryStatus.Soon or TokenExpiryStatus.Today => this.expiresInDays,
			_ => null
		};

		/// <summary>
		/// A short text saying that the token has expired or expires soon, or null otherwise.
		/// </summary>
		public string? ExpiryText => TokenExpiry.GetText(this.expiryStatus, this.expiresInDays);

		/// <summary>
		/// If <see cref="ExpiryText"/> has a text to show.
		/// </summary>
		public bool HasExpiryText => this.IsExpired || this.IsExpiringSoon;

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
		/// If the token has a category.
		/// </summary>
		public bool HasCategory => !string.IsNullOrWhiteSpace(this.token.Category);

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
		/// Where the token's row sits in a list card, so the row draws its part of the card.
		/// </summary>
		[ObservableProperty]
		private ListCardPosition cardPosition;

		/// <summary>
		/// Number of unread notifications about the token.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasUnread))]
		[NotifyPropertyChangedFor(nameof(UnreadText))]
		[NotifyPropertyChangedFor(nameof(AccessibleDescription))]
		private int unreadCount;

		/// <summary>
		/// If there are unread notifications about the token.
		/// </summary>
		public bool HasUnread => this.UnreadCount > 0;

		/// <summary>
		/// The number of unread notifications, as a phrase a screen reader can announce.
		/// </summary>
		public string UnreadText => ServiceRef.Localizer[nameof(AppResources.TokenUnreadCount), this.UnreadCount];

		/// <summary>
		/// What a screen reader announces for the token's row: its name, category, value, expiry and unread notifications.
		/// </summary>
		public string AccessibleDescription
		{
			get
			{
				List<string> Parts = [this.token.FriendlyName];

				if (this.HasCategory)
					Parts.Add(this.token.Category);

				Parts.Add(MoneyToString.ToString(this.token.Value) + " " + this.token.Currency);

				if (this.ExpiryText is string Expiry)
					Parts.Add(Expiry);

				if (this.HasUnread)
					Parts.Add(this.UnreadText);

				return string.Join(", ", Parts);
			}
		}

		/// <summary>
		/// Loads the glyph shown in token lists.
		/// </summary>
		private async Task LoadGlyphAsync()
		{
			try
			{
				TokenGlyph Glyph = await TokenGlyph.CreateAsync(this.token, TokenGlyph.ListBoxSize);

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
		/// Renders the token's embedded layout to PNG bytes. Rendering runs off the UI thread.
		/// </summary>
		/// <returns>PNG bytes, or null if the token has no embedded layout.</returns>
		public Task<byte[]?> RenderEmbeddedLayoutPngAsync()
		{
			if (!this.token.HasEmbeddedLayout)
				return Task.FromResult<byte[]?>(null);

			return Task.Run(async () =>
			{
				using SKImage Img = await this.token.RenderEmbeddedLayout();
				using SKData Data = Img.Encode(SKEncodedImageFormat.Png, 100);

				return (byte[]?)Data.ToArray();
			});
		}

		/// <summary>
		/// Command executed when the token has been clicked or tapped.
		/// </summary>
		[RelayCommand]
		private async Task Clicked()
		{
			if (this.Open is not null)
			{
				await this.Open(this);
				return;
			}

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
