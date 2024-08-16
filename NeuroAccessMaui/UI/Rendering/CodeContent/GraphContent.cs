using Waher.Content.Markdown;
using Waher.Events;
using Waher.Script;
using Waher.Script.Graphs;

namespace NeuroAccessMaui.UI.Rendering.CodeContent
{
	/// <summary>
	/// Script graph content.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class GraphContent : Waher.Content.Markdown.Model.CodeContent.GraphContent, ICodeContentMauiXamlRenderer
	{
		/// <summary>
		/// Script graph content.
		/// </summary>
		public GraphContent()
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
			try
			{
				Graph G = await GetGraph(Rows);
				await Renderer.RenderObject(G, true, Document.Settings.Variables ?? []);
				return true;
			}
			catch (Exception ex)
			{
				Log.Exception(ex);
				return false;
			}
		}
	}
}
