using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Pages.Identity;
using NeuroAccessMaui.Resources.Languages;
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
			ServiceRef.XmppService.ConnectionStateChanged += this.Contracts_ConnectionStateChanged;
			ServiceRef.XmppService.PetitionForPeerReviewIdReceived += this.Contracts_PetitionForPeerReviewIdReceived;
			ServiceRef.XmppService.PetitionForIdentityReceived += this.Contracts_PetitionForIdentityReceived;
			ServiceRef.XmppService.PetitionForSignatureReceived += this.Contracts_PetitionForSignatureReceived;
			ServiceRef.XmppService.PetitionedIdentityResponseReceived += this.Contracts_PetitionedIdentityResponseReceived;
			ServiceRef.XmppService.PetitionedPeerReviewIdResponseReceived += this.Contracts_PetitionedPeerReviewResponseReceived;
			ServiceRef.XmppService.SignaturePetitionResponseReceived += this.Contracts_SignaturePetitionResponseReceived;

			this.EndLoad(true);
		}

		return Task.CompletedTask;
	}

	public override Task Unload()
	{
		if (this.BeginUnload())
		{
			ServiceRef.XmppService.ConnectionStateChanged -= this.Contracts_ConnectionStateChanged;
			ServiceRef.XmppService.PetitionForPeerReviewIdReceived -= this.Contracts_PetitionForPeerReviewIdReceived;
			ServiceRef.XmppService.PetitionForIdentityReceived -= this.Contracts_PetitionForIdentityReceived;
			ServiceRef.XmppService.PetitionForSignatureReceived -= this.Contracts_PetitionForSignatureReceived;
			ServiceRef.XmppService.PetitionedIdentityResponseReceived -= this.Contracts_PetitionedIdentityResponseReceived;
			ServiceRef.XmppService.PetitionedPeerReviewIdResponseReceived -= this.Contracts_PetitionedPeerReviewResponseReceived;
			ServiceRef.XmppService.SignaturePetitionResponseReceived -= this.Contracts_SignaturePetitionResponseReceived;

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
				ViewIdentityNavigationArgs Args = new(
					Identity, e.RequestorFullJid, e.SignatoryIdentityId,
					e.PetitionId, e.Purpose, e.ContentToSign);

				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), Args);
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}

	private async Task Contracts_PetitionForIdentityReceived(object Sender, LegalIdentityPetitionEventArgs e)
	{
		try
		{
			LegalIdentity Identity;

			if (e.RequestedIdentityId == ServiceRef.TagProfile.LegalIdentity?.Id)
			{
				Identity = ServiceRef.TagProfile.LegalIdentity;
			}
			else
			{
				(bool Succeeded, LegalIdentity LegalId) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.GetLegalIdentity(e.RequestedIdentityId));

				if (!Succeeded || LegalId is null)
				{
					return;
				}

				Identity = LegalId;
			}

			if (Identity is null)
			{
				ServiceRef.LogService.LogWarning("Identity is missing or cannot be retrieved, ignore.",
					new KeyValuePair<string, object>("Type", this.GetType().Name),
					new KeyValuePair<string, object>("Method", nameof(Contracts_PetitionForIdentityReceived)));

				return;
			}

			if (Identity.State == IdentityState.Compromised ||
				Identity.State == IdentityState.Rejected)
			{
				await ServiceRef.NetworkService.TryRequest(() =>
				{
					return ServiceRef.XmppService.SendPetitionIdentityResponse(
						e.RequestedIdentityId, e.PetitionId, e.RequestorFullJid, false);
				});
			}
			else
			{
				Identity = e.RequestorIdentity;

				if (Identity is not null)
				{
					await ServiceRef.NavigationService.GoToAsync(nameof(PetitionIdentityPage), new PetitionIdentityNavigationArgs(
						Identity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose));
				}
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}

	private async Task Contracts_PetitionForSignatureReceived(object Sender, SignaturePetitionEventArgs e)
	{
		try
		{
			LegalIdentity Identity;

			if (e.SignatoryIdentityId == ServiceRef.TagProfile.LegalIdentity?.Id)
			{
				Identity = ServiceRef.TagProfile.LegalIdentity;
			}
			else
			{
				(bool Succeeded, LegalIdentity LegalId) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.GetLegalIdentity(e.SignatoryIdentityId));

				if (!Succeeded || LegalId is null)
				{
					return;
				}

				Identity = LegalId;
			}

			if (Identity is null)
			{
				ServiceRef.LogService.LogWarning("Identity is missing or cannot be retrieved, ignore.",
					new KeyValuePair<string, object>("Type", this.GetType().Name),
					new KeyValuePair<string, object>("Method", nameof(Contracts_PetitionForSignatureReceived)));

				return;
			}

			if (Identity.State == IdentityState.Compromised || Identity.State == IdentityState.Rejected)
			{
				await ServiceRef.NetworkService.TryRequest(() =>
				{
					return ServiceRef.XmppService.SendPetitionSignatureResponse(e.SignatoryIdentityId, e.ContentToSign,
						new byte[0], e.PetitionId, e.RequestorFullJid, false);
				});
			}
			else
			{
				Identity = e.RequestorIdentity;

				if (Identity is not null)
				{
					await ServiceRef.NavigationService.GoToAsync(nameof(PetitionSignaturePage), new PetitionSignatureNavigationArgs(
						Identity, e.RequestorFullJid, e.SignatoryIdentityId, e.ContentToSign, e.PetitionId, e.Purpose));
				}
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}

	private async Task Contracts_PetitionedIdentityResponseReceived(object Sender, LegalIdentityPetitionResponseEventArgs e)
	{
		try
		{
			LegalIdentity Identity = e.RequestedIdentity;

			if (!e.Response || Identity is null)
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Message)],
					ServiceRef.Localizer[nameof(AppResources.SignaturePetitionDenied)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			else
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage),
					new ViewIdentityNavigationArgs(Identity));
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
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
					{
						await ServiceRef.UiSerializer.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.PeerReviewRejected)],
							ServiceRef.Localizer[nameof(AppResources.APeerYouRequestedToReviewHasRejected)],
							ServiceRef.Localizer[nameof(AppResources.Ok)]);
					}
					else
					{
						StringBuilder Xml = new();
						ServiceRef.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
						byte[] Data = Encoding.UTF8.GetBytes(Xml.ToString());
						bool? Result;

						try
						{
							Result = ServiceRef.XmppService.ValidateSignature(Identity, Data, e.Signature);
						}
						catch (Exception ex)
						{
							await ServiceRef.UiSerializer.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message);
							return;
						}

						if (!Result.HasValue || !Result.Value)
						{
							await ServiceRef.UiSerializer.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.PeerReviewRejected)],
								ServiceRef.Localizer[nameof(AppResources.APeerYouRequestedToReviewHasBeenRejectedDueToSignatureError)],
								ServiceRef.Localizer[nameof(AppResources.Ok)]);
						}
						else
						{
							(bool Succeeded, LegalIdentity LegalIdentity) = await ServiceRef.NetworkService.TryRequest(
								() => ServiceRef.XmppService.AddPeerReviewIdAttachment(
									ServiceRef.TagProfile.LegalIdentity, Identity, e.Signature));

							if (Succeeded)
							{
								await ServiceRef.UiSerializer.DisplayAlert(
									ServiceRef.Localizer[nameof(AppResources.PeerReviewAccepted)],
									ServiceRef.Localizer[nameof(AppResources.APeerReviewYouhaveRequestedHasBeenAccepted)],
									ServiceRef.Localizer[nameof(AppResources.Ok)]);
							}
						}
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message,
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}

	private async Task Contracts_ConnectionStateChanged(object _, XmppState NewState)
	{
		try
		{
			if (ServiceRef.XmppService.IsOnline && ServiceRef.TagProfile.IsCompleteOrWaitingForValidation())
			{
				if (ServiceRef.TagProfile.LegalIdentity is not null)
				{
					string id = ServiceRef.TagProfile.LegalIdentity.Id;
					await Task.Delay(Constants.Timeouts.XmppInit);
					this.DownloadLegalIdentityInternal(id);
				}
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
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
			ServiceRef.LogService.LogException(ex);
		}
	}

	protected async Task DownloadLegalIdentity(string LegalId)
	{
		bool isConnected =
			ServiceRef.XmppService is not null &&
			await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect) &&
			ServiceRef.XmppService.IsOnline;

		if (!isConnected)
		{
			return;
		}

		(bool succeeded, LegalIdentity identity) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.GetLegalIdentity(LegalId), displayAlert: false);
		if (succeeded)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				string userMessage = null;
				bool gotoRegistrationPage = false;

				if (identity.State == IdentityState.Compromised)
				{
					userMessage = ServiceRef.Localizer[nameof(AppResources.YourLegalIdentityHasBeenCompromised)];
					await ServiceRef.TagProfile.CompromiseLegalIdentity(identity);
					gotoRegistrationPage = true;
				}
				else if (identity.State == IdentityState.Obsoleted)
				{
					userMessage = ServiceRef.Localizer[nameof(AppResources.YourLegalIdentityHasBeenObsoleted)];
					await ServiceRef.TagProfile.RevokeLegalIdentity(identity);
					gotoRegistrationPage = true;
				}
				else if (identity.State == IdentityState.Approved && !await ServiceRef.XmppService.HasPrivateKey(identity.Id))
				{
					bool Response = await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.WarningTitle)],
						ServiceRef.Localizer[nameof(AppResources.UnableToGetAccessToYourPrivateKeys)],
						ServiceRef.Localizer[nameof(AppResources.Continue)],
						ServiceRef.Localizer[nameof(AppResources.Repair)]);

					if (Response)
					{
						await ServiceRef.TagProfile.SetLegalIdentity(identity);
					}
					else
					{
						try
						{
							File.WriteAllText(Path.Combine(ServiceRef.StorageService.DataFolder, "Start.txt"), DateTime.Now.AddHours(1).Ticks.ToString());
						}
						catch (Exception ex)
						{
							ServiceRef.LogService.LogException(ex);
						}

						await App.Stop();
						return;
					}
				}
				else
				{
					await ServiceRef.TagProfile.SetLegalIdentity(identity);
				}

				if (gotoRegistrationPage)
				{
					await App.SetRegistrationPageAsync();

					// After navigating to the registration page, show the user why this happened.
					if (!string.IsNullOrWhiteSpace(userMessage))
					{
						// Do a begin invoke here so the page animation has time to finish,
						// and the view model loads state et.c. before showing the alert.
						// This gives a better UX experience.
						MainThread.BeginInvokeOnMainThread(async () =>
						{
							await ServiceRef.UiSerializer.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.YourLegalIdentity)], userMessage);
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
			LegalIdentity identity = await ServiceRef.XmppService.GetLegalIdentity(LegalId);
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(identity));
			});
		}
		catch (ForbiddenException)
		{
			// This happens if you try to view someone else's legal identity.
			// When this happens, try to send a petition to view it instead.
			// Normal operation. Should not be logged.

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				bool succeeded = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.PetitionIdentity(LegalId, Guid.NewGuid().ToString(), Purpose));
				if (succeeded)
				{
					await ServiceRef.UiSerializer.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.PetitionSent)],
						ServiceRef.Localizer[nameof(AppResources.APetitionHasBeenSentToTheOwner)]);
				}
			});
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
			await ServiceRef.UiSerializer.DisplayException(ex);
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
		{
			throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.InvalidTagSignatureId)]);
		}

		string JID = Request[..i];
		string Key = Request[(i + 1)..];

		LegalIdentity ID = (ServiceRef.TagProfile.LegalIdentity)
			?? throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.NoLegalIdSelected)]);

		if (ID.State != IdentityState.Approved)
		{
			throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.LegalIdNotApproved)]);
		}

		string IdRef = ServiceRef.TagProfile.LegalIdentity?.Id ?? string.Empty;

		if (!await App.VerifyPin())
		{
			return;
		}

		StringBuilder Xml = new();

		Xml.Append("<ql xmlns='https://tagroot.io/schema/Signature' key='");
		Xml.Append(XML.Encode(Key));
		Xml.Append("' legalId='");
		Xml.Append(XML.Encode(IdRef));
		Xml.Append("'/>");

		if (!ServiceRef.XmppService.IsOnline &&
			!await ServiceRef.XmppService.WaitForConnectedState(TimeSpan.FromSeconds(10)))
		{
			throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.AppNotConnected)]);
		}

		await ServiceRef.XmppService.IqSetAsync(JID, Xml.ToString());
	}

	private async Task Contracts_SignaturePetitionResponseReceived(object Sender, SignaturePetitionResponseEventArgs e)
	{
		try
		{
			LegalIdentity Identity = e.RequestedIdentity;

			if (!e.Response || Identity is null)
			{
				await ServiceRef.UiSerializer.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Message)],
					ServiceRef.Localizer[nameof(AppResources.PetitionToViewLegalIdentityWasDenied)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			else
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
			}
		}
		catch (Exception ex)
		{
			ServiceRef.LogService.LogException(ex);
		}
	}
}
