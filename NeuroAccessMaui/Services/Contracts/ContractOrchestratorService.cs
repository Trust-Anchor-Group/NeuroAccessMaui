using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using EDaler;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Authentication;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Identities;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.Services.Xmpp;
using NeuroAccessMaui.UI.Controls.Toasts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Onboarding;
using NeuroAccessMaui.UI.Pages.Main;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionIdentity;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionPeerReview;
using NeuroAccessMaui.UI.Pages.Petitions.PetitionSignature;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Networking.XMPP.StanzaErrors;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Waher.Runtime.Inventory;
using Waher.Security;
using NeuroAccessMaui.Services.UI.Popups;

namespace NeuroAccessMaui.Services.Contracts
{
	[Singleton]
	internal class ContractOrchestratorService : LoadableService, IContractOrchestratorService
	{

		private readonly IAuthenticationService authenticationService = ServiceRef.Provider.GetRequiredService<IAuthenticationService>();
		private readonly ConcurrentDictionary<string, string> identityPetitions = new(StringComparer.Ordinal);
		private readonly ConcurrentDictionary<string, string> contractPetitions = new(StringComparer.Ordinal);
		private string? lastLegalId;

		public ContractOrchestratorService()
		{
		}

		/// <inheritdoc/>
		public event EventHandlerAsync<IdentityPetitionAnsweredEventArgs>? IdentityPetitionAnswered;

