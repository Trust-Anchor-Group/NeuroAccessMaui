using System.Security.Cryptography;
using System.Text;
using System.Xml;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Pages;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Opens onboarding links.
	/// </summary>
	public class OnboardingLink : ILinkOpener
	{
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
		public async Task<bool> TryOpenLink(Uri Link, bool ShowErrorIfUnable)
		{
			try
			{

				if (ServiceRef.TagProfile.Step == Services.Tag.RegistrationStep.GetStarted || ServiceRef.TagProfile.Step == Services.Tag.RegistrationStep.ContactSupport)
				{
					await this.ScanQrCode(Link.ToString());
					return true;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}

			if (ShowErrorIfUnable)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.ThisCodeCannotBeClaimedAtThisTime)]);
			}

			return false;
		}

		private async Task ScanQrCode(string Url)
		{
			if (string.IsNullOrEmpty(Url))
				return;

			string Scheme = Constants.UriSchemes.GetScheme(Url) ?? string.Empty;

			if (!string.Equals(Scheme, Constants.UriSchemes.Onboarding, StringComparison.OrdinalIgnoreCase))
				return;

			string[] Parts = Url.Split(':');

			if (Parts.Length != 5)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.InvalidInvitationCode)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);

				return;
			}

			string Domain = Parts[1];
			string Code = Parts[2];
			string KeyStr = Parts[3];
			string IVStr = Parts[4];
			string EncryptedStr;
			Uri Uri;

			try
			{
				Uri = new Uri("https://" + Domain + "/Onboarding/GetInfo");
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.InvalidInvitationCode)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return;
			}

			try
			{
				ContentBinaryResponse Response = await InternetContent.PostAsync(Uri, Encoding.ASCII.GetBytes(Code), "text/plain",
					null,                               // Certificate
					App.ValidateCertificateCallback,          // RemoteCertificateValidator
					new KeyValuePair<string, string>("Accept", "text/plain"));
				Response.AssertOk();

				ContentResponse Decoded = await InternetContent.DecodeAsync(Response.ContentType, Response.Encoded, Uri);
				Decoded.AssertOk();

				EncryptedStr = (string)Decoded.Decoded;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.UnableToAccessInvitation)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return;
			}

			try
			{
				byte[] Key = Convert.FromBase64String(KeyStr);
				byte[] IV = Convert.FromBase64String(IVStr);
				byte[] Encrypted = Convert.FromBase64String(EncryptedStr);

				using Aes Aes = Aes.Create();
				Aes.BlockSize = 128;
				Aes.KeySize = 256;
				Aes.Mode = CipherMode.CBC;
				Aes.Padding = PaddingMode.PKCS7;

				using ICryptoTransform Decryptor = Aes.CreateDecryptor(Key, IV);
				byte[] Decrypted = Decryptor.TransformFinalBlock(Encrypted, 0, Encrypted.Length);
				string Xml = Encoding.UTF8.GetString(Decrypted);

				XmlDocument Doc = new()
				{
					PreserveWhitespace = true
				};

				Doc.LoadXml(Xml);

				if ((Doc.DocumentElement is null) || (Doc.DocumentElement.NamespaceURI != ContractsClient.NamespaceOnboarding))
					throw new Exception("Invalid Invitation XML");

				LinkedList<XmlElement> ToProcess = new();
				ToProcess.AddLast(Doc.DocumentElement);

				bool AccountDone = false;
				XmlElement? LegalIdDefinition = null;
				string? Pin = null;

				while (ToProcess.First is not null)
				{
					XmlElement E = ToProcess.First.Value;
					ToProcess.RemoveFirst();
					switch (E.LocalName)
					{

						case "ApiKey":
							KeyStr = XML.Attribute(E, "key");
							string Secret = XML.Attribute(E, "secret");
							Domain = XML.Attribute(E, "domain");

							await SelectDomain(Domain, KeyStr, Secret);

							await ServiceRef.UiService.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.InvitationAccepted)],
								ServiceRef.Localizer[nameof(AppResources.InvitedToCreateAccountOnDomain), Domain],
								ServiceRef.Localizer[nameof(AppResources.Ok)]);
							break;

						case "Account":
							string UserName = XML.Attribute(E, "userName");
							string Password = XML.Attribute(E, "password");
							string PasswordMethod = XML.Attribute(E, "passwordMethod");
							Domain = XML.Attribute(E, "domain");

							string DomainBak = ServiceRef.TagProfile?.Domain ?? string.Empty;
							bool DefaultConnectivityBak = ServiceRef.TagProfile?.DefaultXmppConnectivity ?? false;
							string ApiKeyBak = ServiceRef.TagProfile?.ApiKey ?? string.Empty;
							string ApiSecretBak = ServiceRef.TagProfile?.ApiSecret ?? string.Empty;

							await SelectDomain(Domain, string.Empty, string.Empty);

							if (!await this.ConnectToAccount(UserName, Password, PasswordMethod, string.Empty, LegalIdDefinition, Pin ?? string.Empty))
							{
								ServiceRef.TagProfile?.SetDomain(DomainBak, DefaultConnectivityBak, ApiKeyBak, ApiSecretBak);
								throw new Exception("Invalid account.");
							}

							AccountDone = true;
							break;

						case "LegalId":
							LegalIdDefinition = E;
							break;

						case "Pin":
							//Pin = XML.Attribute(E, "pin");
							break;

						case "Transfer":
							foreach (XmlNode N in E.ChildNodes)
							{
								if (N is XmlElement E2)
								{
									ToProcess.AddLast(E2);
								}
							}
							break;

						default:
							throw new Exception("Invalid Invitation XML");
					}
				}

				if (AccountDone && LegalIdDefinition is not null)
				{
					BaseViewModel.GoToRegistrationStep(RegistrationStep.DefinePassword);
				}
				else if (AccountDone)
				{
					BaseViewModel.GoToRegistrationStep(RegistrationStep.ValidatePhone);
				}
				else
					BaseViewModel.GoToRegistrationStep(RegistrationStep.ChooseProvider);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.InvalidInvitationCode)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}

		}
		private static async Task SelectDomain(string Domain, string Key, string Secret)
		{
			bool DefaultConnectivity;

			try
			{
				(string HostName, int PortNumber, bool IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(Domain);
				DefaultConnectivity = HostName == Domain && PortNumber == XmppCredentials.DefaultPort;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				DefaultConnectivity = false;
			}

			ServiceRef.TagProfile.SetDomain(Domain, DefaultConnectivity, Key, Secret);
		}

		private async Task<bool> ConnectToAccount(string AccountName, string Password, string PasswordMethod, string LegalIdentityJid, XmlElement? LegalIdDefinition, string Pin)
		{
			try
			{
				async Task OnConnected(XmppClient client)
				{
					DateTime Now = DateTime.Now;
					LegalIdentity? CreatedIdentity = null;
					LegalIdentity? ApprovedIdentity = null;

					bool ServiceDiscoverySucceeded;

					if (ServiceRef.TagProfile.NeedsUpdating())
					{
						ServiceDiscoverySucceeded = await ServiceRef.XmppService.DiscoverServices(client);
					}
					else
					{
						ServiceDiscoverySucceeded = true;
					}

					if (ServiceDiscoverySucceeded && !string.IsNullOrEmpty(ServiceRef.TagProfile.LegalJid))
					{
						bool DestroyContractsClient = false;

						if (!client.TryGetExtension(typeof(ContractsClient), out IXmppExtension Extension) ||
							Extension is not ContractsClient ContractsClient)
						{
							ContractsClient = new ContractsClient(client, ServiceRef.TagProfile.LegalJid);
							DestroyContractsClient = true;
						}

						try
						{
							if (LegalIdDefinition is not null)
								await ContractsClient.ImportKeys(LegalIdDefinition);

							LegalIdentity[] Identities = await ContractsClient.GetLegalIdentitiesAsync();

							foreach (LegalIdentity Identity in Identities)
							{
								try
								{
									if ((string.IsNullOrEmpty(LegalIdentityJid) || string.Compare(LegalIdentityJid, Identity.Id, StringComparison.OrdinalIgnoreCase) == 0) &&
										Identity.HasClientSignature &&
										Identity.HasClientPublicKey &&
										Identity.From <= Now &&
										Identity.To >= Now &&
										(Identity.State == IdentityState.Approved || Identity.State == IdentityState.Created) &&
										Identity.ValidateClientSignature() &&
										await ContractsClient.HasPrivateKey(Identity))
									{
										if (Identity.State == IdentityState.Approved)
										{
											ApprovedIdentity = Identity;
											break;
										}

										CreatedIdentity ??= Identity;
									}
								}
								catch (Exception)
								{
									// Keys might not be available. Ignore at this point. Keys will be generated later.
								}
							}

							LegalIdentity? SelectedIdentity = ApprovedIdentity ?? CreatedIdentity;

							string SelectedId;

							if (SelectedIdentity is not null)
							{
								await ServiceRef.TagProfile.SetAccountAndLegalIdentity(AccountName, client.PasswordHash, client.PasswordHashMethod, SelectedIdentity);
								SelectedId = SelectedIdentity.Id;

								ServiceRef.TagProfile.SetXmppPasswordNeedsUpdating(true);
							}
							else
							{
								ServiceRef.TagProfile.SetAccount(AccountName, client.PasswordHash, client.PasswordHashMethod);
								SelectedId = string.Empty;
							}

							if (!string.IsNullOrEmpty(Pin))
							{
								ServiceRef.TagProfile.LocalPassword = Pin;
							}

							foreach (LegalIdentity Identity in Identities)
							{
								if (Identity.Id == SelectedId)
								{
									continue;
								}

								switch (Identity.State)
								{
									case IdentityState.Approved:
									case IdentityState.Created:
										await ContractsClient.ObsoleteLegalIdentityAsync(Identity.Id);
										break;
								}
							}
						}
						finally
						{
							if (DestroyContractsClient)
							{
								ContractsClient.Dispose();
							}
						}
					}
				}

				(string HostName, int PortNumber, bool IsIpAddress) = await ServiceRef.NetworkService.LookupXmppHostnameAndPort(
					ServiceRef.TagProfile?.Domain ?? string.Empty);

				(bool Succeeded, string? ErrorMessage, string[]? Alternatives) = await ServiceRef.XmppService.TryConnectAndConnectToAccount(
					ServiceRef.TagProfile?.Domain ?? string.Empty,
					IsIpAddress, HostName, PortNumber, AccountName, Password, PasswordMethod, Constants.LanguageCodes.Default,
					typeof(App).Assembly, OnConnected);

				if (!Succeeded)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ErrorMessage ?? string.Empty,
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}

				return Succeeded;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.UnableToConnectTo), ServiceRef.TagProfile?.Domain ?? string.Empty],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}

			return false;
		}
	}
}
