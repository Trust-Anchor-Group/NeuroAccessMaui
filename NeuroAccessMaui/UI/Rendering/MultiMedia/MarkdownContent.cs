using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;

namespace NeuroAccessMaui.UI.Rendering.Multimedia
{
	/// <summary>
	/// Markdown content.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class MarkdownContent : Waher.Content.Markdown.Model.Multimedia.MarkdownContent, IMultimediaMauiXamlRenderer
    {
        /// <summary>
        /// Markdown content.
        /// </summary>
        public MarkdownContent()
        {
        }

		/// <summary>
		/// Generates Maui XAML for the multimedia content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Items">Multimedia items.</param>
		/// <param name="ChildNodes">Child nodes.</param>
		/// <param name="AloneInParagraph">If the element is alone in a paragraph.</param>
		/// <param name="Document">Markdown document containing element.</param>
		public Task RenderMauiXaml(MauiXamlRenderer Renderer, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes,
			bool AloneInParagraph, MarkdownDocument Document)
		{
			return ProcessInclusion(Renderer, Items, Document);
		}
	}
}