		/// <inheritdoc/>
		public override Task Load(bool IsResuming, CancellationToken CancellationToken)
		{
			if (this.BeginLoad(IsResuming, CancellationToken))
			{
				ServiceRef.XmppService.ConnectionStateChanged += this.Contracts_ConnectionStateChanged;
				ServiceRef.XmppService.PetitionForPeerReviewIdReceived += this.Contracts_PetitionForPeerReviewIdReceived;
				ServiceRef.XmppService.PetitionForIdentityReceived += this.Contracts_PetitionForIdentityReceived;
				ServiceRef.XmppService.PetitionForSignatureReceived += this.Contracts_PetitionForSignatureReceived;
				ServiceRef.XmppService.PetitionedIdentityResponseReceived += this.Contracts_PetitionedIdentityResponseReceived;
				ServiceRef.XmppService.PetitionedPeerReviewIdResponseReceived += this.Contracts_PetitionedPeerReviewResponseReceived;
				ServiceRef.XmppService.SignaturePetitionResponseReceived += this.Contracts_SignaturePetitionResponseReceived;
				ServiceRef.XmppService.ContractProposalReceived += this.Contracts_ContractProposalReceived;
				ServiceRef.XmppService.ContractSigned += this.Contracts_ContractSigned;
				ServiceRef.XmppService.ContractUpdated += this.Contracts_ContractUpdated;
				ServiceRef.XmppService.ContractCreated += this.Contracts_ContractCreated;
				ServiceRef.XmppService.ContractDeleted += this.Contracts_ContractDeleted;
				ServiceRef.XmppService.PetitionForContractReceived += this.Contracts_PetitionForContractReceived;
				ServiceRef.XmppService.PetitionedContractResponseReceived += this.Contracts_PetitionedContractResponseReceived;
				ServiceRef.TagProfile.OnPropertiesChanged += this.TagProfile_OnPropertiesChanged;

				this.lastLegalId = ServiceRef.TagProfile.LegalIdentity?.Id;

				// The connection may already be up, for example when the app resumes.
				if (ServiceRef.XmppService.IsOnline && this.lastLegalId is not null)
					this.StartSync(false);

				this.EndLoad(true);
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public override Task Unload()
		{
			if (this.IsLoading || this.BeginUnload())
			{
				ServiceRef.XmppService.ConnectionStateChanged -= this.Contracts_ConnectionStateChanged;
				ServiceRef.XmppService.PetitionForPeerReviewIdReceived -= this.Contracts_PetitionForPeerReviewIdReceived;
				ServiceRef.XmppService.PetitionForIdentityReceived -= this.Contracts_PetitionForIdentityReceived;
				ServiceRef.XmppService.PetitionForSignatureReceived -= this.Contracts_PetitionForSignatureReceived;
				ServiceRef.XmppService.PetitionedIdentityResponseReceived -= this.Contracts_PetitionedIdentityResponseReceived;
				ServiceRef.XmppService.PetitionedPeerReviewIdResponseReceived -= this.Contracts_PetitionedPeerReviewResponseReceived;
				ServiceRef.XmppService.SignaturePetitionResponseReceived -= this.Contracts_SignaturePetitionResponseReceived;
				ServiceRef.XmppService.ContractProposalReceived -= this.Contracts_ContractProposalReceived;
				ServiceRef.XmppService.ContractSigned -= this.Contracts_ContractSigned;
				ServiceRef.XmppService.ContractUpdated -= this.Contracts_ContractUpdated;
				ServiceRef.XmppService.ContractCreated -= this.Contracts_ContractCreated;
				ServiceRef.XmppService.ContractDeleted -= this.Contracts_ContractDeleted;
				ServiceRef.XmppService.PetitionForContractReceived -= this.Contracts_PetitionForContractReceived;
				ServiceRef.XmppService.PetitionedContractResponseReceived -= this.Contracts_PetitionedContractResponseReceived;
				ServiceRef.TagProfile.OnPropertiesChanged -= this.TagProfile_OnPropertiesChanged;

				this.IsLoading = false;
				this.EndUnload();
			}

			return Task.CompletedTask;
		}

		#region Event Handlers

		private async Task Contracts_PetitionForPeerReviewIdReceived(object? Sender, SignaturePetitionEventArgs e)
		{
			try
			{
				LegalIdentity Identity = e.RequestorIdentity;

				if (Identity?.Properties is not null)
				{
					foreach (Property Property in Identity.Properties)
					{
						switch (Property.Name)
						{
							case Constants.XmppProperties.FirstName:
							case Constants.XmppProperties.MiddleNames:
							case Constants.XmppProperties.LastNames:
							case Constants.XmppProperties.FullName:
							case Constants.XmppProperties.PersonalNumber:
							case Constants.XmppProperties.Address:
							case Constants.XmppProperties.Address2:
							case Constants.XmppProperties.Area:
							case Constants.XmppProperties.City:
							case Constants.XmppProperties.ZipCode:
							case Constants.XmppProperties.Region:
							case Constants.XmppProperties.Country:
							case Constants.XmppProperties.Nationality:
							case Constants.XmppProperties.Gender:
							case Constants.XmppProperties.BirthDay:
							case Constants.XmppProperties.BirthMonth:
							case Constants.XmppProperties.BirthYear:
							case Constants.XmppProperties.OrgName:
							case Constants.XmppProperties.OrgNumber:
							case Constants.XmppProperties.OrgAddress:
							case Constants.XmppProperties.OrgAddress2:
							case Constants.XmppProperties.OrgArea:
							case Constants.XmppProperties.OrgCity:
							case Constants.XmppProperties.OrgZipCode:
							case Constants.XmppProperties.OrgRegion:
							case Constants.XmppProperties.OrgCountry:
							case Constants.XmppProperties.OrgDepartment:
							case Constants.XmppProperties.OrgRole:
							case Constants.XmppProperties.DeviceId:
							case Constants.XmppProperties.Jid:
							case Constants.XmppProperties.Phone:
							case Constants.XmppProperties.EMail:
								break;

							default:
								byte[] Signature = await ServiceRef.XmppService.Sign(e.ContentToSign, SignWith.LatestApprovedId);

								await ServiceRef.XmppService.SendPetitionSignatureResponse(e.SignatoryIdentityId, e.ContentToSign, Signature,
									e.PetitionId, e.RequestorFullJid, false);

								return;
						}
					}

					PetitionPeerReviewNavigationArgs Args = new(Identity, e.RequestorFullJid, e.SignatoryIdentityId, e.PetitionId,
						e.Purpose, e.ContentToSign);

					await ServiceRef.NavigationService.GoToAsync(nameof(PetitionPeerReviewPage), Args);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async Task Contracts_PetitionForIdentityReceived(object? Sender, LegalIdentityPetitionEventArgs e)
		{
			try
			{
				LegalIdentity Identity;

				if (e.RequestedIdentityId == ServiceRef.TagProfile.LegalIdentity?.Id)
					Identity = ServiceRef.TagProfile.LegalIdentity;
				else
				{
					(bool Succeeded, LegalIdentity? LegalId) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.GetLegalIdentity(e.RequestedIdentityId));

					if (!Succeeded || LegalId is null)
						return;

					Identity = LegalId;
				}

				if (Identity is null)
				{
					ServiceRef.LogService.LogWarning("Identity is missing or cannot be retrieved, ignore.",
						new KeyValuePair<string, object?>("Type", this.GetType().Name),
						new KeyValuePair<string, object?>("Method", nameof(Contracts_PetitionForIdentityReceived)));

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
						RequestIdentityNotificationEvent Event = new(e);
						await ServiceRef.Provider.GetRequiredService<IPopupService>().PopAsync();
						//await ServiceRef.NotificationService.NewEvent(Event);
						await ServiceRef.NavigationService.GoToAsync(nameof(PetitionIdentityPage), new PetitionIdentityNavigationArgs(
							Identity, e.RequestorFullJid, e.RequestedIdentityId, e.PetitionId, e.Purpose));
						NotificationIntent Intent = CreateIdentityPetitionNotificationIntent(e);
						await this.MarkPetitionNotificationConsumedAsync(Intent);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async Task Contracts_PetitionForSignatureReceived(object? Sender, SignaturePetitionEventArgs e)
		{
			try
			{
				LegalIdentity Identity;

				if (e.SignatoryIdentityId == ServiceRef.TagProfile.LegalIdentity?.Id)
					Identity = ServiceRef.TagProfile.LegalIdentity;
				else
				{
					(bool Succeeded, LegalIdentity? LegalId) = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.GetLegalIdentity(e.SignatoryIdentityId));
					if (!Succeeded || LegalId is null)
						return;

					Identity = LegalId;
				}

				if (Identity is null)
				{
					ServiceRef.LogService.LogWarning("Identity is missing or cannot be retrieved, ignore.",
						new KeyValuePair<string, object?>("Type", this.GetType().Name),
						new KeyValuePair<string, object?>("Method", nameof(Contracts_PetitionForSignatureReceived)));

					return;
				}

				if (Identity.State == IdentityState.Compromised || Identity.State == IdentityState.Rejected)
				{
					await ServiceRef.NetworkService.TryRequest(() =>
					{
						return ServiceRef.XmppService.SendPetitionSignatureResponse(
							e.SignatoryIdentityId, e.ContentToSign, [], e.PetitionId, e.RequestorFullJid, false);
					});
				}
				else
				{
					Identity = e.RequestorIdentity;

					if (Identity is not null)
					{
						await ServiceRef.NavigationService.GoToAsync(nameof(PetitionSignaturePage), new PetitionSignatureNavigationArgs(
							Identity, e.RequestorFullJid, e.SignatoryIdentityId, e.ContentToSign, e.PetitionId, e.Purpose));
						NotificationIntent Intent = CreateSignaturePetitionNotificationIntent(e);
						await this.MarkPetitionNotificationConsumedAsync(Intent);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async Task Contracts_PetitionedIdentityResponseReceived(object? Sender, LegalIdentityPetitionResponseEventArgs e)
		{
			try
			{
				LegalIdentity? Identity = e.Response ? e.RequestedIdentity : null;

				if (!string.IsNullOrEmpty(e.PetitionId) && this.identityPetitions.TryRemove(e.PetitionId, out string? LegalId))
				{
					await this.IdentityPetitionAnswered.Raise(this, new IdentityPetitionAnsweredEventArgs(e.PetitionId, LegalId, Identity is not null, Identity));

					// A contract page that asked updates the part in place.
					if (ServiceRef.NavigationService.CurrentPage is NewContractPage or ViewContractPage)
						return;
				}

				if (Identity is null)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.IdentityRequestDeclined)],
						ServiceRef.Localizer[nameof(AppResources.IdentityRequestDeclinedText)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
				else if (ServiceRef.NavigationService.CurrentPage is not (NewContractPage or ViewContractPage))
					await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async Task Contracts_PetitionedPeerReviewResponseReceived(object? Sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				LegalIdentity? ReviewedIdentity = ServiceRef.TagProfile.IdentityApplication;
				if (ReviewedIdentity is null)
					return;

				if (!e.Response)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.PeerReviewRejected)],
						ServiceRef.Localizer[nameof(AppResources.APeerYouRequestedToReviewHasRejected)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);

					return;
				}

				LegalIdentity ReviewerIdentity = e.RequestedIdentity;
				if (ReviewerIdentity is null)
					return;

				try
				{
					StringBuilder Xml = new();
					ReviewedIdentity.Serialize(Xml, true, true, true, true, true, true, true);
					string s = Xml.ToString();
					byte[] Data = Encoding.UTF8.GetBytes(s);
					bool? Result;

					try
					{
						Result = ServiceRef.XmppService.ValidateSignature(ReviewerIdentity, Data, e.Signature);
					}
					catch (Exception ex)
					{
						await ServiceRef.UiService.DisplayException(ex);
						return;
					}

					if (!Result.HasValue || !Result.Value)
					{
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.PeerReviewRejected)],
							ServiceRef.Localizer[nameof(AppResources.APeerYouRequestedToReviewHasBeenRejectedDueToSignatureError)],
							ServiceRef.Localizer[nameof(AppResources.Ok)]);
					}
					else
					{
						(bool Succeeded, LegalIdentity? LegalIdentity) = await ServiceRef.NetworkService.TryRequest(async () =>
						{
							LegalIdentity Result = await ServiceRef.XmppService.AddPeerReviewIdAttachment(ReviewedIdentity, ReviewerIdentity, e.Signature);
							await ServiceRef.TagProfile.IncrementNrPeerReviews();
							return Result;
						});

						if (Succeeded)
						{
							await ServiceRef.UiService.DisplayAlert(
								ServiceRef.Localizer[nameof(AppResources.PeerReviewAccepted)],
								ServiceRef.Localizer[nameof(AppResources.APeerReviewYouhaveRequestedHasBeenAccepted)],
								ServiceRef.Localizer[nameof(AppResources.Ok)]);
						}
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ex.Message,
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private static NotificationIntent CreateIdentityPetitionNotificationIntent(LegalIdentityPetitionEventArgs e)
		{
			string Title = ServiceRef.Localizer[nameof(AppResources.NotificationPetitionIdentityTitle), ToBareJid(e.RequestorFullJid)];
			string Body = ServiceRef.Localizer[nameof(AppResources.NotificationPetitionIdentityBody)];

			NotificationIntent Intent = new()
			{
				Channel = Constants.PushChannels.Petitions,
				Title = Title,
				Body = Body,
				Action = NotificationAction.OpenPetition,
				EntityId = e.RequestorFullJid,
				CorrelationId = e.PetitionId,
				Presentation = NotificationPresentation.StoreOnly
			};

			Intent.Extras["petitionId"] = e.PetitionId ?? string.Empty;
			Intent.Extras["requestedIdentityId"] = e.RequestedIdentityId ?? string.Empty;
			Intent.Extras["requestorIdentityId"] = e.RequestorIdentity?.Id ?? string.Empty;

			return Intent;
		}

		private static NotificationIntent CreateSignaturePetitionNotificationIntent(SignaturePetitionEventArgs e)
		{
			string Title = ServiceRef.Localizer[nameof(AppResources.NotificationPetitionSignatureTitle), ToBareJid(e.RequestorFullJid)];
			string Body = ServiceRef.Localizer[nameof(AppResources.NotificationPetitionSignatureBody)];

			NotificationIntent Intent = new()
			{
				Channel = Constants.PushChannels.Petitions,
				Title = Title,
				Body = Body,
				Action = NotificationAction.OpenPetition,
				EntityId = e.RequestorFullJid,
				CorrelationId = e.PetitionId,
				Presentation = NotificationPresentation.StoreOnly
			};

			byte[] ContentToSign = e.ContentToSign ?? Array.Empty<byte>();
			string ContentToSignBase64 = Convert.ToBase64String(ContentToSign);
			string Purpose = e.Purpose ?? string.Empty;
			string RequestorIdentityId = e.RequestorIdentity?.Id ?? string.Empty;

			Intent.Extras["signatoryId"] = e.SignatoryIdentityId ?? string.Empty;
			Intent.Extras["petitionId"] = e.PetitionId ?? string.Empty;
			Intent.Extras["requestorIdentityId"] = RequestorIdentityId;
			Intent.Extras["purpose"] = Purpose;
			Intent.Extras["contentToSign"] = ContentToSignBase64;

			return Intent;
		}

		private async Task MarkPetitionNotificationConsumedAsync(NotificationIntent Intent)
		{
			try
			{
				INotificationServiceV2 NotificationService = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
				string NotificationId = NotificationService.ComputeId(Intent, NotificationSource.Xmpp);
				await NotificationService.MarkConsumedAsync(NotificationId, CancellationToken.None);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private static string ToBareJid(string Jid)
		{
			if (string.IsNullOrWhiteSpace(Jid))
				return Jid;

			int SlashIndex = Jid.IndexOf('/');
			return SlashIndex > -1 ? Jid.Substring(0, SlashIndex) : Jid;
		}

		private Task Contracts_ConnectionStateChanged(object _, XmppState NewState)
		{
			if (NewState == XmppState.Connected && ServiceRef.TagProfile.LegalIdentity is not null)
				this.StartSync(false);

			return Task.CompletedTask;
		}

		private void TagProfile_OnPropertiesChanged(object? Sender, EventArgs e)
		{
			string? LegalId = ServiceRef.TagProfile.LegalIdentity?.Id;
			if (string.IsNullOrEmpty(LegalId) || string.Equals(LegalId, this.lastLegalId, StringComparison.OrdinalIgnoreCase))
				return;

			this.lastLegalId = LegalId;

			// A new identity, for example after keys were imported during onboarding. Read the contracts it takes part in.
			if (ServiceRef.XmppService.IsOnline)
				this.StartSync(true);
		}

		private void StartSync(bool Force)
		{
			RunInBackground(() => ServiceRef.ContractSyncService.SyncAsync(Force, CancellationToken.None));
		}

		/// <summary>
		/// Runs work outside the XMPP event handler, so a server request made by the work can't hold up incoming messages.
		/// </summary>
		/// <param name="Action">The work.</param>
		private static void RunInBackground(Func<Task> Action)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					await Action();
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			});
		}

		private Task Contracts_ContractSigned(object? Sender, ContractSignedEventArgs e)
		{
			RunInBackground(async () =>
			{
				ContractReference? Before = await ServiceRef.ContractStore.GetAsync(e.ContractId);
				ContractRefreshResult Result = await ServiceRef.ContractSyncService.RefreshAsync(e.ContractId, ContractOrigin.Event, false, CancellationToken.None);

				if (Result.Status == ContractRefreshStatus.Deleted)
				{
					if (Before is not null)
						await this.NotifyDeletedAsync(e.ContractId, Before);
				}
				else if (Result.Status == ContractRefreshStatus.Updated && Result.Reference is not null && !IsMyLegalId(e.LegalId))
					await this.NotifySignedAsync(Result.Reference, Result.Contract, e.LegalId, e.Role);
			});

			return Task.CompletedTask;
		}

		private Task Contracts_ContractUpdated(object? Sender, ContractReferenceEventArgs e)
		{
			RunInBackground(async () =>
			{
				ContractReference? Before = await ServiceRef.ContractStore.GetAsync(e.ContractId);
				ContractRefreshResult Result = await ServiceRef.ContractSyncService.RefreshAsync(e.ContractId, ContractOrigin.Event, false, CancellationToken.None);

				if (Result.Status == ContractRefreshStatus.Deleted)
				{
					if (Before is not null)
						await this.NotifyDeletedAsync(e.ContractId, Before);
				}
				else if (Result.Status == ContractRefreshStatus.Updated && Result.Reference is not null)
					await this.NotifyUpdatedAsync(Result.Reference);
			});

			return Task.CompletedTask;
		}

		private Task Contracts_ContractCreated(object? Sender, ContractReferenceEventArgs e)
		{
			// Created by the user's own account, for example on another device. Stored without a notification.
			RunInBackground(() => ServiceRef.ContractSyncService.RefreshAsync(e.ContractId, ContractOrigin.Created, true, CancellationToken.None));
			return Task.CompletedTask;
		}

		private Task Contracts_ContractDeleted(object? Sender, ContractReferenceEventArgs e)
		{
			RunInBackground(async () =>
			{
				ContractReference? Before = await ServiceRef.ContractStore.GetAsync(e.ContractId);
				await ServiceRef.ContractStore.MarkDeletedAsync(e.ContractId);

				// When the user deleted the contract, the row is already gone, and nothing is said.
				if (Before is not null)
					await this.NotifyDeletedAsync(e.ContractId, Before);
			});

			return Task.CompletedTask;
		}

		private Task Contracts_ContractProposalReceived(object? Sender, ContractProposalEventArgs e)
		{
			RunInBackground(() => this.HandleInvitationAsync(e));
			return Task.CompletedTask;
		}

		/// <summary>
		/// Keeps an invitation to sign a contract, with the contract when it can be read, and tells the user about it.
		/// Nothing opens by itself.
		/// </summary>
		/// <param name="e">The invitation.</param>
		private async Task HandleInvitationAsync(ContractProposalEventArgs e)
		{
			Contract? Contract = null;

			try
			{
				Contract = await ServiceRef.XmppService.GetContract(e.ContractId);
			}
			catch (ForbiddenException)
			{
				// Access may not have been granted yet. The invitation is kept without the contract.
			}
			catch (ItemNotFoundException)
			{
				return;
			}
			catch (Exception Ex)
			{
				if (!ContractSyncService.IsConnectionError(Ex))
					ServiceRef.LogService.LogException(Ex);
			}

			if (Contract?.State == ContractState.Deleted)
				return;

			ContractInvitation Invitation = new(e.ContractId, e.Role, e.FromBareJID, e.MessageText, DateTime.UtcNow);
			ContractReference Ref = await ServiceRef.ContractStore.RecordInvitationAsync(Invitation, Contract);

			string Sender = await GetPersonNameAsync(e.FromBareJID, null, null);
			string Title = Ref.ContractLoaded
				? ServiceRef.Localizer[nameof(AppResources.NotificationInvitationTitle), GetContractName(Ref, e.ContractId)]
				: ServiceRef.Localizer[nameof(AppResources.NotificationInvitationTitleUnnamed)];
			string Body = string.IsNullOrEmpty(e.Role)
				? ServiceRef.Localizer[nameof(AppResources.NotificationInvitationBodyNoRole), Sender]
				: ServiceRef.Localizer[nameof(AppResources.NotificationInvitationBody), Sender, e.Role];

			NotificationIntent Intent = CreateContractIntent(e.ContractId, Title, Body);
			Intent.Extras["role"] = e.Role ?? string.Empty;
			Intent.Extras["fromJid"] = e.FromBareJID ?? string.Empty;
			Intent.Extras["message"] = e.MessageText ?? string.Empty;
			Intent.Extras["kind"] = "invitation";

			await AddNotificationAsync(Intent, null);
			await this.ShowToastAsync(Intent);
		}

		private Task Contracts_PetitionForContractReceived(object? Sender, ContractPetitionEventArgs e)
		{
			RunInBackground(() => this.HandleContractPetitionAsync(e));
			return Task.CompletedTask;
		}

		/// <summary>
		/// Tells the user that someone asks to see one of their contracts. The request is answered from the petition page.
		/// </summary>
		/// <param name="e">The request.</param>
		private async Task HandleContractPetitionAsync(ContractPetitionEventArgs e)
		{
			ContractReference? Ref = await ServiceRef.ContractStore.GetAsync(e.RequestedContractId);
			string Requester = await GetPersonNameAsync(ToBareJid(e.RequestorFullJid), e.RequestorIdentity, null);
			string Title = ServiceRef.Localizer[nameof(AppResources.NotificationContractPetitionTitle), Requester, GetContractName(Ref, e.RequestedContractId)];
			string Body = string.IsNullOrWhiteSpace(e.Purpose)
				? ServiceRef.Localizer[nameof(AppResources.NotificationPetitionContractBody)]
				: e.Purpose;

			NotificationIntent Intent = new()
			{
				Channel = Constants.PushChannels.Petitions,
				Title = Title,
				Body = Body,
				Action = NotificationAction.OpenPetition,
				EntityId = e.RequestorFullJid,
				CorrelationId = e.PetitionId,
				Presentation = NotificationPresentation.StoreOnly
			};

			Intent.Extras["contractId"] = e.RequestedContractId ?? string.Empty;
			Intent.Extras["petitionId"] = e.PetitionId ?? string.Empty;
			Intent.Extras["requestorIdentityId"] = e.RequestorIdentity?.Id ?? string.Empty;
			Intent.Extras["purpose"] = e.Purpose ?? string.Empty;

			await AddNotificationAsync(Intent, XmppService.SerializeIdentity(e.RequestorIdentity));
			await this.ShowToastAsync(Intent);
		}

		private Task Contracts_PetitionedContractResponseReceived(object? Sender, ContractPetitionResponseEventArgs e)
		{
			if (string.IsNullOrEmpty(e.PetitionId) || !this.contractPetitions.TryRemove(e.PetitionId, out string? ContractId))
				return Task.CompletedTask;

			RunInBackground(() => this.HandleContractAccessAnswerAsync(ContractId, e.Response));
			return Task.CompletedTask;
		}

		/// <summary>
		/// Handles the answer to a request to see a contract. The contract that comes with the answer isn't decrypted, so
		/// it is read again. When the request was declined, the contract that came with the answer is dropped.
		/// </summary>
		/// <param name="ContractId">The ID of the requested contract.</param>
		/// <param name="Accepted">If the request was accepted.</param>
		private async Task HandleContractAccessAnswerAsync(string ContractId, bool Accepted)
		{
			if (!Accepted)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Message)],
					ServiceRef.Localizer[nameof(AppResources.PetitionToViewContractWasDenied)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return;
			}

			ContractRefreshResult Result = await ServiceRef.ContractSyncService.RefreshAsync(ContractId, ContractOrigin.Opened, false, CancellationToken.None);
			if (Result.Contract is null)
				return;

			ViewContractViewModel? OpenPage = await MainThread.InvokeOnMainThreadAsync(() =>
				ServiceRef.NavigationService.CurrentPage is ViewContractPage { BindingContext: ViewContractViewModel ViewModel } &&
				string.Equals(ViewModel.Contract?.ContractId, ContractId, StringComparison.OrdinalIgnoreCase) ? ViewModel : null);

			if (OpenPage is not null)
			{
				await OpenPage.ShowAccessGrantedContractAsync(Result.Contract);
				return;
			}

			string Name = Result.Reference is not null ? GetContractName(Result.Reference, ContractId) : await ContractModel.GetCategory(Result.Contract) ?? ContractId;
			NotificationIntent Intent = CreateContractIntent(ContractId,
				ServiceRef.Localizer[nameof(AppResources.NotificationContractAccessGrantedTitle), Name],
				ServiceRef.Localizer[nameof(AppResources.NotificationContractAccessGrantedBody)]);

			await AddNotificationAsync(Intent, null);
			await this.ShowToastAsync(Intent);
		}

		#region Notifications and toasts

		private async Task NotifySignedAsync(ContractReference Ref, Contract? Contract, string? LegalId, string? Role)
		{
			string? BareJid = Contract?.ClientSignatures?.FirstOrDefault(Signature =>
				string.Equals(Signature.LegalId, LegalId, StringComparison.OrdinalIgnoreCase))?.BareJid;

			string Signer = await GetPersonNameAsync(BareJid, null, LegalId);
			string Name = GetContractName(Ref, Ref.ContractId?.Value ?? string.Empty);
			string Body = string.IsNullOrEmpty(Role)
				? ServiceRef.Localizer[nameof(AppResources.NotificationContractSignedByBodyNoRole), Signer]
				: ServiceRef.Localizer[nameof(AppResources.NotificationContractSignedByBody), Signer, Role];

			NotificationIntent Intent = CreateContractIntent(Ref.ContractId?.Value ?? string.Empty,
				ServiceRef.Localizer[nameof(AppResources.NotificationContractWasSignedTitle), Name], Body);

			await AddNotificationAsync(Intent, null);
		}

		private async Task NotifyUpdatedAsync(ContractReference Ref)
		{
			string Name = GetContractName(Ref, Ref.ContractId?.Value ?? string.Empty);
			string Title = Ref.State == ContractState.Obsoleted
				? ServiceRef.Localizer[nameof(AppResources.NotificationContractWasWithdrawnTitle), Name]
				: ServiceRef.Localizer[nameof(AppResources.NotificationContractWasUpdatedTitle), Name];

			NotificationIntent Intent = CreateContractIntent(Ref.ContractId?.Value ?? string.Empty, Title,
				ServiceRef.Localizer[nameof(AppResources.NotificationContractUpdatedBody)]);

			await AddNotificationAsync(Intent, null);
		}

		private async Task NotifyDeletedAsync(string ContractId, ContractReference Before)
		{
			string Name = GetContractName(Before, ContractId);
			NotificationIntent Intent = CreateContractIntent(ContractId,
				ServiceRef.Localizer[nameof(AppResources.NotificationContractWasDeletedTitle), Name],
				ServiceRef.Localizer[nameof(AppResources.NotificationContractDeletedBody)]);

			Intent.Extras["kind"] = "deleted";

			await AddNotificationAsync(Intent, null);
		}

		private static NotificationIntent CreateContractIntent(string ContractId, string Title, string Body)
		{
			NotificationIntent Intent = new()
			{
				Channel = Constants.PushChannels.Contracts,
				Title = Title,
				Body = Body,
				Action = NotificationAction.OpenContract,
				EntityId = ContractId,
				CorrelationId = ContractId,
				Presentation = NotificationPresentation.StoreOnly
			};

			Intent.Extras["contractId"] = ContractId;
			return Intent;
		}

		private static async Task AddNotificationAsync(NotificationIntent Intent, string? RawPayload)
		{
			try
			{
				INotificationServiceV2 NotificationService = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
				await NotificationService.AddAsync(Intent, NotificationSource.Xmpp, RawPayload, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Shows a notification as a toast, when the app is in the foreground. Tapping the toast opens what the notification
		/// points to, and marks it as handled.
		/// </summary>
		/// <param name="Intent">The notification.</param>
		private async Task ShowToastAsync(NotificationIntent Intent)
		{
			try
			{
				NotificationToastView? Toast = await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (Application.Current is null || Application.Current.Windows.Count == 0 || ServiceRef.NavigationService.CurrentPage is null)
						return null;

					return new NotificationToastView(Intent.Title, Intent.Body, () => this.OnToastTappedAsync(Intent));
				});

				if (Toast is null)
					return;

				await ServiceRef.ToastService.ShowAsync(Toast, new ToastOptions()
				{
					Duration = TimeSpan.FromSeconds(6)
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task OnToastTappedAsync(NotificationIntent Intent)
		{
			try
			{
				await ServiceRef.ToastService.HideAsync();
				await MarkNotificationsConsumedAsync(Intent.Channel, Intent.Action, Intent.CorrelationId);
				await ServiceRef.Provider.GetRequiredService<INotificationIntentRouter>().RouteAsync(Intent, true, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Marks stored notifications as handled. Records are found by channel, action and correlation ID, since records
		/// merged from several events keep the ID of the first one.
		/// </summary>
		/// <param name="Channel">The notification channel.</param>
		/// <param name="Action">The notification action.</param>
		/// <param name="CorrelationId">The correlation ID: a contract ID or a petition ID.</param>
		private static async Task MarkNotificationsConsumedAsync(string? Channel, NotificationAction Action, string? CorrelationId)
		{
			if (string.IsNullOrEmpty(Channel) || string.IsNullOrEmpty(CorrelationId))
				return;

			try
			{
				INotificationServiceV2 NotificationService = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
				IReadOnlyList<NotificationRecord> Records = await NotificationService.GetAsync(new NotificationQuery()
				{
					Channels = [Channel]
				}, CancellationToken.None);

				string ActionName = Action.ToString();

				foreach (NotificationRecord Record in Records)
				{
					if (Record.State != NotificationState.Consumed &&
						string.Equals(Record.Action, ActionName, StringComparison.Ordinal) &&
						string.Equals(Record.CorrelationId, CorrelationId, StringComparison.OrdinalIgnoreCase))
					{
						await NotificationService.MarkConsumedAsync(Record.Id, CancellationToken.None);
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Removes stored notifications for a contract or a petition.
		/// </summary>
		/// <param name="Channel">The notification channel.</param>
		/// <param name="CorrelationId">The correlation ID: a contract ID or a petition ID.</param>
		private static async Task DeleteNotificationsAsync(string Channel, string CorrelationId)
		{
			try
			{
				INotificationServiceV2 NotificationService = ServiceRef.Provider.GetRequiredService<INotificationServiceV2>();
				IReadOnlyList<NotificationRecord> Records = await NotificationService.GetAsync(new NotificationQuery()
				{
					Channels = [Channel]
				}, CancellationToken.None);

				List<string> Ids = [];
				foreach (NotificationRecord Record in Records)
				{
					if (string.Equals(Record.CorrelationId, CorrelationId, StringComparison.OrdinalIgnoreCase) ||
						string.Equals(Record.EntityId, CorrelationId, StringComparison.OrdinalIgnoreCase))
					{
						Ids.Add(Record.Id);
					}
				}

				if (Ids.Count > 0)
					await NotificationService.DeleteAsync(Ids, CancellationToken.None);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Gets the name a notification uses for a contract: its title, else the names of the other parties, else its ID.
		/// </summary>
		private static string GetContractName(ContractReference? Ref, string ContractId)
		{
			if (Ref?.ContractLoaded == true)
			{
				if (!string.IsNullOrWhiteSpace(Ref.Category))
					return Ref.Category;

				if (!string.IsNullOrWhiteSpace(Ref.Name))
					return Ref.Name;
			}

			return ContractId;
		}

		/// <summary>
		/// Gets the name a notification uses for a person: from contacts, else from their identity, else their bare JID.
		/// </summary>
		/// <param name="BareJid">The person's bare JID, if known.</param>
		/// <param name="Identity">The person's legal identity, if known.</param>
		/// <param name="LegalId">The person's legal ID, if known.</param>
		/// <returns>A display name.</returns>
		private static async Task<string> GetPersonNameAsync(string? BareJid, LegalIdentity? Identity, string? LegalId)
		{
			try
			{
				if (!string.IsNullOrEmpty(BareJid))
				{
					ContactInfo? Info = await ContactInfo.FindByBareJid(BareJid);
					if (!string.IsNullOrEmpty(Info?.FriendlyName))
						return Info.FriendlyName;
				}

				if (!string.IsNullOrEmpty(LegalId))
				{
					ContactInfo? Info = await ContactInfo.FindByLegalId(LegalId);
					if (!string.IsNullOrEmpty(Info?.FriendlyName))
						return Info.FriendlyName;

					Identity ??= Info?.LegalIdentity;
				}

				if (Identity is not null)
				{
					string Name = ContactInfo.GetFriendlyName(Identity);
					if (!string.IsNullOrEmpty(Name))
						return Name;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			if (!string.IsNullOrEmpty(BareJid))
				return BareJid;

			return LegalId ?? string.Empty;
		}

		private static bool IsMyLegalId(string? LegalId)
		{
			return !string.IsNullOrEmpty(LegalId) &&
				string.Equals(LegalId, ServiceRef.TagProfile.LegalIdentity?.Id, StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#endregion

		protected static async Task ReDownloadLegalIdentity()
		{
			if (ServiceRef.XmppService is null ||
				!await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect) ||
				!ServiceRef.XmppService.IsOnline ||
				ServiceRef.TagProfile.LegalIdentity is null)
			{
				return;
			}

			LegalIdentity? Identity;

			try
			{
				Identity = await ServiceRef.XmppService!.GetLegalIdentity(ServiceRef.TagProfile.LegalIdentity.Id);
			}
			catch (ForbiddenException)    // Old ID belonging to a previous account, for example. Simply discard.
			{
				await ServiceRef.TagProfile.ClearLegalIdentity();
				await ServiceRef.NavigationService.GoToAsync(nameof(OnboardingPage), new OnboardingNavigationArgs() { Scenario = OnboardingScenario.FullSetup });
				return;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				return;
			}

			if (Identity is not null)
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					try
					{

						string? UserMessage = null;
						bool GotoRegistrationPage = false;

						if (Identity.State == IdentityState.Compromised)
						{
							UserMessage = ServiceRef.Localizer[nameof(AppResources.YourLegalIdentityHasBeenCompromised)];
							await ServiceRef.TagProfile.CompromiseLegalIdentity(Identity);
							GotoRegistrationPage = true;
						}
						else if (Identity.State == IdentityState.Obsoleted)
						{
							UserMessage = ServiceRef.Localizer[nameof(AppResources.YourLegalIdentityHasBeenObsoleted)];
							await ServiceRef.TagProfile.RevokeLegalIdentity(Identity);
							GotoRegistrationPage = true;
						}
						else if (Identity.State == IdentityState.Approved)
						{
							bool HasPrivateKeys = false;
							try
							{
								HasPrivateKeys = await ServiceRef.XmppService.HasPrivateKey(Identity.Id);

							}
							catch (Exception Ex)
							{
								ServiceRef.LogService.LogException(Ex);
							}

							if (HasPrivateKeys)
							{
								await ServiceRef.TagProfile.SetLegalIdentity(Identity, true);
								return;
							}

							bool Response = await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.WarningTitle)],
							ServiceRef.Localizer[nameof(AppResources.UnableToGetAccessToYourPrivateKeys)],
							ServiceRef.Localizer[nameof(AppResources.Continue)],
							ServiceRef.Localizer[nameof(AppResources.Repair)]);

							if (Response)
								await ServiceRef.TagProfile.SetLegalIdentity(Identity, true);
							else
							{
								try
								{
									File.WriteAllText(Path.Combine(ServiceRef.StorageService.DataFolder, "Start.txt"),
										DateTime.Now.AddHours(1).Ticks.ToString(CultureInfo.InvariantCulture));
								}
								catch (Exception ex)
								{
									ServiceRef.LogService.LogException(ex);
								}

								await App.StopAsync();
								return;
							}
						}
						else
							await ServiceRef.TagProfile.SetLegalIdentity(Identity, true);

						if (GotoRegistrationPage)
						{
							//await App.SetRegistrationPageAsync();

							// After navigating to the registration page, show the user why this happened.
							if (!string.IsNullOrWhiteSpace(UserMessage))
							{
								// Do a begin invoke here so the page animation has time to finish,
								// and the view model loads state et.c. before showing the alert.
								// This gives a better UX experience.
								MainThread.BeginInvokeOnMainThread(async () =>
								{
									await ServiceRef.UiService.DisplayAlert(
										ServiceRef.Localizer[nameof(AppResources.YourLegalIdentity)], UserMessage);
								});
							}
						}
					}
					catch (Exception E)
					{
						ServiceRef.LogService.LogException(E);
					}
				});
			}
		}

		/// <summary>
		/// Downloads the specified <see cref="LegalIdentity"/> and opens the corresponding page in the app to show it.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity to show.</param>
		/// <param name="Purpose">The purpose to state if the identity can't be downloaded and needs to be petitioned instead.</param>
		public async Task OpenLegalIdentity(string LegalId, string Purpose)
		{
			try
			{
				bool Connected = await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect);

				if (!Connected)
					throw new TimeoutException();

				LegalIdentity Identity = await ServiceRef.XmppService.GetLegalIdentity(LegalId);
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
				});
			}
			catch (ForbiddenException)
			{
				// This happens if you try to view someone else's legal identity.
				// When this happens, try to send a petition to view it instead.
				// Normal operation. Should not be logged.

				await this.PetitionLegalIdentity(LegalId, Purpose);
			}
			catch (TimeoutException)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.AppNotConnected)],
					ServiceRef.Localizer[nameof(AppResources.PleaseTryAgain)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
					ServiceRef.Localizer[nameof(AppResources.PleaseTryAgain)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}

		/// <inheritdoc/>
		public Task PetitionLegalIdentity(string LegalId, string Purpose)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				bool Succeeded = await ServiceRef.NetworkService.TryRequest(() => ServiceRef.XmppService.PetitionIdentity(LegalId, Guid.NewGuid().ToString(), Purpose));
				if (Succeeded)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.PetitionSent)],
						ServiceRef.Localizer[nameof(AppResources.APetitionHasBeenSentToTheOwner)]);
				}
			});

			return Task.CompletedTask;
		}

		/// <summary>
		/// Tries to get a legal identity.
		/// </summary>
		/// <param name="LegalId">The id of the legal identity to show.</param>
		/// <param name="Purpose">The purpose to state if the identity can't be downloaded and needs to be petitioned instead.</param>
		/// <returns>Legal Identity, if possible to get, null otherwise.</returns>
		public async Task<LegalIdentity?> TryGetLegalIdentity(string LegalId, string Purpose)
		{
			try
			{
				LegalIdentity Identity = await ServiceRef.XmppService.GetLegalIdentity(LegalId);
				return Identity;
			}
			catch (ForbiddenException)
			{
				// The identity is protected. Normal operation, not logged. A petition is only sent when the user asks for one.
				return null;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				return null;
			}
		}

		/// <inheritdoc/>
		public Task OpenContract(string ContractId, string Purpose, Dictionary<CaseInsensitiveString, object>? ParameterValues)
		{
			return this.OpenContractAsync(ContractId, new ContractOpenOptions()
			{
				ParameterValues = ParameterValues
			});
		}

		/// <inheritdoc/>
		public Task OpenContract(Contract Contract, Dictionary<CaseInsensitiveString, object>? ParameterValues)
		{
			return this.OpenContractAsync(Contract, new ContractOpenOptions()
			{
				ParameterValues = ParameterValues
			});
		}

		/// <inheritdoc/>
		public async Task PetitionContract(string ContractId, string Purpose)
		{
			try
			{
				await this.OfferContractAccessRequestAsync(ContractId, null);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <inheritdoc/>
		public async Task<bool> OpenContractAsync(CaseInsensitiveString ContractId, ContractOpenOptions? Options)
		{
			try
			{
				if (string.IsNullOrEmpty(ContractId?.Value))
					return false;

				ContractReference? Ref = await ServiceRef.ContractStore.GetAsync(ContractId);
				Contract? Saved = null;

				if (Ref?.ContractLoaded == true)
				{
					try
					{
						Saved = await Ref.GetContract();
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}

				// Show the saved copy at once. The contract page refreshes it in the background. Templates are read again,
				// since the wizard needs the current state.
				if (Saved is not null && Saved.PartsMode != ContractParts.TemplateOnly)
				{
					await this.OpenContractPageAsync(Saved, Ref, Options);
					return true;
				}

				if (!ServiceRef.XmppService.IsOnline)
					await ServiceRef.XmppService.WaitForConnectedState(Constants.Timeouts.XmppConnect);

				ContractRefreshResult Result = await ServiceRef.ContractSyncService.RefreshAsync(ContractId, ContractOrigin.Opened, false, CancellationToken.None);

				switch (Result.Status)
				{
					case ContractRefreshStatus.Updated:
					case ContractRefreshStatus.Unchanged:
						await this.OpenContractAsync(Result.Contract!, Result.Reference ?? Ref, Options);
						return true;

					case ContractRefreshStatus.Deleted:
						if (Result.Reference?.ContractLoaded == true && await Result.Reference.GetContract() is Contract KeptCopy)
						{
							await this.OpenContractPageAsync(KeptCopy, Result.Reference, Options);
							return true;
						}

						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
							ServiceRef.Localizer[nameof(AppResources.ContractWasDeleted)],
							ServiceRef.Localizer[nameof(AppResources.Ok)]);
						return false;

					case ContractRefreshStatus.Forbidden:
						if (Saved is not null)
						{
							await this.OpenContractAsync(Saved, Ref, Options);
							return true;
						}

						await this.OfferContractAccessRequestAsync(ContractId, Ref);
						return false;

					case ContractRefreshStatus.Offline:
						if (Saved is not null)
						{
							await this.OpenContractAsync(Saved, Ref, Options);
							return true;
						}

						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.AppNotConnected)],
							ServiceRef.Localizer[nameof(AppResources.PleaseTryAgain)],
							ServiceRef.Localizer[nameof(AppResources.Ok)]);
						return false;

					default:
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
							ServiceRef.Localizer[nameof(AppResources.ContractCouldNotBeOpened)],
							ServiceRef.Localizer[nameof(AppResources.Ok)]);
						return false;
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ContractErrorMessages.GetMessage(Ex),
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return false;
			}
		}

		/// <inheritdoc/>
		public async Task OpenContractAsync(Contract Contract, ContractOpenOptions? Options)
		{
			try
			{
				ContractReference? Ref = await ServiceRef.ContractStore.GetAsync(Contract.ContractId);

				if (Contract.PartsMode == ContractParts.TemplateOnly)
				{
					// Reading the template through the sync service also keeps it in the list of templates.
					ContractRefreshResult Result = await ServiceRef.ContractSyncService.RefreshAsync(Contract.ContractId, ContractOrigin.Opened, false, CancellationToken.None);
					Contract = Result.Contract ?? Contract;
					Ref = Result.Reference ?? Ref;
				}

				await this.OpenContractAsync(Contract, Ref, Options);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod()));
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ContractErrorMessages.GetMessage(Ex),
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
		}

		/// <summary>
		/// Opens a contract where it belongs: approved templates in the new-contract wizard, and everything else on the contract page.
		/// </summary>
		private async Task OpenContractAsync(Contract Contract, ContractReference? Ref, ContractOpenOptions? Options)
		{
			if (Contract.PartsMode == ContractParts.TemplateOnly && Contract.State == ContractState.Approved)
				await this.OpenTemplateAsync(Contract, Options?.ParameterValues);
			else
				await this.OpenContractPageAsync(Contract, Ref, Options);
		}

		/// <summary>
		/// Opens an approved template in the new-contract wizard. Token and payment templates get the creation attributes
		/// of the user's trust provider as parameter values.
		/// </summary>
		private async Task OpenTemplateAsync(Contract Template, Dictionary<CaseInsensitiveString, object>? ParameterValues)
		{
			if (Template.ForMachinesNamespace == NeuroFeaturesClient.NamespaceNeuroFeatures ||
				Template.ForMachinesNamespace == Constants.ContractMachineNames.PaymentInstructionsNamespace)
			{
				CreationAttributesEventArgs CreationAttr = await ServiceRef.XmppService.GetNeuroFeatureCreationAttributes();
				ServiceRef.TagProfile.TrustProviderId = CreationAttr.TrustProviderId;
				ParameterValues ??= [];
				ParameterValues.TryAdd(new CaseInsensitiveString("TrustProvider"), CreationAttr.TrustProviderId);
				ParameterValues.TryAdd(new CaseInsensitiveString("Currency"), CreationAttr.Currency);
				ParameterValues.TryAdd(new CaseInsensitiveString("CommissionPercent"), CreationAttr.Commission);
			}

			NewContractNavigationArgs Args = new(Template, ParameterValues);

			await MainThread.InvokeOnMainThreadAsync(() =>
				ServiceRef.NavigationService.GoToAsync(nameof(NewContractPage), Args, BackMethod.CurrentPage));
		}

		/// <summary>
		/// Opens the contract page. An open invitation that the caller didn't pass is taken from the stored contract, so
		/// the page shows its banner and preselects the role.
		/// </summary>
		private async Task OpenContractPageAsync(Contract Contract, ContractReference? Ref, ContractOpenOptions? Options)
		{
			bool HasInvitation = Ref?.HasOpenInvitation == true;
			string? Role = Options?.InvitedRole ?? (HasInvitation ? Ref!.InvitedRole : null);
			string? Message = Options?.InvitationMessage ?? (HasInvitation ? Ref!.InvitationMessage : null);
			string? From = Options?.InvitedBy ?? (HasInvitation ? Ref!.InvitedBy : null);

			ViewContractNavigationArgs Args = new(Contract, Options?.IsReadOnly ?? false, Role, Message, From, null, Ref);

			await MainThread.InvokeOnMainThreadAsync(() =>
				ServiceRef.NavigationService.GoToAsync(nameof(ViewContractPage), Args, BackMethod.Pop));
		}

		/// <summary>
		/// Tells the user that a contract can't be read, and offers what can be done: declining an unreadable invitation,
		/// or asking the people who signed the contract for access.
		/// </summary>
		/// <param name="ContractId">The ID of the contract.</param>
		/// <param name="Ref">The stored contract, if any.</param>
		private async Task OfferContractAccessRequestAsync(CaseInsensitiveString ContractId, ContractReference? Ref)
		{
			if (Ref?.HasOpenInvitation == true)
			{
				string Sender = await GetPersonNameAsync(Ref.InvitedBy, null, null);

				bool Decline = await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ContractCantBeRead)],
					ServiceRef.Localizer[nameof(AppResources.InvitationUnreadable), Sender],
					ServiceRef.Localizer[nameof(AppResources.DeclineInvitation)],
					ServiceRef.Localizer[nameof(AppResources.Close)]);

				if (Decline)
					await this.DeclineInvitationAsync(ContractId);

				return;
			}

			bool Ask = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.ContractCantBeRead)],
				ServiceRef.Localizer[nameof(AppResources.ContractForbiddenAskAccess)],
				ServiceRef.Localizer[nameof(AppResources.AskForAccess)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);

