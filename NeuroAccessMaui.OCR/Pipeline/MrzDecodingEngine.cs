using System.Globalization;
using System.Linq;
using NeuroAccess.Nfc.TravelDocuments;
using NeuroAccessMaui.OCR.Models;

namespace NeuroAccessMaui.OCR.Pipeline
{
	internal static class MrzDecodingEngine
	{
		private const int MaxLineLengthVariants = 5;
		private const int MaxLineVariantCombinations = 8;
		private const int MaxFlattenedBlockCandidates = 6;

		private static readonly IReadOnlyDictionary<char, char[]> DigitConfusions = new Dictionary<char, char[]>
		{
			{ 'O', ['0'] },
			{ 'Q', ['0'] },
			{ 'I', ['1'] },
			{ 'L', ['1'] },
			{ 'Z', ['2'] },
			{ 'S', ['5'] },
			{ 'B', ['8'] },
			{ 'G', ['6'] },
			{ '<', ['0'] }
		};

		private static readonly IReadOnlyDictionary<char, char[]> LetterConfusions = new Dictionary<char, char[]>
		{
			{ '0', ['O'] },
			{ '1', ['I', 'L'] },
			{ '2', ['Z'] },
			{ '5', ['S'] },
			{ '6', ['G'] },
			{ '8', ['B'] }
		};

		private static readonly IReadOnlyList<MrzFormatShape> Shapes =
		[
			new MrzFormatShape(MrzFormat.Td1, 30, 3),
			new MrzFormatShape(MrzFormat.Td2, 36, 2),
			new MrzFormatShape(MrzFormat.Td3, 44, 2)
		];

		public static IReadOnlyList<MrzDecodedCandidate> Decode(IReadOnlyList<StructuredTextObservation> Observations)
		{
			ArgumentNullException.ThrowIfNull(Observations);

			List<MrzSourceCandidate> SourceCandidates = CreateSourceCandidates(Observations);
			if (SourceCandidates.Count == 0)
				return Array.Empty<MrzDecodedCandidate>();

			List<MrzDecodedCandidate> DecodedCandidates = new List<MrzDecodedCandidate>(SourceCandidates.Count);
			foreach (MrzSourceCandidate SourceCandidate in SourceCandidates)
				DecodedCandidates.Add(DecodeCandidate(SourceCandidate));

			return DecodedCandidates
				.OrderByDescending(static Candidate => Candidate.AcceptanceRank)
				.ThenByDescending(static Candidate => Candidate.ParserValidated)
				.ThenByDescending(static Candidate => Candidate.PassedRequiredCheckCount)
				.ThenByDescending(static Candidate => Candidate.Score)
				.ToList();
		}

		private static List<MrzSourceCandidate> CreateSourceCandidates(IReadOnlyList<StructuredTextObservation> Observations)
		{
			List<MrzSourceCandidate> Results = new List<MrzSourceCandidate>();
			HashSet<string> Seen = new HashSet<string>(StringComparer.Ordinal);

			List<StructuredTextObservation> BlockObservations = Observations
				.Where(static Observation => Observation.SourceKind == StructuredTextObservationSourceKind.Block)
				.ToList();
			List<StructuredTextObservation> LineObservations = Observations
				.Where(static Observation => Observation.SourceKind == StructuredTextObservationSourceKind.Line && Observation.LineIndex.HasValue)
				.OrderBy(static Observation => Observation.LineIndex!.Value)
				.ToList();

			List<MrzSourceCandidate> BlockCandidates = new List<MrzSourceCandidate>();
			foreach (StructuredTextObservation Observation in BlockObservations)
			{
				foreach (MrzFormatShape Shape in Shapes)
				{
					foreach (MrzSourceCandidate Candidate in CreateCandidatesFromObservation(Observation, Shape, "Block"))
					{
						BlockCandidates.Add(Candidate);
						AddCandidate(Results, Seen, Candidate);
					}
				}
			}

			List<MrzSourceCandidate> LineCandidates = new List<MrzSourceCandidate>();
			foreach (MrzFormatShape Shape in Shapes)
			{
				foreach (MrzSourceCandidate Candidate in CreateCandidatesFromLineObservations(LineObservations, Shape))
				{
					LineCandidates.Add(Candidate);
					AddCandidate(Results, Seen, Candidate);
				}
			}

			foreach (MrzSourceCandidate BlockCandidate in BlockCandidates)
			{
				foreach (MrzSourceCandidate LineCandidate in LineCandidates)
				{
					if (BlockCandidate.Format != LineCandidate.Format)
						continue;

					MrzSourceCandidate HybridCandidate = CreateHybridCandidate(LineCandidate, BlockCandidate);
					AddCandidate(Results, Seen, HybridCandidate);
				}
			}

			return Results;
		}

		private static void AddCandidate(List<MrzSourceCandidate> Results, HashSet<string> Seen, MrzSourceCandidate Candidate)
		{
			string Key = Candidate.Format + "|" + Candidate.Text;
			if (!Seen.Add(Key))
				return;

			Results.Add(Candidate);
		}

		private static IEnumerable<MrzSourceCandidate> CreateCandidatesFromObservation(
			StructuredTextObservation Observation,
			MrzFormatShape Shape,
			string Source)
		{
			List<string> RawLines = SplitAndNormalizeLines(Observation);
			if (RawLines.Count >= Shape.LineCount)
			{
				for (int StartIndex = 0; StartIndex <= RawLines.Count - Shape.LineCount; StartIndex++)
				{
					List<List<MrzLineNormalizationCandidate>> VariantSets = new List<List<MrzLineNormalizationCandidate>>(Shape.LineCount);
					for (int Index = 0; Index < Shape.LineCount; Index++)
						VariantSets.Add(CreateLineLengthCandidates(RawLines[StartIndex + Index], Shape.LineLength));

					foreach (MrzLineCombination Combination in CreateLineVariantCombinations(VariantSets, MaxLineVariantCombinations))
					{
						List<string> Transformations = new List<string>
						{
							"NormalizeObservationText",
							CreateWindowSelectionTransformation(StartIndex, StartIndex + Shape.LineCount - 1, RawLines.Count)
						};
						Transformations.AddRange(Combination.Transformations);

						yield return new MrzSourceCandidate(
							Shape.Format,
							Combination.Lines,
							Source,
							new[] { Observation },
							Transformations,
							StartIndex,
							StartIndex + Shape.LineCount - 1,
							RawLines.Count);
					}
				}

				yield break;
			}

			string Flattened = NormalizeObservationText(Observation.RawText, false);
			int TotalLength = Shape.LineLength * Shape.LineCount;
			if (Flattened.Length < TotalLength)
				yield break;

			foreach (MrzLineCombination Combination in CreateFlattenedBlockCandidates(Flattened, Shape.LineLength, Shape.LineCount))
			{
				List<string> Transformations = new List<string> { "NormalizeObservationText", "FlattenObservationText" };
				Transformations.AddRange(Combination.Transformations);
				yield return new MrzSourceCandidate(
					Shape.Format,
					Combination.Lines,
					Source,
					new[] { Observation },
					Transformations,
					null,
					null,
					RawLines.Count);
			}
		}

