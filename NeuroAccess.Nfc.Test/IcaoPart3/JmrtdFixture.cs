using NeuroAccess.Nfc.TravelDocuments;

namespace NeuroAccess.Nfc.Test.IcaoPart3
{
	/// <summary>
	/// Represents one committed JMRTD fixture.
	/// </summary>
	public sealed class JmrtdFixture
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JmrtdFixture"/> class.
		/// </summary>
		/// <param name="Name">Fixture name.</param>
		/// <param name="Files">Elementary files keyed by file identifier.</param>
		public JmrtdFixture(string Name, Dictionary<ushort, byte[]> Files)
		{
			this.Name = Name;
			this.Files = Files;
		}

		/// <summary>
		/// Gets the fixture name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets elementary files keyed by file identifier.
		/// </summary>
		public Dictionary<ushort, byte[]> Files { get; }
	}

	/// <summary>
	/// Loads committed JMRTD fixtures.
	/// </summary>
	public static class JmrtdFixtureLoader
	{
		/// <summary>
		/// Loads a committed JMRTD fixture by name.
		/// </summary>
		/// <param name="FixtureName">Fixture name.</param>
		/// <returns>Loaded fixture.</returns>
		public static JmrtdFixture Load(string FixtureName)
		{
			string FixturePath = Path.Combine(GetJmrtdTestDataPath(), FixtureName);
			if (!Directory.Exists(FixturePath))
				throw new InvalidOperationException("Missing JMRTD fixture: " + FixtureName);

			Dictionary<ushort, byte[]> Files = [];
			foreach (string FilePath in Directory.GetFiles(FixturePath, "EF.*.bin"))
			{
				string FileName = Path.GetFileName(FilePath);
				if (TryMapFileName(FileName, out ushort FileId))
					Files[FileId] = File.ReadAllBytes(FilePath);
			}

			return new JmrtdFixture(FixtureName, Files);
		}

		/// <summary>
		/// Gets the committed JMRTD test data path.
		/// </summary>
		/// <returns>JMRTD test data path.</returns>
		public static string GetJmrtdTestDataPath()
		{
			string Candidate = Path.Combine(AppContext.BaseDirectory, "TestData", "JMRTD");
			if (Directory.Exists(Candidate))
				return Candidate;

			string CurrentDirectory = AppContext.BaseDirectory;
			while (!string.IsNullOrEmpty(CurrentDirectory))
			{
				Candidate = Path.Combine(CurrentDirectory, "NeuroAccess.Nfc.Test", "TestData", "JMRTD");
				if (Directory.Exists(Candidate))
					return Candidate;

				DirectoryInfo? Parent = Directory.GetParent(CurrentDirectory);
				if (Parent is null)
					break;

				CurrentDirectory = Parent.FullName;
			}

			throw new InvalidOperationException("Unable to find JMRTD test data directory.");
		}

		private static bool TryMapFileName(string FileName, out ushort FileId)
		{
			FileId = FileName switch
			{
				"EF.COM.bin" => EF.COM,
				"EF.SOD.bin" => EF.SOD,
				"EF.DG1.bin" => EF.DG1,
				"EF.DG2.bin" => EF.DG2,
				"EF.DG3.bin" => EF.DG3,
				"EF.DG4.bin" => EF.DG4,
				"EF.DG5.bin" => EF.DG5,
				"EF.DG6.bin" => 0x0106,
				"EF.DG7.bin" => EF.DG7,
				"EF.DG8.bin" => 0x0108,
				"EF.DG9.bin" => 0x0109,
				"EF.DG10.bin" => 0x010a,
				"EF.DG11.bin" => EF.DG11,
				"EF.DG12.bin" => EF.DG12,
				"EF.DG13.bin" => EF.DG13,
				"EF.DG14.bin" => EF.DG14,
				"EF.DG15.bin" => EF.DG15,
				"EF.DG16.bin" => EF.DG16,
				"EF.DIR.bin" => EF.DIR,
				_ => 0
			};

			return FileId != 0;
		}
	}
}
