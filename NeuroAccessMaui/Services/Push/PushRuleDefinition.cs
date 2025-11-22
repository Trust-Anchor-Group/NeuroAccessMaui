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
		/// Optional pattern script (unused today).
		/// </summary>
		public string PatternScript { get; }

		/// <summary>
		/// Script that produces the payload forwarded to Firebase/APNS.
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

			string PetitionFrom = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.PetitionFrom)]);
			string IdentityUpdated = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.IdentityUpdated)]);
			string ContractCreated = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractCreated)]);
			string ContractSigned = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractSigned)]);
			string ContractUpdated = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractUpdated)]);
			string ContractDeleted = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractDeleted)]);
			string ContractProposed = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractProposed)]);
			string BalanceUpdated = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.BalanceUpdated)]);
			string TokenAdded = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.TokenAdded)]);
			string TokenRemoved = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.TokenRemoved)]);
			string AccessRequest = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.AccessRequest)]);
			string ReadRequest = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ReadRequest)]);
			string ControlRequest = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ControlRequest)]);

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
					"{",
					"'myTitle': FriendlyName,",
					"'myBody': InnerText(GetElement(Stanza,'body')),",
					"'fromJid': FromJid,",
					"'rosterName': FriendlyName,",
					"'isObject': exists(Content) and !empty(Markdown:= InnerText(Content)) and (Left(Markdown,2)='![' or (Left(Markdown,3)='```' and Right(Markdown,3)='```')),",
					$"'action': '{NotificationAction.OpenChat}',",
					"'entityId': FromJid,",
					"'correlationId': GetAttribute(Stanza,'id'),",
					$"'channelId': '{Constants.PushChannels.Messages}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{PetitionFrom} ' + FriendlyName,",
					"'myBody': GetAttribute(E,'purpose'),",
					"'fromJid': FromJid,",
					"'rosterName': FriendlyName,",
					$"'action': '{NotificationAction.OpenProfile}',",
					"'entityId': FromJid,",
					"'correlationId': GetAttribute(E,'petitionId'),",
					$"'channelId': '{Constants.PushChannels.Petitions}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{PetitionFrom} ' + FriendlyName,",
					"'myBody': GetAttribute(E,'purpose'),",
					"'fromJid': FromJid,",
					"'rosterName': FriendlyName,",
					$"'action': '{NotificationAction.OpenProfile}',",
					"'entityId': FromJid,",
					"'correlationId': GetAttribute(E,'petitionId'),",
					$"'channelId': '{Constants.PushChannels.Petitions}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{PetitionFrom} ' + FriendlyName,",
					"'myBody': GetAttribute(E,'purpose'),",
					"'fromJid': FromJid,",
					"'rosterName': FriendlyName,",
					$"'action': '{NotificationAction.OpenProfile}',",
					"'entityId': FromJid,",
					"'correlationId': GetAttribute(E,'petitionId'),",
					$"'channelId': '{Constants.PushChannels.Petitions}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{IdentityUpdated}',",
					"'legalId': GetAttribute(E,'id'),",
					$"'action': '{NotificationAction.OpenProfile}',",
					"'entityId': GetAttribute(E,'id'),",
					$"'channelId': '{Constants.PushChannels.Identities}',",
					"'content_available': true",
					"}")));

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
					$"'myTitle': '{ContractCreated}',",
					"'contractId': GetAttribute(E,'contractId'),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
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
					$"'myTitle': '{ContractSigned}',",
					"'contractId': GetAttribute(E,'contractId'),",
					"'legalId': GetAttribute(E,'legalId'),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
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
					$"'myTitle': '{ContractUpdated}',",
					"'contractId': GetAttribute(E,'contractId'),",
					$"'action': '{NotificationAction.OpenSettings}',",
					"'entityId': GetAttribute(E,'contractId'),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
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
					$"'myTitle': '{ContractDeleted}',",
					"'contractId': GetAttribute(E,'contractId'),",
					$"'action': '{NotificationAction.OpenSettings}',",
					"'entityId': GetAttribute(E,'contractId'),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{ContractProposed}',",
					"'myBody': GetAttribute(E,'message'),",
					"'contractId': Num(GetAttribute(E,'contractId')),",
					"'role': Num(GetAttribute(E,'role')),",
					$"'action': '{NotificationAction.OpenSettings}',",
					"'entityId': GetAttribute(E,'contractId'),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{BalanceUpdated}',",
					"'amount': Num(GetAttribute(E,'amount')),",
					"'currency': GetAttribute(E,'currency'),",
					"'timestamp': DateTime(GetAttribute(E,'timestamp')),",
					$"'action': '{NotificationAction.OpenSettings}',",
					"'entityId': GetAttribute(E,'currency'),",
					$"'channelId': '{Constants.PushChannels.EDaler}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{TokenAdded}',",
					"'myBody': GetAttribute(E2,'friendlyName'),",
					"'value': Num(GetAttribute(E2,'value')),",
					"'currency': GetAttribute(E2,'currency'),",
					$"'action': '{NotificationAction.OpenSettings}',",
					"'entityId': GetAttribute(E2,'tokenId'),",
					$"'channelId': '{Constants.PushChannels.Tokens}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{TokenRemoved}',",
					"'myBody': GetAttribute(E2,'friendlyName'),",
					"'value': Num(GetAttribute(E2,'value')),",
					"'currency': GetAttribute(E2,'currency'),",
					$"'action': '{NotificationAction.OpenSettings}',",
					"'entityId': GetAttribute(E2,'tokenId'),",
					$"'channelId': '{Constants.PushChannels.Tokens}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{AccessRequest}',",
					"'myBody': RosterName(ToJid,RemoteJid),",
					"'remoteJid': RemoteJid,",
					"'jid': GetAttribute(E,'jid'),",
					"'key': GetAttribute(E,'key'),",
					"'q': 'isFriend',",
					$"'action': '{NotificationAction.OpenPresenceRequest}',",
					"'entityId': RemoteJid,",
					$"'channelId': '{Constants.PushChannels.Provisioning}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{ReadRequest}',",
					"'myBody': RosterName(ToJid,RemoteJid),",
					"'remoteJid': RemoteJid,",
					"'jid': GetAttribute(E,'jid'),",
					"'key': GetAttribute(E,'key'),",
					"'q': 'canRead',",
					$"'action': '{NotificationAction.OpenPresenceRequest}',",
					"'entityId': RemoteJid,",
					$"'channelId': '{Constants.PushChannels.Provisioning}',",
					"'content_available': true",
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
					"{",
					$"'myTitle': '{ControlRequest}',",
					"'myBody': RosterName(ToJid,RemoteJid),",
					"'remoteJid': RemoteJid,",
					"'jid': GetAttribute(E,'jid'),",
					"'key': GetAttribute(E,'key'),",
					"'q': 'canControl',",
					$"'action': '{NotificationAction.OpenPresenceRequest}',",
					"'entityId': RemoteJid,",
					$"'channelId': '{Constants.PushChannels.Provisioning}',",
					"'content_available': true",
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