		private static IEnumerable<MrzSourceCandidate> CreateCandidatesFromLineObservations(
			IReadOnlyList<StructuredTextObservation> Observations,
			MrzFormatShape Shape)
		{
			if (Observations.Count < Shape.LineCount)
				yield break;

			for (int StartIndex = 0; StartIndex <= Observations.Count - Shape.LineCount; StartIndex++)
			{
				List<StructuredTextObservation> Selected = Observations
					.Skip(StartIndex)
					.Take(Shape.LineCount)
					.ToList();
				List<List<MrzLineNormalizationCandidate>> VariantSets = new List<List<MrzLineNormalizationCandidate>>(Shape.LineCount);
				foreach (StructuredTextObservation Observation in Selected)
				{
					string SourceLine = Observation.Lines.Count > 0 ? Observation.Lines[0] : Observation.RawText;
					VariantSets.Add(CreateLineLengthCandidates(SourceLine, Shape.LineLength));
				}

				foreach (MrzLineCombination Combination in CreateLineVariantCombinations(VariantSets, MaxLineVariantCombinations))
				{
					List<string> Transformations = new List<string>
					{
						"NormalizeLineObservations",
						CreateWindowSelectionTransformation(StartIndex, StartIndex + Shape.LineCount - 1, Observations.Count)
					};
					Transformations.AddRange(Combination.Transformations);

					yield return new MrzSourceCandidate(
						Shape.Format,
						Combination.Lines,
						"Line",
						Selected,
						Transformations,
						StartIndex,
						StartIndex + Shape.LineCount - 1,
						Observations.Count);
				}
			}
		}

		private static MrzSourceCandidate CreateHybridCandidate(MrzSourceCandidate LineCandidate, MrzSourceCandidate BlockCandidate)
		{
			List<string> Lines = new List<string>(LineCandidate.Lines.Count);
			List<string> Transformations = new List<string>(LineCandidate.AppliedTransformations);
			Transformations.AddRange(BlockCandidate.AppliedTransformations);
			Transformations.Add("MergeBlockAndLineObservations");

			for (int LineIndex = 0; LineIndex < LineCandidate.Lines.Count; LineIndex++)
			{
				string LineText = LineCandidate.Lines[LineIndex];
				string BlockText = BlockCandidate.Lines[LineIndex];
				char[] Buffer = LineText.ToCharArray();
				for (int CharacterIndex = 0; CharacterIndex < Buffer.Length; CharacterIndex++)
				{
					if (Buffer[CharacterIndex] == '<' && BlockText[CharacterIndex] != '<')
						Buffer[CharacterIndex] = BlockText[CharacterIndex];
				}

				Lines.Add(new string(Buffer));
			}

			List<StructuredTextObservation> CombinedObservations = new List<StructuredTextObservation>();
			CombinedObservations.AddRange(LineCandidate.Observations);
			CombinedObservations.AddRange(BlockCandidate.Observations);

			return new MrzSourceCandidate(
				LineCandidate.Format,
				Lines,
				"Hybrid",
				CombinedObservations,
				Transformations,
				LineCandidate.WindowStartIndex ?? BlockCandidate.WindowStartIndex,
				LineCandidate.WindowEndIndex ?? BlockCandidate.WindowEndIndex,
				Math.Max(LineCandidate.WindowTotalCount, BlockCandidate.WindowTotalCount));
		}

