using Waher.Content.Markdown;
using Waher.Content.Markdown.Rendering;

namespace NeuroAccessMaui.UI.Rendering
{
	/// <summary>
	/// Interface for code content Maui XAML renderers.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public interface ICodeContentMauiXamlRenderer : ICodeContentRenderer
	{
		/// <summary>
		/// Generates Maui XAML for the code content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Rows">Code rows.</param>
		/// <param name="Language">Language.</param>
		/// <param name="Indent">Code block indentation.</param>
		/// <param name="Document">Markdown document containing element.</param>
		/// <returns>If renderer was able to generate output.</returns>
		Task<bool> RenderMauiXaml(MauiXamlRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document);
	}
}
