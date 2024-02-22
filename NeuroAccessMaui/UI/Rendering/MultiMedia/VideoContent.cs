using System.Globalization;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;

namespace NeuroAccessMaui.UI.Rendering.Multimedia
{
	/// <summary>
	/// Video content.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class VideoContent : Waher.Content.Markdown.Model.Multimedia.VideoContent, IMultimediaXamarinFormsXamlRenderer
	{
		/// <summary>
		/// Video content.
		/// </summary>
		public VideoContent()
		{
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML for the multimedia content.
		/// </summary>
		/// <param name="Renderer">Renderer.</param>
		/// <param name="Items">Multimedia items.</param>
		/// <param name="ChildNodes">Child nodes.</param>
		/// <param name="AloneInParagraph">If the element is alone in a paragraph.</param>
		/// <param name="Document">Markdown document containing element.</param>
		public Task RenderXamarinFormsXaml(XamarinFormsXamlRenderer Renderer, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, 
			bool AloneInParagraph, MarkdownDocument Document)
		{
			XmlWriter Output = Renderer.XmlOutput;

			foreach (MultimediaItem Item in Items)
			{
				Output.WriteStartElement("MediaElement");
				Output.WriteAttributeString("Source", Document.CheckURL(Item.Url, Document.URL));

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