		private static MrzDecodedCandidate DecodeCandidate(MrzSourceCandidate SourceCandidate)
		{
			List<string> Lines = SourceCandidate.Lines.Select(static Line => Line).ToList();
			List<string> AppliedTransformations = new List<string>(SourceCandidate.AppliedTransformations);
			List<string> RepairOperations = new List<string>();
			int RepairCount = 0;

			NormalizeCandidateLines(Lines, SourceCandidate.Format, AppliedTransformations);
			RepairRequiredFields(Lines, SourceCandidate.Format, RepairOperations, ref RepairCount);

			string NormalizedText = string.Join("\n", Lines);
			MrzValidationResult Validation = MrzValidator.Validate(NormalizedText);
			bool DocumentNumberCheckPassed = Validation.DocumentNumberCheckPassed;
			bool DateOfBirthCheckPassed = Validation.DateOfBirthCheckPassed;
			bool ExpiryCheckPassed = Validation.ExpiryCheckPassed;
			int RequiredCheckCount = Validation.RequiredCheckCount;
			int PassedRequiredCheckCount = Validation.PassedRequiredCheckCount;
			bool ParserValidated = Validation.IsParsed;
			DocumentInformation? ParsedInformation = Validation.DocumentInformation;
			MrzAcceptanceClassification Acceptance = ParserValidated
				? MrzAcceptanceClassification.Accepted
				: PassedRequiredCheckCount >= 3
					? MrzAcceptanceClassification.ReviewRequired
					: MrzAcceptanceClassification.Rejected;

			float ObservationConfidence = SourceCandidate.Observations.Count == 0
				? 0f
				: SourceCandidate.Observations.Average(static Observation => Observation.Confidence);
			float SourceBias = SourceCandidate.Source switch
			{
				"Hybrid" => 0.18f,
				"Line" => 0.12f,
				_ => 0.06f
			};
			float WindowPositionBias = CalculateWindowPositionBias(SourceCandidate);
			float StructuralBias = CalculateStructuralBias(Lines, SourceCandidate.Format);
			float ValidationBias = Acceptance switch
			{
				MrzAcceptanceClassification.Accepted => 0.45f,
				MrzAcceptanceClassification.ReviewRequired => 0.24f,
				_ => 0f
			};
			float Score = ValidationBias
				+ (PassedRequiredCheckCount * 0.10f)
				+ SourceBias
				+ WindowPositionBias
				+ StructuralBias
				+ (ObservationConfidence * 0.10f)
				- (RepairCount * 0.04f);
			Score = Math.Clamp(Score, 0f, 1f);

			Dictionary<string, string> Metadata = new Dictionary<string, string>
			{
				{ "MrzCandidateFormat", SourceCandidate.Format.ToString() },
				{ "MrzCandidateSource", SourceCandidate.Source },
				{ "MrzAcceptanceClassification", Acceptance.ToString() },
				{ "MrzParserValidated", ParserValidated.ToString() },
				{ "MrzDocumentNumberCheckPassed", DocumentNumberCheckPassed.ToString() },
				{ "MrzDateOfBirthCheckPassed", DateOfBirthCheckPassed.ToString() },
				{ "MrzExpiryCheckPassed", ExpiryCheckPassed.ToString() },
				{ "MrzCompositeCheckPassed", Validation.CompositeCheckPassed.ToString() },
				{ "MrzRequiredCheckCount", RequiredCheckCount.ToString(CultureInfo.InvariantCulture) },
				{ "MrzPassedRequiredCheckCount", PassedRequiredCheckCount.ToString(CultureInfo.InvariantCulture) },
				{ "MrzRepairCount", RepairCount.ToString(CultureInfo.InvariantCulture) },
				{ "MrzObservationCount", SourceCandidate.Observations.Count.ToString(CultureInfo.InvariantCulture) },
				{ "MrzWindowPositionBias", WindowPositionBias.ToString("0.000", CultureInfo.InvariantCulture) },
				{ "MrzStructuralBias", StructuralBias.ToString("0.000", CultureInfo.InvariantCulture) },
				{ "MrzScore", Score.ToString("0.000", CultureInfo.InvariantCulture) },
				{ "MrzAppliedTransformations", string.Join(" | ", AppliedTransformations.Concat(RepairOperations)) }
			};

			if (SourceCandidate.WindowStartIndex.HasValue)
				Metadata["MrzWindowStartIndex"] = SourceCandidate.WindowStartIndex.Value.ToString(CultureInfo.InvariantCulture);
			if (SourceCandidate.WindowEndIndex.HasValue)
				Metadata["MrzWindowEndIndex"] = SourceCandidate.WindowEndIndex.Value.ToString(CultureInfo.InvariantCulture);
			if (SourceCandidate.WindowTotalCount > 0)
				Metadata["MrzWindowTotalCount"] = SourceCandidate.WindowTotalCount.ToString(CultureInfo.InvariantCulture);

			for (int ObservationIndex = 0; ObservationIndex < SourceCandidate.Observations.Count; ObservationIndex++)
			{
				StructuredTextObservation Observation = SourceCandidate.Observations[ObservationIndex];
				string Prefix = Observation.SourceKind == StructuredTextObservationSourceKind.Block
					? "MrzBlockObservation"
					: "MrzLineObservation" + Observation.LineIndex.GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
				Metadata[Prefix + "RawText"] = Observation.RawText;
			}

			return new MrzDecodedCandidate(
				SourceCandidate.Format,
				NormalizedText,
				Lines,
				SourceCandidate.Source,
				AppliedTransformations.Concat(RepairOperations).ToArray(),
				Acceptance,
				ParserValidated,
				PassedRequiredCheckCount,
				RepairCount,
				Score,
				Metadata,
				ParsedInformation);
		}

		private static string CreateWindowSelectionTransformation(int StartIndex, int EndIndex, int TotalCount)
		{
			return $"SelectObservationWindow[{StartIndex}-{EndIndex}]Of{TotalCount}";
		}

		private static float CalculateWindowPositionBias(MrzSourceCandidate SourceCandidate)
		{
			if (!SourceCandidate.WindowEndIndex.HasValue || SourceCandidate.WindowTotalCount <= 0)
				return 0f;

			float RelativeBottomPosition = (float)(SourceCandidate.WindowEndIndex.Value + 1) / SourceCandidate.WindowTotalCount;
			return Math.Clamp(RelativeBottomPosition, 0f, 1f) * 0.12f;
		}

		private static float CalculateStructuralBias(IReadOnlyList<string> Lines, MrzFormat Format)
		{
			if (Lines.Count == 0)
				return 0f;

			float AggregateScore = 0f;
			for (int LineIndex = 0; LineIndex < Lines.Count; LineIndex++)
				AggregateScore += CalculateLineStructuralScore(Lines[LineIndex], Format, LineIndex);

			float AverageScore = AggregateScore / Lines.Count;
			return Math.Clamp(AverageScore, 0f, 1f) * 0.12f;
		}

		private static float CalculateLineStructuralScore(string Line, MrzFormat Format, int LineIndex)
		{
			if (string.IsNullOrEmpty(Line))
				return 0f;

			int DigitCount = 0;
			int LetterCount = 0;
			int FillerCount = 0;
			foreach (char Character in Line)
			{
				if (Character >= '0' && Character <= '9')
					DigitCount++;
				else if (Character >= 'A' && Character <= 'Z')
					LetterCount++;
				else if (Character == '<')
					FillerCount++;
			}

			float DigitRatio = (float)DigitCount / Line.Length;
			float LetterRatio = (float)LetterCount / Line.Length;
			float FillerRatio = (float)FillerCount / Line.Length;

			return (Format, LineIndex) switch
			{
				(MrzFormat.Td1, 0) => Math.Clamp((DigitRatio * 0.45f) + (LetterRatio * 0.20f) + (FillerRatio * 0.35f), 0f, 1f),
				(MrzFormat.Td1, 1) => Math.Clamp((DigitRatio * 0.65f) + (LetterRatio * 0.05f) + (FillerRatio * 0.30f), 0f, 1f),
				(MrzFormat.Td1, 2) => Math.Clamp((LetterRatio * 0.45f) + (FillerRatio * 0.55f), 0f, 1f),
				(_, 0) when Format is MrzFormat.Td2 or MrzFormat.Td3 => Math.Clamp((LetterRatio * 0.45f) + (FillerRatio * 0.55f), 0f, 1f),
				(_, 1) when Format is MrzFormat.Td2 or MrzFormat.Td3 => Math.Clamp((DigitRatio * 0.50f) + (LetterRatio * 0.12f) + (FillerRatio * 0.38f), 0f, 1f),
				_ => Math.Clamp((DigitRatio * 0.35f) + (LetterRatio * 0.25f) + (FillerRatio * 0.40f), 0f, 1f)
			};
		}

