using IdApp.Resx;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.MarkdownExtensions.Multimedia
{
	/// <summary>
	/// Implements the nfeat URI Scheme
	/// </summary>
	public class NeuroFeatureUriScheme : MultimediaContent
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
		public override async Task GenerateHTML(StringBuilder Output, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MarkdownElement E in ChildNodes)
				await E.GenerateHTML(Output);
		}

		/// <inheritdoc/>
		public override async Task GenerateLaTeX(StringBuilder Output, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MarkdownElement E in ChildNodes)
				await E.GenerateLaTeX(Output);
		}

		/// <inheritdoc/>
		public override async Task GenerateXAML(XmlWriter Output, Waher.Content.Markdown.Model.TextAlignment TextAlignment, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MarkdownElement E in ChildNodes)
				await E.GenerateXAML(Output, TextAlignment);
		}

		/// <inheritdoc/>
		public override async Task GenerateXamarinForms(XmlWriter Output, XamarinRenderingState State, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MultimediaItem Item in Items)
			{
				Output.WriteStartElement("StackLayout");
				Output.WriteAttributeString("Orientation", "Vertical");
				Output.WriteAttributeString("HorizontalOptions", "Center");

				Output.WriteStartElement("Label");
				Output.WriteAttributeString("Text", "✻"); // TODO: SVG icon
				Output.WriteAttributeString("FontSize", "Large");
				Output.WriteAttributeString("HorizontalOptions", "Center");
				Output.WriteEndElement();

				Output.WriteStartElement("StackLayout");
				Output.WriteAttributeString("Orientation", "Horizontal");
				Output.WriteAttributeString("HorizontalOptions", "Center");

				Output.WriteStartElement("Label");
				Output.WriteAttributeString("LineBreakMode", "WordWrap");
				Output.WriteAttributeString("TextType", "Html");
				Output.WriteAttributeString("FontSize", "Medium");
				StringBuilder Html = new();

				foreach (MarkdownElement E in ChildNodes)
					await E.GenerateHTML(Html);

				Output.WriteValue(Html.ToString());
				Output.WriteEndElement();

				Output.WriteEndElement();

				Output.WriteStartElement("StackLayout.GestureRecognizers");

				Output.WriteStartElement("TapGestureRecognizer");
				Output.WriteAttributeString("Command", "{Binding Path=NeuroFeatureUriClicked}");
				Output.WriteAttributeString("CommandParameter", Item.Url);
				Output.WriteEndElement();

				Output.WriteEndElement();
				Output.WriteEndElement();
				break;
			}
		}

		/// <inheritdoc/>
		public override async Task GenerateSmartContractXml(XmlWriter Output, SmartContractRenderState State, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MarkdownElement E in ChildNodes)
				await E.GenerateSmartContractXml(Output, State);
		}
	}
}
