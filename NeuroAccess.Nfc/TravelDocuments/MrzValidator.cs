using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Identifies the supported MRZ layouts.
	/// </summary>
	public enum MrzLayout
	{
		/// <summary>
		/// TD1 layout with three 30-character lines.
		/// </summary>
		Td1 = 0,

		/// <summary>
		/// TD2 layout with two 36-character lines.
		/// </summary>
		Td2 = 1,

		/// <summary>
		/// TD3 layout with two 44-character lines.
		/// </summary>
		Td3 = 2
	}

	/// <summary>
	/// Represents the outcome of MRZ parsing and validation.
	/// </summary>
	public sealed class MrzValidationResult
	{
		internal MrzValidationResult(
			bool IsParsed,
			MrzLayout? Layout,
			string NormalizedMrz,
			DocumentInformation? DocumentInformation,
			bool DocumentNumberCheckPassed,
			bool DateOfBirthCheckPassed,
			bool ExpiryCheckPassed,
			bool CompositeCheckPassed,
			int RequiredCheckCount,
			int PassedRequiredCheckCount,
			string? FailureReason)
		{
			this.IsParsed = IsParsed;
			this.Layout = Layout;
			this.NormalizedMrz = NormalizedMrz;
			this.DocumentInformation = DocumentInformation;
			this.DocumentNumberCheckPassed = DocumentNumberCheckPassed;
			this.DateOfBirthCheckPassed = DateOfBirthCheckPassed;
			this.ExpiryCheckPassed = ExpiryCheckPassed;
			this.CompositeCheckPassed = CompositeCheckPassed;
			this.RequiredCheckCount = RequiredCheckCount;
			this.PassedRequiredCheckCount = PassedRequiredCheckCount;
			this.FailureReason = FailureReason;
		}

		/// <summary>
		/// Gets a value indicating whether the MRZ parsed into a document model.
		/// </summary>
		public bool IsParsed { get; }

		/// <summary>
		/// Gets the detected MRZ layout, when one could be determined.
		/// </summary>
		public MrzLayout? Layout { get; }

		/// <summary>
		/// Gets the normalized MRZ text used for validation.
		/// </summary>
		public string NormalizedMrz { get; }

		/// <summary>
		/// Gets the parsed document information when parsing succeeded.
		/// </summary>
		public DocumentInformation? DocumentInformation { get; }

		/// <summary>
		/// Gets a value indicating whether the document-number check digit passed.
		/// </summary>
		public bool DocumentNumberCheckPassed { get; }

		/// <summary>
		/// Gets a value indicating whether the date-of-birth check digit passed.
		/// </summary>
		public bool DateOfBirthCheckPassed { get; }

		/// <summary>
		/// Gets a value indicating whether the expiry-date check digit passed.
		/// </summary>
		public bool ExpiryCheckPassed { get; }

		/// <summary>
		/// Gets a value indicating whether the layout composite or overall check passed.
		/// </summary>
		public bool CompositeCheckPassed { get; }

		/// <summary>
		/// Gets the number of required checks for the detected layout.
		/// </summary>
		public int RequiredCheckCount { get; }

		/// <summary>
		/// Gets the number of required checks that passed.
		/// </summary>
		/// <remarks>
		/// This count covers the document-number, date-of-birth, expiry-date, and composite checks.
		/// </remarks>
		public int PassedRequiredCheckCount { get; }

		/// <summary>
		/// Gets the validation failure reason when parsing was unsuccessful.
		/// </summary>
		public string? FailureReason { get; }
	}

	/// <summary>
	/// Provides shared MRZ parsing and validation services for OCR and NFC flows.
	/// </summary>
	public static class MrzValidator
	{
		private static readonly IReadOnlyDictionary<char, char> CheckDigitConfusions = new Dictionary<char, char>
		{
			{ 'O', '0' },
			{ 'Q', '0' },
			{ 'I', '1' },
			{ 'L', '1' },
			{ 'Z', '2' },
			{ 'S', '5' },
			{ 'B', '8' },
			{ 'G', '6' },
			{ '<', '0' }
		};

		private static readonly Regex Td2MrzNumber9CharsPlus = new(@"^(?'DocType'.{1,2})<(?'Issuer'[A-Z<]{3})(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*)<*\n(?'Nr1'.{9})<(?'Nationality'[A-Z<]{3})(?'Birth'.{6})(?'BirthCheck'\d)(?'Gender'[MF<])(?'Expires'.{6})(?'ExpiryCheck'\d)(?'Tail'.{7})(?'OverallCheck'\d)$", RegexOptions.Multiline);
		private static readonly Regex Td2MrzNumber9Chars = new(@"^(?'DocType'.{1,2})<(?'Issuer'[A-Z<]{3})(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*)<*\n(?'Nr'.{9})(?'NrCheck'\d)(?'Nationality'[A-Z<]{3})(?'Birth'.{6})(?'BirthCheck'\d)(?'Gender'[MF<])(?'Expires'.{6})(?'ExpiryCheck'\d)(?'Optional'.{7})(?'OverallCheck'\d)$", RegexOptions.Multiline);
		private static readonly Regex Td1MrzNumber9CharsPlus = new(@"^(?'DocType'.{1,2})<(?'Issuer'[A-Z<]{3})(?'Nr1'.{9})<(?'Nr2'.{3})(?'NrCheck'\d)(?'Optional1'.{11})\n(?'Birth'.{6})(?'BirthCheck'\d)(?'Gender'[MF<])(?'Expires'.{6})(?'ExpiryCheck'\d)(?'Nationality'[A-Z<]{3})(?'Optional2'.{11})(?'OverallCheck'\d)\n(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*).*$", RegexOptions.Multiline);
		private static readonly Regex Td1MrzNumber9Chars = new(@"^(?'DocType'.{1,2})<(?'Issuer'[A-Z<]{3})(?'Nr'.{9})(?'NrCheck'.)(?'Optional1'.{15})\n(?'Birth'[^<]{6})(?'BirthCheck'\d)(?'Gender'[MF<])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)(?'Nationality'[A-Z<]{3})(?'Optional2'.{11})(?'OverallCheck'\d)\n(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*).*$", RegexOptions.Multiline);
		private static readonly Regex Td3MrzNumber9Chars = new(@"^(?'DocType'.{1,2})<(?'Issuer'[A-Z<]{3})(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*)<*\n(?'Nr'.{9})(?'NrCheck'\d)(?'Nationality'[A-Z<]{3})(?'Birth'[^<]{6})(?'BirthCheck'\d)(?'Gender'[MF<])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)(?'Optional'.{14})(?'OptionalCheck'\d)(?'OverallCheck'\d)$", RegexOptions.Multiline);

		private static readonly ParserDefinition[] OrderedPatterns =
		[
			new ParserDefinition(MrzLayout.Td2, Td2MrzNumber9CharsPlus, AssembleTd2Info2),
			new ParserDefinition(MrzLayout.Td2, Td2MrzNumber9Chars, AssembleTd2Info1),
			new ParserDefinition(MrzLayout.Td3, Td3MrzNumber9Chars, AssembleTd3Info1),
			new ParserDefinition(MrzLayout.Td1, Td1MrzNumber9CharsPlus, AssembleTd1Info2),
			new ParserDefinition(MrzLayout.Td1, Td1MrzNumber9Chars, AssembleTd1Info1)
		];

		/// <summary>
		/// Attempts to parse MRZ text into document information.
		/// </summary>
		/// <param name="Mrz">The MRZ text to parse.</param>
		/// <param name="Info">The parsed document information.</param>
		/// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
		public static bool TryParse(string Mrz, [NotNullWhen(true)] out DocumentInformation? Info)
		{
			MrzValidationResult Validation = Validate(Mrz);
			Info = Validation.DocumentInformation;
			return Validation.IsParsed;
		}

		/// <summary>
		/// Validates MRZ text and returns detailed check information.
		/// </summary>
		/// <param name="Mrz">The MRZ text to validate.</param>
		/// <returns>The validation result.</returns>
		public static MrzValidationResult Validate(string Mrz)
		{
			ArgumentNullException.ThrowIfNull(Mrz);

			string NormalizedMrz = NormalizeMrz(Mrz);
			string[] Lines = SplitLines(NormalizedMrz);
			MrzLayout? Layout = TryDetectLayout(Lines);
			int RequiredCheckCount = Layout.HasValue ? GetRequiredCheckCount(Layout.Value) : 0;
			int PassedRequiredCheckCount = 0;
			bool DocumentNumberCheckPassed = false;
			bool DateOfBirthCheckPassed = false;
			bool ExpiryCheckPassed = false;
			bool CompositeCheckPassed = false;

			if (Layout.HasValue)
			{
				PassedRequiredCheckCount = CountPassedRequiredChecks(
					Layout.Value,
					Lines,
					out DocumentNumberCheckPassed,
					out DateOfBirthCheckPassed,
					out ExpiryCheckPassed,
					out CompositeCheckPassed);
			}

			DocumentInformation? ParsedInformation;
			MrzLayout? ParsedLayout;
			if (Layout.HasValue)
			{
				if (TryParseStandard(Layout.Value, Lines, out ParsedInformation)
					&& IsSemanticallyValid(Layout.Value, ParsedInformation))
				{
					return new MrzValidationResult(
						true,
						Layout,
						NormalizedMrz,
						ParsedInformation,
						DocumentNumberCheckPassed,
						DateOfBirthCheckPassed,
						ExpiryCheckPassed,
						CompositeCheckPassed,
						RequiredCheckCount,
						PassedRequiredCheckCount,
						null);
				}

				if (TryParseFallback(NormalizedMrz, Layout, out ParsedInformation, out ParsedLayout)
					&& ParsedLayout.HasValue
					&& IsSemanticallyValid(ParsedLayout.Value, ParsedInformation))
				{
					return new MrzValidationResult(
						true,
						ParsedLayout,
						NormalizedMrz,
						ParsedInformation,
						DocumentNumberCheckPassed,
						DateOfBirthCheckPassed,
						ExpiryCheckPassed,
						CompositeCheckPassed,
						RequiredCheckCount,
						PassedRequiredCheckCount,
						null);
				}

				return new MrzValidationResult(
					false,
					Layout,
					NormalizedMrz,
					null,
					DocumentNumberCheckPassed,
					DateOfBirthCheckPassed,
					ExpiryCheckPassed,
					CompositeCheckPassed,
					RequiredCheckCount,
					PassedRequiredCheckCount,
					"StrictLayoutValidationFailed");
			}

			if (TryParseFallback(NormalizedMrz, null, out ParsedInformation, out ParsedLayout)
				&& ParsedLayout.HasValue
				&& IsSemanticallyValid(ParsedLayout.Value, ParsedInformation))
			{
				return new MrzValidationResult(
					true,
					ParsedLayout,
					NormalizedMrz,
					ParsedInformation,
					DocumentNumberCheckPassed,
					DateOfBirthCheckPassed,
					ExpiryCheckPassed,
					CompositeCheckPassed,
					RequiredCheckCount,
					PassedRequiredCheckCount,
					null);
			}

			return new MrzValidationResult(
				false,
				Layout,
				NormalizedMrz,
				null,
				DocumentNumberCheckPassed,
				DateOfBirthCheckPassed,
				ExpiryCheckPassed,
				CompositeCheckPassed,
				RequiredCheckCount,
				PassedRequiredCheckCount,
				Layout.HasValue ? "ValidationFailed" : "LayoutNotRecognized");
		}

		/// <summary>
		/// Calculates the MRZ check digit for a value.
		/// </summary>
		/// <param name="Value">The value to evaluate.</param>
		/// <returns>The calculated check digit, or the null character if the value contains unsupported characters.</returns>
		public static char CalculateCheckDigit(string Value)
		{
			string CheckDigit = CalculateCheckDigitText(Value);
			return CheckDigit.Length == 1 ? CheckDigit[0] : '\0';
		}

		private static bool TryParseFallback(
			string NormalizedMrz,
			MrzLayout? ExpectedLayout,
			[NotNullWhen(true)] out DocumentInformation? Info,
			out MrzLayout? Layout)
		{
			foreach (ParserDefinition Definition in OrderedPatterns)
			{
				if (ExpectedLayout.HasValue && Definition.Layout != ExpectedLayout.Value)
					continue;

				Match Match = Definition.Pattern.Match(NormalizedMrz);
				if (!Match.Success)
					continue;

				DocumentInformation? Candidate = Definition.Factory(Match);
				if (Candidate is null)
					continue;

				Layout = Definition.Layout;
				Info = Candidate;
				return true;
			}

			Info = null;
			Layout = null;
			return false;
		}

		private static bool IsSemanticallyValid(MrzLayout Layout, DocumentInformation Info)
		{
			return Layout switch
			{
				MrzLayout.Td1 => IsSemanticallyValidTd1(Info),
				MrzLayout.Td2 => IsSemanticallyValidTd2(Info),
				MrzLayout.Td3 => IsSemanticallyValidTd3(Info),
				_ => false
			};
		}

		private static bool IsSemanticallyValidTd1(DocumentInformation Info)
		{
			return HasValidDocumentType(Info.DocumentType)
				&& HasValidCountryCode(Info.IssuingState)
				&& HasValidDocumentNumber(Info.DocumentNumber)
				&& HasValidMrzDate(Info.DateOfBirth)
				&& HasValidGender(Info.Gender)
				&& HasValidMrzDate(Info.ExpiryDate)
				&& HasValidCountryCode(Info.Nationality)
				&& HasValidIdentifiers(Info.PrimaryIdentifier, Info.SecondaryIdentifier)
				&& HasValidMrzInformation(Info.MRZ_Information);
		}

		private static bool IsSemanticallyValidTd2(DocumentInformation Info)
		{
			return HasValidDocumentType(Info.DocumentType)
				&& HasValidCountryCode(Info.IssuingState)
				&& HasValidDocumentNumber(Info.DocumentNumber)
				&& HasValidCountryCode(Info.Nationality)
				&& HasValidMrzDate(Info.DateOfBirth)
				&& HasValidGender(Info.Gender)
				&& HasValidMrzDate(Info.ExpiryDate)
				&& HasValidIdentifiers(Info.PrimaryIdentifier, Info.SecondaryIdentifier)
				&& HasValidMrzInformation(Info.MRZ_Information);
		}

		private static bool IsSemanticallyValidTd3(DocumentInformation Info)
		{
			return HasValidDocumentType(Info.DocumentType)
				&& HasValidCountryCode(Info.IssuingState)
				&& HasValidDocumentNumber(Info.DocumentNumber)
				&& HasValidCountryCode(Info.Nationality)
				&& HasValidMrzDate(Info.DateOfBirth)
				&& HasValidGender(Info.Gender)
				&& HasValidMrzDate(Info.ExpiryDate)
				&& HasValidIdentifiers(Info.PrimaryIdentifier, Info.SecondaryIdentifier)
				&& HasValidMrzInformation(Info.MRZ_Information);
		}

		private static bool HasValidDocumentType(string? Value)
		{
			return !string.IsNullOrWhiteSpace(Value)
				&& Value.Length <= 2
				&& Value.All(static Character => Character is >= 'A' and <= 'Z');
		}

		private static bool HasValidCountryCode(string? Value)
		{
			return !string.IsNullOrWhiteSpace(Value)
				&& Value.Length == 3
				&& Value.All(static Character => Character is >= 'A' and <= 'Z');
		}

		private static bool HasValidDocumentNumber(string? Value)
		{
			return !string.IsNullOrWhiteSpace(Value)
				&& Value.All(static Character => (Character is >= 'A' and <= 'Z') || (Character is >= '0' and <= '9'));
		}

		private static bool HasValidGender(string? Value)
		{
			return Value is "M" or "F" or "<";
		}

		private static bool HasValidMrzDate(string? Value)
		{
			if (string.IsNullOrWhiteSpace(Value) || Value.Length != 6 || !Value.All(static Character => Character is >= '0' and <= '9'))
				return false;

			if (Value == "000000")
				return false;

			int Month = int.Parse(Value.Substring(2, 2), System.Globalization.CultureInfo.InvariantCulture);
			int Day = int.Parse(Value.Substring(4, 2), System.Globalization.CultureInfo.InvariantCulture);
			if (Month < 1 || Month > 12)
				return false;
			if (Day < 1)
				return false;

			int MaxDay = Month switch
			{
				4 or 6 or 9 or 11 => 30,
				2 => 29,
				_ => 31
			};

			return Day <= MaxDay;
		}

		private static bool HasValidIdentifiers(string[]? PrimaryIdentifier, string[]? SecondaryIdentifier)
		{
			return HasValidIdentifierParts(PrimaryIdentifier, true)
				&& HasValidIdentifierParts(SecondaryIdentifier, false);
		}

		private static bool HasValidIdentifierParts(string[]? Parts, bool Required)
		{
			if (Parts is null || Parts.Length == 0)
				return !Required;

			bool HasContent = false;
			foreach (string Part in Parts)
			{
				if (string.IsNullOrWhiteSpace(Part))
					continue;

				if (!Part.All(static Character => Character is >= 'A' and <= 'Z'))
					return false;

				HasContent = true;
			}

			return Required ? HasContent : true;
		}

		private static bool HasValidMrzInformation(string? Value)
		{
			return !string.IsNullOrWhiteSpace(Value)
				&& Value.All(static Character => (Character is >= 'A' and <= 'Z') || (Character is >= '0' and <= '9') || Character == '<');
		}

		private static bool TryParseStandard(
			MrzLayout Layout,
			string[] Lines,
			[NotNullWhen(true)] out DocumentInformation? Info)
		{
			switch (Layout)
			{
				case MrzLayout.Td1:
					return TryParseTd1Standard(Lines, out Info);

				case MrzLayout.Td2:
					return TryParseTd2Standard(Lines, out Info);

				case MrzLayout.Td3:
					return TryParseTd3Standard(Lines, out Info);

				default:
					Info = null;
					return false;
			}
		}

		private static bool TryParseTd1Standard(string[] Lines, [NotNullWhen(true)] out DocumentInformation? Info)
		{
			if (Lines.Length != 3 || Lines[0].Length != 30 || Lines[1].Length != 30 || Lines[2].Length != 30)
			{
				Info = null;
				return false;
			}

			string Line1 = Lines[0];
			string Line2 = Lines[1];
			string Line3 = Lines[2];
			if (Line1[14] == '<')
			{
				Info = null;
				return false;
			}

			string DocumentNumber = Line1.Substring(5, 9);
			if (!IsCheckDigitValid(DocumentNumber, Line1[14]))
			{
				Info = null;
				return false;
			}

			string DateOfBirth = Line2.Substring(0, 6);
			if (!IsCheckDigitValid(DateOfBirth, Line2[6]))
			{
				Info = null;
				return false;
			}

			string ExpiryDate = Line2.Substring(8, 6);
			if (!IsCheckDigitValid(ExpiryDate, Line2[14]))
			{
				Info = null;
				return false;
			}

			string Composite = Line1.Substring(5, 10) + Line1.Substring(15, 15) + Line2.Substring(0, 7) + Line2.Substring(8, 7) + Line2.Substring(18, 11);
			if (!IsCheckDigitValid(Composite, Line2[29]))
			{
				Info = null;
				return false;
			}

			Info = new DocumentInformation
			{
				DocumentType = ExtractDocumentType(Line1.Substring(0, 2)),
				IssuingState = Line1.Substring(2, 3),
				DocumentNumber = DocumentNumber.Replace("<", string.Empty),
				DateOfBirth = DateOfBirth,
				Gender = Line2.Substring(7, 1),
				ExpiryDate = ExpiryDate,
				Nationality = Line2.Substring(15, 3),
				OptionalData = (Line1.Substring(15, 15) + Line2.Substring(18, 11)).Replace("<", string.Empty),
				MRZ_Information = DocumentNumber + Line1[14] + DateOfBirth + Line2[6] + ExpiryDate + Line2[14]
			};
			PopulateNames(Info, Line3);
			return true;
		}

		private static bool TryParseTd2Standard(string[] Lines, [NotNullWhen(true)] out DocumentInformation? Info)
		{
			if (Lines.Length != 2 || Lines[0].Length != 36 || Lines[1].Length != 36)
			{
				Info = null;
				return false;
			}

			string Line1 = Lines[0];
			string Line2 = Lines[1];
			string DocumentNumber = Line2.Substring(0, 9);
			string DateOfBirth = Line2.Substring(13, 6);
			string ExpiryDate = Line2.Substring(21, 6);
			if (!IsCheckDigitValid(DocumentNumber, Line2[9])
				|| !IsCheckDigitValid(DateOfBirth, Line2[19])
				|| !IsCheckDigitValid(ExpiryDate, Line2[27]))
			{
				Info = null;
				return false;
			}

			string Composite = Line2.Substring(0, 10) + Line2.Substring(13, 7) + Line2.Substring(21, 7) + Line2.Substring(28, 7);
			if (!IsCheckDigitValid(Composite, Line2[35]))
			{
				Info = null;
				return false;
			}

			Info = new DocumentInformation
			{
				DocumentType = ExtractDocumentType(Line1.Substring(0, 2)),
				IssuingState = Line1.Substring(2, 3),
				DocumentNumber = DocumentNumber.Replace("<", string.Empty),
				Nationality = Line2.Substring(10, 3),
				DateOfBirth = DateOfBirth,
				Gender = Line2.Substring(20, 1),
				ExpiryDate = ExpiryDate,
				OptionalData = Line2.Substring(28, 7).Replace("<", string.Empty),
				MRZ_Information = DocumentNumber + Line2[9] + DateOfBirth + Line2[19] + ExpiryDate + Line2[27]
			};
			PopulateNames(Info, Line1.Substring(5));
			return true;
		}

		private static bool TryParseTd3Standard(string[] Lines, [NotNullWhen(true)] out DocumentInformation? Info)
		{
			if (Lines.Length != 2 || Lines[0].Length != 44 || Lines[1].Length != 44)
			{
				Info = null;
				return false;
			}

			string Line1 = Lines[0];
			string Line2 = Lines[1];
			string DocumentNumber = Line2.Substring(0, 9);
			string DateOfBirth = Line2.Substring(13, 6);
			string ExpiryDate = Line2.Substring(21, 6);
			string OptionalData = Line2.Substring(28, 14);
			if (!IsCheckDigitValid(DocumentNumber, Line2[9])
				|| !IsCheckDigitValid(DateOfBirth, Line2[19])
				|| !IsCheckDigitValid(ExpiryDate, Line2[27])
				|| !IsCheckDigitValid(OptionalData, Line2[42]))
			{
				Info = null;
				return false;
			}

			string Composite = Line2.Substring(0, 10) + Line2.Substring(13, 7) + Line2.Substring(21, 7) + Line2.Substring(28, 15);
			if (!IsCheckDigitValid(Composite, Line2[43]))
			{
				Info = null;
				return false;
			}

			Info = new DocumentInformation
			{
				DocumentType = ExtractDocumentType(Line1.Substring(0, 2)),
				IssuingState = Line1.Substring(2, 3),
				DocumentNumber = DocumentNumber.Replace("<", string.Empty),
				Nationality = Line2.Substring(10, 3),
				DateOfBirth = DateOfBirth,
				Gender = Line2.Substring(20, 1),
				ExpiryDate = ExpiryDate,
				OptionalData = OptionalData.Replace("<", string.Empty),
				MRZ_Information = DocumentNumber + Line2[9] + DateOfBirth + Line2[19] + ExpiryDate + Line2[27]
			};
			PopulateNames(Info, Line1.Substring(5));
			return true;
		}

		private static int CountPassedRequiredChecks(
			MrzLayout Layout,
			string[] Lines,
			out bool DocumentNumberCheckPassed,
			out bool DateOfBirthCheckPassed,
			out bool ExpiryCheckPassed,
			out bool CompositeCheckPassed)
		{
			switch (Layout)
			{
				case MrzLayout.Td1:
					DocumentNumberCheckPassed = Lines.Length >= 1 && Lines[0].Length >= 15 && IsCheckDigitValid(Lines[0].Substring(5, 9), Lines[0][14]);
					DateOfBirthCheckPassed = Lines.Length >= 2 && Lines[1].Length >= 7 && IsCheckDigitValid(Lines[1].Substring(0, 6), Lines[1][6]);
					ExpiryCheckPassed = Lines.Length >= 2 && Lines[1].Length >= 15 && IsCheckDigitValid(Lines[1].Substring(8, 6), Lines[1][14]);
					CompositeCheckPassed = Lines.Length >= 2 && Lines[0].Length == 30 && Lines[1].Length == 30
						&& IsCheckDigitValid(Lines[0].Substring(5, 10) + Lines[0].Substring(15, 15) + Lines[1].Substring(0, 7) + Lines[1].Substring(8, 7) + Lines[1].Substring(18, 11), Lines[1][29]);
					break;

				case MrzLayout.Td2:
					DocumentNumberCheckPassed = Lines.Length >= 2 && Lines[1].Length >= 10 && IsCheckDigitValid(Lines[1].Substring(0, 9), Lines[1][9]);
					DateOfBirthCheckPassed = Lines.Length >= 2 && Lines[1].Length >= 20 && IsCheckDigitValid(Lines[1].Substring(13, 6), Lines[1][19]);
					ExpiryCheckPassed = Lines.Length >= 2 && Lines[1].Length >= 28 && IsCheckDigitValid(Lines[1].Substring(21, 6), Lines[1][27]);
					CompositeCheckPassed = Lines.Length >= 2 && Lines[1].Length == 36
						&& IsCheckDigitValid(Lines[1].Substring(0, 10) + Lines[1].Substring(13, 7) + Lines[1].Substring(21, 7) + Lines[1].Substring(28, 7), Lines[1][35]);
					break;

				default:
					DocumentNumberCheckPassed = Lines.Length >= 2 && Lines[1].Length >= 10 && IsCheckDigitValid(Lines[1].Substring(0, 9), Lines[1][9]);
					DateOfBirthCheckPassed = Lines.Length >= 2 && Lines[1].Length >= 20 && IsCheckDigitValid(Lines[1].Substring(13, 6), Lines[1][19]);
					ExpiryCheckPassed = Lines.Length >= 2 && Lines[1].Length >= 28 && IsCheckDigitValid(Lines[1].Substring(21, 6), Lines[1][27]);
					CompositeCheckPassed = Lines.Length >= 2 && Lines[1].Length == 44
						&& IsCheckDigitValid(Lines[1].Substring(0, 10) + Lines[1].Substring(13, 7) + Lines[1].Substring(21, 7) + Lines[1].Substring(28, 15), Lines[1][43]);
					break;
			}

			int Count = 0;
			if (DocumentNumberCheckPassed)
				Count++;
			if (DateOfBirthCheckPassed)
				Count++;
			if (ExpiryCheckPassed)
				Count++;
			if (CompositeCheckPassed)
				Count++;
			return Count;
		}

		private static int GetRequiredCheckCount(MrzLayout Layout)
		{
			return Layout switch
			{
				MrzLayout.Td1 => 4,
				MrzLayout.Td2 => 4,
				MrzLayout.Td3 => 4,
				_ => 0
			};
		}

		private static MrzLayout? TryDetectLayout(string[] Lines)
		{
			if (Lines.Length == 3 && Lines.All(static Line => Line.Length == 30))
				return MrzLayout.Td1;
			if (Lines.Length == 2 && Lines.All(static Line => Line.Length == 36))
				return MrzLayout.Td2;
			if (Lines.Length == 2 && Lines.All(static Line => Line.Length == 44))
				return MrzLayout.Td3;
			return null;
		}

		private static string NormalizeMrz(string Mrz)
		{
			StringBuilder Builder = new StringBuilder(Mrz.Length);
			foreach (char Character in Mrz.ToUpperInvariant())
			{
				if (Character == '\r')
					continue;
				if (Character == '\n')
				{
					Builder.Append('\n');
					continue;
				}

				Builder.Append(NormalizeMrzCharacter(Character));
			}

			string[] RawLines = Builder.ToString()
				.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (RawLines.Length == 1)
			{
				string Flat = RawLines[0];
				switch (Flat.Length)
				{
					case 90:
						return string.Join("\n", [Flat.Substring(0, 30), Flat.Substring(30, 30), Flat.Substring(60, 30)]);

					case 72:
						return string.Join("\n", [Flat.Substring(0, 36), Flat.Substring(36, 36)]);

					case 88:
						return string.Join("\n", [Flat.Substring(0, 44), Flat.Substring(44, 44)]);
				}
			}

			return string.Join("\n", RawLines);
		}

		private static string[] SplitLines(string NormalizedMrz)
		{
			if (string.IsNullOrWhiteSpace(NormalizedMrz))
				return [];

			return NormalizedMrz.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		}

		private static char NormalizeMrzCharacter(char Character)
		{
			if (char.IsWhiteSpace(Character))
				return '<';
			if (Character == '<' || Character == '«' || Character == '‹' || Character == '›')
				return '<';
			if ((Character >= 'A' && Character <= 'Z') || (Character >= '0' && Character <= '9'))
				return Character;
			return Character;
		}

		private static bool IsCheckDigitValid(string Field, char ExpectedCheckDigit)
		{
			return NormalizeCheckDigitChar(ExpectedCheckDigit) == CalculateCheckDigit(Field);
		}

		private static char NormalizeCheckDigitChar(char Character)
		{
			char Normalized = NormalizeMrzCharacter(char.ToUpperInvariant(Character));
			if (Normalized >= '0' && Normalized <= '9')
				return Normalized;
			return CheckDigitConfusions.TryGetValue(Normalized, out char Replacement) ? Replacement : '\0';
		}

		private static string CalculateCheckDigitText(string Value)
		{
			int Sum = 0;
			int Index = 0;
			foreach (char Character in Value)
			{
				int CharacterValue = GetMrzCharacterValue(Character);
				if (CharacterValue < 0)
					return string.Empty;

				Sum += CharacterValue * Weights[Index++ % 3];
			}

			return new string((char)('0' + (Sum % 10)), 1);
		}

		private static int GetMrzCharacterValue(char Character)
		{
			if (Character >= '0' && Character <= '9')
				return Character - '0';
			if (Character >= 'A' && Character <= 'Z')
				return (Character - 'A') + 10;
			if (Character == '<')
				return 0;
			return -1;
		}

		private static string ExtractDocumentType(string Value)
		{
			return Value.Replace("<", string.Empty);
		}

		private static void PopulateNames(DocumentInformation Info, string Value)
		{
			string[] Parts = Value.TrimEnd('<').Split("<<", 2, StringSplitOptions.None);
			Info.PrimaryIdentifier = SplitIdentifierPart(Parts[0]);
			Info.SecondaryIdentifier = Parts.Length > 1 ? SplitIdentifierPart(Parts[1]) : [];
		}

		private static string[] SplitIdentifierPart(string Value)
		{
			return Value.Split('<', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		}

		private static DocumentInformation? AssembleTd2Info2(Match Match)
		{
			if (!TryParseTd2OverflowTail(
				Match.Groups["Nr1"].Value,
				Match.Groups["Tail"].Value,
				Match.Groups["Birth"].Value,
				Match.Groups["BirthCheck"].Value,
				Match.Groups["Expires"].Value,
				Match.Groups["ExpiryCheck"].Value,
				Match.Groups["OverallCheck"].Value,
				out string? DocumentNumberSuffix,
				out string? NumberCheck,
				out string? OptionalData))
				return null;

			DocumentInformation Result = AssembleInfo(Match);
			Result.DocumentNumber = Match.Groups["Nr1"].Value + DocumentNumberSuffix;
			Result.OptionalData = OptionalData;
			return CalcTd2OverflowMrzInfo(Result, Match, NumberCheck, OptionalData) ? Result : null;
		}

		private static DocumentInformation? AssembleTd1Info2(Match Match)
		{
			DocumentInformation Result = AssembleInfo(Match);
			Result.DocumentNumber = Match.Groups["Nr1"].Value + Match.Groups["Nr2"].Value;
			return CalcTd1MrzInfo(Result, Match) ? Result : null;
		}

		private static DocumentInformation? AssembleTd1Info1(Match Match)
		{
			DocumentInformation Result = AssembleInfo(Match);
			Result.DocumentNumber = Match.Groups["Nr"].Value;
			return CalcTd1MrzInfo(Result, Match) ? Result : null;
		}

		private static DocumentInformation? AssembleTd2Info1(Match Match)
		{
			DocumentInformation Result = AssembleInfo(Match);
			Result.DocumentNumber = Match.Groups["Nr"].Value;
			return CalcMrzInfo(Result, Match, MrzLayout.Td2) ? Result : null;
		}

		private static DocumentInformation? AssembleTd3Info1(Match Match)
		{
			DocumentInformation Result = AssembleInfo(Match);
			Result.DocumentNumber = Match.Groups["Nr"].Value;
			return CalcMrzInfo(Result, Match, MrzLayout.Td3) ? Result : null;
		}

		private static bool CalcTd1MrzInfo(DocumentInformation Info, Match Match)
		{
			if (Info.DocumentNumber is null || Info.DateOfBirth is null || Info.ExpiryDate is null)
				return false;

			string NumberCheck = Match.Groups["NrCheck"].Value;
			if (NumberCheck != CalculateCheckDigitText(Info.DocumentNumber))
				return false;

			string BirthCheck = Match.Groups["BirthCheck"].Value;
			if (BirthCheck != CalculateCheckDigitText(Info.DateOfBirth))
				return false;

			string ExpiryCheck = Match.Groups["ExpiryCheck"].Value;
			if (ExpiryCheck != CalculateCheckDigitText(Info.ExpiryDate))
				return false;

			string OptionalLine1 = Match.Groups["Optional1"].Value;
			string OptionalLine2 = Match.Groups["Optional2"].Value;
			string OverallCheck = Match.Groups["OverallCheck"].Value;
			string Composite = Info.DocumentNumber + NumberCheck + OptionalLine1 + Info.DateOfBirth + BirthCheck + Info.ExpiryDate + ExpiryCheck + OptionalLine2;
			if (OverallCheck != CalculateCheckDigitText(Composite))
				return false;

			Info.OptionalData = (OptionalLine1 + OptionalLine2).Replace("<", string.Empty);
			Info.MRZ_Information = Info.DocumentNumber + NumberCheck + Info.DateOfBirth + BirthCheck + Info.ExpiryDate + ExpiryCheck;
			Info.DocumentNumber = Info.DocumentNumber.Replace("<", string.Empty);
			return true;
		}

		private static bool CalcTd2OverflowMrzInfo(DocumentInformation Info, Match Match, string NumberCheck, string OptionalData)
		{
			if (Info.DocumentNumber is null || Info.DateOfBirth is null || Info.ExpiryDate is null)
				return false;

			if (NumberCheck != CalculateCheckDigitText(Info.DocumentNumber))
				return false;

			string BirthCheck = Match.Groups["BirthCheck"].Value;
			if (BirthCheck != CalculateCheckDigitText(Info.DateOfBirth))
				return false;

			string ExpiryCheck = Match.Groups["ExpiryCheck"].Value;
			if (ExpiryCheck != CalculateCheckDigitText(Info.ExpiryDate))
				return false;

			string Composite = Info.DocumentNumber + NumberCheck + Info.DateOfBirth + BirthCheck + Info.ExpiryDate + ExpiryCheck + OptionalData;
			if (Match.Groups["OverallCheck"].Value != CalculateCheckDigitText(Composite))
				return false;

			Info.OptionalData = OptionalData.Replace("<", string.Empty);
			Info.MRZ_Information = Info.DocumentNumber + NumberCheck + Info.DateOfBirth + BirthCheck + Info.ExpiryDate + ExpiryCheck;
			Info.DocumentNumber = Info.DocumentNumber.Replace("<", string.Empty);
			return true;
		}

		private static bool CalcMrzInfo(DocumentInformation Info, Match Match, MrzLayout Layout)
		{
			if (Info.DocumentNumber is null || Info.DateOfBirth is null || Info.ExpiryDate is null)
				return false;

			string NumberCheck = Match.Groups["NrCheck"].Value;
			if (NumberCheck != CalculateCheckDigitText(Info.DocumentNumber))
				return false;

			string BirthCheck = Match.Groups["BirthCheck"].Value;
			if (BirthCheck != CalculateCheckDigitText(Info.DateOfBirth))
				return false;

			string ExpiryCheck = Match.Groups["ExpiryCheck"].Value;
			if (ExpiryCheck != CalculateCheckDigitText(Info.ExpiryDate))
				return false;

			string OptionalValue = Match.Groups["Optional"].Value;
			if (Layout == MrzLayout.Td3)
			{
				if (!Match.Groups["OptionalCheck"].Success)
					return false;

				string OptionalCheck = Match.Groups["OptionalCheck"].Value;
				if (OptionalCheck != CalculateCheckDigitText(OptionalValue))
					return false;
			}

			if (Match.Groups["OverallCheck"].Success)
			{
				string Composite = Layout switch
				{
					MrzLayout.Td2 => Info.DocumentNumber + NumberCheck + Info.DateOfBirth + BirthCheck + Info.ExpiryDate + ExpiryCheck + OptionalValue,
					MrzLayout.Td3 => Info.DocumentNumber + NumberCheck + Info.DateOfBirth + BirthCheck + Info.ExpiryDate + ExpiryCheck + OptionalValue + Match.Groups["OptionalCheck"].Value,
					_ => string.Empty
				};

				if (Match.Groups["OverallCheck"].Value != CalculateCheckDigitText(Composite))
					return false;
			}

			Info.OptionalData = OptionalValue.Replace("<", string.Empty);
			Info.MRZ_Information = Info.DocumentNumber + NumberCheck + Info.DateOfBirth + BirthCheck + Info.ExpiryDate + ExpiryCheck;
			Info.DocumentNumber = Info.DocumentNumber.Replace("<", string.Empty);
			return true;
		}

		private static DocumentInformation AssembleInfo(Match Match)
		{
			DocumentInformation Result = new DocumentInformation
			{
				DocumentType = ExtractDocumentType(Match.Groups["DocType"].Value),
				IssuingState = Match.Groups["Issuer"].Value,
				Nationality = Match.Groups["Nationality"].Value,
				Gender = Match.Groups["Gender"].Value,
				DocumentNumber = Match.Groups["Nr"].Value,
				DateOfBirth = Match.Groups["Birth"].Value,
				ExpiryDate = Match.Groups["Expires"].Value,
				OptionalData = Match.Groups["Optional"].Value
			};
			Result.PrimaryIdentifier = SplitIdentifierPart(Match.Groups["PID"].Value);
			Result.SecondaryIdentifier = SplitIdentifierPart(Match.Groups["SID"].Value);
			return Result;
		}

		private static bool TryParseTd2OverflowTail(
			string DocumentNumberPrefix,
			string Tail,
			string DateOfBirth,
			string BirthCheck,
			string ExpiryDate,
			string ExpiryCheck,
			string OverallCheck,
			[NotNullWhen(true)] out string? DocumentNumberSuffix,
			[NotNullWhen(true)] out string? NumberCheck,
			[NotNullWhen(true)] out string? OptionalData)
		{
			for (int CheckDigitIndex = 0; CheckDigitIndex < Tail.Length; CheckDigitIndex++)
			{
				string CandidateSuffix = Tail.Substring(0, CheckDigitIndex);
				string CandidateNumberCheck = Tail.Substring(CheckDigitIndex, 1);
				string CandidateOptionalData = Tail.Substring(CheckDigitIndex + 1);
				string CandidateDocumentNumber = DocumentNumberPrefix + CandidateSuffix;

				if (CandidateNumberCheck != CalculateCheckDigitText(CandidateDocumentNumber))
					continue;

				string Composite = CandidateDocumentNumber
					+ CandidateNumberCheck
					+ DateOfBirth
					+ BirthCheck
					+ ExpiryDate
					+ ExpiryCheck
					+ CandidateOptionalData;
				if (OverallCheck != CalculateCheckDigitText(Composite))
					continue;

				DocumentNumberSuffix = CandidateSuffix;
				NumberCheck = CandidateNumberCheck;
				OptionalData = CandidateOptionalData;
				return true;
			}

			DocumentNumberSuffix = null;
			NumberCheck = null;
			OptionalData = null;
			return false;
		}

		private static readonly int[] Weights = [7, 3, 1];

		private delegate DocumentInformation? DocumentInformationFromMatch(Match Match);

		private readonly record struct ParserDefinition(
			MrzLayout Layout,
			Regex Pattern,
			DocumentInformationFromMatch Factory);
	}
}