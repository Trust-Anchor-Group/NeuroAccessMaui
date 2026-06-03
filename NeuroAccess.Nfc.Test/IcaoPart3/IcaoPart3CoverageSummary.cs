using System.Xml.Linq;

namespace NeuroAccess.Nfc.Test.IcaoPart3
{
	/// <summary>
	/// Creates stable XML summaries for ICAO Part 3 coverage results.
	/// </summary>
	public static class IcaoPart3CoverageSummary
	{
		/// <summary>
		/// Creates a coverage summary XML document.
		/// </summary>
		/// <param name="Matrix">Matrix used to classify the cases.</param>
		/// <param name="Results">Coverage results to summarize.</param>
		/// <returns>Coverage summary XML document.</returns>
		public static XDocument Create(IcaoPart3Matrix Matrix, IReadOnlyCollection<IcaoPart3CoverageResult> Results)
		{
			IcaoPart3CoverageResult[] OrderedResults = Results
				.OrderBy(Result => Result.CaseId, StringComparer.Ordinal)
				.ToArray();

			XElement Statuses = new("statuses",
				Enum.GetValues<IcaoPart3CoverageStatus>()
					.Select(Status => CreateStatusElement(Status, OrderedResults)));

			return new XDocument(
				new XDeclaration("1.0", "utf-8", null),
				new XElement("icaoPart3CoverageSummary",
					new XAttribute("matrixVersion", Matrix.Version),
					new XAttribute("matrixDate", Matrix.Date),
					new XAttribute("totalCases", OrderedResults.Length),
					CreateCapabilitiesElement(OrderedResults),
					Statuses));
		}

		/// <summary>
		/// Formats a coverage summary XML document for committed baseline comparison.
		/// </summary>
		/// <param name="Document">Document to format.</param>
		/// <returns>Formatted XML.</returns>
		public static string Format(XDocument Document)
		{
			return Document.Declaration + Environment.NewLine + Document;
		}

		private static XElement CreateStatusElement(IcaoPart3CoverageStatus Status,
			IcaoPart3CoverageResult[] Results)
		{
			IcaoPart3CoverageResult[] StatusResults = Results
				.Where(Result => Result.Status == Status)
				.ToArray();

			XElement Element = new("status",
				new XAttribute("name", Status.ToString()),
				new XAttribute("count", StatusResults.Length));

			foreach (IGrouping<string, IcaoPart3CoverageResult> ReasonGroup in StatusResults
				.GroupBy(Result => Result.Reason, StringComparer.Ordinal)
				.OrderByDescending(Group => Group.Count())
				.ThenBy(Group => Group.Key, StringComparer.Ordinal))
			{
				Element.Add(new XElement("reason",
					new XAttribute("count", ReasonGroup.Count()),
					new XAttribute("text", ReasonGroup.Key),
					ReasonGroup
						.OrderBy(Result => Result.CaseId, StringComparer.Ordinal)
						.Take(8)
						.Select(Result => new XElement("case", new XAttribute("id", Result.CaseId)))));
			}

			return Element;
		}

		private static XElement CreateCapabilitiesElement(IcaoPart3CoverageResult[] Results)
		{
			return new XElement("capabilities",
				Results
					.GroupBy(Result => Result.Capability, StringComparer.Ordinal)
					.OrderByDescending(Group => Group.Count())
					.ThenBy(Group => Group.Key, StringComparer.Ordinal)
					.Select(Group => new XElement("capability",
						new XAttribute("name", Group.Key),
						new XAttribute("count", Group.Count()),
						new XAttribute("passed", Group.Count(Result => Result.Status == IcaoPart3CoverageStatus.Passed)),
						new XAttribute("blockedSecureMessaging",
							Group.Count(Result => Result.Status == IcaoPart3CoverageStatus.BlockedSecureMessaging)),
						new XAttribute("blockedMissingFixture",
							Group.Count(Result => Result.Status == IcaoPart3CoverageStatus.BlockedMissingFixture)),
						new XAttribute("blockedUnsupportedProtocol",
							Group.Count(Result => Result.Status == IcaoPart3CoverageStatus.BlockedUnsupportedProtocol)),
						Group
							.OrderBy(Result => Result.CaseId, StringComparer.Ordinal)
							.Take(8)
							.Select(Result => new XElement("case",
								new XAttribute("id", Result.CaseId),
								new XAttribute("status", Result.Status.ToString()))))));
		}
	}
}
