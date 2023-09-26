using System.Reflection;
using System.Xml;
using NeuroAccessMaui.Pages.Registration;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Waher.Networking.XMPP.ServiceDiscovery;
using Waher.Persistence;
using Waher.Runtime.Inventory;
using Waher.Runtime.Temporary;

namespace NeuroAccessMaui.Services.Xmpp;

/// <summary>
/// Represents an abstraction of a connection to an XMPP Server.
/// </summary>
[DefaultImplementation(typeof(XmppService))]
public interface IXmppService : ILoadableService
{
	#region Lifecycle

	/// <summary>
	/// Can be used to <c>await</c> the server's connection state, i.e. skipping all intermediate states but <see cref="XmppState.Connected"/>.
	/// </summary>
	/// <param name="timeout">Maximum timeout before giving up.</param>
	/// <returns>If connected</returns>
	Task<bool> WaitForConnectedState(TimeSpan timeout);

	/// <summary>
	/// Perform a shutdown in critical situations. Attempts to shut down XMPP connection as fast as possible.
	/// </summary>
	Task UnloadFast();

	/// <summary>
	/// An event that triggers whenever the connection state to the XMPP server changes.
	/// </summary>
	event StateChangedEventHandler ConnectionStateChanged;

	#endregion

	#region State

	/// <summary>
	/// Determines whether the connection to the XMPP server is live or not.
	/// </summary>
	bool IsOnline { get; }

	/// <summary>
	/// The current state of the connection to the XMPP server.
	/// </summary>
	XmppState State { get; }

	/// <summary>
	/// The Bare Jid of the current connection, or <c>null</c>.
	/// </summary>
	string BareJid { get; }

	/// <summary>
	/// The latest generic xmpp error, if any.
	/// </summary>
	string LatestError { get; }

	/// <summary>
	/// The latest generic xmpp connection error, if any.
	/// </summary>
	string LatestConnectionError { get; }

	#endregion

	#region Connections

	/// <summary>
	/// To be used during the very first phase of the startup/registration procedure. Tries to connect (and then disconnect) to the specified server.
	/// </summary>
	/// <param name="domain">The server's domain name.</param>
	/// <param name="isIpAddress">If the domain is provided as an IP address.</param>
	/// <param name="hostName">The server's host name.</param>
	/// <param name="portNumber">The xmpp port.</param>
	/// <param name="languageCode">Language code to use for communication.</param>
	/// <param name="appAssembly">The current app's main assembly.</param>
	/// <param name="connectedFunc">A callback to use if and when connected.</param>
	/// <returns>If connected. If not, any error message.</returns>
	Task<(bool succeeded, string errorMessage)> TryConnect(string domain, bool isIpAddress, string hostName, int portNumber,
			string languageCode, Assembly appAssembly, Func<XmppClient, Task> connectedFunc);

	/// <summary>
	/// To be used during the second phase of the startup/registration procedure. Tries to connect (and then disconnect) to the specified server, while also creating an account.
	/// </summary>
	/// <param name="domain">The server's domain name.</param>
	/// <param name="isIpAddress">If the domain is provided as an IP address.</param>
	/// <param name="hostName">The server's host name.</param>
	/// <param name="portNumber">The xmpp port.</param>
	/// <param name="userName">The user name of the account to create.</param>
	/// <param name="password">The password to use.</param>
	/// <param name="languageCode">Language code to use for communication.</param>
	/// <param name="ApiKey">API Key used when creating account.</param>
	/// <param name="ApiSecret">API Secret used when creating account.</param>
	/// <param name="appAssembly">The current app's main assembly.</param>
	/// <param name="connectedFunc">A callback to use if and when connected.</param>
	/// <returns>If connected. If not, any error message.</returns>
	Task<(bool succeeded, string errorMessage)> TryConnectAndCreateAccount(string domain, bool isIpAddress, string hostName,
		int portNumber, string userName, string password, string languageCode, string ApiKey, string ApiSecret,
		Assembly appAssembly, Func<XmppClient, Task> connectedFunc);

	/// <summary>
	/// To be used during the second phase of the startup/registration procedure. Tries to connect (and then disconnect) to the specified server, while also connecting to an existing account.
	/// </summary>
	/// <param name="domain">The server's domain name.</param>
	/// <param name="isIpAddress">If the domain is provided as an IP address.</param>
	/// <param name="hostName">The server's host name.</param>
	/// <param name="portNumber">The xmpp port.</param>
	/// <param name="userName">The user name of the account to create.</param>
	/// <param name="password">The password to use.</param>
	/// <param name="passwordMethod">The password hash method to use. Empty string signifies an unhashed password.</param>
	/// <param name="languageCode">Language code to use for communication.</param>
	/// <param name="appAssembly">The current app's main assembly.</param>
	/// <param name="connectedFunc">A callback to use if and when connected.</param>
	/// <returns>If connected. If not, any error message.</returns>
	Task<(bool succeeded, string errorMessage)> TryConnectAndConnectToAccount(string domain, bool isIpAddress, string hostName,
		int portNumber, string userName, string password, string passwordMethod, string languageCode, Assembly appAssembly,
		Func<XmppClient, Task> connectedFunc);

