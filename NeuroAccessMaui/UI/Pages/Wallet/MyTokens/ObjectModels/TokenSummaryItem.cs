using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.UI.Controls;
using NeuroFeatures;
using SkiaSharp;

namespace NeuroAccessMaui.UI.Pages.Wallet.MyTokens.ObjectModels
{
	/// <summary>
	/// Provides immutable, page-ready summary data for a token without persisting a second token record.
	/// </summary>
	public sealed class TokenSummaryItem
	{
		private static readonly TimeSpan expiryAttentionWindow = TimeSpan.FromDays(30);

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenSummaryItem"/> class.
		/// </summary>
		/// <param name="Token">Authoritative token data represented by the summary.</param>
		/// <param name="NotificationEvents">Unread notification events associated with the token.</param>
		/// <param name="CurrentOwnerJid">Bare JID of the current app user, when available.</param>
		/// <param name="ActivateAsync">Action invoked when the summary is selected.</param>
		/// <param name="UtcNow">Optional clock value used to derive lifecycle status.</param>
		public TokenSummaryItem(
			Token Token,
			NotificationEvent[]? NotificationEvents,
			string? CurrentOwnerJid,
			Func<TokenSummaryItem, Task>? ActivateAsync,
			DateTime? UtcNow = null)
		{
			ArgumentNullException.ThrowIfNull(Token);

			this.Token = Token;
			this.TokenId = Normalize(Token.TokenId);
			this.ShortTokenId = BuildShortTokenId(Token.ShortId, this.TokenId);
			this.Category = Normalize(Token.Category);
			this.Description = Normalize(Token.Description);
			this.DisplayName = FirstNonEmpty(
				Normalize(Token.FriendlyName),
				this.Category,
				this.ShortTokenId,
				ServiceRef.Localizer[nameof(AppResources.Unknown)]);

			this.UnreadCount = NotificationEvents?.Length ?? 0;
			this.IsOwner = !string.IsNullOrWhiteSpace(CurrentOwnerJid) &&
				string.Equals(Token.OwnerJid, CurrentOwnerJid, StringComparison.OrdinalIgnoreCase);
			this.OwnershipText = BuildOwnershipText(Token, this.IsOwner);

			DateTime Now = UtcNow ?? DateTime.UtcNow;
			this.HasExpiry = HasMeaningfulDate(Token.Expires);
			this.IsExpired = this.HasExpiry && ToUtc(Token.Expires) <= Now;
			bool ExpiresSoon = this.HasExpiry &&
				!this.IsExpired &&
				ToUtc(Token.Expires) <= Now.Add(expiryAttentionWindow);

			bool HasValidIdentity = !string.IsNullOrWhiteSpace(this.TokenId);
			this.NeedsAttention = !HasValidIdentity || this.UnreadCount > 0 || this.IsExpired || ExpiresSoon;

			if (!HasValidIdentity)
			{
				this.StatusText = ServiceRef.Localizer[nameof(AppResources.Unavailable)];
				this.StatusTone = StatusPillTone.Neutral;
			}
			else if (this.IsExpired)
			{
				this.StatusText = ServiceRef.Localizer[nameof(AppResources.Expired)];
				this.StatusTone = StatusPillTone.Warning;
			}
			else if (this.NeedsAttention)
			{
				this.StatusText = ServiceRef.Localizer[nameof(AppResources.NeedsAttention)];
				this.StatusTone = StatusPillTone.Warning;
			}
			else
			{
				this.StatusText = ServiceRef.Localizer[nameof(AppResources.Active)];
				this.StatusTone = StatusPillTone.Success;
			}

			this.HasValue = !string.IsNullOrWhiteSpace(Token.Currency) || Token.Value != 0;
			this.ValueText = this.HasValue
				? FormatValue(Token.Value, Token.Currency)
				: string.Empty;
			this.ExpiryText = this.HasExpiry
				? ServiceRef.Localizer[nameof(AppResources.ExpiresFormat), Token.Expires]
				: string.Empty;
			this.UpdatedText = HasMeaningfulDate(Token.Updated)
				? ServiceRef.Localizer[
					nameof(AppResources.LastUpdatedFormat),
					Token.Updated.ToString("g", CultureInfo.CurrentCulture)]
				: string.Empty;

			this.GlyphImage = CreateGlyphImage(Token);
			this.HasGlyphImage = this.GlyphImage is not null;
			this.FallbackGlyphText = BuildFallbackGlyphText(this.DisplayName);

			if (ActivateAsync is null)
			{
				this.ActivateCommand = new AsyncRelayCommand(
					() => Task.CompletedTask,
					() => false);
			}
			else
			{
				this.ActivateCommand = new AsyncRelayCommand(
					() => ActivateAsync(this),
					() => HasValidIdentity);
			}
		}

		/// <summary>
		/// Gets the authoritative token represented by this item.
		/// </summary>
		public Token Token { get; }

		/// <summary>
		/// Gets the durable token identifier.
		/// </summary>
		public string TokenId { get; }

		/// <summary>
		/// Gets the shortened token identifier used by default in the interface.
		/// </summary>
		public string ShortTokenId { get; }

		/// <summary>
		/// Gets the best available friendly display name.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Gets the normalized token category.
		/// </summary>
		public string Category { get; }

		/// <summary>
		/// Gets the normalized token description.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets the localized lifecycle or attention status.
		/// </summary>
		public string StatusText { get; }

		/// <summary>
		/// Gets the theme-driven semantic status tone.
		/// </summary>
		public StatusPillTone StatusTone { get; }

