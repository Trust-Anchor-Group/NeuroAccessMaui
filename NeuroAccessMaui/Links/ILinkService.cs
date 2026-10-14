using NeuroAccessMaui.Models;
using NeuroAccessMaui.Services;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Routes links to their handlers, prepares them before they are used, and opens them.
	/// </summary>
	public interface ILinkService
	{
		/// <summary>
		/// Gets the application schemes the general scanner accepts for the current profile.
		/// </summary>
		/// <returns>The allowed URI schemes.</returns>
		string[] GetAllowedSchemes();

		/// <summary>
		/// Prepares the default action of a link without executing it.
		/// </summary>
		/// <param name="Text">The URI text.</param>
		/// <param name="AllowedSchemes">The schemes permitted by the caller, or <c>null</c> or empty to permit any scheme that has a handler.</param>
		/// <param name="CancellationToken">Cancellation for this attempt.</param>
		/// <returns>The prepared link, or the reason it could not be prepared.</returns>
		Task<PreparationResult<PreparedLink>> PrepareAsync(string Text, IReadOnlyCollection<string>? AllowedSchemes, CancellationToken CancellationToken);

		/// <summary>
		/// Reads a legal identity link, resolving the identity when it is accessible, without opening it.
		/// </summary>
		/// <param name="Text">The URI text.</param>
		/// <param name="CancellationToken">Cancellation for this attempt.</param>
		/// <returns>The identity reference, or the reason it could not be read.</returns>
		Task<PreparationResult<IdentityReference>> ReadIdentityAsync(string Text, CancellationToken CancellationToken);

		/// <summary>
		/// Reads the identifier of a legal identity link without looking the identity up, so it works offline.
		/// </summary>
		/// <param name="Text">The URI text.</param>
		/// <returns>An identity reference without the identity, or the reason the link could not be read.</returns>
		PreparationResult<IdentityReference> ParseIdentity(string Text);

		/// <summary>
		/// Reads a smart contract link, resolving the contract when it is accessible, without opening it.
		/// </summary>
		/// <param name="Text">The URI text.</param>
		/// <param name="CancellationToken">Cancellation for this attempt.</param>
		/// <returns>The contract data, or the reason it could not be read.</returns>
		Task<PreparationResult<ContractLinkData>> ReadContractAsync(string Text, CancellationToken CancellationToken);

		/// <summary>
		/// Retrieves, decrypts, and validates an onboarding invitation without applying it.
		/// </summary>
		/// <param name="Text">The invitation URI text.</param>
		/// <param name="CancellationToken">Cancellation for this attempt.</param>
		/// <returns>The invitation, or the reason it could not be read.</returns>
		Task<PreparationResult<Invitation>> ReadInvitationAsync(string Text, CancellationToken CancellationToken);

		/// <summary>
		/// Prepares and opens a link received by the application, presenting errors at this entry point.
		/// </summary>
		/// <param name="Text">The URI text.</param>
		/// <param name="ShowErrorIfUnable">Whether to show a localized error when the link cannot be used.</param>
		/// <param name="AllowedSchemes">The application schemes permitted, or <c>null</c> for <see cref="GetAllowedSchemes"/>.
		/// Onboarding and application callback links, and schemes that are not application schemes, are always permitted.</param>
		/// <param name="IsTrustedSource">Whether the link comes from a trusted source, such as the user's own server. Links from other
		/// sources ask the user before they open outside the application.</param>
		/// <returns>Whether the link was opened without an error.</returns>
		Task<bool> OpenAsync(string Text, bool ShowErrorIfUnable, IReadOnlyCollection<string>? AllowedSchemes = null,
			bool IsTrustedSource = false);

		/// <summary>
		/// Executes a prepared link and presents a safe error if its action fails.
		/// </summary>
		/// <param name="Link">The prepared link.</param>
		/// <param name="ShowErrorIfUnable">Whether to show a localized error if the action fails.</param>
		/// <returns>Whether the action completed without an error.</returns>
		Task<bool> ExecuteAsync(PreparedLink Link, bool ShowErrorIfUnable = true);
	}
}
