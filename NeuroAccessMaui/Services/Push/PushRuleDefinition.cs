using System.Security.Cryptography;
using System.Text;
using EDaler;
using NeuroAccessMaui.Resources.Languages;
using NeuroFeatures;
using NeuroAccessMaui.Services.Notification;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Push;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Content;

namespace NeuroAccessMaui.Services.Push
{
	/// <summary>
	/// Describes a push notification rule to provision in the XMPP broker.
	/// </summary>
	public sealed class PushRuleDefinition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PushRuleDefinition"/> class.
		/// </summary>
		public PushRuleDefinition(MessageType messageType, string localName, string @namespace, string channel, string messageVariable, string patternScript, string contentScript)
		{
			this.MessageType = messageType;
			this.LocalName = localName;
			this.Namespace = @namespace;
			this.Channel = channel;
			this.MessageVariable = messageVariable;
			this.PatternScript = patternScript;
			this.ContentScript = contentScript;
		}

		/// <summary>
		/// XMPP message type the rule applies to.
		/// </summary>
		public MessageType MessageType { get; }

		/// <summary>
		/// Local name of the stanza element.
		/// </summary>
		public string LocalName { get; }

		/// <summary>
		/// Namespace of the stanza element.
		/// </summary>
		public string Namespace { get; }

		/// <summary>
		/// Push channel identifier.
		/// </summary>
		public string Channel { get; }

		/// <summary>
		/// Variable name for the stanza passed into the rule.
		/// </summary>
		public string MessageVariable { get; }

		/// <summary>
		/// Optional pattern script to filter matching stanzas.
		/// </summary>
		public string PatternScript { get; }

