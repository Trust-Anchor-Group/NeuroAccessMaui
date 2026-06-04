using System.Xml.Linq;

namespace NeuroAccess.Nfc.Test.IcaoPart3
{
	/// <summary>
	/// Represents the compact ICAO Part 3 test matrix used by the test suite.
	/// </summary>
	public sealed class IcaoPart3Matrix
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IcaoPart3Matrix"/> class.
		/// </summary>
		/// <param name="Document">Source document name.</param>
		/// <param name="Version">Source document version.</param>
		/// <param name="Date">Source document date.</param>
		/// <param name="CaseCount">Expected case count.</param>
		/// <param name="StructuredApduCount">Expected structured APDU count.</param>
		/// <param name="IndexSha256">SHA-256 hash of the source index XML.</param>
		/// <param name="VocabularySha256">SHA-256 hash of the source vocabulary XML.</param>
		/// <param name="Cases">Test cases.</param>
		public IcaoPart3Matrix(string Document, string Version, string Date, int CaseCount,
			int StructuredApduCount, string IndexSha256, string VocabularySha256,
			IcaoPart3Case[] Cases)
		{
			this.Document = Document;
			this.Version = Version;
			this.Date = Date;
			this.CaseCount = CaseCount;
			this.StructuredApduCount = StructuredApduCount;
			this.IndexSha256 = IndexSha256;
			this.VocabularySha256 = VocabularySha256;
			this.Cases = Cases;
		}

		/// <summary>
		/// Gets the source document name.
		/// </summary>
		public string Document { get; }

		/// <summary>
		/// Gets the source document version.
		/// </summary>
		public string Version { get; }

		/// <summary>
		/// Gets the source document date.
		/// </summary>
		public string Date { get; }

		/// <summary>
		/// Gets the expected case count.
		/// </summary>
		public int CaseCount { get; }

		/// <summary>
		/// Gets the expected structured APDU count.
		/// </summary>
		public int StructuredApduCount { get; }

		/// <summary>
		/// Gets the SHA-256 hash of the source index XML.
		/// </summary>
		public string IndexSha256 { get; }

		/// <summary>
		/// Gets the SHA-256 hash of the source vocabulary XML.
		/// </summary>
		public string VocabularySha256 { get; }

		/// <summary>
		/// Gets the test cases.
		/// </summary>
		public IcaoPart3Case[] Cases { get; }

		/// <summary>
		/// Loads an ICAO Part 3 matrix XML file.
		/// </summary>
		/// <param name="FileName">Matrix file name.</param>
		/// <returns>Parsed matrix.</returns>
		public static IcaoPart3Matrix Load(string FileName)
		{
			XDocument Document = XDocument.Load(FileName);
			XElement Root = Document.Root ?? throw new InvalidDataException("Missing matrix root.");
			XElement CasesElement = Root.Element("cases") ?? throw new InvalidDataException("Missing cases element.");

			IcaoPart3Case[] Cases = CasesElement.Elements("case")
				.Select(ParseCase)
				.ToArray();

			return new IcaoPart3Matrix(
				Required(Root, "document"),
				Required(Root, "version"),
				Required(Root, "date"),
				Int32.Parse(Required(Root, "caseCount")),
				Int32.Parse(Required(Root, "structuredApduCount")),
				Required(Root, "indexSha256"),
				Required(Root, "vocabularySha256"),
				Cases);
		}

		private static IcaoPart3Case ParseCase(XElement Element)
		{
			string[] ProfileTokens = Element.Element("profileTokens")?.Elements("token")
				.Select(Token => Token.Value)
				.ToArray() ?? [];
			string[] Preconditions = Element.Element("preconditions")?.Elements("precondition")
				.Select(Precondition => Precondition.Value)
				.ToArray() ?? [];
			string[] TargetFiles = Element.Element("targetFiles")?.Elements("targetFile")
				.Select(Target => Required(Target, "name"))
				.ToArray() ?? [];
			IcaoPart3Step[] Steps = Element.Element("steps")?.Elements("step")
				.Select(ParseStep)
				.ToArray() ?? [];

			return new IcaoPart3Case(
				Required(Element, "id"),
				Required(Element, "category"),
				Required(Element, "section"),
				Boolean.Parse(Required(Element, "requiresSecureMessaging")),
				Boolean.Parse(Required(Element, "requiresRuntimeApduGeneration")),
				Element.Element("purpose")?.Value ?? string.Empty,
				ProfileTokens,
				Preconditions,
				TargetFiles,
				Steps);
		}

		private static IcaoPart3Step ParseStep(XElement Element)
		{
			IcaoPart3Apdu[] Apdus = Element.Element("apdus")?.Elements("apdu")
				.Select(ParseApdu)
				.ToArray() ?? [];
			IcaoPart3ExpectedResult Expected = ParseExpected(
				Element.Element("expected") ?? throw new InvalidDataException("Missing expected element."));

			return new IcaoPart3Step(
				Int32.Parse(Required(Element, "number")),
				Optional(Element, "targetFile"),
				Optional(Element, "targetFileId"),
				Apdus,
				Expected);
		}

		private static IcaoPart3Apdu ParseApdu(XElement Element)
		{
			return new IcaoPart3Apdu(
				Required(Element, "action"),
				Boolean.Parse(Required(Element, "protected")),
				Boolean.Parse(Required(Element, "template")),
				Element.Value.Trim());
		}

