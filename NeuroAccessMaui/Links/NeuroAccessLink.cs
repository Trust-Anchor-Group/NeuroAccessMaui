using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Script;
using Waher.Security.JWT;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens Neuro-Access App links.
	/// </summary>
	public class NeuroAccessLink : ILinkOpener
	{
		/// <summary>
		/// Opens ID App links.
		/// </summary>
		public NeuroAccessLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.Equals(Constants.UriSchemes.NeuroAccess, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public Task<bool> TryOpenLink(Uri Link)
		{
			string? Token = Constants.UriSchemes.RemoveScheme(Link.OriginalString);
			if (string.IsNullOrEmpty(Token))
				return Task.FromResult(false);

			JwtToken Parsed = ServiceRef.CryptoService.ParseAndValidateJwtToken(Token);
			if (Parsed is null)
				return Task.FromResult(false);

			if (!Parsed.TryGetClaim("cmd", out object Obj) || Obj is not string Command ||
				!Parsed.TryGetClaim(JwtClaims.ClientId, out Obj) || Obj is not string ClientId ||
				ClientId != ServiceRef.CryptoService.DeviceID ||
				!Parsed.TryGetClaim(JwtClaims.Issuer, out Obj) || Obj is not string Issuer ||
				Issuer != ServiceRef.CryptoService.DeviceID ||
				!Parsed.TryGetClaim(JwtClaims.Subject, out Obj) || Obj is not string Subject ||
				Subject != ServiceRef.XmppService.BareJid)
			{
				return Task.FromResult(false);
			}

			switch (Command)
			{
				case "bes":  // Buy eDaler Successful
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId ||
						!Parsed.TryGetClaim("amt", out object Amount) ||
						!Parsed.TryGetClaim("cur", out Obj) || Obj is not string Currency)
					{
						return Task.FromResult(false);
					}

					decimal AmountDec;

					try
					{
						AmountDec = Expression.ToDecimal(Amount);
					}
					catch (Exception)
					{
						return Task.FromResult(false);
					}

					ServiceRef.XmppService.BuyEDalerCompleted(TransactionId, AmountDec, Currency);
					return Task.FromResult(true);

				case "bef":  // Buy eDaler Failed
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId2)
						return Task.FromResult(false);

					ServiceRef.XmppService.BuyEDalerFailed(TransactionId2, ServiceRef.Localizer[nameof(AppResources.PaymentFailed)]);
					return Task.FromResult(true);

				case "bec":  // Buy eDaler Cancelled
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId3)
						return Task.FromResult(false);

					ServiceRef.XmppService.BuyEDalerFailed(TransactionId3, ServiceRef.Localizer[nameof(AppResources.PaymentCancelled)]);
					return Task.FromResult(true);

				case "ses":  // Sell eDaler Successful
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId4 ||
						!Parsed.TryGetClaim("amt", out Amount) ||
						!Parsed.TryGetClaim("cur", out Obj) || Obj is not string Currency4)
					{
						return Task.FromResult(false);
					}

					try
					{
						AmountDec = Expression.ToDecimal(Amount);
					}
					catch (Exception)
					{
						return Task.FromResult(false);
					}

					ServiceRef.XmppService.SellEDalerCompleted(TransactionId4, AmountDec, Currency4);
					return Task.FromResult(true);

				case "sef":  // Sell eDaler Failed
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId5)
						return Task.FromResult(false);

					ServiceRef.XmppService.SellEDalerFailed(TransactionId5, ServiceRef.Localizer[nameof(AppResources.PaymentFailed)]);
					return Task.FromResult(true);

				case "sec":  // Sell eDaler Cancelled
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId6)
						return Task.FromResult(false);

					ServiceRef.XmppService.SellEDalerFailed(TransactionId6, ServiceRef.Localizer[nameof(AppResources.PaymentCancelled)]);
					return Task.FromResult(true);

				case "beos":  // Buy eDaler Get Options Successful
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId7)
						return Task.FromResult(false);

					ServiceRef.XmppService.BuyEDalerGetOptionsCompleted(TransactionId7, new IDictionary<CaseInsensitiveString, object>[0]);
					return Task.FromResult(true);

				case "beof":  // Buy eDaler Get Options Failed
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId8)
						return Task.FromResult(false);

					ServiceRef.XmppService.BuyEDalerGetOptionsFailed(TransactionId8, ServiceRef.Localizer[nameof(AppResources.UnableToGetOptions)]);
					return Task.FromResult(true);

				case "beoc":  // Buy eDaler Get Options Cancelled
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId9)
						return Task.FromResult(false);

					ServiceRef.XmppService.BuyEDalerGetOptionsFailed(TransactionId9, ServiceRef.Localizer[nameof(AppResources.GettingOptionsCancelled)]);
					return Task.FromResult(true);

				case "seos":  // Sell eDaler Get Options Successful
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId10)
						return Task.FromResult(false);

					ServiceRef.XmppService.SellEDalerGetOptionsCompleted(TransactionId10, new IDictionary<CaseInsensitiveString, object>[0]);
					return Task.FromResult(true);

				case "seof":  // Sell eDaler Get Options Failed
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId11)
						return Task.FromResult(false);

					ServiceRef.XmppService.SellEDalerGetOptionsFailed(TransactionId11, ServiceRef.Localizer[nameof(AppResources.UnableToGetOptions)]);
					return Task.FromResult(true);

				case "seoc":  // Sell eDaler Get Options Cancelled
					if (!Parsed.TryGetClaim("tid", out Obj) || Obj is not string TransactionId12)
						return Task.FromResult(false);

					ServiceRef.XmppService.SellEDalerGetOptionsFailed(TransactionId12, ServiceRef.Localizer[nameof(AppResources.GettingOptionsCancelled)]);
					return Task.FromResult(true);

				default:
					return Task.FromResult(false);
			}
		}
	}
}
