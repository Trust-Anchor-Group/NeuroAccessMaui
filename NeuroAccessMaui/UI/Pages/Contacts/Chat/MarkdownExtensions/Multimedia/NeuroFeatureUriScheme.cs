using NeuroAccessMaui.UI.Rendering;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Rendering;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.MarkdownExtensions.Multimedia
{
	/// <summary>
	/// Implements the nfeat URI Scheme
	/// </summary>
	public class NeuroFeatureUriScheme : MultimediaContent, IMultimediaMauiXamlRenderer
	{
		/// <summary>
		/// Implements the nfeat URI Scheme
		/// </summary>
		public NeuroFeatureUriScheme()
		{
		}

		/// <inheritdoc/>
		public override Grade Supports(MultimediaItem Item)
		{
			if (Item.Url.StartsWith("nfeat:", StringComparison.OrdinalIgnoreCase))
				return Grade.Excellent;
			else
				return Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public override bool EmbedInlineLink(string Url)
		{
			return true;
		}

		/// <inheritdoc/>
		public async Task RenderXamarinFormsXaml(MauiXamlRenderer Renderer, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			XmlWriter Output = Renderer.XmlOutput;

			foreach (MultimediaItem Item in Items)
			{
				Output.WriteStartElement("VerticalStackLayout");
				Output.WriteAttributeString("HorizontalOptions", "Center");

				Output.WriteStartElement("Label");
				Output.WriteAttributeString("Text", "✻"); // TODO: SVG icon
				Output.WriteAttributeString("FontSize", "Large");
				Output.WriteAttributeString("HorizontalOptions", "Center");
				Output.WriteEndElement();

				Output.WriteStartElement("VerticalStackLayout");
				Output.WriteAttributeString("HorizontalOptions", "Center");

				Output.WriteStartElement("Label");
				Output.WriteAttributeString("LineBreakMode", "WordWrap");
				Output.WriteAttributeString("TextType", "Html");
				Output.WriteAttributeString("FontSize", "Medium");

				using HtmlRenderer Html = new(new HtmlSettings()
				{
					XmlEntitiesOnly = true
				});

				foreach (MarkdownElement E in ChildNodes)
					await E.Render(Html);

				Output.WriteValue(Html.ToString());
				Output.WriteEndElement();

				Output.WriteEndElement();

				Output.WriteStartElement("VerticalStackLayout.GestureRecognizers");

				Output.WriteStartElement("TapGestureRecognizer");
				Output.WriteAttributeString("Command", "{Binding Path=NeuroFeatureUriClicked}");
				Output.WriteAttributeString("CommandParameter", Item.Url);
				Output.WriteEndElement();

				Output.WriteEndElement();
				Output.WriteEndElement();
				break;
			}
		}
	}
}
