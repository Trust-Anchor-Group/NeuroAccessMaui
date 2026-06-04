using System.Xml.Linq;

namespace NeuroAccess.Nfc.Test.IcaoPart3
{
	/// <summary>
	/// High-level outcome used in the reviewable ICAO Part 3 execution report.
	/// </summary>
	public enum IcaoPart3ExecutionOutcome
	{
		/// <summary>
		/// The case executed and matched the expected result.
		/// </summary>
		Passed,

		/// <summary>
		/// The case executed and did not match the expected result.
		/// </summary>
		Failed,

		/// <summary>
		/// The case could not be executed by the current implementation or harness.
		/// </summary>
		Inconclusive
	}

	/// <summary>
	/// Creates XML reports that present ICAO Part 3 results as passed, failed, or inconclusive.
	/// </summary>
	public static class IcaoPart3ExecutionReport
	{
		/// <summary>
		/// Creates a report for the provided matrix and coverage results.
		/// </summary>
		/// <param name="Matrix">ICAO Part 3 matrix.</param>
		/// <param name="Results">Coverage results.</param>
		/// <param name="SuiteName">Human-readable suite name.</param>
		/// <returns>Execution report XML.</returns>
		public static XDocument Create(IcaoPart3Matrix Matrix, IReadOnlyCollection<IcaoPart3CoverageResult> Results,
			string SuiteName)
		{
			Dictionary<string, IcaoPart3Case> CasesById = Matrix.Cases
				.ToDictionary(Case => Case.Id, StringComparer.Ordinal);
			IcaoPart3CoverageResult[] OrderedResults = Results
				.OrderBy(Result => Result.CaseId, StringComparer.Ordinal)
				.ToArray();

			return new XDocument(
				new XDeclaration("1.0", "utf-8", null),
				new XElement("icaoPart3ExecutionReport",
					new XAttribute("suite", SuiteName),
					new XAttribute("matrixVersion", Matrix.Version),
					new XAttribute("matrixDate", Matrix.Date),
					new XAttribute("totalCases", OrderedResults.Length),
					CreateOutcomeSummary(OrderedResults),
					CreateCoverageStatusSummary(OrderedResults),
					CreateCapabilitySummary(OrderedResults),
					CreateCategorySummary(OrderedResults, CasesById),
					CreateCaseElements(OrderedResults, CasesById)));
		}

		/// <summary>
		/// Formats a report XML document.
		/// </summary>
		/// <param name="Document">Report document.</param>
		/// <returns>Formatted XML.</returns>
		public static string Format(XDocument Document)
		{
			return Document.Declaration + Environment.NewLine + Document;
		}

		/// <summary>
		/// Maps a detailed coverage status to the high-level execution outcome.
		/// </summary>
		/// <param name="Status">Coverage status.</param>
		/// <returns>Execution outcome.</returns>
		public static IcaoPart3ExecutionOutcome ToOutcome(IcaoPart3CoverageStatus Status)
		{
			return Status switch
			{
				IcaoPart3CoverageStatus.Passed => IcaoPart3ExecutionOutcome.Passed,
				IcaoPart3CoverageStatus.Failed => IcaoPart3ExecutionOutcome.Failed,
				_ => IcaoPart3ExecutionOutcome.Inconclusive
			};
		}

		private static XElement CreateOutcomeSummary(IcaoPart3CoverageResult[] Results)
		{
			return new XElement("outcomes",
				Enum.GetValues<IcaoPart3ExecutionOutcome>()
					.Select(Outcome => new XElement("outcome",
						new XAttribute("name", Outcome.ToString()),
						new XAttribute("count", Results.Count(Result => ToOutcome(Result.Status) == Outcome)))));
		}

		private static XElement CreateCoverageStatusSummary(IcaoPart3CoverageResult[] Results)
		{
			return new XElement("coverageStatuses",
				Enum.GetValues<IcaoPart3CoverageStatus>()
					.Select(Status => new XElement("status",
						new XAttribute("name", Status.ToString()),
						new XAttribute("count", Results.Count(Result => Result.Status == Status)))));
		}

		private static XElement CreateCategorySummary(IcaoPart3CoverageResult[] Results,
			Dictionary<string, IcaoPart3Case> CasesById)
		{
			return new XElement("categories",
				Results
					.Where(Result => CasesById.ContainsKey(Result.CaseId))
					.GroupBy(Result => CasesById[Result.CaseId].Category, StringComparer.Ordinal)
					.OrderBy(Group => Group.Key, StringComparer.Ordinal)
					.Select(Group => new XElement("category",
						new XAttribute("name", Group.Key),
						new XAttribute("passed", Group.Count(Result => ToOutcome(Result.Status) == IcaoPart3ExecutionOutcome.Passed)),
						new XAttribute("failed", Group.Count(Result => ToOutcome(Result.Status) == IcaoPart3ExecutionOutcome.Failed)),
						new XAttribute("inconclusive", Group.Count(Result => ToOutcome(Result.Status) == IcaoPart3ExecutionOutcome.Inconclusive)))));
		}

		private static XElement CreateCapabilitySummary(IcaoPart3CoverageResult[] Results)
		{
			return new XElement("capabilities",
				Results
					.GroupBy(Result => Result.Capability, StringComparer.Ordinal)
					.OrderByDescending(Group => Group.Count())
					.ThenBy(Group => Group.Key, StringComparer.Ordinal)
					.Select(Group => new XElement("capability",
						new XAttribute("name", Group.Key),
						new XAttribute("passed", Group.Count(Result => ToOutcome(Result.Status) == IcaoPart3ExecutionOutcome.Passed)),
						new XAttribute("failed", Group.Count(Result => ToOutcome(Result.Status) == IcaoPart3ExecutionOutcome.Failed)),
						new XAttribute("inconclusive",
							Group.Count(Result => ToOutcome(Result.Status) == IcaoPart3ExecutionOutcome.Inconclusive)))));
		}

		private static XElement CreateCaseElements(IcaoPart3CoverageResult[] Results,
			Dictionary<string, IcaoPart3Case> CasesById)
		{
			return new XElement("cases",
				Results.Select(Result =>
				{
					CasesById.TryGetValue(Result.CaseId, out IcaoPart3Case? Case);
					return new XElement("case",
						new XAttribute("id", Result.CaseId),
						new XAttribute("category", Case?.Category ?? string.Empty),
						new XAttribute("section", Case?.Section ?? string.Empty),
						new XAttribute("outcome", ToOutcome(Result.Status).ToString()),
						new XAttribute("coverageStatus", Result.Status.ToString()),
						new XAttribute("capability", Result.Capability),
						new XAttribute("evidence", GetEvidence(Result)),
						new XAttribute("reason", Result.Reason));
				}));
		}

		private static string GetEvidence(IcaoPart3CoverageResult Result)
		{
			if (Result.Status == IcaoPart3CoverageStatus.Failed)
				return "Failure";

			if (Result.Status != IcaoPart3CoverageStatus.Passed)
				return "NotExecutable";

			if (Result.Reason.StartsWith("Concrete APDUs", StringComparison.Ordinal))
				return "VirtualCardApduHarness";

			if (Result.Reason.StartsWith("Fixture contains", StringComparison.Ordinal))
				return "FixturePresence";

			if (Result.Reason.StartsWith("TravelDocumentsClient", StringComparison.Ordinal))
				return "TravelDocumentsClient";

			if (Result.Reason.StartsWith("JMRTD BAC oracle", StringComparison.Ordinal))
				return "JmrtdBacOracle";

			return "Harness";
		}
	}
}
