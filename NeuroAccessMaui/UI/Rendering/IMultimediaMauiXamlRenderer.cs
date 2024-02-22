using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Rendering;

namespace NeuroAccessMaui.UI.Rendering
{
	/// <summary>
	/// Interface for multimedia content Maui XAML renderers.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public interface IMultimediaMauiXamlRenderer : IMultimediaRenderer
	{
		/// <summary>
		/// Generates Maui XAML for the multimedia content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Items">Multimedia items.</param>
		/// <param name="ChildNodes">Child nodes.</param>
		/// <param name="AloneInParagraph">If the element is alone in a paragraph.</param>
		/// <param name="Document">Markdown document containing element.</param>
		Task RenderXamarinFormsXaml(MauiXamlRenderer Renderer, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, 
			bool AloneInParagraph, MarkdownDocument Document);
	}
}
