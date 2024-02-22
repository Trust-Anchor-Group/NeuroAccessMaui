using System.Text;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Xml;

namespace NeuroAccessMaui.UI.Rendering
{
	/// <summary>
	/// Markdown rendering extensions for Xamarin.Forms XAML.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public static class XamarinFormsExtensions
	{
		/// <summary>
		/// Generates Xamarin.Forms XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <returns>Xamarin.Forms XAML</returns>
		public static Task<string> GenerateXamarinForms(this MarkdownDocument Document)
		{
			return Document.GenerateXamarinForms(XML.WriterSettings(false, true));
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="XmlSettings">XML settings.</param>
		/// <returns>Xamarin.Forms XAML</returns>
		public static async Task<string> GenerateXamarinForms(this MarkdownDocument Document, XmlWriterSettings XmlSettings)
		{
			StringBuilder Output = new();
			await Document.GenerateXamarinForms(Output, XmlSettings);
			return Output.ToString();
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Output">Xamarin.Forms XAML will be output here.</param>
		public static Task GenerateXamarinForms(this MarkdownDocument Document, StringBuilder Output)
		{
			return Document.GenerateXamarinForms(Output, XML.WriterSettings(false, true));
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Output">Xamarin.Forms XAML will be output here.</param>
		/// <param name="XmlSettings">XML settings.</param>
		public static Task GenerateXamarinForms(this MarkdownDocument Document, StringBuilder Output, XmlWriterSettings XmlSettings)
		{
			return Document.GenerateXamarinForms(Output, XmlSettings, new XamlSettings());
		}

		/// <summary>
		/// Generates Xamarin.Forms XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Output">Xamarin.Forms XAML will be output here.</param>
		/// <param name="XmlSettings">XML settings.</param>
		/// <param name="XamlSettings">XAML Settings.</param>
		public static async Task GenerateXamarinForms(this MarkdownDocument Document, StringBuilder Output, XmlWriterSettings XmlSettings,
			XamlSettings XamlSettings)
		{
			using XamarinFormsXamlRenderer Renderer = new(Output, XmlSettings, XamlSettings);
			await Document.RenderDocument(Renderer);
		}

		/// <summary>
		/// Generates WPF XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Output">WPF XAML will be output here.</param>
		/// <param name="XamlSettings">XAML Settings.</param>
		public static async Task GenerateXamarinForms(this MarkdownDocument Document, StringBuilder Output, XamlSettings XamlSettings)
		{
			using XamarinFormsXamlRenderer Renderer = new(Output, XML.WriterSettings(false, true), XamlSettings);
			await Document.RenderDocument(Renderer);
		}

		/// <summary>
		/// Generates WPF XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="XamlSettings">XAML Settings.</param>
		/// <returns>WPF XAML</returns>
		public static async Task<string> GenerateXamarinForms(this MarkdownDocument Document, XamlSettings XamlSettings)
		{
			StringBuilder Output = new();
			await Document.GenerateXamarinForms(Output, XamlSettings);
			return Output.ToString();
		}
	}
}
