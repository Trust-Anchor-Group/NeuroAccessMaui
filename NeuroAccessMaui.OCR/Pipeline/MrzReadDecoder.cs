using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Pipeline
{
	/// <summary>
	/// Turns recognized MRZ text into validated <see cref="MrzFrameRead"/> instances and final scan results.
	/// </summary>
	internal static class MrzReadDecoder
	{
		/// <summary>
		/// Decodes recognizer output once and selects the best candidate across all MRZ layouts.
		/// </summary>
		/// <param name="Recognition">The recognizer output.</param>
		/// <param name="DocumentKindHint">The expected document kind, used only to break ties between layouts.</param>
		/// <returns>The decoded read, or <see langword="null"/> if no MRZ candidate could be formed.</returns>
		internal static MrzFrameRead? Decode(OcrTextRecognitionResult Recognition, OcrDocumentKindHint DocumentKindHint)
		{
			ArgumentNullException.ThrowIfNull(Recognition);

			List<string> Lines = Recognition.Lines
				.Select(NormalizeRecognizedLine)
				.Where(static Line => Line.Length > 0)
				.ToList();
			if (Lines.Count == 0)
				return null;

			StructuredTextObservation BlockObservation = new StructuredTextObservation(
				StructuredTextObservationSourceKind.Block,
				string.Join('\n', Lines),
				Lines,
				Recognition.Confidence);

			// A read that already has the exact shape of a layout and validates without repairs needs no search.
			MrzDecodingEngine.MrzDecodedCandidate? Exact = MrzDecodingEngine.DecodeExactShape(BlockObservation);
			if (Exact is not null)
			{
				MrzFrameRead ExactRead = CreateRead(Exact, Recognition.RawText, Recognition.Confidence);
				if (ExactRead.IsStrictSuccess && ExactRead.RepairCount == 0)
					return ExactRead;
			}

			List<StructuredTextObservation> Observations = new List<StructuredTextObservation> { BlockObservation };
			for (int Index = 0; Index < Lines.Count; Index++)
			{
				Observations.Add(new StructuredTextObservation(
					StructuredTextObservationSourceKind.Line,
					Lines[Index],
					new[] { Lines[Index] },
					Recognition.Confidence,
					Index));
			}

			IReadOnlyList<MrzDecodingEngine.MrzDecodedCandidate> Candidates = MrzDecodingEngine.Decode(Observations);
			if (Candidates.Count == 0)
				return null;

			MrzDecodingEngine.MrzDecodedCandidate Selected = SelectCandidate(Candidates, DocumentKindHint);
			return CreateRead(Selected, Recognition.RawText, Recognition.Confidence);
		}

		/// <summary>
		/// Decodes a set of fixed-length lines, such as a per-character vote across several reads, in a known layout.
		/// </summary>
		/// <param name="Lines">The MRZ lines.</param>
		/// <param name="Format">The layout the lines were produced in.</param>
		/// <param name="Confidence">The confidence to attribute to the lines.</param>
		/// <returns>The decoded read, or <see langword="null"/> if the lines could not be decoded in that layout.</returns>
		internal static MrzFrameRead? DecodeLines(IReadOnlyList<string> Lines, MrzDocumentFormat Format, float Confidence)
		{
			ArgumentNullException.ThrowIfNull(Lines);

			string Text = string.Join('\n', Lines);
			List<StructuredTextObservation> Observations = new List<StructuredTextObservation>
			{
				new StructuredTextObservation(StructuredTextObservationSourceKind.Block, Text, Lines, Confidence)
			};
			MrzDecodingEngine.MrzDecodedCandidate? Candidate = MrzDecodingEngine
				.Decode(Observations)
				.FirstOrDefault(Decoded => MapFormat(Decoded.Format) == Format);

			return Candidate is null ? null : CreateRead(Candidate, Text, Confidence);
		}

		/// <summary>
		/// Creates a successful scan result from a validated read.
		/// </summary>
		/// <param name="Read">The validated read.</param>
		/// <param name="TargetKind">The scan target kind.</param>
		/// <param name="SourceKind">The kind of image the read came from.</param>
		/// <param name="Metadata">The metadata to attach to the result.</param>
		/// <returns>The scan result.</returns>
		internal static OcrScanResult CreateSuccessResult(
			MrzFrameRead Read,
			OcrScanTargetKind TargetKind,
			OcrScanSourceKind SourceKind,
			Dictionary<string, string> Metadata)
		{
			ArgumentNullException.ThrowIfNull(Read);
			ArgumentNullException.ThrowIfNull(Metadata);
			if (!Read.IsStrictSuccess || Read.Document is null)
				throw new ArgumentException("Only validated reads can produce a successful scan result.", nameof(Read));

			Metadata["MrzFailureCategory"] = MrzScanFailureCategory.None.ToString();
			return new OcrScanResult(
				TargetKind,
				SourceKind,
				OnnxMrzScanEngine.ProviderIdentifier,
				OcrScanValidationStatus.Succeeded,
				Read.RawText,
				Read.NormalizedText,
				new MrzScanResult(Read.Format, Read.NormalizedText, Read.Document),
				Math.Max(Read.RecognitionConfidence, Read.DecodeScore),
				null,
				Metadata);
		}

		private static MrzDecodingEngine.MrzDecodedCandidate SelectCandidate(
			IReadOnlyList<MrzDecodingEngine.MrzDecodedCandidate> Candidates,
			OcrDocumentKindHint DocumentKindHint)
		{
			MrzDecodingEngine.MrzDecodedCandidate Best = Candidates[0];
			MrzDecodingEngine.MrzFormat? PreferredFormat = DocumentKindHint switch
			{
				OcrDocumentKindHint.IdentityCard => MrzDecodingEngine.MrzFormat.Td1,
				OcrDocumentKindHint.Passport => MrzDecodingEngine.MrzFormat.Td3,
				_ => null
			};
			if (!PreferredFormat.HasValue || Best.Format == PreferredFormat.Value)
				return Best;

			return Candidates.FirstOrDefault(Candidate =>
				Candidate.Format == PreferredFormat.Value
				&& Candidate.AcceptanceRank == Best.AcceptanceRank
				&& Candidate.ParserValidated == Best.ParserValidated
				&& Candidate.PassedRequiredCheckCount == Best.PassedRequiredCheckCount) ?? Best;
		}

		private static MrzFrameRead CreateRead(MrzDecodingEngine.MrzDecodedCandidate Candidate, string RawText, float RecognitionConfidence)
		{
			bool IsStrictSuccess = Candidate.ParserValidated
				&& Candidate.AcceptanceClassification == MrzDecodingEngine.MrzAcceptanceClassification.Accepted
				&& Candidate.ParsedInformation is not null;

			return new MrzFrameRead(
				MapFormat(Candidate.Format),
				Candidate.Lines,
				RawText,
				RecognitionConfidence,
				Candidate.Score,
				IsStrictSuccess,
				Candidate.PassedRequiredCheckCount,
				Candidate.RepairCount,
				IsStrictSuccess ? Candidate.ParsedInformation : null,
				Candidate.Metadata);
		}

		private static string NormalizeRecognizedLine(string Text)
		{
			if (string.IsNullOrWhiteSpace(Text))
				return string.Empty;

			char[] Buffer = new char[Text.Length];
			int Count = 0;
			foreach (char Character in Text)
			{
				if (char.IsWhiteSpace(Character))
					continue;

				char Upper = char.ToUpperInvariant(Character);
				Buffer[Count++] = Upper == '«' ? '<' : Upper;
			}

			return new string(Buffer, 0, Count);
		}

		private static MrzDocumentFormat MapFormat(MrzDecodingEngine.MrzFormat Format)
		{
			return Format switch
			{
				MrzDecodingEngine.MrzFormat.Td1 => MrzDocumentFormat.Td1,
				MrzDecodingEngine.MrzFormat.Td2 => MrzDocumentFormat.Td2,
				_ => MrzDocumentFormat.Td3
			};
		}
	}
}
