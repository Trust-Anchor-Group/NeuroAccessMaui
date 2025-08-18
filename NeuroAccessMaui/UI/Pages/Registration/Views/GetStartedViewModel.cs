using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Tag;
using Waher.Content.Xml;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using NeuroAccessMaui.Services.UI.QR;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class GetStartedViewModel() : BaseRegistrationViewModel(RegistrationStep.GetStarted)
	{
		[ObservableProperty]
		private bool isLoading = true;
		
		private bool handlesOnboardingLink = false;

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			WeakReferenceMessenger.Default.Register<OnboardingLinkProcessingMessage>(this, (_, m) =>
			{
				this.handlesOnboardingLink = m.IsProcessing;
				MainThread.BeginInvokeOnMainThread(() => this.IsLoading = m.IsProcessing);
			});

			await Task.Delay(3000);
			if (!this.handlesOnboardingLink)
			{
				/*
				if (Clipboard.HasText && ServiceRef.TagProfile.Step == RegistrationStep.GetStarted)
				{
					string? ClipboardText = await Clipboard.GetTextAsync();
					if (!string.IsNullOrEmpty(ClipboardText) && ClipboardText.StartsWith(Constants.UriSchemes.Onboarding, StringComparison.OrdinalIgnoreCase))
					{
						await QrCode.OpenUrl(ClipboardText);
					}
				}
				*/
				MainThread.BeginInvokeOnMainThread(() => this.IsLoading = false);
			}
		}

		protected override Task OnDispose()
		{
			WeakReferenceMessenger.Default.Unregister<OnboardingLinkProcessingMessage>(this);
			return base.OnDispose();
		}

		[RelayCommand]
		private void NewAccount()
		{
			GoToRegistrationStep(RegistrationStep.ValidatePhone);
		}

		[RelayCommand]
		private void ExistingAccount()
		{
			GoToRegistrationStep(RegistrationStep.ContactSupport);
		}

		[RelayCommand]
		private async Task ScanQrCode()
		{

			string? Url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrPageTitleScanInvitation),
				[Constants.UriSchemes.Onboarding]);

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

			await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = true);


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
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ServiceRef.Localizer[nameof(AppResources.InvalidInvitationCode)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);

				return;
			}

			this.IsBusy = true;

			try
			{
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
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.UnableToAccessInvitation)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					this.IsBusy = false;
					await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);

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
						GoToRegistrationStep(RegistrationStep.DefinePassword);
					}
					else if (AccountDone)
					{
						GoToRegistrationStep(RegistrationStep.ValidatePhone);
					}
					else
						GoToRegistrationStep(RegistrationStep.ChooseProvider);
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
			finally
			{
				this.IsBusy = false;
				await MainThread.InvokeOnMainThreadAsync(() => this.IsLoading = false);
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