		private static void NormalizeCandidateLines(List<string> Lines, MrzFormat Format, List<string> AppliedTransformations)
		{
			switch (Format)
			{
				case MrzFormat.Td1:
					Lines[0] = NormalizeTd1Line1(Lines[0], AppliedTransformations);
					Lines[1] = NormalizeTd1Line2(Lines[1], AppliedTransformations);
					Lines[2] = NormalizeNameLine(Lines[2], AppliedTransformations);
					break;

				case MrzFormat.Td2:
					Lines[0] = NormalizeTd2Line1(Lines[0], AppliedTransformations);
					Lines[1] = NormalizeTd2Line2(Lines[1], AppliedTransformations);
					break;

				case MrzFormat.Td3:
					Lines[0] = NormalizeTd3Line1(Lines[0], AppliedTransformations);
					Lines[1] = NormalizeTd3Line2(Lines[1], AppliedTransformations);
					break;
			}
		}

		private static void RepairRequiredFields(List<string> Lines, MrzFormat Format, List<string> RepairOperations, ref int RepairCount)
		{
			switch (Format)
			{
				case MrzFormat.Td1:
					RepairFieldByCheckDigit(Lines, 0, 5, 9, Lines[0][14], MrzCharacterClass.AlphaNumericFiller, "DocumentNumber", RepairOperations, ref RepairCount);
					RepairFieldByCheckDigit(Lines, 1, 0, 6, Lines[1][6], MrzCharacterClass.Digits, "DateOfBirth", RepairOperations, ref RepairCount);
					RepairFieldByCheckDigit(Lines, 1, 8, 6, Lines[1][14], MrzCharacterClass.Digits, "Expiry", RepairOperations, ref RepairCount);
					break;

				case MrzFormat.Td2:
					RepairFieldByCheckDigit(Lines, 1, 0, 9, Lines[1][9], MrzCharacterClass.AlphaNumericFiller, "DocumentNumber", RepairOperations, ref RepairCount);
					RepairFieldByCheckDigit(Lines, 1, 13, 6, Lines[1][19], MrzCharacterClass.Digits, "DateOfBirth", RepairOperations, ref RepairCount);
					RepairFieldByCheckDigit(Lines, 1, 21, 6, Lines[1][27], MrzCharacterClass.Digits, "Expiry", RepairOperations, ref RepairCount);
					break;

				case MrzFormat.Td3:
					RepairFieldByCheckDigit(Lines, 1, 0, 9, Lines[1][9], MrzCharacterClass.AlphaNumericFiller, "DocumentNumber", RepairOperations, ref RepairCount);
					RepairFieldByCheckDigit(Lines, 1, 13, 6, Lines[1][19], MrzCharacterClass.Digits, "DateOfBirth", RepairOperations, ref RepairCount);
					RepairFieldByCheckDigit(Lines, 1, 21, 6, Lines[1][27], MrzCharacterClass.Digits, "Expiry", RepairOperations, ref RepairCount);
					break;
			}
		}

		private static void RepairFieldByCheckDigit(
			List<string> Lines,
			int LineIndex,
			int Start,
			int Length,
			char ExpectedCheckDigit,
			MrzCharacterClass CharacterClass,
			string FieldName,
			List<string> RepairOperations,
			ref int RepairCount)
		{
			char NormalizedCheckDigit = NormalizeCheckDigitChar(ExpectedCheckDigit);
			string CurrentField = Lines[LineIndex].Substring(Start, Length);
			if (MrzValidator.CalculateCheckDigit(CurrentField) == NormalizedCheckDigit)
				return;

			string? RepairedField = TryRepairField(CurrentField, NormalizedCheckDigit, CharacterClass, out int AppliedEdits);
			if (string.IsNullOrWhiteSpace(RepairedField) || RepairedField == CurrentField)
				return;

			Lines[LineIndex] = ReplaceSubstring(Lines[LineIndex], Start, Length, RepairedField);
			RepairOperations.Add($"Repair{FieldName}CheckDigit");
			RepairCount += AppliedEdits;
		}

		private static string? TryRepairField(
			string Field,
			char ExpectedCheckDigit,
			MrzCharacterClass CharacterClass,
			out int AppliedEdits)
		{
			List<FieldRepairCandidate> Beam = new List<FieldRepairCandidate>
			{
				new FieldRepairCandidate(Field, 0f, 0)
			};

			for (int CharacterIndex = 0; CharacterIndex < Field.Length; CharacterIndex++)
			{
				List<FieldRepairCandidate> Next = new List<FieldRepairCandidate>();
				foreach (FieldRepairCandidate Candidate in Beam)
				{
					foreach ((char Replacement, float Cost) in GetAlternatives(Candidate.Text[CharacterIndex], CharacterClass))
					{
						int EditDelta = Replacement == Candidate.Text[CharacterIndex] ? 0 : 1;
						if (Candidate.Edits + EditDelta > 2)
							continue;

						string Replaced = ReplaceCharacter(Candidate.Text, CharacterIndex, Replacement);
						Next.Add(new FieldRepairCandidate(Replaced, Candidate.Cost + Cost, Candidate.Edits + EditDelta));
					}
				}

				Beam = Next
					.OrderBy(static Candidate => Candidate.Cost)
					.ThenBy(static Candidate => Candidate.Edits)
					.Take(48)
					.ToList();
			}

			FieldRepairCandidate? BestCandidate = Beam
				.Where(Candidate => MrzValidator.CalculateCheckDigit(Candidate.Text) == ExpectedCheckDigit)
				.OrderBy(static Candidate => Candidate.Cost)
				.ThenBy(static Candidate => Candidate.Edits)
				.FirstOrDefault();

			if (BestCandidate is null)
			{
				AppliedEdits = 0;
				return null;
			}

			AppliedEdits = BestCandidate.Edits;
			return BestCandidate.Text;
		}

		private static IEnumerable<(char Replacement, float Cost)> GetAlternatives(char Character, MrzCharacterClass CharacterClass)
		{
			HashSet<char> Seen = new HashSet<char>();
			char Normalized = NormalizeMrzCharacter(Character, true);
			foreach ((char Replacement, float Cost) in YieldAlternatives(Normalized, CharacterClass))
			{
				if (Seen.Add(Replacement))
					yield return (Replacement, Cost);
			}
		}

