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
		public async Task<bool> RenderMauiXaml(MauiXamlRenderer Renderer, string[] Rows, string Language, int Indent, 
			MarkdownDocument Document)
		{
			string Text = Waher.Content.Markdown.Rendering.CodeContent.TextContent.DecodeBase64EncodedText(Rows, ref Language);
			Text = Rendering.CodeContent.TextContent.MakePretty(Text, Language);

			Rows = Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

			ContentView Bakup = (ContentView)this.currentElement;
			VerticalStackLayout BlockStackLayout = new();
			int i, c = Rows.Length;

			for (i = 0; i < c; i++)
			{
				Label ContentLabel = new Label
				{
					LineBreakMode = LineBreakMode.NoWrap,
					HorizontalTextAlignment = this.LabelAlignment(),
					FontFamily = "SpaceGroteskRegular",
					Text = Rows[i]
				};

				BlockStackLayout.Add(ContentLabel);
			}

			return true;
		}
	}
}