		private static IcaoPart3ExpectedResult ParseExpected(XElement Element)
		{
			string[] SwAny = Element.Element("swAny")?.Elements("sw")
				.Select(Sw => Sw.Value)
				.ToArray() ?? [];
			bool? ResponseDataEmpty = null;
			string? ResponseDataEmptyValue = Optional(Element, "responseDataEmpty");
			if (!string.IsNullOrEmpty(ResponseDataEmptyValue))
				ResponseDataEmpty = Boolean.Parse(ResponseDataEmptyValue);

			return new IcaoPart3ExpectedResult(
				SwAny,
				ResponseDataEmpty,
				Optional(Element, "dataPrefix"));
		}

		private static string Required(XElement Element, string Name)
		{
			return Optional(Element, Name) ?? throw new InvalidDataException("Missing attribute: " + Name);
		}

		private static string? Optional(XElement Element, string Name)
		{
			return Element.Attribute(Name)?.Value;
		}
	}

	/// <summary>
	/// Represents one ICAO Part 3 test case.
	/// </summary>
	public sealed class IcaoPart3Case
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IcaoPart3Case"/> class.
		/// </summary>
		public IcaoPart3Case(string Id, string Category, string Section,
			bool RequiresSecureMessaging, bool RequiresRuntimeApduGeneration,
			string Purpose, string[] ProfileTokens, string[] Preconditions,
			string[] TargetFiles, IcaoPart3Step[] Steps)
		{
			this.Id = Id;
			this.Category = Category;
			this.Section = Section;
			this.RequiresSecureMessaging = RequiresSecureMessaging;
			this.RequiresRuntimeApduGeneration = RequiresRuntimeApduGeneration;
			this.Purpose = Purpose;
			this.ProfileTokens = ProfileTokens;
			this.Preconditions = Preconditions;
			this.TargetFiles = TargetFiles;
			this.Steps = Steps;
		}

		/// <summary>Gets the test case identifier.</summary>
		public string Id { get; }

		/// <summary>Gets the test case category.</summary>
		public string Category { get; }

		/// <summary>Gets the source section.</summary>
		public string Section { get; }

		/// <summary>Gets whether secure messaging is required.</summary>
		public bool RequiresSecureMessaging { get; }

		/// <summary>Gets whether APDUs contain runtime placeholders.</summary>
		public bool RequiresRuntimeApduGeneration { get; }

		/// <summary>Gets the test purpose.</summary>
		public string Purpose { get; }

		/// <summary>Gets profile tokens.</summary>
		public string[] ProfileTokens { get; }

		/// <summary>Gets normalized preconditions.</summary>
		public string[] Preconditions { get; }

		/// <summary>Gets target files.</summary>
		public string[] TargetFiles { get; }

		/// <summary>Gets test steps.</summary>
		public IcaoPart3Step[] Steps { get; }
	}

	/// <summary>
	/// Represents one ICAO Part 3 test step.
	/// </summary>
	public sealed class IcaoPart3Step
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IcaoPart3Step"/> class.
		/// </summary>
		public IcaoPart3Step(int Number, string? TargetFile, string? TargetFileId,
			IcaoPart3Apdu[] Apdus, IcaoPart3ExpectedResult Expected)
		{
			this.Number = Number;
			this.TargetFile = TargetFile;
			this.TargetFileId = TargetFileId;
			this.Apdus = Apdus;
			this.Expected = Expected;
		}

		/// <summary>Gets the step number.</summary>
		public int Number { get; }

		/// <summary>Gets the target file name, if known.</summary>
		public string? TargetFile { get; }

		/// <summary>Gets the target file identifier, if known.</summary>
		public string? TargetFileId { get; }

		/// <summary>Gets APDU templates or commands.</summary>
		public IcaoPart3Apdu[] Apdus { get; }

		/// <summary>Gets expected results.</summary>
		public IcaoPart3ExpectedResult Expected { get; }
	}

	/// <summary>
	/// Represents one APDU in an ICAO Part 3 step.
	/// </summary>
	public sealed class IcaoPart3Apdu
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IcaoPart3Apdu"/> class.
		/// </summary>
		public IcaoPart3Apdu(string Action, bool Protected, bool Template, string Value)
		{
			this.Action = Action;
			this.Protected = Protected;
			this.Template = Template;
			this.Value = Value;
		}

		/// <summary>Gets the normalized APDU action.</summary>
		public string Action { get; }

		/// <summary>Gets whether the APDU is protected.</summary>
		public bool Protected { get; }

		/// <summary>Gets whether the APDU contains runtime placeholders.</summary>
		public bool Template { get; }

		/// <summary>Gets the APDU bytes as hex or template text.</summary>
		public string Value { get; }
	}

	/// <summary>
	/// Represents expected APDU result information.
	/// </summary>
	public sealed class IcaoPart3ExpectedResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IcaoPart3ExpectedResult"/> class.
		/// </summary>
		public IcaoPart3ExpectedResult(string[] SwAny, bool? ResponseDataEmpty, string? DataPrefix)
		{
			this.SwAny = SwAny;
			this.ResponseDataEmpty = ResponseDataEmpty;
			this.DataPrefix = DataPrefix;
		}

		/// <summary>Gets accepted status words.</summary>
		public string[] SwAny { get; }

		/// <summary>Gets whether response data must be empty.</summary>
		public bool? ResponseDataEmpty { get; }

		/// <summary>Gets the expected response data prefix.</summary>
		public string? DataPrefix { get; }
	}
}