	#endregion

	#region Password

	/// <summary>
	/// Changes the password of the account.
	/// </summary>
	/// <param name="NewPassword">New password</param>
	/// <returns>If change was successful.</returns>
	Task<bool> ChangePassword(string NewPassword);

	#endregion

	#region Components & Services

	/// <summary>
	/// Performs a Service Discovery on a remote entity.
	/// </summary>
	/// <param name="FullJid">Full JID of entity.</param>
	/// <returns>Service Discovery response.</returns>
	Task<ServiceDiscoveryEventArgs> SendServiceDiscoveryRequest(string FullJid);

	/// <summary>
	/// Run this method to discover services for any given XMPP server.
	/// </summary>
	/// <param name="Client">The client to use. Can be <c>null</c>, in which case the default is used.</param>
	/// <returns>If TAG services were found.</returns>
	Task<bool> DiscoverServices(XmppClient Client = null);

	#endregion

	#region Transfer

	/// <summary>
	/// Registers a Transfer ID Code
	/// </summary>
	/// <param name="Code">Transfer Code</param>
	Task AddTransferCode(string Code);

	#endregion

	#region IQ Stanzas (Information Query)

	/// <summary>
	/// Performs an asynchronous IQ Set request/response operation.
	/// </summary>
	/// <param name="To">Destination address</param>
	/// <param name="Xml">XML to embed into the request.</param>
	/// <returns>Response XML element.</returns>
	/// <exception cref="TimeoutException">If a timeout occurred.</exception>
	/// <exception cref="XmppException">If an IQ error is returned.</exception>
	Task<XmlElement> IqSetAsync(string To, string Xml);

	#endregion

	#region Tokens

	/// <summary>
	/// Gets a token for use with APIs that are either distributed or use different
	/// protocols, when the client needs to authenticate itself using the current
	/// XMPP connection.
	/// </summary>
	/// <param name="Seconds">Number of seconds for which the token should be valid.</param>
	/// <returns>Token, if able to get a token, or null otherwise.</returns>
	Task<string> GetApiToken(int Seconds);

	/// <summary>
	/// Performs an HTTP POST to a protected API on the server, over the current XMPP connection,
	/// authenticating the client using the credentials already provided over XMPP.
	/// </summary>
	/// <param name="LocalResource">Local Resource on the server to POST to.</param>
	/// <param name="Data">Data to post. This will be encoded using encoders in the type inventory.</param>
	/// <param name="Headers">Headers to provide in the POST.</param>
	/// <returns>Decoded response from the resource.</returns>
	/// <exception cref="Exception">Any communication error will be handle by raising the corresponding exception.</exception>
	Task<object> PostToProtectedApi(string LocalResource, object Data, params KeyValuePair<string, string>[] Headers);

	#endregion

	#region HTTP File Upload

	/// <summary>
	/// Returns <c>true</c> if file upload is supported, <c>false</c> otherwise.
	/// </summary>
	bool FileUploadIsSupported { get; }

	/// <summary>
	/// Uploads a file to the upload component.
	/// </summary>
	/// <param name="FileName">Name of file.</param>
	/// <param name="ContentType">Internet content type.</param>
	/// <param name="ContentSize">Size of content.</param>
	Task<HttpFileUploadEventArgs> RequestUploadSlotAsync(string FileName, string ContentType, long ContentSize);

	#endregion

	#region Legal Identities

	/// <summary>
	/// Gets important attributes for a successful ID Application.
	/// </summary>
	/// <returns>ID Application attributes.</returns>
	Task<IdApplicationAttributesEventArgs> GetIdApplicationAttributes();

	/// <summary>
	/// Adds a legal identity.
	/// </summary>
	/// <param name="Model">The model holding all the values needed.</param>
	/// <param name="Attachments">The physical attachments to upload.</param>
	/// <returns>Legal Identity</returns>
	Task<LegalIdentity> AddLegalIdentity(RegisterIdentityModel Model, params LegalIdentityAttachment[] Attachments);

	/// <summary>
	/// Returns a list of legal identities.
	/// </summary>
	/// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
	/// <returns>Legal Identities</returns>
	Task<LegalIdentity[]> GetLegalIdentities(XmppClient client = null);

	/// <summary>
	/// Gets a specific legal identity.
	/// </summary>
	/// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
	/// <returns>Legal identity object</returns>
	Task<LegalIdentity> GetLegalIdentity(CaseInsensitiveString legalIdentityId);

