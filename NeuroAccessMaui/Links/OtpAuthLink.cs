using System;
using System.Collections.Generic;
using System.Text;
using Waher.Runtime.Inventory;
using Waher.Security.TOTP;

namespace NeuroAccessMaui.Links
{
	internal class OtpAuthLink : ILinkOpener
	{
		public OtpAuthLink()
		{
		}


		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return string.Equals(Link.Scheme, Constants.UriSchemes.OtpAuth, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public async Task<bool> TryOpenLink(Uri Link, bool ShowErrorIfUnable)
		{
			ExternalCredential NewOtp = await ExternalCredential.CreateAsync(Link.ToString());

			if (NewOtp is null)
				return false;

			return true;
		}
	}
}
