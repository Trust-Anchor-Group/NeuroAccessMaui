using NeuroAccessMaui.Resources.Languages;

namespace NeuroAccessMaui.Services.Wallet.Tokens
{
	/// <summary>
	/// How soon a token expires.
	/// </summary>
	public enum TokenExpiryStatus
	{
		/// <summary>
		/// The token doesn't expire.
		/// </summary>
		None,

		/// <summary>
		/// The token expires later than <see cref="TokenExpiry.SoonDays"/> days from today.
		/// </summary>
		Later,

		/// <summary>
		/// The token expires within <see cref="TokenExpiry.SoonDays"/> days, but not today.
		/// </summary>
		Soon,

		/// <summary>
		/// The token expires later today.
		/// </summary>
		Today,

		/// <summary>
		/// The token has expired.
		/// </summary>
		Expired
	}

	/// <summary>
	/// Works out how soon a token expires, and how to say it.
	/// </summary>
	public static class TokenExpiry
	{
		/// <summary>
		/// Tokens that expire within this many days are marked as expiring soon.
		/// </summary>
		public const int SoonDays = 7;

		/// <summary>
		/// Checks if a token's expiry date means that it expires at all. Tokens that never expire use the last year.
		/// </summary>
		/// <param name="Expires">When the token expires.</param>
		/// <returns>If the token expires.</returns>
		public static bool HasExpiry(DateTime Expires)
		{
			return Expires > DateTime.MinValue && Expires.Year < 9999;
		}

		/// <summary>
		/// Gets how soon a token expires. Days are counted in the user's time zone, so "today" is the user's today.
		/// </summary>
		/// <param name="Expires">When the token expires.</param>
		/// <param name="NowUtc">The current time, in UTC.</param>
		/// <param name="DaysLeft">Calendar days left until the token expires, or 0 when it has expired or doesn't expire.</param>
		/// <returns>How soon the token expires.</returns>
		public static TokenExpiryStatus GetStatus(DateTime Expires, DateTime NowUtc, out int DaysLeft)
		{
			DaysLeft = 0;

			if (!HasExpiry(Expires))
				return TokenExpiryStatus.None;

			DateTime ExpiresUtc = ToUniversal(Expires);
			if (ExpiresUtc <= NowUtc)
				return TokenExpiryStatus.Expired;

			DateTime ExpiresDay = ExpiresUtc.ToLocalTime().Date;
			DateTime Today = NowUtc.ToLocalTime().Date;

			DaysLeft = (int)(ExpiresDay - Today).TotalDays;
			if (DaysLeft <= 0)
			{
				DaysLeft = 0;
				return TokenExpiryStatus.Today;
			}

			return DaysLeft <= SoonDays ? TokenExpiryStatus.Soon : TokenExpiryStatus.Later;
		}

		/// <summary>
		/// Gets a short text saying how soon a token expires, for tokens that have expired or expire soon.
		/// </summary>
		/// <param name="Status">How soon the token expires.</param>
		/// <param name="DaysLeft">Calendar days left until the token expires.</param>
		/// <returns>The text, or null when the expiry isn't worth pointing out.</returns>
		public static string? GetText(TokenExpiryStatus Status, int DaysLeft)
		{
			return Status switch
			{
				TokenExpiryStatus.Expired => ServiceRef.Localizer[nameof(AppResources.Expired)],
				TokenExpiryStatus.Today => ServiceRef.Localizer[nameof(AppResources.ExpiresToday)],
				TokenExpiryStatus.Soon when DaysLeft == 1 => ServiceRef.Localizer[nameof(AppResources.ExpiresTomorrow)],
				TokenExpiryStatus.Soon => ServiceRef.Localizer[nameof(AppResources.ExpiresInDays), DaysLeft],
				_ => null
			};
		}

		/// <summary>
		/// Treats a time without a known kind as UTC, which is how token times are sent.
		/// </summary>
		/// <param name="Value">A time.</param>
		/// <returns>The same time, in UTC.</returns>
		private static DateTime ToUniversal(DateTime Value)
		{
			return Value.Kind switch
			{
				DateTimeKind.Local => Value.ToUniversalTime(),
				DateTimeKind.Unspecified => DateTime.SpecifyKind(Value, DateTimeKind.Utc),
				_ => Value
			};
		}
	}
}