	/// <summary>
	/// Checks if a legal identity is in the contacts list.
	/// </summary>
	/// <param name="legalIdentityId">The id of the legal identity to retrieve.</param>
	/// <returns>If the legal identity is in the contacts list.</returns>
	Task<bool> IsContact(CaseInsensitiveString legalIdentityId);

	/// <summary>
	/// Checks if the client has access to the private keys of the specified legal identity.
	/// </summary>
	/// <param name="legalIdentityId">The id of the legal identity.</param>
	/// <param name="client">The Xmpp client instance. Can be null, in that case the default one is used.</param>
	/// <returns>If private keys are available.</returns>
	Task<bool> HasPrivateKey(CaseInsensitiveString legalIdentityId, XmppClient client = null);

	/// <summary>
	/// Marks the legal identity as obsolete.
	/// </summary>
	/// <param name="legalIdentityId">The id to mark as obsolete.</param>
	/// <returns>Legal Identity</returns>
	Task<LegalIdentity> ObsoleteLegalIdentity(CaseInsensitiveString legalIdentityId);

	/// <summary>
	/// Marks the legal identity as compromised.
	/// </summary>
	/// <param name="legalIdentityId">The legal id to mark as compromised.</param>
	/// <returns>Legal Identity</returns>
	Task<LegalIdentity> CompromiseLegalIdentity(CaseInsensitiveString legalIdentityId);

	/// <summary>
	/// Petitions a legal identity.
	/// </summary>
	/// <param name="LegalId">The id of the legal identity.</param>
	/// <param name="PetitionId">The petition id.</param>
	/// <param name="Purpose">The purpose of the petitioning.</param>
	Task PetitionIdentity(CaseInsensitiveString LegalId, string PetitionId, string Purpose);

	/// <summary>
	/// Sends a response to a petitioning identity request.
	/// </summary>
	/// <param name="LegalId">The id of the legal identity.</param>
	/// <param name="PetitionId">The petition id.</param>
	/// <param name="RequestorFullJid">The full Jid of the requestor.</param>
	/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
	Task SendPetitionIdentityResponse(CaseInsensitiveString LegalId, string PetitionId, string RequestorFullJid, bool Response);

	/// <summary>
	/// An event that fires when a legal identity changes.
	/// </summary>
	event LegalIdentityEventHandler LegalIdentityChanged;

	/// <summary>
	/// An event that fires when a petition for an identity is received.
	/// </summary>
	event LegalIdentityPetitionEventHandler PetitionForIdentityReceived;

	/// <summary>
	/// An event that fires when a petitioned identity response is received.
	/// </summary>
	event LegalIdentityPetitionResponseEventHandler PetitionedIdentityResponseReceived;

	/// <summary>
	/// Exports Keys to XML.
	/// </summary>
	/// <param name="Output">XML output.</param>
	Task ExportSigningKeys(XmlWriter Output);

	/// <summary>
	/// Imports keys
	/// </summary>
	/// <param name="Xml">XML Definition of keys.</param>
	/// <returns>If keys could be loaded into the client.</returns>
	Task<bool> ImportSigningKeys(XmlElement Xml);

	/// <summary>
	/// Validates a legal identity.
	/// </summary>
	/// <param name="Identity">Legal Identity</param>
	/// <returns>The validity of the identity.</returns>
	Task<IdentityStatus> ValidateIdentity(LegalIdentity Identity);

	#endregion

	#region Attachments

	/// <summary>
	/// Gets an attachment for a contract.
	/// </summary>
	/// <param name="Url">The url of the attachment.</param>
	/// <param name="Timeout">Max timeout allowed when retrieving an attachment.</param>
	/// <param name="SignWith">How the request is signed. For identity attachments, especially for attachments to an identity being created, <see cref="SignWith.CurrentKeys"/> should be used. For requesting attachments relating to a contract, <see cref="SignWith.LatestApprovedId"/> should be used.</param>
	/// <returns>Content-Type, and attachment file.</returns>
	Task<KeyValuePair<string, TemporaryFile>> GetAttachment(string Url, SignWith SignWith, TimeSpan Timeout);

	#endregion

	#region Peer Review

	/// <summary>
	/// Sends a petition to a third-party to review a legal identity.
	/// </summary>
	/// <param name="LegalId">The legal id to petition.</param>
	/// <param name="Identity">The legal id to peer review.</param>
	/// <param name="PetitionId">The petition id.</param>
	/// <param name="Purpose">The purpose.</param>
	Task PetitionPeerReviewId(CaseInsensitiveString LegalId, LegalIdentity Identity, string PetitionId, string Purpose);