		private static IEnumerable<(char Replacement, float Cost)> YieldAlternatives(char Character, MrzCharacterClass CharacterClass)
		{
			yield return (NormalizeCharacterByClass(Character, CharacterClass), 0f);

			switch (CharacterClass)
			{
				case MrzCharacterClass.Digits:
				case MrzCharacterClass.CheckDigit:
					if (DigitConfusions.TryGetValue(Character, out char[]? DigitAlternatives))
					{
						foreach (char Alternative in DigitAlternatives)
							yield return (Alternative, 0.15f);
					}
					break;

				case MrzCharacterClass.Letters:
				case MrzCharacterClass.Names:
					if (LetterConfusions.TryGetValue(Character, out char[]? LetterAlternatives))
					{
						foreach (char Alternative in LetterAlternatives)
							yield return (Alternative, 0.20f);
					}
					break;

				case MrzCharacterClass.AlphaNumericFiller:
					if (DigitConfusions.TryGetValue(Character, out char[]? AlnumDigitAlternatives))
					{
						foreach (char Alternative in AlnumDigitAlternatives)
							yield return (Alternative, 0.15f);
					}

					if (LetterConfusions.TryGetValue(Character, out char[]? AlnumLetterAlternatives))
					{
						foreach (char Alternative in AlnumLetterAlternatives)
							yield return (Alternative, 0.20f);
					}

					if (Character == '0' || Character == 'O')
						yield return ('<', 0.18f);
					if (Character == '<')
						yield return ('0', 0.20f);
					break;
			}
		}

		private static string NormalizeTd1Line1(string Line, List<string> Transformations)
		{
			return ReplaceSegments(Line,
				(0, 2, MrzCharacterClass.Letters),
				(2, 3, MrzCharacterClass.Letters),
				(5, 9, MrzCharacterClass.AlphaNumericFiller),
				(14, 1, MrzCharacterClass.CheckDigit),
				(15, 15, MrzCharacterClass.AlphaNumericFiller),
				Transformations);
		}

		private static string NormalizeTd1Line2(string Line, List<string> Transformations)
		{
			return ReplaceSegments(Line,
				(0, 6, MrzCharacterClass.Digits),
				(6, 1, MrzCharacterClass.CheckDigit),
				(7, 1, MrzCharacterClass.Sex),
				(8, 6, MrzCharacterClass.Digits),
				(14, 1, MrzCharacterClass.CheckDigit),
				(15, 3, MrzCharacterClass.Letters),
				(18, 11, MrzCharacterClass.AlphaNumericFiller),
				(29, 1, MrzCharacterClass.CheckDigit),
				Transformations);
		}

		private static string NormalizeTd2Line1(string Line, List<string> Transformations)
		{
			return ReplaceSegments(Line,
				(0, 2, MrzCharacterClass.Letters),
				(2, 3, MrzCharacterClass.Letters),
				(5, 31, MrzCharacterClass.Names),
				Transformations);
		}

		private static string NormalizeTd2Line2(string Line, List<string> Transformations)
		{
			return ReplaceSegments(
				Line,
				new (int Start, int Length, MrzCharacterClass CharacterClass)[]
				{
					(0, 9, MrzCharacterClass.AlphaNumericFiller),
					(9, 1, MrzCharacterClass.CheckDigit),
					(10, 3, MrzCharacterClass.Letters),
					(13, 6, MrzCharacterClass.Digits),
					(19, 1, MrzCharacterClass.CheckDigit),
					(20, 1, MrzCharacterClass.Sex),
					(21, 6, MrzCharacterClass.Digits),
					(27, 1, MrzCharacterClass.CheckDigit),
					(28, 7, MrzCharacterClass.AlphaNumericFiller),
					(35, 1, MrzCharacterClass.CheckDigit)
				},
				Transformations);
		}

		private static string NormalizeTd3Line1(string Line, List<string> Transformations)
		{
			return ReplaceSegments(Line,
				(0, 2, MrzCharacterClass.Letters),
				(2, 3, MrzCharacterClass.Letters),
				(5, 39, MrzCharacterClass.Names),
				Transformations);
		}

		private static string NormalizeTd3Line2(string Line, List<string> Transformations)
		{
			return ReplaceSegments(
				Line,
				new (int Start, int Length, MrzCharacterClass CharacterClass)[]
				{
					(0, 9, MrzCharacterClass.AlphaNumericFiller),
					(9, 1, MrzCharacterClass.CheckDigit),
					(10, 3, MrzCharacterClass.Letters),
					(13, 6, MrzCharacterClass.Digits),
					(19, 1, MrzCharacterClass.CheckDigit),
					(20, 1, MrzCharacterClass.Sex),
					(21, 6, MrzCharacterClass.Digits),
					(27, 1, MrzCharacterClass.CheckDigit),
					(28, 14, MrzCharacterClass.AlphaNumericFiller),
					(42, 1, MrzCharacterClass.CheckDigit),
					(43, 1, MrzCharacterClass.CheckDigit)
				},
				Transformations);
		}

		private static string NormalizeNameLine(string Line, List<string> Transformations)
		{
			return ReplaceSegments(Line, (0, Line.Length, MrzCharacterClass.Names), Transformations);
		}

		private static string ReplaceSegments(
			string Line,
			(int Start, int Length, MrzCharacterClass CharacterClass) FirstSegment,
			List<string> Transformations)
		{
			return ReplaceSegments(Line, new[] { FirstSegment }, Transformations);
		}

		private static string ReplaceSegments(
			string Line,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment1,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment2,
			List<string> Transformations)
		{
			return ReplaceSegments(Line, new[] { Segment1, Segment2 }, Transformations);
		}

		private static string ReplaceSegments(
			string Line,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment1,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment2,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment3,
			List<string> Transformations)
		{
			return ReplaceSegments(Line, new[] { Segment1, Segment2, Segment3 }, Transformations);
		}

		private static string ReplaceSegments(
			string Line,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment1,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment2,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment3,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment4,
			List<string> Transformations)
		{
			return ReplaceSegments(Line, new[] { Segment1, Segment2, Segment3, Segment4 }, Transformations);
		}

		private static string ReplaceSegments(
			string Line,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment1,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment2,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment3,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment4,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment5,
			List<string> Transformations)
		{
			return ReplaceSegments(Line, new[] { Segment1, Segment2, Segment3, Segment4, Segment5 }, Transformations);
		}