		/// <summary>
		/// Gets the localized ownership relationship.
		/// </summary>
		public string OwnershipText { get; }

		/// <summary>
		/// Gets the localized token value and currency.
		/// </summary>
		public string ValueText { get; }

		/// <summary>
		/// Gets the localized expiry context.
		/// </summary>
		public string ExpiryText { get; }

		/// <summary>
		/// Gets the localized last-updated context.
		/// </summary>
		public string UpdatedText { get; }

		/// <summary>
		/// Gets the optional token glyph.
		/// </summary>
		public ImageSource? GlyphImage { get; }

		/// <summary>
		/// Gets a value indicating whether a token glyph is available.
		/// </summary>
		public bool HasGlyphImage { get; }

		/// <summary>
		/// Gets the fallback character shown when the token has no usable glyph.
		/// </summary>
		public string FallbackGlyphText { get; }

		/// <summary>
		/// Gets a value indicating whether the token has a normalized category.
		/// </summary>
		public bool HasCategory => !string.IsNullOrWhiteSpace(this.Category);

		/// <summary>
		/// Gets a value indicating whether the token has a normalized description.
		/// </summary>
		public bool HasDescription => !string.IsNullOrWhiteSpace(this.Description);

		/// <summary>
		/// Gets a value indicating whether an ownership relationship can be shown.
		/// </summary>
		public bool HasOwnership => !string.IsNullOrWhiteSpace(this.OwnershipText);

		/// <summary>
		/// Gets a value indicating whether value information is meaningful for the token.
		/// </summary>
		public bool HasValue { get; }

		/// <summary>
		/// Gets a value indicating whether the token has a meaningful expiry.
		/// </summary>
		public bool HasExpiry { get; }

		/// <summary>
		/// Gets a value indicating whether the token has expired.
		/// </summary>
		public bool IsExpired { get; }

		/// <summary>
		/// Gets a value indicating whether the current app user owns the token.
		/// </summary>
		public bool IsOwner { get; }

		/// <summary>
		/// Gets a value indicating whether the summary should call attention to the token.
		/// </summary>
		public bool NeedsAttention { get; }

		/// <summary>
		/// Gets the number of unread notification events associated with the token.
		/// </summary>
		public int UnreadCount { get; }

		/// <summary>
		/// Gets a value indicating whether unread token events are available.
		/// </summary>
		public bool HasUnreadEvents => this.UnreadCount > 0;

		/// <summary>
		/// Gets the command that opens or selects the token according to collection context.
		/// </summary>
		public IAsyncRelayCommand ActivateCommand { get; }

		private static string Normalize(string? Value)
		{
			return Value?.Trim() ?? string.Empty;
		}

		private static string FirstNonEmpty(params string[] Values)
		{
			foreach (string Value in Values)
			{
				if (!string.IsNullOrWhiteSpace(Value))
					return Value;
			}

			return string.Empty;
		}

		private static string BuildShortTokenId(string? ShortTokenId, string TokenId)
		{
			string NormalizedShortId = Normalize(ShortTokenId);
			if (!string.IsNullOrEmpty(NormalizedShortId))
				return NormalizedShortId;

			if (TokenId.Length <= 18)
				return TokenId;

			return TokenId[..8] + "…" + TokenId[^6..];
		}

		private static string BuildFallbackGlyphText(string DisplayName)
		{
			string TrimmedName = DisplayName.Trim();
			if (string.IsNullOrEmpty(TrimmedName))
				return "•";

			return TrimmedName[..1].ToUpper(CultureInfo.CurrentCulture);
		}

		private static string BuildOwnershipText(Token Token, bool IsOwner)
		{
			if (IsOwner)
				return ServiceRef.Localizer[nameof(AppResources.OwnedByYou)];

			string OwnerName = Normalize(Token.Owner);
			if (string.IsNullOrEmpty(OwnerName))
				OwnerName = BuildShortTokenId(null, Normalize(Token.OwnerJid));

			return string.IsNullOrEmpty(OwnerName)
				? string.Empty
				: string.Concat(
					ServiceRef.Localizer[nameof(AppResources.Owner)],
					": ",
					OwnerName);
		}

		private static bool HasMeaningfulDate(DateTime Value)
		{
			return Value > DateTime.MinValue && Value < DateTime.MaxValue;
		}

		private static DateTime ToUtc(DateTime Value)
		{
			return Value.Kind == DateTimeKind.Utc ? Value : Value.ToUniversalTime();
		}

		private static string FormatValue(decimal Value, string? Currency)
		{
			string Amount = Value.ToString("0.############################", CultureInfo.CurrentCulture);
			string NormalizedCurrency = Normalize(Currency);

			return string.IsNullOrEmpty(NormalizedCurrency)
				? Amount
				: string.Concat(Amount, " ", NormalizedCurrency);
		}

		private static ImageSource? CreateGlyphImage(Token Token)
		{
			byte[]? Glyph = Token.Glyph;
			if (Glyph is null ||
				Glyph.Length == 0 ||
				string.IsNullOrWhiteSpace(Token.GlyphContentType) ||
				!Token.GlyphContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			try
			{
				using SKData GlyphData = SKData.CreateCopy(Glyph);
				using SKCodec? Codec = SKCodec.Create(GlyphData);
				if (Codec is null || Codec.Info.Width <= 0 || Codec.Info.Height <= 0)
					return null;

				byte[] GlyphCopy = [.. Glyph];
				return ImageSource.FromStream(() => new MemoryStream(GlyphCopy, false));
			}
			catch
			{
				return null;
			}
		}
	}
}
