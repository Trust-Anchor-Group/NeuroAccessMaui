using NeuroAccessMaui.Models;

namespace NeuroAccessMaui.Services.UI.QR
{
	/// <summary>
	/// Scans codes, preparing their content before the scanner closes, and generates QR code images.
	/// </summary>
	public interface IQrService
	{
		/// <summary>
		/// Scans a code and returns its text once it passes the scanner's scheme check.
		/// </summary>
		/// <param name="Title">The localization key of the scanner title.</param>
		/// <param name="AllowedSchemes">The permitted schemes, or an empty array to accept any text.</param>
		/// <returns>The scanned text, or <c>null</c> if the scan was cancelled or the camera may not be used.</returns>
		Task<string?> ScanAsync(string? Title, string[] AllowedSchemes);

		/// <summary>
		/// Scans a code and keeps the scanner open until the caller has prepared its content.
		/// </summary>
		/// <remarks>
		/// Invalid or unsupported codes, and temporary failures, are shown in the scanner, which stays open so the user can retry.
		/// </remarks>
		/// <typeparam name="T">The type of prepared value.</typeparam>
		/// <param name="Title">The localization key of the scanner title.</param>
		/// <param name="AllowedSchemes">The permitted schemes, or an empty array to accept any text.</param>
		/// <param name="PrepareAsync">Validates scanned text and prepares the value, without executing any action.</param>
		/// <returns>The prepared value, or <c>null</c> if the scan was cancelled or the camera may not be used.</returns>
		Task<T?> ScanAsync<T>(string? Title, string[] AllowedSchemes,
			Func<string, CancellationToken, Task<PreparationResult<T>>> PrepareAsync) where T : class;

		/// <summary>
		/// Scans a legal identity code and, optionally, resolves the identity before the scanner closes.
		/// </summary>
		/// <param name="Title">The localization key of the scanner title.</param>
		/// <param name="LookUpIdentity">Whether to look the identity up, which needs a connection. When <c>false</c>, only the
		/// identifier is read, so scanning works offline and <see cref="IdentityReference.Identity"/> is <c>null</c>.</param>
		/// <returns>The identity reference, or <c>null</c> if the scan was cancelled.</returns>
		Task<IdentityReference?> ScanIdentityAsync(string Title, bool LookUpIdentity = true);

		/// <summary>
		/// Scans a smart contract code and resolves the contract before the scanner closes.
		/// </summary>
		/// <param name="Title">The localization key of the scanner title.</param>
		/// <returns>The contract data, or <c>null</c> if the scan was cancelled.</returns>
		Task<ContractLinkData?> ScanContractAsync(string Title);

		/// <summary>
		/// Scans an onboarding invitation code and reads and validates the invitation before the scanner closes.
		/// </summary>
		/// <param name="Title">The localization key of the scanner title.</param>
		/// <returns>The invitation, or <c>null</c> if the scan was cancelled.</returns>
		Task<Invitation?> ScanInvitationAsync(string Title);

		/// <summary>
		/// Scans a link, prepares it before the scanner closes, and then runs its normal action.
		/// </summary>
		/// <param name="Title">The localization key of the scanner title.</param>
		/// <param name="AllowedSchemes">The permitted schemes, or <c>null</c> for the schemes enabled for the current profile.</param>
		/// <returns>A task representing scanning and the link's action.</returns>
		Task ScanAndOpenAsync(string Title, string[]? AllowedSchemes = null);

		/// <summary>
		/// Generates a PNG image of a QR code.
		/// </summary>
		/// <param name="Text">The text to encode.</param>
		/// <param name="Width">The image width in pixels.</param>
		/// <param name="Height">The image height in pixels.</param>
		/// <returns>The encoded image.</returns>
		byte[] GeneratePng(string Text, int Width, int Height);

		/// <summary>
		/// Generates a JPEG image of a QR code.
		/// </summary>
		/// <param name="Text">The text to encode.</param>
		/// <param name="Width">The image width in pixels.</param>
		/// <param name="Height">The image height in pixels.</param>
		/// <returns>The encoded image.</returns>
		byte[] GenerateJpg(string Text, int Width, int Height);
	}
}
