using Waher.Content.Markdown;

namespace NeuroAccessMaui.UI.Rendering.CodeContent
{
	/// <summary>
	/// Base64-encoded image content.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class ImageContent : Waher.Content.Markdown.Model.CodeContent.ImageContent, ICodeContentMauiXamlRenderer
	{
		/// <summary>
		/// Base64-encoded image content.
		/// </summary>
		public ImageContent()
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
			await Multimedia.ImageContent.OutputMauiXaml(Renderer.XmlOutput, new Waher.Content.Emoji.ImageSource()
			{
				Url = GenerateUrl(Language, Rows, out _, out _)
			});

			return true;
		}
	}
}
