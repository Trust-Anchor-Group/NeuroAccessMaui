using System.Text;
using System.Xml;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.HumanReadable;
using Waher.Networking.XMPP.Contracts.HumanReadable.BlockElements;

namespace NeuroAccessMaui.UI.Pages.Contracts
{
	/// <summary>
	/// Picks out what a person reads first about any contract: a short summary from its own text, and its key values. It
	/// uses only what the template provides, so it works for every contract.
	/// </summary>
	public static class ContractDigest
	{
		/// <summary>
		/// How long the summary may get, in characters.
		/// </summary>
		private const int maxSummaryLength = 240;

		/// <summary>
		/// How many sentences the summary may have.
		/// </summary>
		private const int maxSummarySentences = 2;

		/// <summary>
		/// The shortest sentence used in the summary. Shorter ones are usually headings.
		/// </summary>
		private const int minSentenceLength = 20;

		/// <summary>
		/// Gets the summary of a contract: the first sentences of its text that have all their values filled in. A sentence
		/// with a value that isn't known yet, such as a name taken from an ID when someone signs, is left out.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="Parameters">The contract's parameters, to fill in the values.</param>
		/// <returns>The summary, or an empty string when the text has no sentence without gaps.</returns>
		public static string GetSummary(Contract Contract, IEnumerable<ObservableParameter> Parameters)
		{
			HumanReadableText? Text = GetText(Contract);
			if (Text?.Body is null)
				return string.Empty;

			Dictionary<string, ObservableParameter> ByName = new(StringComparer.OrdinalIgnoreCase);
			foreach (ObservableParameter Parameter in Parameters)
				ByName[Parameter.Name] = Parameter;

			List<string> Sentences = [];
			int Length = 0;

			foreach (Paragraph Paragraph in GetParagraphs(Text.Body))
			{
				foreach ((string Sentence, bool HasGap) in GetSentences(Paragraph, ByName))
				{
					if (HasGap || Sentence.Length < minSentenceLength)
						continue;

					if (Sentences.Count > 0 && Length + Sentence.Length > maxSummaryLength)
						return string.Join(" ", Sentences);

					Sentences.Add(Sentence);
					Length += Sentence.Length;

					if (Sentences.Count >= maxSummarySentences)
						return string.Join(" ", Sentences);
				}

				if (Sentences.Count > 0)
					break;
			}

			return string.Join(" ", Sentences);
		}

		/// <summary>
		/// Gets the contract's key values: amounts with a currency or unit first, totals before the rest, then the other
		/// values in the template's order. Values from IDs, hidden values and the currency itself are left out.
		/// </summary>
		/// <param name="Contract">The contract.</param>
		/// <param name="Parameters">The contract's parameters, in the template's order.</param>
		/// <param name="Count">How many values to pick.</param>
		/// <returns>The key values.</returns>
		public static IReadOnlyList<ObservableParameter> GetKeyDetails(Contract Contract, IReadOnlyList<ObservableParameter> Parameters,
			int Count)
		{
			ObservableParameter[] Candidates = [.. Parameters.Where(Parameter =>
				Parameter is not ObservableRoleParameter &&
				Parameter.Parameter is not AttachmentParameter &&
				!Parameter.IsValueHidden &&
				!Parameter.RowIsMuted &&
				!ParameterUnit.IsCurrency(Parameter.Parameter, Contract))];

			IEnumerable<ObservableParameter> Amounts = Candidates
				.Where(Parameter => !string.IsNullOrEmpty(Parameter.UnitText) && Parameter.UnitText != "%")
				.OrderBy(Parameter => Parameter.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) ? 0 : 1);

			return [.. Amounts.Concat(Candidates).Distinct().Take(Count)];
		}

		/// <summary>
		/// Gets the contract's text in the phone's language, or else its first text.
		/// </summary>
		private static HumanReadableText? GetText(Contract Contract)
		{
			HumanReadableText[]? Texts = Contract.ForHumans;
			if (Texts is null || Texts.Length == 0)
				return null;

			string Language = Contract.DeviceLanguage();
			return Texts.FirstOrDefault(Text => string.Equals(Text.Language, Language, StringComparison.OrdinalIgnoreCase)) ?? Texts[0];
		}

		/// <summary>
		/// Gets the paragraphs of a text in reading order, also those inside sections.
		/// </summary>
		private static IEnumerable<Paragraph> GetParagraphs(IEnumerable<BlockElement> Blocks)
		{
			foreach (BlockElement Block in Blocks)
			{
				if (Block is Paragraph Paragraph)
					yield return Paragraph;
				else if (Block is Section Section && Section.Body is not null)
				{
					foreach (Paragraph Inner in GetParagraphs(Section.Body))
						yield return Inner;
				}
			}
		}

		/// <summary>
		/// Splits a paragraph into sentences, with the parameters' values filled in.
		/// </summary>
		/// <param name="Paragraph">The paragraph.</param>
		/// <param name="Parameters">The contract's parameters, by name.</param>
		/// <returns>Each sentence, and if it has a value that isn't known yet.</returns>
		private static List<(string Sentence, bool HasGap)> GetSentences(Paragraph Paragraph, Dictionary<string, ObservableParameter> Parameters)
		{
			StringBuilder Xml = new();
			Paragraph.Serialize(Xml);

			XmlDocument Doc = new() { XmlResolver = null };
			try
			{
				Doc.LoadXml(Xml.ToString());
			}
			catch (XmlException)
			{
				return [];
			}

			List<(string, bool)> Result = [];
			StringBuilder Current = new();
			bool HasGap = false;

			void Add(string Piece)
			{
				foreach (char Ch in Piece)
				{
					Current.Append(Ch);

					if (Ch is '.' or '!' or '?')
					{
						Result.Add((Normalize(Current.ToString()), HasGap));
						Current.Clear();
						HasGap = false;
					}
				}
			}

			void Walk(XmlNode Node)
			{
				foreach (XmlNode Child in Node.ChildNodes)
				{
					if (Child is XmlText or XmlWhitespace or XmlSignificantWhitespace)
						Add(Child.Value ?? string.Empty);
					else if (Child is XmlElement Element && Element.LocalName == "parameter")
					{
						string Name = Element.GetAttribute("name");

						if (Parameters.TryGetValue(Name, out ObservableParameter? Parameter) && !Parameter.RowIsMuted)
							Current.Append(Parameter.RowValue);
						else
							HasGap = true;
					}
					else
						Walk(Child);
				}
			}

			if (Doc.DocumentElement is not null)
				Walk(Doc.DocumentElement);

			if (Current.ToString().Trim().Length > 0)
				Result.Add((Normalize(Current.ToString()), HasGap));

			return Result;
		}

		/// <summary>
		/// Joins the spaces in a sentence, and puts no space before punctuation.
		/// </summary>
		private static string Normalize(string Sentence)
		{
			string Text = System.Text.RegularExpressions.Regex.Replace(Sentence, @"\s+", " ").Trim();
			return System.Text.RegularExpressions.Regex.Replace(Text, @"\s+([.,;:!?)])", "$1").Replace("( ", "(");
		}
	}
}