		private static string ReplaceSegments(
			string Line,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment1,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment2,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment3,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment4,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment5,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment6,
			List<string> Transformations)
		{
			return ReplaceSegments(Line, new[] { Segment1, Segment2, Segment3, Segment4, Segment5, Segment6 }, Transformations);
		}

		private static string ReplaceSegments(
			string Line,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment1,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment2,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment3,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment4,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment5,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment6,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment7,
			List<string> Transformations)
		{
			return ReplaceSegments(Line, new[] { Segment1, Segment2, Segment3, Segment4, Segment5, Segment6, Segment7 }, Transformations);
		}

		private static string ReplaceSegments(
			string Line,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment1,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment2,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment3,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment4,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment5,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment6,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment7,
			(int Start, int Length, MrzCharacterClass CharacterClass) Segment8,
			List<string> Transformations)
		{
			return ReplaceSegments(Line, new[] { Segment1, Segment2, Segment3, Segment4, Segment5, Segment6, Segment7, Segment8 }, Transformations);
		}

		private static string ReplaceSegments(
			string Line,
			IReadOnlyList<(int Start, int Length, MrzCharacterClass CharacterClass)> Segments,
			List<string> Transformations)
		{
			char[] Buffer = Line.ToCharArray();
			foreach ((int Start, int Length, MrzCharacterClass CharacterClass) Segment in Segments)
			{
				for (int Index = Segment.Start; Index < Segment.Start + Segment.Length && Index < Buffer.Length; Index++)
				{
					char Normalized = NormalizeCharacterByClass(Buffer[Index], Segment.CharacterClass);
					if (Normalized != Buffer[Index])
						Transformations.Add($"Normalize{Segment.CharacterClass}");
					Buffer[Index] = Normalized;
				}
			}

			return new string(Buffer);
		}

		private static char NormalizeCharacterByClass(char Character, MrzCharacterClass CharacterClass)
		{
			char Normalized = NormalizeMrzCharacter(Character, true);
			switch (CharacterClass)
			{
				case MrzCharacterClass.Digits:
				case MrzCharacterClass.CheckDigit:
					if (Normalized >= '0' && Normalized <= '9')
						return Normalized;
					if (DigitConfusions.TryGetValue(Normalized, out char[]? DigitAlternatives))
						return DigitAlternatives[0];
					return Normalized;

				case MrzCharacterClass.Letters:
					if (Normalized >= 'A' && Normalized <= 'Z')
						return Normalized;
					if (LetterConfusions.TryGetValue(Normalized, out char[]? LetterAlternatives))
						return LetterAlternatives[0];
					return Normalized;

				case MrzCharacterClass.Names:
					if (Normalized == '<' || (Normalized >= 'A' && Normalized <= 'Z'))
						return Normalized;
					if (LetterConfusions.TryGetValue(Normalized, out char[]? NameAlternatives))
						return NameAlternatives[0];
					return Normalized;

				case MrzCharacterClass.Sex:
					if (Normalized == 'M' || Normalized == 'F' || Normalized == '<')
						return Normalized;
					return Normalized;

				default:
					if (Normalized == '<' || (Normalized >= 'A' && Normalized <= 'Z') || (Normalized >= '0' && Normalized <= '9'))
						return Normalized;
					return Normalized;
			}
		}

		private static string NormalizeObservationText(string Value, bool KeepNewLines)
		{
			if (string.IsNullOrWhiteSpace(Value))
				return string.Empty;

			char[] Buffer = Value.ToUpperInvariant().ToCharArray();
			for (int Index = 0; Index < Buffer.Length; Index++)
			{
				char Character = Buffer[Index];
				if (Character == '\r')
				{
					Buffer[Index] = KeepNewLines ? '\n' : '<';
					continue;
				}

				if (Character == '\n')
				{
					Buffer[Index] = KeepNewLines ? '\n' : '<';
					continue;
				}

				Buffer[Index] = NormalizeMrzCharacter(Character, false);
			}

			return new string(Buffer);
		}

		private static List<string> SplitAndNormalizeLines(StructuredTextObservation Observation)
		{
			List<string> Results = new List<string>();
			if (Observation.Lines.Count > 0)
			{
				foreach (string Line in Observation.Lines)
				{
					string Normalized = NormalizeObservationText(Line, false);
					if (!string.IsNullOrWhiteSpace(Normalized))
						Results.Add(Normalized);
				}
			}
			else
			{
				string[] Lines = NormalizeObservationText(Observation.RawText, true)
					.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (string Line in Lines)
				{
					string Normalized = NormalizeObservationText(Line, false);
					if (!string.IsNullOrWhiteSpace(Normalized))
						Results.Add(Normalized);
				}
			}

			return Results;
		}

		private static List<MrzLineNormalizationCandidate> CreateLineLengthCandidates(string Value, int Length)
		{
			string Normalized = NormalizeObservationText(Value, false);
			List<MrzLineNormalizationCandidate> Candidates = new List<MrzLineNormalizationCandidate>();
			if (Normalized.Length == Length)
			{
				AddLineLengthCandidate(Candidates, Normalized, "ExactLineLength", 1f);
				return Candidates;
			}

			if (Normalized.Length < Length)
			{
				AddLineLengthCandidate(
					Candidates,
					Normalized.PadRight(Length, '<'),
					"PadLineToLength",
					CalculateMrzWindowScore(Normalized));
				AddLineLengthCandidate(
					Candidates,
					Normalized.PadLeft(Length, '<'),
					"PadLineStartToLength",
					CalculateMrzWindowScore(Normalized) * 0.96f);
				int LeftPadding = Math.Max(0, (Length - Normalized.Length) / 2);
				AddLineLengthCandidate(
					Candidates,
					new string('<', LeftPadding) + Normalized.PadRight(Length - LeftPadding, '<'),
					"PadLineCenterToLength",
					CalculateMrzWindowScore(Normalized) * 0.94f);
				return Candidates
					.OrderByDescending(static Candidate => Candidate.Score)
					.Take(MaxLineLengthVariants)
					.ToList();
			}

			AddLineWindowCandidate(Candidates, Normalized, 0, Length, "TrimLineStart");
			AddLineWindowCandidate(Candidates, Normalized, Normalized.Length - Length, Length, "TrimLineEnd");
			AddLineWindowCandidate(Candidates, Normalized, Math.Max(0, (Normalized.Length - Length) / 2), Length, "TrimLineCenter");

			List<MrzLineNormalizationCandidate> SlidingCandidates = new List<MrzLineNormalizationCandidate>();
			for (int StartIndex = 0; StartIndex <= Normalized.Length - Length; StartIndex++)
				AddLineWindowCandidate(SlidingCandidates, Normalized, StartIndex, Length, CreateLineWindowTransformation(StartIndex, StartIndex + Length - 1));

			foreach (MrzLineNormalizationCandidate Candidate in SlidingCandidates
				.OrderByDescending(static Candidate => Candidate.Score)
				.Take(MaxLineLengthVariants))
			{
				AddLineLengthCandidate(Candidates, Candidate.Text, Candidate.Transformation, Candidate.Score);
			}

			return Candidates
				.OrderByDescending(static Candidate => Candidate.Score)
				.Take(MaxLineLengthVariants)
				.ToList();
		}

