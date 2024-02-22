using System.Text;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Xml;

namespace NeuroAccessMaui.UI.Rendering
{
	/// <summary>
	/// Markdown rendering extensions for Maui XAML.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public static class MauiXamlExtensions
	{
		/// <summary>
		/// Generates Maui XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <returns>Maui XAML</returns>
		public static Task<string> GenerateMauiXaml(this MarkdownDocument Document)
		{
			return Document.GenerateMauiXaml(XML.WriterSettings(false, true));
		}

		/// <summary>
		/// Generates Maui XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="XmlSettings">XML settings.</param>
		/// <returns>Maui XAML</returns>
		public static async Task<string> GenerateMauiXaml(this MarkdownDocument Document, XmlWriterSettings XmlSettings)
		{
			StringBuilder Output = new();
			await Document.GenerateMauiXaml(Output, XmlSettings);
			return Output.ToString();
		}

		/// <summary>
		/// Generates Maui XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Output">Maui XAML will be output here.</param>
		public static Task GenerateMauiXaml(this MarkdownDocument Document, StringBuilder Output)
		{
			return Document.GenerateMauiXaml(Output, XML.WriterSettings(false, true));
		}

		/// <summary>
		/// Generates Maui XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Output">Maui XAML will be output here.</param>
		/// <param name="XmlSettings">XML settings.</param>
		public static Task GenerateMauiXaml(this MarkdownDocument Document, StringBuilder Output, XmlWriterSettings XmlSettings)
		{
			return Document.GenerateMauiXaml(Output, XmlSettings, new XamlSettings());
		}

		/// <summary>
		/// Generates Maui XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Output">Maui XAML will be output here.</param>
		/// <param name="XmlSettings">XML settings.</param>
		/// <param name="XamlSettings">XAML Settings.</param>
		public static async Task GenerateMauiXaml(this MarkdownDocument Document, StringBuilder Output, XmlWriterSettings XmlSettings,
			XamlSettings XamlSettings)
		{
			using MauiXamlRenderer Renderer = new(Output, XmlSettings, XamlSettings);
			await Document.RenderDocument(Renderer);
		}

		/// <summary>
		/// Generates Maui XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="Output">Maui XAML will be output here.</param>
		/// <param name="XamlSettings">XAML Settings.</param>
		public static async Task GenerateMauiXaml(this MarkdownDocument Document, StringBuilder Output, XamlSettings XamlSettings)
		{
			using MauiXamlRenderer Renderer = new(Output, XML.WriterSettings(false, true), XamlSettings);
			await Document.RenderDocument(Renderer);
		}

		/// <summary>
		/// Generates Maui XAML from the markdown text.
		/// </summary>
		/// <param name="Document">Markdown document.</param>
		/// <param name="XamlSettings">XAML Settings.</param>
		/// <returns>Maui XAML</returns>
		public static async Task<string> GenerateMauiXaml(this MarkdownDocument Document, XamlSettings XamlSettings)
		{
			StringBuilder Output = new();
			await Document.GenerateMauiXaml(Output, XamlSettings);
			return Output.ToString();
		}
	}
}
