using NeuroAccessMaui.UI.Rendering;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Functions;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.HumanReadable;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="Contract"/> class.
	/// </summary>
	public static class ContractExtensions
	{
		/// <summary>
		/// Returns the language to use when displaying a contract on the device
		/// </summary>
		/// <param name="Contract">Contract</param>
		/// <returns>Language to use when displaying contract.</returns>
		public static string DeviceLanguage(this Contract Contract)
		{
			string[] Languages = Contract.GetLanguages();
			string Language = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

			foreach (string Option in Languages)
			{
				if (string.Equals(Option.Before("-"), Language, StringComparison.OrdinalIgnoreCase))
					return Option;
			}

			return Contract.DefaultLanguage;
		}

		/// <summary>
		/// Creates a human-readable XAML document for the contract.
		/// </summary>
		/// <param name="Contract">Contract reference.</param>
		/// <param name="Language">Desired language</param>
		/// <returns>Markdown</returns>
		public static Task<string> ToMauiXaml(this Contract Contract, string Language)
		{
			return Contract.ToMauiXaml(Contract.ForHumans, Language);
		}

		/// <summary
		/// Creates a human-readable VerticalStackLayout for the Contract.
		/// </summary>
		/// <param name="Contract">Contract reference.</param>
		/// <param name="Language">Desired language</param>
		/// <returns>Markdown</returns>
		public static Task<VerticalStackLayout?> ToMaui(this Contract Contract, string Language)
		{
			return Contract.ToMaui(Contract.ForHumans, Language);
		}

		/// <summary>
		/// Selects a human-readable text, and generates a XAML document from it.
		/// </summary>
		/// <param name="Contract">Contract reference.</param>
		/// <param name="Text">Collection of texts in different languages.</param>
		/// <param name="Language">Language</param>
		/// <returns>XAML document.</returns>
		public static Task<string> ToMauiXaml(this Contract Contract, HumanReadableText[] Text, string Language)
		{
			return Contract.Select(Text, Language)?.ToMauiXaml(Contract) ?? Task.FromResult<string>(string.Empty);
		}

		/// <summary>
		/// Selects a human-readable text, and generates a VerticalStackLayout from it.
		/// </summary>
		/// <param name="Contract">Contract reference.</param>
		/// <param name="Text">Collection of texts in different languages.</param>
		/// <param name="Language">Language</param>
		/// <returns>VerticalStackLayout</returns>
		public static Task<VerticalStackLayout?> ToMaui(this Contract Contract, HumanReadableText[] Text, string Language)
		{
			return Contract.Select(Text, Language)?.ToMaui(Contract) ?? Task.FromResult<VerticalStackLayout?>(null);
		}

		/// <summary>
		/// Generates XAML for the human-readable text.
		/// </summary>
		/// <param name="Text">Human-readable text being rendered.</param>
		/// <param name="Contract">Contract, of which the human-readable text is part.</param>
		/// <returns>XAML</returns>
		public static async Task<string> ToMauiXaml(this HumanReadableText Text, Contract Contract)
		{
			MarkdownDocument Doc = await Text.GenerateMarkdownDocument(Contract);
			return await Doc.GenerateMauiXaml();
		}

		/// <summary>
		/// Generates a VerticalStackLayout for the human-readable text.
		/// </summary>
		/// <param name="Text">Human-readable text being rendered.</param>
		/// <param name="Contract">Contract, of which the human-readable text is part.</param>
		/// <returns>VerticalStackLayout</returns>
		public static async Task<VerticalStackLayout?> ToMaui(this HumanReadableText Text, Contract Contract)
		{
			//MarkdownDocument Doc = await Text.GenerateMarkdownDocument(Contract);

			string str =
				"""
				### Headings

				# h1 Heading

				## h2 Heading

				### h3 Heading

				#### h4 Heading

				##### h5 Heading

				###### h6 Heading

				### Links

				[Link text](mailto:tobias.hansson@trustanchorgroup.com)

				[Link with title](https://github.com/trust-anchor-group)

				### Images

				![Markdown logo](https://www.fullstackpython.com/img/logos/markdown.png)
				![Syki Logo](/logo512.png 'My logo')

				### Lists

				#### Unordered

				-   Lorem ipsum dolor sit amet
				-   Lorem ipsum dolor sit amet
				    -   Lorem ipsum dolor sit amet
				        -   Lorem ipsum dolor sit amet
				        -   Lorem ipsum dolor sit amet
				        -   Lorem ipsum dolor sit amet
				-   Lorem ipsum dolor sit amet

				#### Ordered

				1. Lorem ipsum dolor sit amet
				2. Lorem ipsum dolor sit amet
				3. Lorem ipsum dolor sit amet

				Start numbering with offset:

				57. Lorem ipsum dolor sit amet
				1. Lorem ipsum dolor sit amet

				#### Checkboxes

				-   [ ] Lorem ipsum dolor sit amet
				-   [x] Lorem ipsum dolor sit amet
				-   [ ] Lorem ipsum dolor sit amet

				### Emphasis

				**Bold text**

				_Italic text_

				~~Strikethrough~~

				### Horizontal Rule

				---

				### Blockquotes

				> Blockquotes
				>
				> > Nested blockquotes
				> >
				> > > Nested blockquotes

				### Code

				Inline `code`

				```
				Sample text here...
				```

				Syntax highlighting

				```js
				var foo = function (bar) {
				    return bar++
				}

				console.log(foo(5))
				```

				### Tables

				| Heading1 | Heading2                   |
				| -------- | -------------------------- |
				| row1     | Lorem ipsum dolor sit amet |
				| row2     | Lorem ipsum dolor sit amet |
				| row3     | Lorem ipsum dolor sit amet |

				Right aligned columns

				| Heading1 |                   Heading2 |
				| -------: | -------------------------: |
				|     row1 | Lorem ipsum dolor sit amet |
				|     row2 | Lorem ipsum dolor sit amet |
				|     row3 | Lorem ipsum dolor sit amet |

				### HTML

				This is inline <span style="color: red;">html</span>

				<audio controls>
				    <source src="/uploads/medium-drill-burst.mp3" type="audio`/mpeg" />
				    Your browser does not support the audio element.
				</audio>

				### XSS Atack

				<script>alert('XSS Atack. When you see this you should use sanitizer.')</script>
				
				""";

			MarkdownDocument Doc = await MarkdownDocument.CreateAsync(str);

			return await Doc.GenerateMaui();
		}

		/// <summary>
		/// Creates a human-readable XAML document for the contract.
		/// </summary>
		/// <param name="Description">Description being rendered.</param>
		/// <param name="Language">Desired language</param>
		/// <param name="Contract">Contract hosting the object.</param>
		/// <returns>Markdown</returns>
		public static Task<string> ToMauiXaml(this LocalizableDescription Description, string Language, Contract Contract)
		{
			return Contract.ToMauiXaml(Description.Descriptions, Language);
		}
	}
}
