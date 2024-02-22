using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Rendering;
using System.Globalization;
using System.Text;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.MarkdownExtensions.CodeBlocks
{
	/// <summary>
	/// Handles embedded Smart Contracts.
	/// </summary>
	public class IoTScCodeBlock : ICodeContent, ICodeContentMauiXamlRenderer
	{
		private MarkdownDocument? document;

		/// <summary>
		/// Handles embedded Smart Contracts.
		/// </summary>
		public IoTScCodeBlock()
		{
		}

		/// <summary>
		/// Markdown document.
		/// </summary>
		public MarkdownDocument? Document => this.document;

		/// <summary>
		/// Generates Xamarin XAML
		/// </summary>
		public async Task<bool> RenderMauiXaml(MauiXamlRenderer Renderer, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			XmlWriter Output = Renderer.XmlOutput;
			Contract Contract;

			try
			{
				StringBuilder sb = new();

				foreach (string Row in Rows)
					sb.AppendLine(Row);

				XmlDocument Doc = new()
				{
					PreserveWhitespace = true
				};
				Doc.LoadXml(sb.ToString());

				ParsedContract Parsed = await Contract.Parse(Doc.DocumentElement, ServiceRef.XmppService.ContractsClient);
				if (Parsed is null)
					return false;

				Contract = Parsed.Contract;
			}
			catch (Exception ex)
			{
				Output.WriteStartElement("Label");
				Output.WriteAttributeString("Text", ex.Message);
				Output.WriteAttributeString("FontFamily", "Courier New");
				Output.WriteAttributeString("TextColor", "Red");
				Output.WriteEndElement();

				return false;
			}

			Output.WriteStartElement("VerticalStackLayout");
			Output.WriteAttributeString("HorizontalOptions", "Center");

			bool ImageShown = false;

			if (Contract.Attachments is not null)
			{
				(string? FileName, int Width, int Height) = await PhotosLoader.LoadPhotoAsTemporaryFile(Contract.Attachments, 300, 300);

				if (!string.IsNullOrEmpty(FileName))
				{
					Output.WriteStartElement("Image");
					Output.WriteAttributeString("Source", FileName);
					Output.WriteAttributeString("WidthRequest", Width.ToString(CultureInfo.InvariantCulture));
					Output.WriteAttributeString("HeightRequest", Height.ToString(CultureInfo.InvariantCulture));
					Output.WriteEndElement();

					ImageShown = true;
				}
			}

			if (!ImageShown)
			{
				Output.WriteStartElement("Label");
				Output.WriteAttributeString("Text", "¶"); // TODO: SVG icon
				Output.WriteAttributeString("FontSize", "Large");
				Output.WriteAttributeString("HorizontalOptions", "Center");
				Output.WriteEndElement();
			}

			string? FriendlyName = await ContractModel.GetCategory(Contract);

			Output.WriteStartElement("Label");
			Output.WriteAttributeString("LineBreakMode", "WordWrap");
			Output.WriteAttributeString("FontSize", "Medium");
			Output.WriteAttributeString("HorizontalOptions", "Center");
			Output.WriteAttributeString("Text", FriendlyName);
			Output.WriteEndElement();

			Output.WriteStartElement("VerticalStackLayout.GestureRecognizers");

			StringBuilder Xml = new();
			Contract.Serialize(Xml, true, true, true, true, true, true, true);

			Output.WriteStartElement("TapGestureRecognizer");
			Output.WriteAttributeString("Command", "{Binding Path=IotScUriClicked}");
			Output.WriteAttributeString("CommandParameter", Constants.UriSchemes.IotSc + ":" + Xml.ToString());
			Output.WriteEndElement();

			Output.WriteEndElement();
			Output.WriteEndElement();

			return true;
		}

		/// <summary>
		/// Registers the Markdown document in which the construct resides.
		/// </summary>
		/// <param name="Document">Markdown document</param>
		public void Register(MarkdownDocument Document)
		{
			this.document = Document;
		}

		/// <summary>
		/// How much the module supports code of a given language (i.e. type of content)
		/// </summary>
		/// <param name="Language">Code language</param>
		/// <returns>Grade of support.</returns>
		public Grade Supports(string Language)
		{
			return string.Equals(Language, Constants.UriSchemes.IotSc, StringComparison.OrdinalIgnoreCase) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
