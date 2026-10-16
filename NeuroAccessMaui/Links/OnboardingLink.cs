using System.Security.Cryptography;
using System.Text;
using System.Xml;
using NeuroAccessMaui.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages.Onboarding;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens onboarding links, and reads and validates the invitations they refer to.
	/// </summary>
	/// <remarks>
	/// Invitation links have the form <c>obinfo:domain:code:key:iv</c>. The key and IV decrypt the invitation, so the link
	/// must never be logged or shown.
	/// </remarks>
	public class OnboardingLink : ILinkOpener
	{
		private const int fetchTimeoutMilliseconds = 15000;
		private const int maxInvitationCharacters = 1024 * 1024;
		private const int maxResponseBytes = 2 * 1024 * 1024;

		/// <summary>
		/// Opens onboarding links.
		/// </summary>
		public OnboardingLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return string.Equals(Link.Scheme, Constants.UriSchemes.Onboarding, StringComparison.OrdinalIgnoreCase) ? Grade.Ok : Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public async Task<PreparedLink?> PrepareAsync(Uri Link, CancellationToken CancellationToken)
		{
			if (!CanClaimInvitation())
			{
				// The invitation is not read when it cannot be claimed; opening the link explains why instead.
				return new PreparedLink(Link, () => ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.ThisCodeCannotBeClaimedAtThisTime)]));
			}

			Invitation? Invitation = await this.ReadAsync(Link, CancellationToken);
			if (Invitation is null)
				return null;

			return new PreparedLink(Link, async () =>
			{
				OnboardingNavigationArgs Args = new(OnboardingScenario.FullSetup, Invitation.Uri.OriginalString)
				{
					PreparedInvitation = Invitation
				};

				await ServiceRef.NavigationService.SetRootAsync(nameof(OnboardingPage), Args);
			});
		}

		/// <summary>
		/// Retrieves, decrypts, and validates an invitation without applying it.
		/// </summary>
		/// <param name="Link">The invitation link.</param>
		/// <param name="CancellationToken">Cancellation for reading the invitation.</param>
		/// <returns>The validated invitation.</returns>
		/// <exception cref="FormatException">The link or the invitation is malformed.</exception>
		/// <exception cref="CryptographicException">The invitation cannot be decrypted with the link's key.</exception>
		/// <exception cref="XmlException">The decrypted invitation is not well-formed.</exception>
		public async Task<Invitation?> ReadAsync(Uri Link, CancellationToken CancellationToken)
		{
			XmlDocument Definition = await ReadDefinitionAsync(Link.OriginalString, CancellationToken);
			return new Invitation(Link, Definition);
		}

		/// <summary>
		/// Checks whether the current profile can accept an invitation opened from outside the onboarding flow.
		/// </summary>
		/// <remarks>
		/// A profile that already has an account never accepts one, because opening the invitation would restart onboarding
		/// over the existing account.
		/// </remarks>
		/// <returns>Whether an invitation may be claimed now.</returns>
		private static bool CanClaimInvitation()
		{
			if (!string.IsNullOrWhiteSpace(ServiceRef.TagProfile.Account))
				return false;

			RegistrationStep Step = ServiceRef.TagProfile.Step;
			return Step == RegistrationStep.GetStarted || Step == RegistrationStep.ContactSupport;
		}

		private static async Task<XmlDocument> ReadDefinitionAsync(string Text, CancellationToken CancellationToken)
		{
			string Stage = "Structure";

			try
			{
				string[] Parts = Text.Trim().Split(':');
				if (Parts.Length != 5 ||
					!string.Equals(Parts[0], Constants.UriSchemes.Onboarding, StringComparison.OrdinalIgnoreCase) ||
					string.IsNullOrWhiteSpace(Parts[1]) || Parts[1].Any(char.IsWhiteSpace) ||
					string.IsNullOrWhiteSpace(Parts[2]))
				{
					throw new FormatException("Invalid invitation structure.");
				}

				Stage = "EncryptionParameters";
				byte[] Key = Convert.FromBase64String(Parts[3]);
				byte[] Iv = Convert.FromBase64String(Parts[4]);

				// Existing invitation generators use 128-bit keys, so every AES key size is accepted.
				if (Key.Length is not (16 or 24 or 32) || Iv.Length != 16)
					throw new FormatException("Invalid invitation encryption parameters.");

				CancellationToken.ThrowIfCancellationRequested();

				Stage = "Endpoint";
				Uri Endpoint = CreateEndpoint(Parts[1]);

				Stage = "Fetch";
				ContentBinaryResponse Response = await InternetContent.PostAsync(Endpoint, Encoding.ASCII.GetBytes(Parts[2]), "text/plain",
					null, App.ValidateCertificateCallback, fetchTimeoutMilliseconds,
					new KeyValuePair<string, string>("Accept", "text/plain")).WaitAsync(CancellationToken).ConfigureAwait(false);
				Response.AssertOk();

				if (Response.Encoded is null || Response.Encoded.Length > maxResponseBytes)
					throw new FormatException("Invalid invitation response size.");

				Stage = "ResponseDecoding";
				ContentResponse Decoded = await InternetContent.DecodeAsync(Response.ContentType, Response.Encoded, Endpoint)
					.WaitAsync(CancellationToken).ConfigureAwait(false);
				Decoded.AssertOk();

				if (Decoded.Decoded is not string EncryptedText)
					throw new FormatException("Invalid invitation response.");

				Stage = "Decryption";
				byte[] Encrypted = Convert.FromBase64String(EncryptedText);
				byte[] Plaintext;

				using (Aes Cipher = Aes.Create())
				{
					Cipher.Mode = CipherMode.CBC;
					Cipher.Padding = PaddingMode.PKCS7;

					using ICryptoTransform Decryptor = Cipher.CreateDecryptor(Key, Iv);
					Plaintext = Decryptor.TransformFinalBlock(Encrypted, 0, Encrypted.Length);
				}

				Stage = "XmlParsing";
				XmlDocument Document = new()
				{
					PreserveWhitespace = true,
					XmlResolver = null
				};

				using (StringReader TextReader = new(Encoding.UTF8.GetString(Plaintext)))
				using (XmlReader Reader = XmlReader.Create(TextReader, new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Prohibit,
					XmlResolver = null,
					MaxCharactersInDocument = maxInvitationCharacters
				}))
				{
					Document.Load(Reader);
				}

				Stage = "DocumentValidation";
				ValidateDocument(Document);

				CancellationToken.ThrowIfCancellationRequested();
				return Document;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception Ex)
			{
				// Only the stage and exception type are logged; messages may describe the invitation or its keys.
				ServiceRef.LogService.LogWarning("OnboardingInvitationReadFailed",
					new KeyValuePair<string, object?>("Stage", Stage),
					new KeyValuePair<string, object?>("ExceptionType", Ex.GetType().Name));
				throw;
			}
		}

		/// <summary>
		/// Builds the address that invitations are read from, after checking that the invitation names a public domain.
		/// </summary>
		/// <remarks>
		/// The domain comes from a scanned code or an external link and is contacted before the user confirms anything, so only
		/// a fully qualified DNS name is accepted. IP addresses, single-label names such as <c>localhost</c>, and text that would
		/// change the path or credentials of the request are rejected.
		/// </remarks>
		/// <param name="Domain">The domain named by the invitation.</param>
		/// <returns>The invitation endpoint.</returns>
		/// <exception cref="FormatException">The domain is not a usable public domain name.</exception>
		private static Uri CreateEndpoint(string Domain)
		{
			if (Uri.CheckHostName(Domain) != UriHostNameType.Dns || !Domain.Contains('.') ||
				Domain.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
				Domain.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
			{
				throw new FormatException("Invalid invitation domain.");
			}

			UriBuilder Builder = new(Uri.UriSchemeHttps, Domain)
			{
				Path = "/Onboarding/GetInfo"
			};

			return Builder.Uri;
		}

		/// <summary>
		/// Checks that an invitation is an onboarding definition that names a provider or an account to transfer.
		/// </summary>
		/// <param name="Document">The decrypted invitation.</param>
		/// <exception cref="FormatException">The invitation is not a usable onboarding definition.</exception>
		private static void ValidateDocument(XmlDocument Document)
		{
			XmlElement? Root = Document.DocumentElement;
			if (Root is null || Root.NamespaceURI != ContractsClient.NamespaceOnboarding)
				throw new FormatException("Invalid invitation namespace.");

			Queue<XmlElement> Pending = new();
			Pending.Enqueue(Root);
			bool HasProviderOrAccount = false;

			while (Pending.TryDequeue(out XmlElement? Element))
			{
				switch (Element.LocalName)
				{
					case "Transfer":
						foreach (XmlNode Node in Element.ChildNodes)
						{
							if (Node is XmlElement Child)
								Pending.Enqueue(Child);
						}
						break;

					case "ApiKey":
						// An empty domain is allowed; development invitations let the user enter the provider address.
						HasProviderOrAccount = true;
						break;

					case "Account":
						if (string.IsNullOrWhiteSpace(Element.GetAttribute("userName")) ||
							string.IsNullOrWhiteSpace(Element.GetAttribute("password")) ||
							string.IsNullOrWhiteSpace(Element.GetAttribute("domain")))
						{
							throw new FormatException("Invalid account invitation.");
						}

						HasProviderOrAccount = true;
						break;
				}
			}

			if (!HasProviderOrAccount)
				throw new FormatException("The invitation has no provider or account.");
		}
	}
}
