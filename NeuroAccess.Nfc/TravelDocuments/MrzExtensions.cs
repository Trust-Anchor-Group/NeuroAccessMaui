using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Contains MRZ-related Extensions for Machine-Readable Travel Documents.
	/// </summary>
	public static class MrzExtensions
	{
		/// <summary>
		/// Derives Basic Access Control Keys from the second row of the 
		/// Machine-Readable string in passport (MRZ).
		/// </summary>
		/// <param name="MRZ">Machine-Readable text.</param>
		/// <param name="Info">Parsed Document Information.</param>
		/// <returns>If the string could be parsed.</returns>
		public static bool ParseMrz(string MRZ, [NotNullWhen(true)] out DocumentInformation? Info)
		{
			Match M = td2_mrz_nr9charsplus.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo2(M);
				return Info is not null;
			}

			M = td2_mrz_nr9chars.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo1(M);
				return Info is not null;
			}

			M = td1_mrz_nr9charsplus.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo2(M);
				return Info is not null;
			}

			M = td1_mrz_nr9chars.Match(MRZ);
			if (M.Success)
			{
				Info = AssembleInfo1(M);
				return Info is not null;
			}

			// TODO: Checks

			Info = null;
			return false;
		}

		private static DocumentInformation? AssembleInfo2(Match M)
		{
			DocumentInformation Result = AssembleInfo(M);
			Result.DocumentNumber = M.Groups["Nr1"].Value + M.Groups["Nr2"].Value;

			return CalcMrzInfo(Result, M) ? Result : null;
		}

		private static DocumentInformation? AssembleInfo1(Match M)
		{
			DocumentInformation Result = AssembleInfo(M);
			Result.DocumentNumber = M.Groups["Nr"].Value;

			return CalcMrzInfo(Result, M) ? Result : null;
		}

		private static bool CalcMrzInfo(DocumentInformation Info, Match M)
		{
			if (Info.DocumentNumber is null || Info.DateOfBirth is null || Info.ExpiryDate is null)
				return false;

			string NrCheck = M.Groups["NrCheck"].Value;
			if (NrCheck != CalcCheckDigit(Info.DocumentNumber))
				return false;

			string BirthCheck = M.Groups["BirthCheck"].Value;
			if (BirthCheck != CalcCheckDigit(Info.DateOfBirth))
				return false;

			string ExpiryCheck = M.Groups["ExpiryCheck"].Value;
			if (ExpiryCheck != CalcCheckDigit(Info.ExpiryDate))
				return false;

			if (!string.IsNullOrEmpty(Info.OptionalData))
			{
				string s = Info.OptionalData!.Replace("<", string.Empty);

				if (!string.IsNullOrEmpty(s))
				{
					string OptionalCheck = M.Groups["OptionalCheck"].Value;
					if (OptionalCheck != CalcCheckDigit(Info.OptionalData))
						return false;
				}

				Info.OptionalData = s;
			}

			// TODO: Check OverallCheck

			Info.MRZ_Information = Info.DocumentNumber + NrCheck +
				Info.DateOfBirth + BirthCheck + Info.ExpiryDate + ExpiryCheck;

			Info.DocumentNumber = Info.DocumentNumber.Replace("<", string.Empty);

			// TODO: Check note in §9.7.3 ICAO 9303-p11:
			//
			// TD1-documents with document numbers longer than nine characters, the
			// document number needs to be concatenated from the document number field
			// and the optional data field of the MRZ, excluding the filler character.

			return true;
		}

		private static string CalcCheckDigit(string Value)
		{
			// §4.9, ISO/IEC 9303, Part 3: https://www.icao.int/publications/Documents/9303_p3_cons_en.pdf

			int Sum = 0;
			int i = 0;
			int j;

			foreach (char ch in Value)
			{
				if (ch >= '0' && ch <= '9')
					j = ch - '0';
				else if (ch >= 'A' && ch <= 'Z')
					j = ch - 'A' + 10;
				else if (ch >= 'a' && ch <= 'z')
					j = ch - 'a' + 10;
				else if (ch == '<')
					j = 0;
				else
					return string.Empty;

				j *= weights[i++];
				Sum += j;
				i %= 3;
			}

			return new string((char)('0' + Sum % 10), 1);
		}

		private static readonly int[] weights = [7, 3, 1];

		private static DocumentInformation AssembleInfo(Match M)
		{
			return new DocumentInformation()
			{
				DocumentType = M.Groups["DocType"].Value,
				IssuingState = M.Groups["Issuer"].Value,
				Nationality = M.Groups["Nationality"].Value,
				PrimaryIdentifier = M.Groups["PID"].Value.Split('<'),
				SecondaryIdentifier = M.Groups["SID"].Value.Split('<'),
				Gender = M.Groups["Gender"].Value,
				DocumentNumber = M.Groups["Nr"].Value,
				DateOfBirth = M.Groups["Birth"].Value,
				ExpiryDate = M.Groups["Expires"].Value,
				OptionalData = M.Groups["Optional"].Value
			};
		}

		// TD2, ref: ICAO 9303-5, §B: https://www.icao.int/publications/Documents/9303_p5_cons_en.pdf
		private static readonly Regex td2_mrz_nr9charsplus = new(@"^(?'DocType'.{1,2})<(?'Issuer'\w{3})(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*)<*\n(?'Nr1'[^<]{9})<(?'Nationality'\w{3})(?'Birth'[^<]*)(?'BirthCheck'\d)(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)(?'Nr2'[^<]*)(?'NrCheck'\d)((?'Optional'.*)(?'OptionalCheck'\d))?<*(?'OverallCheck'\d)$", RegexOptions.Multiline);
		private static readonly Regex td2_mrz_nr9chars = new(@"^(?'DocType'.{1,2})<(?'Issuer'\w{3})(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*)<*\n(?'Nr'.{9})(?'NrCheck'\d)(?'Nationality'\w{3})(?'Birth'[^<]{6})(?'BirthCheck'\d)(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)((?'Optional'.*)(?'OptionalCheck'\d))?<*(?'OverallCheck'\d)$", RegexOptions.Multiline);

		// TD1, ref: ICAO 9303-5, §B: https://www.icao.int/publications/Documents/9303_p5_cons_en.pdf
		private static readonly Regex td1_mrz_nr9charsplus = new(@"^(?'DocType'.{1,2})<(?'Issuer'\w{3})(?'Nr1'[^<]{9})<(?'Nr2'.{3})(?'NrCheck'\d)((?'Optional'.*)(?'OptionalCheck'\d))?<*\n(?'Birth'[^<]{6})(?'BirthCheck'\d)(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)(?'Nationality'\w{3})<*(?'OverallCheck'\d)\n(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*).*$", RegexOptions.Multiline);
		private static readonly Regex td1_mrz_nr9chars = new(@"^(?'DocType'.{1,2})<(?'Issuer'\w{3})(?'Nr'.{9})(?'NrCheck'.)((?'Optional'.*)(?'OptionalCheck'\d))?<*\n(?'Birth'[^<]{6})(?'BirthCheck'\d)(?'Gender'[MF])(?'Expires'[^<]{6})(?'ExpiryCheck'\d)(?'Nationality'\w{3})<*(?'OverallCheck'\d)\n(?'PID'[^<]+(<[^<]+)*)<<(?'SID'[^<]+(<[^<]+)*).*$", RegexOptions.Multiline);
	}
}
