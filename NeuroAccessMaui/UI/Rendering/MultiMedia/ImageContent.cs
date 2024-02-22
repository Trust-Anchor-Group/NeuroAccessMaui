using System.Globalization;
using System.Xml;
using Waher.Content.Emoji;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;

namespace NeuroAccessMaui.UI.Rendering.Multimedia
{
	/// <summary>
	/// Image content.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class ImageContent : Waher.Content.Markdown.Model.Multimedia.ImageContent, IMultimediaXamarinFormsXamlRenderer
	{
		/// <summary>
		/// Image content.
		/// </summary>
		public ImageContent()
		{
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML for the markdown element.
		/// </summary>
		/// <param name="Renderer">Renderer</param>
		/// <param name="Items">Multimedia items.</param>
		/// <param name="ChildNodes">Child nodes.</param>
		/// <param name="AloneInParagraph">If the element is alone in a paragraph.</param>
		/// <param name="Document">Markdown document containing element.</param>
		public Task RenderXamarinFormsXaml(XamarinFormsXamlRenderer Renderer, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes,
			bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MultimediaItem Item in Items)
			{
				return OutputXamarinForms(Renderer.XmlOutput, new Waher.Content.Emoji.ImageSource()
				{
					Url = Document.CheckURL(Item.Url, null),
					Width = Item.Width,
					Height = Item.Height
				});
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Outputs an image to Xamarin XAML
		/// </summary>
		/// <param name="Output">WPF XAML output.</param>
		/// <param name="Source">Image source.</param>
		public static async Task OutputXamarinForms(XmlWriter Output, Waher.Content.Emoji.IImageSource Source)
		{
			Source = await CheckDataUri(Source);

			Output.WriteStartElement("Image");
			Output.WriteAttributeString("Source", Source.Url);

			if (Source.Width.HasValue)
				Output.WriteAttributeString("WidthRequest", Source.Width.Value.ToString(CultureInfo.InvariantCulture));

			if (Source.Height.HasValue)
				Output.WriteAttributeString("HeightRequest", Source.Height.Value.ToString(CultureInfo.InvariantCulture));

			// TODO: Tooltip

			Output.WriteEndElement();
		}
	}
}