		private static List<MrzLineCombination> CreateFlattenedBlockCandidates(string Flattened, int LineLength, int LineCount)
		{
			int TotalLength = LineLength * LineCount;
			List<MrzLineCombination> Candidates = new List<MrzLineCombination>();
			if (Flattened.Length < TotalLength)
				return Candidates;

			AddFlattenedBlockCandidate(Candidates, Flattened, 0, LineLength, LineCount, "FlattenedBlockStart");
			AddFlattenedBlockCandidate(Candidates, Flattened, Flattened.Length - TotalLength, LineLength, LineCount, "FlattenedBlockEnd");
			AddFlattenedBlockCandidate(Candidates, Flattened, Math.Max(0, (Flattened.Length - TotalLength) / 2), LineLength, LineCount, "FlattenedBlockCenter");

			List<MrzLineCombination> SlidingCandidates = new List<MrzLineCombination>();
			for (int StartIndex = 0; StartIndex <= Flattened.Length - TotalLength; StartIndex++)
				AddFlattenedBlockCandidate(SlidingCandidates, Flattened, StartIndex, LineLength, LineCount, CreateLineWindowTransformation(StartIndex, StartIndex + TotalLength - 1));

			foreach (MrzLineCombination Candidate in SlidingCandidates
				.OrderByDescending(static Candidate => Candidate.Score)
				.Take(MaxFlattenedBlockCandidates))
			{
				AddLineCombination(Candidates, Candidate);
			}

			return Candidates
				.OrderByDescending(static Candidate => Candidate.Score)
				.Take(MaxFlattenedBlockCandidates)
				.ToList();
		}

		private static List<MrzLineCombination> CreateLineVariantCombinations(
			IReadOnlyList<List<MrzLineNormalizationCandidate>> VariantSets,
			int MaximumCombinationCount)
		{
			List<MrzLineCombination> Combinations = new List<MrzLineCombination>
			{
				new MrzLineCombination(Array.Empty<string>(), Array.Empty<string>(), 0f)
			};

			foreach (List<MrzLineNormalizationCandidate> VariantSet in VariantSets)
			{
				List<MrzLineCombination> NextCombinations = new List<MrzLineCombination>();
				foreach (MrzLineCombination Combination in Combinations)
				{
					foreach (MrzLineNormalizationCandidate Variant in VariantSet)
					{
						List<string> Lines = new List<string>(Combination.Lines) { Variant.Text };
						List<string> Transformations = new List<string>(Combination.Transformations);
						if (!string.IsNullOrWhiteSpace(Variant.Transformation))
							Transformations.Add(Variant.Transformation);

						NextCombinations.Add(new MrzLineCombination(
							Lines,
							Transformations,
							Combination.Score + Variant.Score));
					}
				}

				Combinations = NextCombinations
					.OrderByDescending(static Combination => Combination.Score)
					.Take(MaximumCombinationCount)
					.ToList();
			}

			return Combinations;
		}

		private static void AddLineWindowCandidate(
			List<MrzLineNormalizationCandidate> Candidates,
			string Normalized,
			int StartIndex,
			int Length,
			string Transformation)
		{
			string Text = Normalized.Substring(StartIndex, Length);
			AddLineLengthCandidate(Candidates, Text, Transformation, CalculateMrzWindowScore(Text));
		}

		private static void AddLineLengthCandidate(
			List<MrzLineNormalizationCandidate> Candidates,
			string Text,
			string Transformation,
			float Score)
		{
			if (Candidates.Any(Candidate => string.Equals(Candidate.Text, Text, StringComparison.Ordinal)))
				return;

			Candidates.Add(new MrzLineNormalizationCandidate(Text, Transformation, Score));
		}

		private static void AddFlattenedBlockCandidate(
			List<MrzLineCombination> Candidates,
			string Flattened,
			int StartIndex,
			int LineLength,
			int LineCount,
			string Transformation)
		{
			List<string> Lines = new List<string>(LineCount);
			float Score = 0f;
			for (int LineIndex = 0; LineIndex < LineCount; LineIndex++)
			{
				int LineStart = StartIndex + (LineIndex * LineLength);
				string Line = Flattened.Substring(LineStart, LineLength);
				Lines.Add(Line);
				Score += CalculateMrzWindowScore(Line);
			}

			AddLineCombination(Candidates, new MrzLineCombination(Lines, new[] { Transformation }, Score));
		}

		private static void AddLineCombination(List<MrzLineCombination> Candidates, MrzLineCombination Candidate)
		{
			string Key = string.Join('\n', Candidate.Lines);
			if (Candidates.Any(Existing => string.Equals(string.Join('\n', Existing.Lines), Key, StringComparison.Ordinal)))
				return;

			Candidates.Add(Candidate);
		}

		private static float CalculateMrzWindowScore(string Value)
		{
			if (string.IsNullOrEmpty(Value))
				return 0f;

			int ValidCount = 0;
			int FillerCount = 0;
			int DigitCount = 0;
			int LetterCount = 0;
			foreach (char Character in Value)
			{
				if (Character == '<')
				{
					ValidCount++;
					FillerCount++;
				}
				else if (Character >= '0' && Character <= '9')
				{
					ValidCount++;
					DigitCount++;
				}
				else if (Character >= 'A' && Character <= 'Z')
				{
					ValidCount++;
					LetterCount++;
				}
			}

			float Length = Value.Length;
			float ValidRatio = ValidCount / Length;
			float CharacterMixScore = Math.Clamp(((FillerCount + DigitCount + LetterCount) / Length), 0f, 1f);
			return (ValidRatio * 0.82f) + (CharacterMixScore * 0.18f);
		}

