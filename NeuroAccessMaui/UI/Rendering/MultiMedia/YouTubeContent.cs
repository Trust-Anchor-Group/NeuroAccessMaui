using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;

namespace NeuroAccessMaui.UI.Rendering.Multimedia
{
	/// <summary>
	/// YouTube content.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class YouTubeContent : Waher.Content.Markdown.Model.Multimedia.YouTubeContent, IMultimediaMauiXamlRenderer
	{
		/// <summary>
		/// YouTube content.
		/// </summary>
		public YouTubeContent()
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
		public Task RenderMauiXaml(MauiXamlRenderer Renderer, MultimediaItem[] Items,
			IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			XmlWriter Output = Renderer.XmlOutput;

			foreach (MultimediaItem Item in Items)
			{
				Output.WriteStartElement("WebView");

				Match M = youTubeLink.Match(Item.Url);
				if (M.Success)
					Output.WriteAttributeString("Source", M.Groups["Scheme"].Value + "://www.youtube.com/embed/" + M.Groups["VideoId"].Value);
				else
					Output.WriteAttributeString("Source", Item.Url);

				if (Item.Width.HasValue)
					Output.WriteAttributeString("WidthRequest", Item.Width.Value.ToString(CultureInfo.InvariantCulture));

				if (Item.Height.HasValue)
					Output.WriteAttributeString("HeightRequest", Item.Height.Value.ToString(CultureInfo.InvariantCulture));

				// TODO: Tooltip

				Output.WriteEndElement();

				break;
			}

			return Task.CompletedTask;
		}
	}
}
