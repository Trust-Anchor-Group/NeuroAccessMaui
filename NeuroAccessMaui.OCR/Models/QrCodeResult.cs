using System.Collections.Generic;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents a single decoded QR code.
	/// </summary>
	public sealed class QrCodeResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QrCodeResult"/> class.
		/// </summary>
		/// <param name="Value">The decoded value.</param>
		/// <param name="Format">The reported code format, if available.</param>
		/// <param name="Confidence">The provider confidence score.</param>
		/// <param name="Metadata">Additional provider metadata.</param>
		public QrCodeResult(
			string Value,
			string? Format,
			float Confidence,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(Value);

			this.Value = Value;
			this.Format = Format;
			this.Confidence = Confidence;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the decoded QR value.
		/// </summary>
		public string Value
		{
			get;
		}

		/// <summary>
		/// Gets the reported code format, if available.
		/// </summary>
		public string? Format
		{
			get;
		}

		/// <summary>
		/// Gets the provider confidence score.
		/// </summary>
		public float Confidence
		{
			get;
		}

		/// <summary>
		/// Gets additional provider metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata
		{
			get;
		}
	}
}