	/// <summary>
	/// Adds an attachment for the peer review.
	/// </summary>
	/// <param name="Identity">The identity to which the attachment should be added.</param>
	/// <param name="ReviewerLegalIdentity">The identity of the reviewer.</param>
	/// <param name="PeerSignature">The raw signature data.</param>
	/// <returns>Legal Identity</returns>
	Task<LegalIdentity> AddPeerReviewIdAttachment(LegalIdentity Identity, LegalIdentity ReviewerLegalIdentity, byte[] PeerSignature);

	/// <summary>
	/// An event that fires when a petition for peer review is received.
	/// </summary>
	event SignaturePetitionEventHandler PetitionForPeerReviewIdReceived;

	/// <summary>
	/// An event that fires when a petitioned peer review response is received.
	/// </summary>
	event SignaturePetitionResponseEventHandler PetitionedPeerReviewIdResponseReceived;

	/// <summary>
	/// Gets available service providers for buying eDaler.
	/// </summary>
	/// <returns>Available service providers for peer review of identity applications.</returns>
	Task<ServiceProviderWithLegalId[]> GetServiceProvidersForPeerReviewAsync();

	/// <summary>
	/// Selects a peer-review service as default, for the account, when sending a peer-review request to the
	/// Legal Identity of the Trust Provider hosting the account.
	/// </summary>
	/// <param name="ServiceId">Service ID</param>
	/// <param name="ServiceProvider">Service Provider</param>
	Task SelectPeerReviewService(string ServiceId, string ServiceProvider);

	#endregion

	#region Signatures

	/// <summary>
	/// Signs binary data with the corresponding private key.
	/// </summary>
	/// <param name="data">The data to sign.</param>
	/// <param name="signWith">What keys that can be used to sign the data.</param>
	/// <returns>Signature</returns>
	Task<byte[]> Sign(byte[] data, SignWith signWith);

	/// <summary>Validates a signature of binary data.</summary>
	/// <param name="legalIdentity">Legal identity used to create the signature.</param>
	/// <param name="data">Binary data to sign-</param>
	/// <param name="signature">Digital signature of data</param>
	/// <returns>
	/// true = Signature is valid.
	/// false = Signature is invalid.
	/// null = Client key algorithm is unknown, and veracity of signature could not be established.
	/// </returns>
	bool? ValidateSignature(LegalIdentity legalIdentity, byte[] data, byte[] signature);

	/// <summary>
	/// Sends a response to a petitioning signature request.
	/// </summary>
	/// <param name="LegalId">Legal Identity petitioned.</param>
	/// <param name="Content">Content to be signed.</param>
	/// <param name="Signature">Digital signature of content, made by the legal identity.</param>
	/// <param name="PetitionId">A petition identifier. This identifier will follow the petition, and can be used
	/// to identify the petition request.</param>
	/// <param name="RequestorFullJid">Full JID of requestor.</param>
	/// <param name="Response">If the petition is accepted (true) or rejected (false).</param>
	Task SendPetitionSignatureResponse(CaseInsensitiveString LegalId, byte[] Content, byte[] Signature, string PetitionId, string RequestorFullJid, bool Response);

	/// <summary>
	/// An event that fires when a petition for a signature is received.
	/// </summary>
	event SignaturePetitionEventHandler PetitionForSignatureReceived;

	/// <summary>
	/// Event raised when a response to a signature petition has been received.
	/// </summary>
	event SignaturePetitionResponseEventHandler SignaturePetitionResponseReceived;

	#endregion

	#region Private XML

	/// <summary>
	/// Saves Private XML to the server. Private XML are separated by
	/// Local Name and Namespace of the root element. Only one document
	/// per fully qualified name. When saving private XML, the XML overwrites
	/// any existing XML having the same local name and namespace.
	/// </summary>
	/// <param name="Xml">XML to save.</param>
	Task SavePrivateXml(string Xml);

	/// <summary>
	/// Saves Private XML to the server. Private XML are separated by
	/// Local Name and Namespace of the root element. Only one document
	/// per fully qualified name. When saving private XML, the XML overwrites
	/// any existing XML having the same local name and namespace.
	/// </summary>
	/// <param name="Xml">XML to save.</param>
	Task SavePrivateXml(XmlElement Xml);

	/// <summary>
	/// Loads private XML previously stored, given the local name and
	/// namespace of the XML.
	/// </summary>
	/// <param name="LocalName">Local Name</param>
	/// <param name="Namespace">Namespace</param>
	Task<XmlElement> LoadPrivateXml(string LocalName, string Namespace);

	/// <summary>
	/// Deletes private XML previously saved to the account.
	/// </summary>
	/// <param name="LocalName">Local Name</param>
	/// <param name="Namespace">Namespace</param>
	Task DeletePrivateXml(string LocalName, string Namespace);

	#endregion
}