		/// <summary>
		/// Script that produces the transport-agnostic push payload forwarded to the broker.
		/// </summary>
		public string ContentScript { get; }
	}

	/// <summary>
	/// Provides the set of push rules to provision.
	/// </summary>
	public static class PushRuleDefinitions
	{
		/// <summary>
		/// Gets all rule Definitions.
		/// </summary>
		public static IReadOnlyList<PushRuleDefinition> All { get; } = Build();

		/// <summary>
		/// Gets a hash representing the current rule set.
		/// </summary>
		public static string RuleSetHash { get; } = ComputeHash(All);

		private static IReadOnlyList<PushRuleDefinition> Build()
		{
			List<PushRuleDefinition> Definitions = new();

			string NotificationChatTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationChatTitle)]);
			string NotificationChatBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationChatBody)]);
			string NotificationPetitionIdentityTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationPetitionIdentityTitle)]);
			string NotificationPetitionIdentityBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationPetitionIdentityBody)]);
			string NotificationPetitionContractTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationPetitionContractTitle)]);
			string NotificationPetitionContractBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationPetitionContractBody)]);
			string NotificationPetitionSignatureTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationPetitionSignatureTitle)]);
			string NotificationPetitionSignatureBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationPetitionSignatureBody)]);
			string NotificationIdentityApprovedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationIdentityApprovedTitle)]);
			string NotificationIdentityApprovedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationIdentityApprovedBody)]);
			string NotificationIdentityObsoletedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationIdentityObsoletedTitle)]);
			string NotificationIdentityObsoletedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationIdentityObsoletedBody)]);
			string NotificationIdentityRejectedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationIdentityRejectedTitle)]);
			string NotificationIdentityRejectedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationIdentityRejectedBody)]);
			string NotificationIdentityCompromisedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationIdentityCompromisedTitle)]);
			string NotificationIdentityCompromisedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationIdentityCompromisedBody)]);
			string NotificationContractCreatedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractCreatedTitle)]);
			string NotificationContractCreatedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractCreatedBody)]);
			string NotificationContractSignedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractSignedTitle)]);
			string NotificationContractSignedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractSignedBody)]);
			string NotificationContractUpdatedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractUpdatedTitle)]);
			string NotificationContractUpdatedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractUpdatedBody)]);
			string NotificationContractDeletedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractDeletedTitle)]);
			string NotificationContractDeletedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractDeletedBody)]);
			string NotificationContractProposalTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractProposalTitle)]);
			string NotificationContractProposalBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationContractProposalBody)]);
			string NotificationBalanceUpdatedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationBalanceUpdatedTitle)]);
			string NotificationBalanceUpdatedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationBalanceUpdatedBody)]);
			string NotificationTokenAddedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationTokenAddedTitle)]);
			string NotificationTokenAddedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationTokenAddedBody)]);
			string NotificationTokenRemovedTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationTokenRemovedTitle)]);
			string NotificationTokenRemovedBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationTokenRemovedBody)]);
			string NotificationPresenceAccessTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationPresenceAccessTitle)]);
			string NotificationPresenceAccessBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationPresenceAccessBody)]);
			string NotificationReadAccessTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationReadAccessTitle)]);
			string NotificationReadAccessBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationReadAccessBody)]);
			string NotificationControlAccessTitle = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationControlAccessTitle)]);
			string NotificationControlAccessBody = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.NotificationControlAccessBody)]);

			Definitions.Add(new PushRuleDefinition(
				MessageType.Chat,
				string.Empty,
				string.Empty,
				Constants.PushChannels.Messages,
				"Stanza",
				string.Empty,
				Script(
					"FromJid:=GetAttribute(Stanza,'from');",
					"ToJid:=GetAttribute(Stanza,'to');",
					"FriendlyName:=RosterName(ToJid,FromJid);",
					"Content:=GetElement(Stanza,'content');",
					$"TitleText:={NotificationChatTitle};",
					$"BodyText:={NotificationChatBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': Replace(TitleText,'{0}',FriendlyName), 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenChat}', 'entityId': FromJid, 'correlationId': FromJid }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Messages}' }},",
					"'delivery': { 'priority': 'High', 'silent': false },",
					"'context': { 'fromJid': FromJid, 'toJid': ToJid, 'rosterName': FriendlyName },",
					"'data': { 'isObject': exists(Content) and !empty(Markdown:= InnerText(Content)) and (Left(Markdown,2)='![' or (Left(Markdown,3)='```' and Right(Markdown,3)='```')) }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"petitionIdentityMsg",
				ContractsClient.NamespaceLegalIdentitiesCurrent,
				Constants.PushChannels.Petitions,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'petitionIdentityMsg');",
					"ToJid:=GetAttribute(Stanza,'to');",
					"FromJid:=GetAttribute(E,'from');",
					"FriendlyName:=RosterName(ToJid,FromJid);",
					$"TitleText:={NotificationPetitionIdentityTitle};",
					$"BodyText:={NotificationPetitionIdentityBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': Replace(TitleText,'{0}',FriendlyName), 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenProfile}', 'entityId': FromJid, 'correlationId': GetAttribute(E,'petitionId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Petitions}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'fromJid': FromJid, 'toJid': ToJid, 'rosterName': FriendlyName },",
					"'data': { 'petitionId': GetAttribute(E,'petitionId') }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"petitionContractMsg",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Petitions,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'petitionContractMsg');",
					"ToJid:=GetAttribute(Stanza,'to');",
					"FromJid:=GetAttribute(E,'from');",
					"FriendlyName:=RosterName(ToJid,FromJid);",
					$"TitleText:={NotificationPetitionContractTitle};",
					$"BodyText:={NotificationPetitionContractBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': Replace(TitleText,'{0}',FriendlyName), 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenProfile}', 'entityId': FromJid, 'correlationId': GetAttribute(E,'petitionId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Petitions}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'fromJid': FromJid, 'toJid': ToJid, 'rosterName': FriendlyName },",
					"'data': { 'petitionId': GetAttribute(E,'petitionId') }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"petitionSignatureMsg",
				ContractsClient.NamespaceLegalIdentitiesCurrent,
				Constants.PushChannels.Petitions,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'petitionSignatureMsg');",
					"ToJid:=GetAttribute(Stanza,'to');",
					"FromJid:=GetAttribute(E,'from');",
					"FriendlyName:=RosterName(ToJid,FromJid);",
					$"TitleText:={NotificationPetitionSignatureTitle};",
					$"BodyText:={NotificationPetitionSignatureBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': Replace(TitleText,'{0}',FriendlyName), 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenProfile}', 'entityId': FromJid, 'correlationId': GetAttribute(E,'petitionId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Petitions}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'fromJid': FromJid, 'toJid': ToJid, 'rosterName': FriendlyName },",
					"'data': { 'petitionId': GetAttribute(E,'petitionId') }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"identity",
				ContractsClient.NamespaceLegalIdentitiesCurrent,
				Constants.PushChannels.Identities,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'identity');",
					"Status:=GetElement(E,'status');",
					"State:=GetAttribute(Status,'state');",
					"if (State='Approved') then",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					$"'visual': {{ 'title': {NotificationIdentityApprovedTitle}, 'body': {NotificationIdentityApprovedBody} }},",
					$"'action': {{ 'type': '{NotificationAction.OpenIdentity}', navigationTarget: '{NotificationAction.OpenIdentity}', 'entityId': GetAttribute(E,'id'), 'correlationId': GetAttribute(E,'id') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Identities}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'provider': GetAttribute(Status,'provider') },",
					"'data': { 'legalId': GetAttribute(E,'id'), 'state': State, 'validFrom': GetAttribute(Status,'from'), 'validTo': GetAttribute(Status,'to') }",
					"}",
					"else if (State='Obsoleted') then",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					$"'visual': {{ 'title': {NotificationIdentityObsoletedTitle}, 'body': {NotificationIdentityObsoletedBody} }},",
					$"'action': {{ 'type': '{NotificationAction.OpenIdentity}', navigationTarget: '{NotificationAction.OpenIdentity}', 'entityId': GetAttribute(E,'id'), 'correlationId': GetAttribute(E,'id') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Identities}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'provider': GetAttribute(Status,'provider') },",
					"'data': { 'legalId': GetAttribute(E,'id'), 'state': State, 'validFrom': GetAttribute(Status,'from'), 'validTo': GetAttribute(Status,'to') }",
					"}",
					"else if (State='Rejected') then",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					$"'visual': {{ 'title': {NotificationIdentityRejectedTitle}, 'body': {NotificationIdentityRejectedBody} }},",
					$"'action': {{ 'type': '{NotificationAction.OpenIdentity}', navigationTarget: '{NotificationAction.OpenIdentity}', 'entityId': GetAttribute(E,'id'), 'correlationId': GetAttribute(E,'id') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Identities}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'provider': GetAttribute(Status,'provider') },",
					"'data': { 'legalId': GetAttribute(E,'id'), 'state': State, 'validFrom': GetAttribute(Status,'from'), 'validTo': GetAttribute(Status,'to') }",
					"}",
					"else if (State='Compromised') then",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					$"'visual': {{ 'title': {NotificationIdentityCompromisedTitle}, 'body': {NotificationIdentityCompromisedBody} }},",
					$"'action': {{ 'type': '{NotificationAction.OpenIdentity}', navigationTarget: '{NotificationAction.OpenIdentity}', 'entityId': GetAttribute(E,'id'), 'correlationId': GetAttribute(E,'id') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Identities}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'provider': GetAttribute(Status,'provider') },",
					"'data': { 'legalId': GetAttribute(E,'id'), 'state': State, 'validFrom': GetAttribute(Status,'from'), 'validTo': GetAttribute(Status,'to') }",
					"}",
					"else",
					"null")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractCreated",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractCreated');",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					$"'visual': {{ 'title': {NotificationContractCreatedTitle}, 'body': {NotificationContractCreatedBody} }},",
					$"'action': {{ 'type': '{NotificationAction.OpenContract}', 'entityId': GetAttribute(E,'contractId'), 'correlationId': GetAttribute(E,'contractId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Contracts}' }},",
					"'delivery': { 'priority': 'High' },",
					"'data': { 'contractId': GetAttribute(E,'contractId') }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractSigned",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractSigned');",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					$"'visual': {{ 'title': {NotificationContractSignedTitle}, 'body': {NotificationContractSignedBody} }},",
					$"'action': {{ 'type': '{NotificationAction.OpenContract}', 'entityId': GetAttribute(E,'contractId'), 'correlationId': GetAttribute(E,'contractId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Contracts}' }},",
					"'delivery': { 'priority': 'High' },",
					"'data': { 'contractId': GetAttribute(E,'contractId'), 'legalId': GetAttribute(E,'legalId') }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractUpdated",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractUpdated');",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					$"'visual': {{ 'title': {NotificationContractUpdatedTitle}, 'body': {NotificationContractUpdatedBody} }},",
					$"'action': {{ 'type': '{NotificationAction.OpenContract}', 'entityId': GetAttribute(E,'contractId'), 'correlationId': GetAttribute(E,'contractId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Contracts}' }},",
					"'delivery': { 'priority': 'High' },",
					"'data': { 'contractId': GetAttribute(E,'contractId') }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractDeleted",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractDeleted');",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					$"'visual': {{ 'title': {NotificationContractDeletedTitle}, 'body': {NotificationContractDeletedBody} }},",
					$"'action': {{ 'type': '{NotificationAction.OpenContract}', 'entityId': GetAttribute(E,'contractId'), 'correlationId': GetAttribute(E,'contractId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Contracts}' }},",
					"'delivery': { 'priority': 'High' },",
					"'data': { 'contractId': GetAttribute(E,'contractId') }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractProposal",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractProposal');",
					$"TitleText:={NotificationContractProposalTitle};",
					$"BodyText:={NotificationContractProposalBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': TitleText, 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenContract}', 'entityId': GetAttribute(E,'contractId'), 'correlationId': GetAttribute(E,'contractId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Contracts}' }},",
					"'delivery': { 'priority': 'High' },",
					"'data': { 'contractId': Num(GetAttribute(E,'contractId')), 'role': Num(GetAttribute(E,'role')) }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"balance",
				EDalerClient.NamespaceEDaler,
				Constants.PushChannels.EDaler,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'balance');",
					$"TitleText:={NotificationBalanceUpdatedTitle};",
					$"BodyText:={NotificationBalanceUpdatedBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': TitleText, 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenBalance}', 'entityId': GetAttribute(E,'currency'), 'correlationId': GetAttribute(E,'currency') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.EDaler}' }},",
					"'delivery': { 'priority': 'High' },",
					"'data': { 'amount': Num(GetAttribute(E,'amount')), 'currency': GetAttribute(E,'currency'), 'timestamp': DateTime(GetAttribute(E,'timestamp')) }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"tokenAdded",
				NeuroFeaturesClient.NamespaceNeuroFeatures,
				Constants.PushChannels.Tokens,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'tokenAdded');",
					"E2:=GetElement(E,'token');",
					$"TitleText:={NotificationTokenAddedTitle};",
					$"BodyText:={NotificationTokenAddedBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': TitleText, 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenToken}', 'entityId': GetAttribute(E2,'tokenId'), 'correlationId': GetAttribute(E2,'tokenId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Tokens}' }},",
					"'delivery': { 'priority': 'High' },",
					"'data': { 'tokenId': GetAttribute(E2,'tokenId'), 'value': Num(GetAttribute(E2,'value')), 'currency': GetAttribute(E2,'currency') }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"tokenRemoved",
				NeuroFeaturesClient.NamespaceNeuroFeatures,
				Constants.PushChannels.Tokens,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'tokenRemoved');",
					"E2:=GetElement(E,'token');",
					$"TitleText:={NotificationTokenRemovedTitle};",
					$"BodyText:={NotificationTokenRemovedBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': TitleText, 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenToken}', 'entityId': GetAttribute(E2,'tokenId'), 'correlationId': GetAttribute(E2,'tokenId') }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Tokens}' }},",
					"'delivery': { 'priority': 'High' },",
					"'data': { 'tokenId': GetAttribute(E2,'tokenId'), 'value': Num(GetAttribute(E2,'value')), 'currency': GetAttribute(E2,'currency') }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"isFriend",
				ProvisioningClient.NamespaceProvisioningOwnerCurrent,
				Constants.PushChannels.Provisioning,
				"Stanza",
				string.Empty,
				Script(
					"ToJid:=GetAttribute(Stanza,'to');",
					"E:=GetElement(Stanza,'isFriend');",
					"RemoteJid:=GetAttribute(E,'remoteJid');",
					"FriendlyName:=RosterName(ToJid,RemoteJid);",
					$"TitleText:={NotificationPresenceAccessTitle};",
					$"BodyText:={NotificationPresenceAccessBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': Replace(TitleText,'{0}',FriendlyName), 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenPresenceRequest}', 'entityId': RemoteJid }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Provisioning}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'toJid': ToJid, 'remoteJid': RemoteJid },",
					"'data': { 'remoteJid': RemoteJid, 'jid': GetAttribute(E,'jid'), 'key': GetAttribute(E,'key'), 'q': 'isFriend' }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"canRead",
				ProvisioningClient.NamespaceProvisioningOwnerCurrent,
				Constants.PushChannels.Provisioning,
				"Stanza",
				string.Empty,
				Script(
					"ToJid:=GetAttribute(Stanza,'to');",
					"E:=GetElement(Stanza,'canRead');",
					"RemoteJid:=GetAttribute(E,'remoteJid');",
					"FriendlyName:=RosterName(ToJid,RemoteJid);",
					$"TitleText:={NotificationReadAccessTitle};",
					$"BodyText:={NotificationReadAccessBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': Replace(TitleText,'{0}',FriendlyName), 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenPresenceRequest}', 'entityId': RemoteJid }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Provisioning}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'toJid': ToJid, 'remoteJid': RemoteJid },",
					"'data': { 'remoteJid': RemoteJid, 'jid': GetAttribute(E,'jid'), 'key': GetAttribute(E,'key'), 'q': 'canRead' }",
					"}")));

			Definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"canControl",
				ProvisioningClient.NamespaceProvisioningOwnerCurrent,
				Constants.PushChannels.Provisioning,
				"Stanza",
				string.Empty,
				Script(
					"ToJid:=GetAttribute(Stanza,'to');",
					"E:=GetElement(Stanza,'canControl');",
					"RemoteJid:=GetAttribute(E,'remoteJid');",
					"FriendlyName:=RosterName(ToJid,RemoteJid);",
					$"TitleText:={NotificationControlAccessTitle};",
					$"BodyText:={NotificationControlAccessBody};",
					"{",
					"'payloadKind': 'PushNotificationPayload',",
					"'payloadVersion': 1,",
					"'visual': { 'title': Replace(TitleText,'{0}',FriendlyName), 'body': BodyText },",
					$"'action': {{ 'type': '{NotificationAction.OpenPresenceRequest}', 'entityId': RemoteJid }},",
					$"'channel': {{ 'channelId': '{Constants.PushChannels.Provisioning}' }},",
					"'delivery': { 'priority': 'High' },",
					"'context': { 'toJid': ToJid, 'remoteJid': RemoteJid },",
					"'data': { 'remoteJid': RemoteJid, 'jid': GetAttribute(E,'jid'), 'key': GetAttribute(E,'key'), 'q': 'canControl' }",
					"}")));

			return Definitions;
		}

		private static string ComputeHash(IEnumerable<PushRuleDefinition> rules)
		{
			StringBuilder builder = new();

			foreach (PushRuleDefinition rule in rules)
			{
				builder.Append(rule.MessageType.ToString());
				builder.Append('|');
				builder.Append(rule.LocalName);
				builder.Append('|');
				builder.Append(rule.Namespace);
				builder.Append('|');
				builder.Append(rule.Channel);
				builder.Append('|');
				builder.Append(rule.MessageVariable);
				builder.Append('|');
				builder.Append(rule.PatternScript);
				builder.Append('|');
				builder.Append(rule.ContentScript);
				builder.AppendLine();
			}

			byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
			byte[] hash = SHA256.HashData(bytes);
			return Convert.ToHexString(hash);
		}

		private static string Script(params string[] lines) => string.Join('\n', lines);
	}
}
