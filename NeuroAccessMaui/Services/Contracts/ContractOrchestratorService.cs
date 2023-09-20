using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Pages.Contracts.PetitionSignature;
using NeuroAccessMaui.Pages.Identity.PetitionIdentity;
using NeuroAccessMaui.Pages.Identity.ViewIdentity;
using System.Reflection;
using System.Text;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Services.Contracts;

[Singleton]
internal class ContractOrchestratorService : LoadableService, IContractOrchestratorService
{
	public ContractOrchestratorService()
	{
	}

	public override Task Load(bool isResuming, CancellationToken cancellationToken)
	{
		if (this.BeginLoad(cancellationToken))
		{
			this.XmppService.ConnectionStateChanged += this.Contracts_ConnectionStateChanged;
			this.XmppService.PetitionForPeerReviewIdReceived += this.Contracts_PetitionForPeerReviewIdReceived;
			this.XmppService.PetitionForIdentityReceived += this.Contracts_PetitionForIdentityReceived;
			this.XmppService.PetitionForSignatureReceived += this.Contracts_PetitionForSignatureReceived;
			this.XmppService.PetitionedIdentityResponseReceived += this.Contracts_PetitionedIdentityResponseReceived;
			this.XmppService.PetitionedPeerReviewIdResponseReceived += this.Contracts_PetitionedPeerReviewResponseReceived;
			this.XmppService.SignaturePetitionResponseReceived += this.Contracts_SignaturePetitionResponseReceived;

			this.EndLoad(true);
		}

		return Task.CompletedTask;
	}

	public override Task Unload()
	{
		if (this.BeginUnload())
		{
			this.XmppService.ConnectionStateChanged -= this.Contracts_ConnectionStateChanged;
			this.XmppService.PetitionForPeerReviewIdReceived -= this.Contracts_PetitionForPeerReviewIdReceived;
			this.XmppService.PetitionForIdentityReceived -= this.Contracts_PetitionForIdentityReceived;
			this.XmppService.PetitionForSignatureReceived -= this.Contracts_PetitionForSignatureReceived;
			this.XmppService.PetitionedIdentityResponseReceived -= this.Contracts_PetitionedIdentityResponseReceived;
			this.XmppService.PetitionedPeerReviewIdResponseReceived -= this.Contracts_PetitionedPeerReviewResponseReceived;
			this.XmppService.SignaturePetitionResponseReceived -= this.Contracts_SignaturePetitionResponseReceived;

			this.EndUnload();
		}

		return Task.CompletedTask;
	}

	#region Event Handlers

