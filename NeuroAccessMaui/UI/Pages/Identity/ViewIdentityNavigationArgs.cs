using NeuroAccessMaui.Services.Navigation;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Pages.Identity;

/// <summary>
/// Holds navigation parameters specific to views displaying a legal identity.
/// </summary>
/// <param name="Identity">The identity.</param>
/// <param name="RequestorFullJid">Full JID of requestor.</param>
/// <param name="SignatoryIdentityId">Legal identity of signatory.</param>
/// <param name="PetitionId">ID of petition.</param>
/// <param name="Purpose">Purpose message to display.</param>
/// <param name="ContentToSign">Content to sign.</param>
public class ViewIdentityNavigationArgs(LegalIdentity? Identity,
		string? RequestorFullJid = null, string? SignatoryIdentityId = null,
		string? PetitionId = null, string? Purpose = null, byte[]? ContentToSign = null) : NavigationArgs
{
	/// <summary>
	/// Creates a default instance.
	/// </summary>
	public ViewIdentityNavigationArgs() : this(null) { }

	/// <summary>
	/// The identity to display.
	/// </summary>
	public LegalIdentity? Identity { get; } = Identity;

	/// <summary>
	/// Legal Identity of requesting entity.
	/// </summary>
	public LegalIdentity? RequestorIdentity { get; } = RequestorFullJid is null ? null : Identity;

	/// <summary>
	/// Full JID of requestor.
	/// </summary>
	public string? RequestorFullJid { get; } = RequestorFullJid;

	/// <summary>
	/// Legal identity of petitioned signatory.
	/// </summary>
	public string? SignatoryIdentityId { get; } = SignatoryIdentityId;

	/// <summary>
	/// Petition ID
	/// </summary>
	public string? PetitionId { get; } = PetitionId;

	/// <summary>
	/// Purpose
	/// </summary>
	public string? Purpose { get; } = Purpose;

	/// <summary>
	/// Content to sign.
	/// </summary>
	public byte[]? ContentToSign { get; } = ContentToSign;

}
