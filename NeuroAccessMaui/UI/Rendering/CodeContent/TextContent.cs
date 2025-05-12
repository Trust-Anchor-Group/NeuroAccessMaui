using System.Xml;
using Waher.Content.Markdown; 

namespace NeuroAccessMaui.UI.Rendering.CodeContent
{
	/// <summary>
	/// Base64-encoded image content.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class TextContent : Waher.Content.Markdown.Model.CodeContent.TextContent, ICodeContentMauiXamlRenderer
	{
		/// <summary>
		/// Base64-encoded image content.
		/// </summary>
		public TextContent()
		{
		}

		/// <summary>
		/// Generates Maui XAML for the code content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language.</param>
		/// <param name="Indent">Code block indentation.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If renderer was able to generate output.</returns>
		public Task<bool> RenderMauiXaml(
			MauiXamlRenderer Renderer,
			string[] Rows,
			string Language,
			int Indent,
			MarkdownDocument Document)
		{
			// Decode and prettify
			string Text = Waher.Content.Markdown.Rendering.CodeContent.TextContent.DecodeBase64EncodedText(Rows, ref Language);
			Text = Waher.Content.Markdown.Rendering.CodeContent.TextContent.MakePretty(Text, Language);
			Rows = Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

			XmlWriter Output = Renderer.XmlOutput;

			Output.WriteStartElement("VerticalStackLayout");

			// Emit each line as a Label
			foreach (string Row in Rows)
			{
				// Map Markdown alignment to MAUI HorizontalTextAlignment
				string Alignment = Renderer.Alignment switch
				{
					Waher.Content.Markdown.Model.TextAlignment.Center => "Center",
					Waher.Content.Markdown.Model.TextAlignment.Right => "End",
					_ => "Start",
				};

				Output.WriteStartElement("Label");
				Output.WriteAttributeString("LineBreakMode", "NoWrap");
				Output.WriteAttributeString("HorizontalTextAlignment", Alignment);
				Output.WriteAttributeString("FontFamily", "SpaceGroteskRegular");
				// If you want to preserve leading spaces, you may need to wrap with a Span or encode them
				Output.WriteAttributeString("Text", Row);
				Output.WriteEndElement();
			}

			Output.WriteEndElement();

			return Task.FromResult(true);
		}
	}
}
