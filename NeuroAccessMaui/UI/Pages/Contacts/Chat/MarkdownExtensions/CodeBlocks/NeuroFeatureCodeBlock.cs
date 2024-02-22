using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Rendering;
using NeuroFeatures;
using System.Text;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.MarkdownExtensions.CodeBlocks
{
	/// <summary>
	/// Handles embedded tokens.
	/// </summary>
	public class NeuroFeatureCodeBlock : ICodeContent, ICodeContentXamarinFormsXamlRenderer
	{
		private MarkdownDocument? document;

		/// <summary>
		/// Handles embedded tokens.
		/// </summary>
		public NeuroFeatureCodeBlock()
		{
		}

		/// <summary>
		/// Markdown document.
		/// </summary>
		public MarkdownDocument? Document => this.document;

		/// <summary>
		/// Generates Xamarin XAML
		/// </summary>
		public Task<bool> RenderXamarinFormsXaml(XamarinFormsXamlRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			XmlWriter Output = Renderer.XmlOutput;
			Token Token;

			try
			{
				StringBuilder sb = new();

				foreach (string Row in Rows)
					sb.AppendLine(Row);

				XmlDocument Doc = new()
				{
					PreserveWhitespace = true
				};
				Doc.LoadXml(sb.ToString());

				if (!NeuroFeatures.Token.TryParse(Doc.DocumentElement, out Token))
					throw new Exception(ServiceRef.Localizer[nameof(AppResources.InvalidNeuroFeatureToken)]);
			}
			catch (Exception ex)
			{
				Output.WriteStartElement("Label");
				Output.WriteAttributeString("Text", ex.Message);
				Output.WriteAttributeString("FontFamily", "Courier New");
				Output.WriteAttributeString("TextColor", "Red");
				Output.WriteEndElement();

				return Task.FromResult(false);
			}

			Output.WriteStartElement("VerticalStackLayout");
			Output.WriteAttributeString("HorizontalOptions", "Center");

			Output.WriteStartElement("Label");
			Output.WriteAttributeString("Text", "✻"); // TODO: SVG icon
			Output.WriteAttributeString("FontSize", "Large");
			Output.WriteAttributeString("HorizontalOptions", "Center");
			Output.WriteEndElement();

			Output.WriteStartElement("Label");
			Output.WriteAttributeString("LineBreakMode", "WordWrap");
			Output.WriteAttributeString("FontSize", "Medium");
			Output.WriteAttributeString("HorizontalOptions", "Center");
			Output.WriteAttributeString("Text", Token.FriendlyName);
			Output.WriteEndElement();

			Output.WriteStartElement("StackLayout.GestureRecognizers");

			Output.WriteStartElement("TapGestureRecognizer");
			Output.WriteAttributeString("Command", "{Binding Path=NeuroFeatureUriClicked}");
			Output.WriteAttributeString("CommandParameter", Constants.UriSchemes.NeuroFeature + ":" + Token.ToXml());
			Output.WriteEndElement();

			Output.WriteEndElement();
			Output.WriteEndElement();

			return Task.FromResult(true);
		}

		/// <summary>
		/// Registers the Markdown document in which the construct resides.
		/// </summary>
		/// <param name="Document">Markdown document</param>
		public void Register(MarkdownDocument Document)
		{
			this.document = Document;
		}

		/// <summary>
		/// How much the module supports code of a given language (i.e. type of content)
		/// </summary>
		/// <param name="Language">Code language</param>
		/// <returns>Grade of support.</returns>
		public Grade Supports(string Language)
		{
			return string.Equals(Language, Constants.UriSchemes.NeuroFeature, StringComparison.OrdinalIgnoreCase) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