	private async Task Contracts_PetitionForPeerReviewIdReceived(object Sender, SignaturePetitionEventArgs e)
	{
		try
		{
			LegalIdentity Identity = e.RequestorIdentity;

			if (Identity is not null)
			{
				await this.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity,
					e.RequestorFullJid, e.SignatoryIdentityId, e.PetitionId, e.Purpose, e.ContentToSign));
			}
		}
		catch (Exception ex)
		{
			this.LogService.LogException(ex);
		}
	}

	private async Task Contracts_PetitionForIdentityReceived(object Sender, LegalIdentityPetitionEventArgs e)
	{
		try
		{
			LegalIdentity Identity;

			if (e.RequestedIdentityId == this.TagProfile.LegalIdentity?.Id)
				Identity = this.TagProfile.LegalIdentity;
			else
			{
				(bool Succeeded, LegalIdentity LegalId) = await this.NetworkService.TryRequest(() => this.XmppService.GetLegalIdentity(e.RequestedIdentityId));
				if (Succeeded && LegalId is not null)
					Identity = LegalId;
				else
					return;
			}

			if (Identity is null)
			{
				this.LogService.LogWarning("Identity is missing or cannot be retrieved, ignore.",
					new KeyValuePair<string, object>("Type", this.GetType().Name),
					new KeyValuePair<string, object>("Method", nameof(Contracts_PetitionForIdentityReceived)));

				return;
			}

			if (Identity.State == IdentityState.Compromised ||
				Identity.State == IdentityState.Rejected)
			{
				await this.NetworkService.TryRequest(() =>
				{
					return this.XmppService.SendPetitionIdentityResponse(
						e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
				});
			}
			else
			{
				Identity = e.RequestorIdentity;

				if (Identity is not null)
				{
					await this.NavigationService.GoToAsync(nameof(PetitionIdentityPage), new PetitionIdentityNavigationArgs(
						Identity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose));
				}
			}
		}
		catch (Exception ex)
		{
			this.LogService.LogException(ex);
		}
	}

	private async Task Contracts_PetitionForSignatureReceived(object Sender, SignaturePetitionEventArgs e)
	{
		try
		{
			LegalIdentity Identity;

			if (e.SignatoryIdentityId == this.TagProfile.LegalIdentity?.Id)
				Identity = this.TagProfile.LegalIdentity;
			else
			{
				(bool Succeeded, LegalIdentity LegalId) = await this.NetworkService.TryRequest(() => this.XmppService.GetLegalIdentity(e.SignatoryIdentityId));

				if (Succeeded && LegalId is not null)
					Identity = LegalId;
				else
					return;
			}

			if (Identity is null)
			{
				this.LogService.LogWarning("Identity is missing or cannot be retrieved, ignore.",
					new KeyValuePair<string, object>("Type", this.GetType().Name),
					new KeyValuePair<string, object>("Method", nameof(Contracts_PetitionForSignatureReceived)));

				return;
			}

			if (Identity.State == IdentityState.Compromised || Identity.State == IdentityState.Rejected)
			{
				await this.NetworkService.TryRequest(() =>
				{
					return this.XmppService.SendPetitionSignatureResponse(e.SignatoryIdentityId, e.ContentToSign,
						new byte[0], e.PetitionId, e.RequestorFullJid, false);
				});
			}
			else
			{
				Identity = e.RequestorIdentity;

				if (Identity is not null)
				{
					await this.NavigationService.GoToAsync(nameof(PetitionSignaturePage), new PetitionSignatureNavigationArgs(
						Identity, e.RequestorFullJid, e.SignatoryIdentityId, e.ContentToSign, e.PetitionId, e.Purpose));
				}
			}
		}
		catch (Exception ex)
		{
			this.LogService.LogException(ex);
		}
	}

	private async Task Contracts_PetitionedIdentityResponseReceived(object Sender, LegalIdentityPetitionResponseEventArgs e)
	{
		try
		{
			LegalIdentity Identity = e.RequestedIdentity;

			if (!e.Response || Identity is null)
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Message"], LocalizationResourceManager.Current["SignaturePetitionDenied"], LocalizationResourceManager.Current["Ok"]);
			else
			{
				await this.NavigationService.GoToAsync(nameof(ViewIdentityPage),
					new ViewIdentityNavigationArgs(Identity));
			}
		}
		catch (Exception ex)
		{
			this.LogService.LogException(ex);
		}
	}

	private async Task Contracts_PetitionedPeerReviewResponseReceived(object Sender, SignaturePetitionResponseEventArgs e)
	{
		try
		{
			LegalIdentity Identity = e.RequestedIdentity;

			if (Identity is not null)
			{
				try
				{
					if (!e.Response)
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["PeerReviewRejected"], LocalizationResourceManager.Current["APeerYouRequestedToReviewHasRejected"], LocalizationResourceManager.Current["Ok"]);
					else
					{
						StringBuilder Xml = new();
						this.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
						byte[] Data = Encoding.UTF8.GetBytes(Xml.ToString());
						bool? Result;

						try
						{
							Result = this.XmppService.ValidateSignature(Identity, Data, e.Signature);
						}
						catch (Exception ex)
						{
							await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message);
							return;
						}

						if (!Result.HasValue || !Result.Value)
							await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["PeerReviewRejected"], LocalizationResourceManager.Current["APeerYouRequestedToReviewHasBeenRejectedDueToSignatureError"], LocalizationResourceManager.Current["Ok"]);
						else
						{
							(bool Succeeded, LegalIdentity LegalIdentity) = await this.NetworkService.TryRequest(
								() => this.XmppService.AddPeerReviewIdAttachment(
									this.TagProfile.LegalIdentity, Identity, e.Signature));

							if (Succeeded)
								await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["PeerReviewAccepted"], LocalizationResourceManager.Current["APeerReviewYouhaveRequestedHasBeenAccepted"], LocalizationResourceManager.Current["Ok"]);
						}
					}
				}
				catch (Exception ex)
				{
					this.LogService.LogException(ex);
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
				}
			}
		}
		catch (Exception ex)
		{
			this.LogService.LogException(ex);
		}
	}

	private async Task Contracts_ConnectionStateChanged(object _, XmppState NewState)
	{
		try
		{
			if (this.XmppService.IsOnline && this.TagProfile.IsCompleteOrWaitingForValidation())
			{
				if (this.TagProfile.LegalIdentity is not null)
				{
					string id = this.TagProfile.LegalIdentity.Id;
					await Task.Delay(Constants.Timeouts.XmppInit);
					this.DownloadLegalIdentityInternal(id);
				}
			}
		}
		catch (Exception ex)
		{
			this.LogService.LogException(ex);
		}
	}

	#endregion

	protected virtual async void DownloadLegalIdentityInternal(string LegalId)
	{
		// Run asynchronously so we're not blocking startup UI.
		try
		{
			await this.DownloadLegalIdentity(LegalId);
		}
		catch (Exception ex)
		{
			this.LogService.LogException(ex);
		}
	}

	protected async Task DownloadLegalIdentity(string LegalId)
	{
		bool isConnected =
			this.XmppService is not null &&
			await this.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect) &&
			this.XmppService.IsOnline;

		if (!isConnected)
			return;

		(bool succeeded, LegalIdentity identity) = await this.NetworkService.TryRequest(() => this.XmppService.GetLegalIdentity(LegalId), displayAlert: false);
		if (succeeded)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				string userMessage = null;
				bool gotoRegistrationPage = false;

				if (identity.State == IdentityState.Compromised)
				{
					userMessage = LocalizationResourceManager.Current["YourLegalIdentityHasBeenCompromised"];
					await this.TagProfile.CompromiseLegalIdentity(identity);
					gotoRegistrationPage = true;
				}
				else if (identity.State == IdentityState.Obsoleted)
				{
					userMessage = LocalizationResourceManager.Current["YourLegalIdentityHasBeenObsoleted"];
					await this.TagProfile.RevokeLegalIdentity(identity);
					gotoRegistrationPage = true;
				}
				else if (identity.State == IdentityState.Approved && !await this.XmppService.HasPrivateKey(identity.Id))
				{
					bool Response = await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["WarningTitle"], LocalizationResourceManager.Current["UnableToGetAccessToYourPrivateKeys"],
						LocalizationResourceManager.Current["Continue"], LocalizationResourceManager.Current["Repair"]);

					if (Response)
						await this.TagProfile.SetLegalIdentity(identity);
					else
					{
						try
						{
							File.WriteAllText(Path.Combine(this.StorageService.DataFolder, "Start.txt"), DateTime.Now.AddHours(1).Ticks.ToString());
						}
						catch (Exception ex)
						{
							this.LogService.LogException(ex);
						}

						await App.Stop();
						return;
					}
				}
				else
					await this.TagProfile.SetLegalIdentity(identity);

				if (gotoRegistrationPage)
				{
					await App.Current.SetRegistrationPageAsync();

					// After navigating to the registration page, show the user why this happened.
					if (!string.IsNullOrWhiteSpace(userMessage))
					{
						// Do a begin invoke here so the page animation has time to finish,
						// and the view model loads state et.c. before showing the alert.
						// This gives a better UX experience.
						this.UiSerializer.BeginInvokeOnMainThread(async () =>
						{
							await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["YourLegalIdentity"], userMessage);
						});
					}
				}
			});
		}
	}

	public async Task OpenLegalIdentity(string LegalId, string Purpose)
	{
		try
		{
			LegalIdentity identity = await this.XmppService.GetLegalIdentity(LegalId);
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				await this.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(identity));
			});
		}
		catch (ForbiddenException)
		{
			// This happens if you try to view someone else's legal identity.
			// When this happens, try to send a petition to view it instead.
			// Normal operation. Should not be logged.

			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				bool succeeded = await this.NetworkService.TryRequest(() => this.XmppService.PetitionIdentity(LegalId, Guid.NewGuid().ToString(), Purpose));
				if (succeeded)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["PetitionSent"], LocalizationResourceManager.Current["APetitionHasBeenSentToTheOwner"]);
				}
			});
		}
		catch (Exception ex)
		{
			this.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex);
		}
	}

	/// <summary>
	/// TAG Signature request scanned.
	/// </summary>
	/// <param name="Request">Request string.</param>
	public async Task TagSignature(string Request)
	{
		int i = Request.IndexOf(',');
		if (i < 0)
			throw new InvalidOperationException(LocalizationResourceManager.Current["InvalidTagSignatureId"]);

		string JID = Request[..i];
		string Key = Request[(i + 1)..];

		LegalIdentity ID = (this.TagProfile?.LegalIdentity)
			?? throw new InvalidOperationException(LocalizationResourceManager.Current["NoLegalIdSelected"]);

		if (ID.State != IdentityState.Approved)
			throw new InvalidOperationException(LocalizationResourceManager.Current["LegalIdNotApproved"]);

		string IdRef = this.TagProfile?.LegalIdentity?.Id ?? string.Empty;

		if (!await App.VerifyPin())
			return;

		StringBuilder Xml = new();

		Xml.Append("<ql xmlns='https://tagroot.io/schema/Signature' key='");
		Xml.Append(XML.Encode(Key));
		Xml.Append("' legalId='");
		Xml.Append(XML.Encode(IdRef));
		Xml.Append("'/>");

		if (!this.XmppService.IsOnline &&
			!await this.XmppService.WaitForConnectedState(TimeSpan.FromSeconds(10)))
		{
			throw new InvalidOperationException(LocalizationResourceManager.Current["AppNotConnected"]);
		}

		await this.XmppService.IqSetAsync(JID, Xml.ToString());
	}

	private async Task Contracts_SignaturePetitionResponseReceived(object Sender, SignaturePetitionResponseEventArgs e)
	{
		try
		{
			LegalIdentity Identity = e.RequestedIdentity;

			if (!e.Response || Identity is null)
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Message"], LocalizationResourceManager.Current["PetitionToViewLegalIdentityWasDenied"], LocalizationResourceManager.Current["Ok"]);
			else
				await this.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
		}
		catch (Exception ex)
		{
			this.LogService.LogException(ex);
		}
	}

}