			if (Ask)
				await this.RequestContractAccessAsync(ContractId);
		}

		/// <inheritdoc/>
		public async Task DeclineInvitationAsync(CaseInsensitiveString ContractId)
		{
			await ServiceRef.ContractStore.RemoveAsync(ContractId);
			await DeleteNotificationsAsync(Constants.PushChannels.Contracts, ContractId);
		}

		/// <inheritdoc/>
		public async Task<bool> RequestContractAccessAsync(CaseInsensitiveString ContractId)
		{
			ContractReference? Ref = await ServiceRef.ContractStore.GetAsync(ContractId);
			Contract? Saved = Ref?.ContractLoaded == true ? await Ref.GetContract() : null;

			if (Saved is not null && (Saved.ClientSignatures?.Length ?? 0) == 0)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.AskForAccess)],
					ServiceRef.Localizer[nameof(AppResources.ContractAccessNoSignatures)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return false;
			}

			string? Purpose = await ServiceRef.UiService.DisplayPrompt(
				ServiceRef.Localizer[nameof(AppResources.AskForAccess)],
				ServiceRef.Localizer[nameof(AppResources.ContractAccessPurposePrompt)],
				ServiceRef.Localizer[nameof(AppResources.Send)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);

			if (Purpose is null)
				return false;

			if (string.IsNullOrWhiteSpace(Purpose))
				Purpose = ServiceRef.Localizer[nameof(AppResources.ContractAccessPurposeDefault)];

			string PetitionId = Guid.NewGuid().ToString();
			this.contractPetitions[PetitionId] = ContractId;

			try
			{
				await ServiceRef.XmppService.PetitionContract(ContractId, PetitionId, Purpose.Trim());
			}
			catch (Exception Ex)
			{
				this.contractPetitions.TryRemove(PetitionId, out _);
				ServiceRef.LogService.LogException(Ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ContractErrorMessages.GetMessage(Ex),
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return false;
			}

			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.PetitionSent)],
				ServiceRef.Localizer[nameof(AppResources.ContractAccessRequestSent)],
				ServiceRef.Localizer[nameof(AppResources.Ok)]);

			return true;
		}

		/// <inheritdoc/>
		public async Task RespondToContractPetitionAsync(ContractPetitionRequest Request, bool Accept)
		{
			await ServiceRef.XmppService.SendPetitionContractResponse(Request.ContractId, Request.PetitionId, Request.RequestorFullJid, Accept);
			await MarkNotificationsConsumedAsync(Constants.PushChannels.Petitions, NotificationAction.OpenPetition, Request.PetitionId);
		}

		/// <inheritdoc/>
		public async Task<string?> RequestIdentityAsync(CaseInsensitiveString LegalId, string Purpose)
		{
			string PetitionId = Guid.NewGuid().ToString();
			this.identityPetitions[PetitionId] = LegalId;

			try
			{
				await ServiceRef.XmppService.PetitionIdentity(LegalId, PetitionId, Purpose);
				return PetitionId;
			}
			catch (Exception Ex)
			{
				this.identityPetitions.TryRemove(PetitionId, out _);
				ServiceRef.LogService.LogException(Ex);

				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ContractErrorMessages.GetMessage(Ex),
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return null;
			}
		}

		/// <inheritdoc/>
		public bool IsIdentityRequestPending(CaseInsensitiveString LegalId)
		{
			return this.identityPetitions.Values.Any(Requested => string.Equals(Requested, LegalId.Value, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// TAG Signature request scanned.
		/// </summary>
		/// <param name="Request">Request string.</param>
		public async Task TagSignature(string Request)
		{
			int i = Request.IndexOf(',');

			if (i < 0)
				throw new InvalidOperationException(ServiceRef.Localizer[nameof(AppResources.InvalidTagSignatureId)]);

			string JID = System.Web.HttpUtility.UrlDecode(Request[..i]);
			string Key = Request[(i + 1)..];

			LegalIdentity? ID = ServiceRef.TagProfile.LegalIdentity;

			if (ID is null)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)], ServiceRef.Localizer[nameof(AppResources.NotCompletedOnboardingError)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return;
			}

			if (ID.State != IdentityState.Approved)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)], ServiceRef.Localizer[nameof(AppResources.LegalIdNotApproved)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return;
			}

			string IdRef = ServiceRef.TagProfile.LegalIdentity?.Id ?? string.Empty;

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

		private async Task Contracts_SignaturePetitionResponseReceived(object? Sender, SignaturePetitionResponseEventArgs e)
		{
			try
			{
				LegalIdentity? Identity = e.RequestedIdentity;

				if (!e.Response || Identity is null)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.SignatureRequestDeclined)],
						ServiceRef.Localizer[nameof(AppResources.SignatureRequestDeclinedText)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
				else
				{
					// The app doesn't keep the content of signature requests it sends, so the signature can't be checked
					// against it here. The user is told who signed, and no unrelated page opens.
					string Signer = await GetPersonNameAsync(Identity.GetJid(), Identity, Identity.Id);

					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.SignatureRequestAccepted)],
						ServiceRef.Localizer[nameof(AppResources.SignatureRequestAcceptedText), Signer],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
