using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Main.QR
{
	/// <summary>
	/// Holds navigation parameters specific to views scanning a QR code.
	/// </summary>
	/// <param name="QrTitle">Title to present</param>
	/// <param name="AllowedSchemas">Schemas permitted.</param>
	public class ScanQrCodeNavigationArgs(string? QrTitle, string[] AllowedSchemas) : NavigationArgs
	{
		/// <summary>
		/// Creates an instance of the <see cref="ScanQrCodeNavigationArgs"/> class.
		/// </summary>
		public ScanQrCodeNavigationArgs() : this(string.Empty, []) { }

		/// <summary>
		/// Creates an instance of the <see cref="ScanQrCodeNavigationArgs"/> class whose scanned text is prepared before the scanner closes.
		/// </summary>
		/// <param name="QrTitle">Title to present</param>
		/// <param name="AllowedSchemas">Schemas permitted.</param>
		/// <param name="PrepareAsync">Prepares scanned text that passed the schema check, or <c>null</c> to accept it as it is.</param>
		public ScanQrCodeNavigationArgs(string? QrTitle, string[] AllowedSchemas,
			Func<string, CancellationToken, Task<PreparationStatus>>? PrepareAsync)
			: this(QrTitle, AllowedSchemas)
		{
			this.PrepareAsync = PrepareAsync;
		}

		/// <summary>
		/// The page title.
		/// </summary>
		public string? QrTitle { get; } = QrTitle;

		/// <summary>
		/// The allowed schema to display.
		/// </summary>
		public string[] AllowedSchemas { get; } = AllowedSchemas;

		/// <summary>
		/// Gets the operation that prepares scanned text before the scanner accepts it, or <c>null</c> when no preparation is needed.
		/// </summary>
		/// <remarks>
		/// The operation must not execute the scanned content's action. The scanner stays open until it returns
		/// <see cref="PreparationStatus.Ready"/>, and shows other outcomes so the user can scan again or retry.
		/// </remarks>
		public Func<string, CancellationToken, Task<PreparationStatus>>? PrepareAsync { get; }

		/// <summary>
		/// Task completion source; can be used to wait for a result.
		/// </summary>
		/// <remarks>
		/// It completes exactly once: with the accepted text after the scanner has closed, or with <c>null</c> if the scan is cancelled.
		/// Take it before navigating to the scanner, since the scanner clears it while delivering the result.
		/// </remarks>
		public TaskCompletionSource<string?>? QrCodeScanned { get; internal set; } =
			new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
	}
}
