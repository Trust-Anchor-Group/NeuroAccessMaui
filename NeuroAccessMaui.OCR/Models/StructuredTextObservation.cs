using System.Collections.Generic;

namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Represents one OCR observation that contributes to specialized structured-text decoding.
	/// </summary>
	public sealed class StructuredTextObservation
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StructuredTextObservation"/> class.
		/// </summary>
		/// <param name="SourceKind">The observation source kind.</param>
		/// <param name="RawText">The raw text returned by OCR.</param>
		/// <param name="Lines">The OCR text split into lines.</param>
		/// <param name="Confidence">The OCR confidence.</param>
		/// <param name="LineIndex">The zero-based line index for line observations, if known.</param>
		/// <param name="Metadata">Additional observation metadata.</param>
		public StructuredTextObservation(
			StructuredTextObservationSourceKind SourceKind,
			string RawText,
			IReadOnlyList<string> Lines,
			float Confidence,
			int? LineIndex = null,
			IReadOnlyDictionary<string, string>? Metadata = null)
		{
			ArgumentNullException.ThrowIfNull(RawText);
			ArgumentNullException.ThrowIfNull(Lines);

			this.SourceKind = SourceKind;
			this.RawText = RawText;
			this.Lines = Lines;
			this.Confidence = Confidence;
			this.LineIndex = LineIndex;
			this.Metadata = Metadata ?? new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets the observation source kind.
		/// </summary>
		public StructuredTextObservationSourceKind SourceKind
		{
			get;
		}

		/// <summary>
		/// Gets the raw OCR text.
		/// </summary>
		public string RawText
		{
			get;
		}

		/// <summary>
		/// Gets the OCR text split into lines.
		/// </summary>
		public IReadOnlyList<string> Lines
		{
			get;
		}

		/// <summary>
		/// Gets the OCR confidence.
		/// </summary>
		public float Confidence
		{
			get;
		}

		/// <summary>
		/// Gets the zero-based line index for line observations, if known.
		/// </summary>
		public int? LineIndex
		{
			get;
		}

		/// <summary>
		/// Gets additional observation metadata.
		/// </summary>
		public IReadOnlyDictionary<string, string> Metadata
		{
			get;
		}
	}
}
