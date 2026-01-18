using System;

namespace NeuroAccessMaui.Services.Nfc.Ui
{
	/// <summary>
	/// Represents a user-facing hint derived from a scanned NFC tag.
	/// </summary>
	public sealed class NfcScanHint
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcScanHint"/> class.
		/// </summary>
		/// <param name="Severity">Hint severity.</param>
		/// <param name="Text">Hint text to display to the user.</param>
		public NfcScanHint(NfcScanHintSeverity Severity, string Text)
		{
			this.Severity = Severity;
			this.Text = Text ?? string.Empty;
		}

		/// <summary>
		/// Gets the hint severity.
		/// </summary>
		public NfcScanHintSeverity Severity { get; }

		/// <summary>
		/// Gets the hint text.
		/// </summary>
		public string Text { get; }
	}

	/// <summary>
	/// Severity levels for <see cref="NfcScanHint"/>.
	/// </summary>
	public enum NfcScanHintSeverity
	{
		/// <summary>
		/// Informational hint.
		/// </summary>
		Info = 0,

		/// <summary>
		/// Warning hint.
		/// </summary>
		Warning = 1
	}
}
