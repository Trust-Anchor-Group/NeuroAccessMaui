using System.Collections.Generic;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents the raw text output returned by a provider.
	/// </summary>
	public sealed class OcrTextRecognitionResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OcrTextRecognitionResult"/> class.
		/// </summary>
		/// <param name="RawText">The raw text returned by the provider.</param>
		/// <param name="Lines">The provider text split into lines.</param>
		/// <param name="Confidence">The provider confidence score.</param>
		/// <param name="Metadata">Additional provider metadata.</param>
		public OcrTextRecognitionResult(
			string RawText,
			IReadOnlyList<string> Lines,
			float Confidence,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(RawText);
			ArgumentNullException.ThrowIfNull(Lines);

			this.RawText = RawText;
			this.Lines = Lines;
			this.Confidence = Confidence;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the raw text returned by the provider.
		/// </summary>
		public string RawText
		{
			get;
		}

		/// <summary>
		/// Gets the provider text split into lines.
		/// </summary>
		public IReadOnlyList<string> Lines
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