		private static string CreateLineWindowTransformation(int StartIndex, int EndIndex)
		{
			return $"SelectLineCharacters[{StartIndex}-{EndIndex}]";
		}

		private static char NormalizeMrzCharacter(char Character, bool PreserveNewLine)
		{
			if (PreserveNewLine && Character == '\n')
				return '\n';
			if (char.IsWhiteSpace(Character))
				return '<';
			if (Character == '<' || Character == '«' || Character == '‹' || Character == '›')
				return '<';
			if ((Character >= 'A' && Character <= 'Z') || (Character >= '0' && Character <= '9'))
				return Character;
			return Character;
		}

		private static char NormalizeCheckDigitChar(char Character)
		{
			char Normalized = NormalizeMrzCharacter(Character, false);
			if (Normalized >= '0' && Normalized <= '9')
				return Normalized;
			if (DigitConfusions.TryGetValue(Normalized, out char[]? Alternatives))
				return Alternatives[0];
			return Normalized;
		}

		private static string ReplaceSubstring(string Value, int Start, int Length, string Replacement)
		{
			return Value.Substring(0, Start) + Replacement + Value.Substring(Start + Length);
		}

		private static string ReplaceCharacter(string Value, int Index, char Replacement)
		{
			char[] Buffer = Value.ToCharArray();
			Buffer[Index] = Replacement;
			return new string(Buffer);
		}

		private sealed class MrzLineNormalizationCandidate
		{
			public MrzLineNormalizationCandidate(string Text, string Transformation, float Score)
			{
				this.Text = Text;
				this.Transformation = Transformation;
				this.Score = Score;
			}

			public string Text { get; }

			public string Transformation { get; }

			public float Score { get; }
		}

		private sealed class MrzLineCombination
		{
			public MrzLineCombination(IReadOnlyList<string> Lines, IReadOnlyList<string> Transformations, float Score)
			{
				this.Lines = Lines;
				this.Transformations = Transformations;
				this.Score = Score;
			}

			public IReadOnlyList<string> Lines { get; }

			public IReadOnlyList<string> Transformations { get; }

			public float Score { get; }
		}

		internal sealed class MrzDecodedCandidate
		{
			public MrzDecodedCandidate(
				MrzFormat Format,
				string Text,
				IReadOnlyList<string> Lines,
				string Source,
				IReadOnlyList<string> AppliedTransformations,
				MrzAcceptanceClassification AcceptanceClassification,
				bool ParserValidated,
				int PassedRequiredCheckCount,
				int RepairCount,
				float Score,
				IReadOnlyDictionary<string, string> Metadata,
				DocumentInformation? ParsedInformation)
			{
				this.Format = Format;
				this.Text = Text;
				this.Lines = Lines;
				this.Source = Source;
				this.AppliedTransformations = AppliedTransformations;
				this.AcceptanceClassification = AcceptanceClassification;
				this.ParserValidated = ParserValidated;
				this.PassedRequiredCheckCount = PassedRequiredCheckCount;
				this.RepairCount = RepairCount;
				this.Score = Score;
				this.Metadata = Metadata;
				this.ParsedInformation = ParsedInformation;
			}

			public MrzFormat Format { get; }

			public string Text { get; }

			public IReadOnlyList<string> Lines { get; }

			public string Source { get; }

			public IReadOnlyList<string> AppliedTransformations { get; }

			public MrzAcceptanceClassification AcceptanceClassification { get; }

			public bool ParserValidated { get; }

			public int PassedRequiredCheckCount { get; }

			public int RepairCount { get; }

			public float Score { get; }

			public IReadOnlyDictionary<string, string> Metadata { get; }

			public DocumentInformation? ParsedInformation { get; }

			public int AcceptanceRank => this.AcceptanceClassification switch
			{
				MrzAcceptanceClassification.Accepted => 2,
				MrzAcceptanceClassification.ReviewRequired => 1,
				_ => 0
			};
		}

		internal enum MrzAcceptanceClassification
		{
			Accepted = 0,
			ReviewRequired = 1,
			Rejected = 2
		}

		internal enum MrzFormat
		{
			Td1 = 0,
			Td2 = 1,
			Td3 = 2
		}

		private enum MrzCharacterClass
		{
			Digits = 0,
			CheckDigit = 1,
			Letters = 2,
			Names = 3,
			Sex = 4,
			AlphaNumericFiller = 5
		}

		private sealed class MrzFormatShape
		{
			public MrzFormatShape(MrzFormat Format, int LineLength, int LineCount)
			{
				this.Format = Format;
				this.LineLength = LineLength;
				this.LineCount = LineCount;
			}

			public MrzFormat Format { get; }

			public int LineLength { get; }

			public int LineCount { get; }
		}

		private sealed class MrzSourceCandidate
		{
			public MrzSourceCandidate(
				MrzFormat Format,
				IReadOnlyList<string> Lines,
				string Source,
				IReadOnlyList<StructuredTextObservation> Observations,
				IReadOnlyList<string> AppliedTransformations,
				int? WindowStartIndex,
				int? WindowEndIndex,
				int WindowTotalCount)
			{
				this.Format = Format;
				this.Lines = Lines;
				this.Source = Source;
				this.Observations = Observations;
				this.AppliedTransformations = AppliedTransformations;
				this.WindowStartIndex = WindowStartIndex;
				this.WindowEndIndex = WindowEndIndex;
				this.WindowTotalCount = WindowTotalCount;
			}

			public MrzFormat Format { get; }

			public IReadOnlyList<string> Lines { get; }

			public string Text => string.Join("\n", this.Lines);

			public string Source { get; }

			public IReadOnlyList<StructuredTextObservation> Observations { get; }

			public IReadOnlyList<string> AppliedTransformations { get; }

			public int? WindowStartIndex { get; }

			public int? WindowEndIndex { get; }

			public int WindowTotalCount { get; }
		}

		private sealed class FieldRepairCandidate
		{
			public FieldRepairCandidate(string Text, float Cost, int Edits)
			{
				this.Text = Text;
				this.Cost = Cost;
				this.Edits = Edits;
			}

			public string Text { get; }

			public float Cost { get; }

			public int Edits { get; }
		}
	}
}
