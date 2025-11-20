using System.Threading;
using System.Threading.Tasks;
using EDaler;
using NeuroAccessMaui.Resources.Languages;
using NeuroFeatures;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;
using Waher.Content;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Push notification service
	/// </summary>
	[Singleton]
	public class PushNotificationService : LoadableService, IPushNotificationService
	{
		private readonly Dictionary<PushMessagingService, string> tokens = [];
		private DateTime lastTokenCheck = DateTime.MinValue;
		private bool isInitialized;
		private readonly object tokenVerificationSync = new();
		private Task? pendingTokenVerificationTask;

		/// <summary>
		/// Push notification service
		/// </summary>
		public PushNotificationService()
		{
			//	CrossFirebaseCloudMessaging.Current.NotificationReceived += OnNotificationReceived;
			//	CrossFirebaseCloudMessaging.Current.NotificationTapped += OnNotificationTapped;
		}

		/// <summary>
		/// Loads the specified service.
		/// </summary>
		/// <param name="isResuming">Set to <c>true</c> when app is resuming.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override async Task Load(bool isResuming, CancellationToken cancellationToken)
		{
			if (!this.isInitialized)
			{
				this.isInitialized = true;
				App.AppActivated += this.App_AppActivated;
				try
				{
					CrossFirebaseCloudMessaging.Current.TokenChanged += this.FirebaseCloudMessaging_TokenChanged;
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogException(ex);
				}

				this.ScheduleTokenVerification();
			}

			await base.Load(isResuming, cancellationToken);
		}

		/// <summary>
		/// New token received from push notification back-end.
		/// </summary>
		/// <param name="TokenInformation">Token information</param>
		public async Task NewToken(TokenInformation TokenInformation)
		{
			if (!string.IsNullOrEmpty(TokenInformation.Token))
			{
				lock (this.tokens)
				{
					this.tokens[TokenInformation.Service] = TokenInformation.Token;
				}

				await ServiceRef.XmppService.NewPushNotificationToken(TokenInformation);
				await this.OnNewToken.Raise(this, new TokenEventArgs(TokenInformation.Service, TokenInformation.Token, TokenInformation.ClientType));
			}
		}

		private void App_AppActivated(object? sender, EventArgs e)
		{
			this.ScheduleTokenVerification();
		}

		private async void FirebaseCloudMessaging_TokenChanged(object? sender, FCMTokenChangedEventArgs e)
		{
			try
			{
				string? token = e?.Token;
				if (string.IsNullOrEmpty(token))
					return;

				TokenInformation info = new()
				{
					Token = token,
					Service = PushMessagingService.Firebase,
					ClientType = ResolveClientType()
				};

				await this.CheckPushNotificationToken(info);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private void ScheduleTokenVerification()
		{
			Task VerificationTask;

			lock (this.tokenVerificationSync)
			{
				if (this.pendingTokenVerificationTask is not null && !this.pendingTokenVerificationTask.IsCompleted)
					return;

				VerificationTask = MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await this.CheckPushNotificationToken(null);
				});

				this.pendingTokenVerificationTask = VerificationTask;
			}

			VerificationTask.ContinueWith(t =>
			{
				Exception? Exception = t.Exception?.GetBaseException() ?? t.Exception;
				if (Exception is not null)
					ServiceRef.LogService.LogException(Exception);
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		private static ClientType ResolveClientType()
		{
			if (DeviceInfo.Platform == DevicePlatform.Android)
				return ClientType.Android;

			if (DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
				return ClientType.iOS;

			return ClientType.Other;
		}

		/// <summary>
		/// Event raised when a new token is made available.
		/// </summary>
		public event EventHandlerAsync<TokenEventArgs>? OnNewToken;

		/// <summary>
		/// Tries to get a token from a push notification service.
		/// </summary>
		/// <param name="Source">Source of token</param>
		/// <param name="Token">Token, if found.</param>
		/// <returns>If a token was found for the corresponding source.</returns>
		public bool TryGetToken(PushMessagingService Source, out string? Token)
		{
			lock (this.tokens)
			{
				return this.tokens.TryGetValue(Source, out Token);
			}
		}

		private static async Task<bool> ForceTokenReport(TokenInformation TokenInformation)
		{
			string OldToken = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationToken, string.Empty);
			DateTime ReportDate = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationReportDate, DateTime.MinValue);

			return (DateTime.UtcNow.Subtract(ReportDate).TotalDays > 7) || (TokenInformation.Token != OldToken);
		}

		/// <summary>
		/// Checks if the Push Notification Token is current and registered properly.
		/// </summary>
		/// <param name="TokenInformation">Non null if we got it from the OnNewToken</param>
		public async Task CheckPushNotificationToken(TokenInformation? TokenInformation)
		{
			try
			{
				DateTime Now = DateTime.Now;

				if (ServiceRef.XmppService.IsOnline &&
					ServiceRef.XmppService.SupportsPushNotification &&
					Now.Subtract(this.lastTokenCheck).TotalHours >= 1)
				{
					this.lastTokenCheck = Now;

					if (TokenInformation is null)
					{
						TokenInformation = await ServiceRef.PlatformSpecific.GetPushNotificationToken();
						if (string.IsNullOrEmpty(TokenInformation.Token))
							return;
					}

					bool ForceReport = await ForceTokenReport(TokenInformation);

					string Version = AppInfo.VersionString + "." + AppInfo.BuildString;
					string PrevVersion = await RuntimeSettings.GetAsync(Constants.Settings.PushNotificationConfigurationVersion, string.Empty);
					bool IsVersionChanged = Version != PrevVersion;

					if (IsVersionChanged || ForceReport)
					{
						string? Token = TokenInformation.Token;

						if (!string.IsNullOrEmpty(Token))
						{
							PushMessagingService Service = TokenInformation.Service;
							ClientType ClientType = TokenInformation.ClientType;
							await ServiceRef.XmppService.ReportNewPushNotificationToken(Token, Service, ClientType);

							await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationToken, TokenInformation.Token);
							await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationReportDate, DateTime.UtcNow);
						}
					}

					if (IsVersionChanged)
					{
						// it will force the rules update if somehing goes wrong.
						await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationConfigurationVersion, string.Empty);
						await ServiceRef.XmppService.ClearPushNotificationRules();

						#region Message Rules

						#region Messages

						string MessagesContent = $"""
    FromJid:=GetAttribute(Stanza,'from');
    ToJid:=GetAttribute(Stanza,'to');
    FriendlyName:=RosterName(ToJid,FromJid);
    Content:=GetElement(Stanza,'content');
    {'{'}'myTitle': FriendlyName,
    'myBody': InnerText(GetElement(Stanza,'body')),
    'fromJid': FromJid,
    'rosterName': FriendlyName,
    'isObject': exists(Content) and !empty(Markdown:= InnerText(Content)) and (Left(Markdown,2)='![' or (Left(Markdown,3)='```' and Right(Markdown,3)='```')),
    'channelId': '{Constants.PushChannels.Messages}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Chat, string.Empty, string.Empty,
							 Constants.PushChannels.Messages, "Stanza", string.Empty, MessagesContent);

						#endregion

						#region Petitions

						// Identity Petition
						string PetitionIdentityContent = $"""
    E:=GetElement(Stanza,'petitionIdentityMsg');
    ToJid:=GetAttribute(Stanza,'to');
    FromJid:=GetAttribute(E,'from');
    FriendlyName:=RosterName(ToJid,FromJid);
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.PetitionFrom)])} ' + FriendlyName,
    'myBody': GetAttribute(E,'purpose'),
    'fromJid': FromJid,
    'rosterName': FriendlyName,
    'channelId': '{Constants.PushChannels.Petitions}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "petitionIdentityMsg", ContractsClient.NamespaceLegalIdentitiesCurrent,
							 Constants.PushChannels.Petitions, "Stanza", string.Empty, PetitionIdentityContent);

						// Contract Petition
						string PetitionContractContent = $"""
    E:=GetElement(Stanza,'petitionContractMsg');
    ToJid:=GetAttribute(Stanza,'to');
    FromJid:=GetAttribute(E,'from');
    FriendlyName:=RosterName(ToJid,FromJid);
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.PetitionFrom)])} ' + FriendlyName,
    'myBody': GetAttribute(E,'purpose'),
    'fromJid': FromJid,
    'rosterName': FriendlyName,
    'channelId': '{Constants.PushChannels.Petitions}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "petitionContractMsg", ContractsClient.NamespaceSmartContractsCurrent,
							 Constants.PushChannels.Petitions, "Stanza", string.Empty, PetitionContractContent);

						// Signature Petition
						string PetitionSignatureContent = $"""
    E:=GetElement(Stanza,'petitionSignatureMsg');
    ToJid:=GetAttribute(Stanza,'to');
    FromJid:=GetAttribute(E,'from');
    FriendlyName:=RosterName(ToJid,FromJid);
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.PetitionFrom)])} ' + FriendlyName,
    'myBody': GetAttribute(E,'purpose'),
    'fromJid': FromJid,
    'rosterName': FriendlyName,
    'channelId': '{Constants.PushChannels.Petitions}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "petitionSignatureMsg", ContractsClient.NamespaceLegalIdentitiesCurrent,
							 Constants.PushChannels.Petitions, "Stanza", string.Empty, PetitionSignatureContent);

						#endregion

						#region Identities

						string IdentityContent = $"""
    E:=GetElement(Stanza,'identity');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.IdentityUpdated)])}',
    'legalId': GetAttribute(E,'id'),
    'channelId': '{Constants.PushChannels.Identities}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "identity", ContractsClient.NamespaceLegalIdentitiesCurrent,
							 Constants.PushChannels.Identities, "Stanza", string.Empty, IdentityContent);

						#endregion

						#region Contracts

						// Contract Created
						string ContractCreatedContent = $"""
    E:=GetElement(Stanza,'contractCreated');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractCreated)])}',
    'contractId': GetAttribute(E,'contractId'),
    'channelId': '{Constants.PushChannels.Contracts}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "contractCreated", ContractsClient.NamespaceSmartContractsCurrent,
							 Constants.PushChannels.Contracts, "Stanza", string.Empty, ContractCreatedContent);

						// Contract Signed
						string ContractSignedContent = $"""
    E:=GetElement(Stanza,'contractSigned');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractSigned)])}',
    'contractId': GetAttribute(E,'contractId'),
    'legalId': GetAttribute(E,'legalId'),
    'channelId': '{Constants.PushChannels.Contracts}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "contractSigned", ContractsClient.NamespaceSmartContractsCurrent,
							 Constants.PushChannels.Contracts, "Stanza", string.Empty, ContractSignedContent);

						// Contract Updated
						string ContractUpdatedContent = $"""
    E:=GetElement(Stanza,'contractUpdated');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractUpdated)])}',
    'contractId': GetAttribute(E,'contractId'),
    'channelId': '{Constants.PushChannels.Contracts}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "contractUpdated", ContractsClient.NamespaceSmartContractsCurrent,
							 Constants.PushChannels.Contracts, "Stanza", string.Empty, ContractUpdatedContent);

						// Contract Deleted
						string ContractDeletedContent = $"""
    E:=GetElement(Stanza,'contractDeleted');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractDeleted)])}',
    'contractId': GetAttribute(E,'contractId'),
    'channelId': '{Constants.PushChannels.Contracts}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "contractDeleted", ContractsClient.NamespaceSmartContractsCurrent,
							 Constants.PushChannels.Contracts, "Stanza", string.Empty, ContractDeletedContent);

						// Contract Proposal
						string ContractProposalContent = $"""
    E:=GetElement(Stanza,'contractProposal');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractProposed)])}',
    'myBody': GetAttribute(E,'message'),
    'contractId': Num(GetAttribute(E,'contractId')),
    'role': Num(GetAttribute(E,'role')),
    'channelId': '{Constants.PushChannels.Contracts}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "contractProposal", ContractsClient.NamespaceSmartContractsCurrent,
							 Constants.PushChannels.Contracts, "Stanza", string.Empty, ContractProposalContent);

						#endregion

						#region eDaler

						string EdalerContent = $"""
    E:=GetElement(Stanza,'balance');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.BalanceUpdated)])}',
    'amount': Num(GetAttribute(E,'amount')),
    'currency': GetAttribute(E,'currency'),
    'timestamp': DateTime(GetAttribute(E,'timestamp')),
    'channelId': '{Constants.PushChannels.EDaler}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "balance", EDalerClient.NamespaceEDaler,
							 Constants.PushChannels.EDaler, "Stanza", string.Empty, EdalerContent);

						#endregion

						#region Neuro-Features

						// Token Added
						string TokenAddedContent = $"""
    E:=GetElement(Stanza,'tokenAdded');
    E2:=GetElement(E,'token');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.TokenAdded)])}',
    'myBody': GetAttribute(E2,'friendlyName'),
    'value': Num(GetAttribute(E2,'value')),
    'currency': GetAttribute(E2,'currency'),
    'channelId': '{Constants.PushChannels.Tokens}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "tokenAdded", NeuroFeaturesClient.NamespaceNeuroFeatures,
							 Constants.PushChannels.Tokens, "Stanza", string.Empty, TokenAddedContent);

						// Token Removed
						string TokenRemovedContent = $"""
    E:=GetElement(Stanza,'tokenRemoved');
    E2:=GetElement(E,'token');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.TokenRemoved)])}',
    'myBody': GetAttribute(E2,'friendlyName'),
    'value': Num(GetAttribute(E2,'value')),
    'currency': GetAttribute(E2,'currency'),
    'channelId': '{Constants.PushChannels.Tokens}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "tokenRemoved", NeuroFeaturesClient.NamespaceNeuroFeatures,
							 Constants.PushChannels.Tokens, "Stanza", string.Empty, TokenRemovedContent);

						#endregion

						#region Provisioning

						// Friendship Requests
						string FriendRequestContent = $"""
    ToJid:=GetAttribute(Stanza,'to');
    E:=GetElement(Stanza,'isFriend');
    RemoteJid:=GetAttribute(E,'remoteJid');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.AccessRequest)])}',
    'myBody': RosterName(ToJid,RemoteJid),
    'remoteJid': RemoteJid,
    'jid': GetAttribute(E,'jid'),
    'key': GetAttribute(E,'key'),
    'q': 'isFriend',
    'channelId': '{Constants.PushChannels.Provisioning}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "isFriend", ProvisioningClient.NamespaceProvisioningOwnerCurrent,
							 Constants.PushChannels.Provisioning, "Stanza", string.Empty, FriendRequestContent);

						// Readout Requests
						string ReadRequestContent = $"""
    ToJid:=GetAttribute(Stanza,'to');
    E:=GetElement(Stanza,'canRead');
    RemoteJid:=GetAttribute(E,'remoteJid');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ReadRequest)])}',
    'myBody': RosterName(ToJid,RemoteJid),
    'remoteJid': RemoteJid,
    'jid': GetAttribute(E,'jid'),
    'key': GetAttribute(E,'key'),
    'q': 'canRead',
    'channelId': '{Constants.PushChannels.Provisioning}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "canRead", ProvisioningClient.NamespaceProvisioningOwnerCurrent,
							 Constants.PushChannels.Provisioning, "Stanza", string.Empty, ReadRequestContent);

						// Control Requests
						string ControlRequestContent = $"""
    ToJid:=GetAttribute(Stanza,'to');
    E:=GetElement(Stanza,'canControl');
    RemoteJid:=GetAttribute(E,'remoteJid');
    {'{'}'myTitle': '{JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ControlRequest)])}',
    'myBody': RosterName(ToJid,RemoteJid),
    'remoteJid': RemoteJid,
    'jid': GetAttribute(E,'jid'),
    'key': GetAttribute(E,'key'),
    'q': 'canControl',
    'channelId': '{Constants.PushChannels.Provisioning}',
    'content_available': true{'}'}
    """;

						await ServiceRef.XmppService.AddPushNotificationRule(
							 MessageType.Normal, "canControl", ProvisioningClient.NamespaceProvisioningOwnerCurrent,
							 Constants.PushChannels.Provisioning, "Stanza", string.Empty, ControlRequestContent);

						#endregion

						#endregion
						await RuntimeSettings.SetAsync(Constants.Settings.PushNotificationConfigurationVersion, Version);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
