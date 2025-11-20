using EDaler;
using NeuroAccessMaui.Resources.Languages;
using NeuroFeatures;
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
		/// Gets all rule definitions.
		/// </summary>
		public static IReadOnlyList<PushRuleDefinition> All { get; } = Build();

		private static IReadOnlyList<PushRuleDefinition> Build()
		{
			List<PushRuleDefinition> definitions = new();

			string petitionFrom = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.PetitionFrom)]);
			string identityUpdated = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.IdentityUpdated)]);
			string contractCreated = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractCreated)]);
			string contractSigned = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractSigned)]);
			string contractUpdated = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractUpdated)]);
			string contractDeleted = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractDeleted)]);
			string contractProposed = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ContractProposed)]);
			string balanceUpdated = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.BalanceUpdated)]);
			string tokenAdded = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.TokenAdded)]);
			string tokenRemoved = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.TokenRemoved)]);
			string accessRequest = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.AccessRequest)]);
			string readRequest = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ReadRequest)]);
			string controlRequest = JSON.Encode(ServiceRef.Localizer[nameof(AppResources.ControlRequest)]);

			definitions.Add(new PushRuleDefinition(
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
					$"'channelId': '{Constants.PushChannels.Messages}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
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
					$"'myTitle': '{petitionFrom} ' + FriendlyName,",
					"'myBody': GetAttribute(E,'purpose'),",
					"'fromJid': FromJid,",
					"'rosterName': FriendlyName,",
					$"'channelId': '{Constants.PushChannels.Petitions}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
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
					$"'myTitle': '{petitionFrom} ' + FriendlyName,",
					"'myBody': GetAttribute(E,'purpose'),",
					"'fromJid': FromJid,",
					"'rosterName': FriendlyName,",
					$"'channelId': '{Constants.PushChannels.Petitions}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
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
					$"'myTitle': '{petitionFrom} ' + FriendlyName,",
					"'myBody': GetAttribute(E,'purpose'),",
					"'fromJid': FromJid,",
					"'rosterName': FriendlyName,",
					$"'channelId': '{Constants.PushChannels.Petitions}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"identity",
				ContractsClient.NamespaceLegalIdentitiesCurrent,
				Constants.PushChannels.Identities,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'identity');",
					"{",
					$"'myTitle': '{identityUpdated}',",
					"'legalId': GetAttribute(E,'id'),",
					$"'channelId': '{Constants.PushChannels.Identities}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractCreated",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractCreated');",
					"{",
					$"'myTitle': '{contractCreated}',",
					"'contractId': GetAttribute(E,'contractId'),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractSigned",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractSigned');",
					"{",
					$"'myTitle': '{contractSigned}',",
					"'contractId': GetAttribute(E,'contractId'),",
					"'legalId': GetAttribute(E,'legalId'),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractUpdated",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractUpdated');",
					"{",
					$"'myTitle': '{contractUpdated}',",
					"'contractId': GetAttribute(E,'contractId'),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractDeleted",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractDeleted');",
					"{",
					$"'myTitle': '{contractDeleted}',",
					"'contractId': GetAttribute(E,'contractId'),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"contractProposal",
				ContractsClient.NamespaceSmartContractsCurrent,
				Constants.PushChannels.Contracts,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'contractProposal');",
					"{",
					$"'myTitle': '{contractProposed}',",
					"'myBody': GetAttribute(E,'message'),",
					"'contractId': Num(GetAttribute(E,'contractId')),",
					"'role': Num(GetAttribute(E,'role')),",
					$"'channelId': '{Constants.PushChannels.Contracts}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
				MessageType.Normal,
				"balance",
				EDalerClient.NamespaceEDaler,
				Constants.PushChannels.EDaler,
				"Stanza",
				string.Empty,
				Script(
					"E:=GetElement(Stanza,'balance');",
					"{",
					$"'myTitle': '{balanceUpdated}',",
					"'amount': Num(GetAttribute(E,'amount')),",
					"'currency': GetAttribute(E,'currency'),",
					"'timestamp': DateTime(GetAttribute(E,'timestamp')),",
					$"'channelId': '{Constants.PushChannels.EDaler}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
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
					$"'myTitle': '{tokenAdded}',",
					"'myBody': GetAttribute(E2,'friendlyName'),",
					"'value': Num(GetAttribute(E2,'value')),",
					"'currency': GetAttribute(E2,'currency'),",
					$"'channelId': '{Constants.PushChannels.Tokens}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
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
					$"'myTitle': '{tokenRemoved}',",
					"'myBody': GetAttribute(E2,'friendlyName'),",
					"'value': Num(GetAttribute(E2,'value')),",
					"'currency': GetAttribute(E2,'currency'),",
					$"'channelId': '{Constants.PushChannels.Tokens}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
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
					$"'myTitle': '{accessRequest}',",
					"'myBody': RosterName(ToJid,RemoteJid),",
					"'remoteJid': RemoteJid,",
					"'jid': GetAttribute(E,'jid'),",
					"'key': GetAttribute(E,'key'),",
					"'q': 'isFriend',",
					$"'channelId': '{Constants.PushChannels.Provisioning}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
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
					$"'myTitle': '{readRequest}',",
					"'myBody': RosterName(ToJid,RemoteJid),",
					"'remoteJid': RemoteJid,",
					"'jid': GetAttribute(E,'jid'),",
					"'key': GetAttribute(E,'key'),",
					"'q': 'canRead',",
					$"'channelId': '{Constants.PushChannels.Provisioning}',",
					"'content_available': true",
					"}")));

			definitions.Add(new PushRuleDefinition(
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
					$"'myTitle': '{controlRequest}',",
					"'myBody': RosterName(ToJid,RemoteJid),",
					"'remoteJid': RemoteJid,",
					"'jid': GetAttribute(E,'jid'),",
					"'key': GetAttribute(E,'key'),",
					"'q': 'canControl',",
					$"'channelId': '{Constants.PushChannels.Provisioning}',",
					"'content_available': true",
					"}")));

			return definitions;
		}

		private static string Script(params string[] lines) => string.Join('\n', lines);
	}
}
