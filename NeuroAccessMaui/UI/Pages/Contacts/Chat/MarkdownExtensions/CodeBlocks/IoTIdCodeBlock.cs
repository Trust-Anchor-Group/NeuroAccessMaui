using System.Globalization;
using System.Text;
using System.Xml;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI.Photos;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.UI.Pages.Contacts.Chat.MarkdownExtensions.CodeBlocks
{
	/// <summary>
	/// Handles embedded Legal IDs.
	/// </summary>
	public class IoTIdCodeBlock : ICodeContent
	{
		private MarkdownDocument? document;

		/// <summary>
		/// Handles embedded Legal IDs.
		/// </summary>
		public IoTIdCodeBlock()
		{
		}

		/// <summary>
		/// If generation of (transportable) Markdown is supported
		/// </summary>
		public bool HandlesMarkdown => false;

		/// <summary>
		/// If generation of HTML is supported
		/// </summary>
		public bool HandlesHTML => false;

		/// <summary>
		/// If generation of LaTeX is supported
		/// </summary>
		public bool HandlesLaTeX => false;

		/// <summary>
		/// If generation of Smart Contract XML is supported
		/// </summary>
		public bool HandlesSmartContract => false;

		/// <summary>
		/// If generation of plain text is supported.
		/// </summary>
		public bool HandlesPlainText => false;

		/// <summary>
		/// If generation of XAML is supported.
		/// </summary>
		public bool HandlesXAML => true;

		/// <summary>
		/// Markdown document.
		/// </summary>
		public MarkdownDocument? Document => this.document;

		/// <summary>
		/// Generates (transportable) Markdown (not supported)
		/// </summary>
		public Task<bool> GenerateMarkdown(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			return Task.FromResult(false);
		}

		/// <summary>
		/// Generates HTML (not supported)
		/// </summary>
		public Task<bool> GenerateHTML(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			return Task.FromResult(false);
		}

		/// <summary>
		/// Generates LaTeX (not supported)
		/// </summary>
		public Task<bool> GenerateLaTeX(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			return Task.FromResult(false);
		}

		/// <summary>
		/// Generates Smart Contract XML (not supported)
		/// </summary>
		public Task<bool> GenerateSmartContractXml(XmlWriter Output, SmartContractRenderState State, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			return Task.FromResult(false);
		}

		/// <summary>
		/// Generates Plain Text (not supported)
		/// </summary>
		public Task<bool> GeneratePlainText(StringBuilder Output, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			return Task.FromResult(false);
		}

		/// <summary>
		/// Generates WPF XAML (not supported)
		/// </summary>
		public Task<bool> GenerateXAML(XmlWriter Output, Waher.Content.Markdown.Model.TextAlignment TextAlignment, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			return Task.FromResult(false);
		}

		/// <summary>
		/// Generates Xamarin XAML
		/// </summary>
		public async Task<bool> GenerateXamarinForms(XmlWriter Output, XamarinRenderingState State, string[] Rows, string Language, int Indent, MarkdownDocument Document)
		{
			LegalIdentity Identity;

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

				Identity = LegalIdentity.Parse(Doc.DocumentElement);
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

			Output.WriteStartElement("StackLayout");
			Output.WriteAttributeString("Orientation", "Vertical");
			Output.WriteAttributeString("HorizontalOptions", "Center");

			bool ImageShown = false;

			if (Identity.Attachments is not null)
			{
				(string? FileName, int Width, int Height) = await PhotosLoader.LoadPhotoAsTemporaryFile(Identity.Attachments, 300, 300);

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
				Output.WriteAttributeString("Text", "👤");   // TODO: SVG icon.
				Output.WriteAttributeString("FontSize", "Large");
				Output.WriteAttributeString("HorizontalOptions", "Center");
				Output.WriteEndElement();
			}

			Output.WriteStartElement("Label");
			Output.WriteAttributeString("LineBreakMode", "WordWrap");
			Output.WriteAttributeString("FontSize", "Medium");
			Output.WriteAttributeString("HorizontalOptions", "Center");
			Output.WriteAttributeString("Text", ContactInfo.GetFriendlyName(Identity));
			Output.WriteEndElement();

			Output.WriteStartElement("StackLayout.GestureRecognizers");

			StringBuilder Xml = new();
			Identity.Serialize(Xml, true, true, true, true, true, true, true);

			Output.WriteStartElement("TapGestureRecognizer");
			Output.WriteAttributeString("Command", "{Binding Path=IotIdUriClicked}");
			Output.WriteAttributeString("CommandParameter", Constants.UriSchemes.IotId + ":" + Xml.ToString());
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
			return string.Equals(Language, Constants.UriSchemes.IotId, StringComparison.OrdinalIgnoreCase) ? Grade.Excellent : Grade.NotAtAll;
		}
	}
}
