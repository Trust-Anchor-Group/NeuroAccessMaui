using NeuroAccess.Nfc.TravelDocuments;

namespace NeuroAccess.Nfc.Test.IcaoPart3
{
	/// <summary>
	/// Provides shared ICAO Part 3 test data and classifier construction helpers.
	/// </summary>
	public static class IcaoPart3TestData
	{
		/// <summary>
		/// Loads the committed ICAO Part 3 matrix.
		/// </summary>
		/// <returns>ICAO Part 3 matrix.</returns>
		public static IcaoPart3Matrix LoadMatrix()
		{
			return IcaoPart3Matrix.Load(GetMatrixPath("matrix.xml"));
		}

		/// <summary>
		/// Creates the default ICAO Part 3 coverage classifier.
		/// </summary>
		/// <returns>Coverage classifier.</returns>
		public static IcaoPart3CoverageClassifier CreateCoverageClassifier()
		{
			JmrtdFixture Fixture = JmrtdFixtureLoader.Load("coverage-lds-all-files");
			JmrtdFixture Iso39794Fixture = JmrtdFixtureLoader.Load("minimal-iso39794-jpeg");
			return new IcaoPart3CoverageClassifier(AddCoverageSupportFiles(Fixture), Iso39794Fixture);
		}

		/// <summary>
		/// Creates coverage results for all cases in a matrix.
		/// </summary>
		/// <param name="Matrix">ICAO Part 3 matrix.</param>
		/// <returns>Coverage results.</returns>
		public static IcaoPart3CoverageResult[] CreateCoverageResults(IcaoPart3Matrix Matrix)
		{
			IcaoPart3CoverageClassifier Classifier = CreateCoverageClassifier();
			return Matrix.Cases
				.Select(Classifier.Classify)
				.ToArray();
		}

		/// <summary>
		/// Gets the path to a committed ICAO Part 3 matrix file.
		/// </summary>
		/// <param name="FileName">Matrix file name.</param>
		/// <returns>Matrix file path.</returns>
		public static string GetMatrixPath(string FileName)
		{
			string Candidate = Path.Combine(AppContext.BaseDirectory, "TestData", "ICAO", "Part3", "v3.2", FileName);
			if (File.Exists(Candidate))
				return Candidate;

			string CurrentDirectory = AppContext.BaseDirectory;
			while (!string.IsNullOrEmpty(CurrentDirectory))
			{
				Candidate = Path.Combine(CurrentDirectory, "NeuroAccess.Nfc.Test", "TestData", "ICAO", "Part3", "v3.2", FileName);
				if (File.Exists(Candidate))
					return Candidate;

				DirectoryInfo Parent = Directory.GetParent(CurrentDirectory);
				if (Parent is null)
					break;

				CurrentDirectory = Parent.FullName;
			}

			throw new InvalidOperationException("Unable to find ICAO Part 3 matrix file: " + FileName);
		}

		/// <summary>
		/// Gets the source path to a committed ICAO Part 3 matrix file.
		/// </summary>
		/// <param name="FileName">Matrix file name.</param>
		/// <returns>Source file path.</returns>
		public static string GetMatrixSourcePath(string FileName)
		{
			string CurrentDirectory = AppContext.BaseDirectory;
			while (!string.IsNullOrEmpty(CurrentDirectory))
			{
				string CandidateDirectory = Path.Combine(CurrentDirectory, "NeuroAccess.Nfc.Test",
					"TestData", "ICAO", "Part3", "v3.2");
				if (Directory.Exists(CandidateDirectory))
					return Path.Combine(CandidateDirectory, FileName);

				DirectoryInfo Parent = Directory.GetParent(CurrentDirectory);
				if (Parent is null)
					break;

				CurrentDirectory = Parent.FullName;
			}

			string MatrixPath = GetMatrixPath("matrix.xml");
			return Path.Combine(Path.GetDirectoryName(MatrixPath) ?? AppContext.BaseDirectory, FileName);
		}

		private static JmrtdFixture AddCoverageSupportFiles(JmrtdFixture Fixture)
		{
			Dictionary<ushort, byte[]> Files = new Dictionary<ushort, byte[]>(Fixture.Files)
			{
				[EF.CardAccess] = HexToBytes("31143012060A04007F0007020204020202010202010D")
			};

			return new JmrtdFixture(Fixture.Name, Files);
		}

		private static byte[] HexToBytes(string Hex)
		{
			byte[] Result = new byte[Hex.Length / 2];
			for (int i = 0; i < Result.Length; i++)
				Result[i] = Byte.Parse(Hex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber,
					System.Globalization.CultureInfo.InvariantCulture);

			return Result;
		}
	}
}
