﻿using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Content.Markdown.Rendering;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.UI.Rendering.Multimedia
{
	/// <summary>
	/// Implements the iotdisco URI Scheme
	/// </summary>
	public class IotDiscoUriScheme : MultimediaContent, IMultimediaMauiXamlRenderer
	{
		/// <summary>
		/// Implements the iotdisco URI Scheme
		/// </summary>
		public IotDiscoUriScheme()
		{
		}

		/// <inheritdoc/>
		public override Grade Supports(MultimediaItem Item)
		{
			if (Item.Url.StartsWith("iotdisco:", StringComparison.OrdinalIgnoreCase))
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
		public async Task RenderMauiXaml(MauiXamlRenderer Renderer, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			XmlWriter Output = Renderer.XmlOutput;

			foreach (MultimediaItem Item in Items)
			{
				Output.WriteStartElement("VerticalStackLayout");
				Output.WriteAttributeString("HorizontalOptions", "Center");

				Output.WriteStartElement("Path");
				Output.WriteAttributeString("VerticalOptions", "Center");
				Output.WriteAttributeString("HorizontalOptions", "Center");
				Output.WriteAttributeString("HeightRequest", "16");
				Output.WriteAttributeString("WidthRequest", "16");
				Output.WriteAttributeString("Aspect", "Uniform");
				Output.WriteAttributeString("Fill", "{AppThemeBinding Light={DynamicResource ContentPrimaryWLLight}, Dark={DynamicResource ContentPrimaryWLDark}}");
				Output.WriteAttributeString("Data", "{x:Static ui:Geometries.ThingPath}");
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
				Output.WriteAttributeString("Command", "{Binding Path=IotDiscoUriClicked}");
				Output.WriteAttributeString("CommandParameter", Item.Url);
				Output.WriteEndElement();

				Output.WriteEndElement();
				Output.WriteEndElement();
				break;
			}
		}
	}
}
