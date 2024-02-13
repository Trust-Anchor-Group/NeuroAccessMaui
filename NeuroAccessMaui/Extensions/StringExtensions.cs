using Waher.Content.Markdown;
using Waher.Events;

namespace NeuroAccessMaui.Extensions
{
	/// <summary>
	/// An extensions class for the <see cref="string"/> class.
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Returns the part of the string that appears before <paramref name="Delimiter"/>. If <paramref name="Delimiter"/>
		/// does not occur, the entire string is returned.
		/// </summary>
		/// <param name="s">String</param>
		/// <param name="Delimiter">Delimiter</param>
		/// <returns>Part of string before <paramref name="Delimiter"/>.</returns>
		public static string Before(this string s, string Delimiter)
		{
			int i = s.IndexOf(Delimiter, StringComparison.Ordinal);
			if (i < 0)
				return s;
			else
				return s[..i];
		}

		/// <summary>
		/// Returns the part of the string that appears before <paramref name="Delimiter"/>. If <paramref name="Delimiter"/>
		/// does not occur, the entire string is returned.
		/// </summary>
		/// <param name="s">String</param>
		/// <param name="Delimiter">Delimiter</param>
		/// <returns>Part of string before <paramref name="Delimiter"/>.</returns>
		public static string After(this string s, string Delimiter)
		{
			int i = s.IndexOf(Delimiter, StringComparison.Ordinal);
			if (i < 0)
				return s;
			else
				return s[(i + 1)..];
		}

		/// <summary>
		/// Returns the number of Unicode symbols, which may be represented by one or two chars, in a string.
		/// </summary>
		public static int GetUnicodeLength(this string Str)
		{
			ArgumentNullException.ThrowIfNull(Str);

			Str = Str.Normalize();

			int UnicodeCount = 0;
			for (int i = 0; i < Str.Length; i++)
			{
				UnicodeCount++;

				// Jump over the second surrogate char.
				if (char.IsSurrogate(Str, i))
					i++;
			}

			return UnicodeCount;
		}

		/// <summary>
		/// Converts Markdown text to Xamarin XAML
		/// </summary>
		/// <param name="Markdown">Markdown</param>
		/// <returns>Xamarin XAML</returns>
		public static async Task<object> MarkdownToXaml(this string Markdown)
		{
			try
			{
				MarkdownSettings Settings = new()
				{
					AllowScriptTag = false,
					EmbedEmojis = false,    // TODO: Emojis
					AudioAutoplay = false,
					AudioControls = false,
					ParseMetaData = false,
					VideoAutoplay = false,
					VideoControls = false
				};

				MarkdownDocument Doc = await MarkdownDocument.CreateAsync(Markdown, Settings);

				string Xaml = await Doc.GenerateXamarinForms();
				Xaml = Xaml.Replace("TextColor=\"{Binding HyperlinkColor}\"", "Style=\"{StaticResource HyperlinkColor}\"");

				return new VerticalStackLayout().LoadFromXaml(Xaml);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);

				VerticalStackLayout Layout = [];

				Layout.Children.Add(new Label()
				{
					Text = ex.Message,
					FontFamily = "Courier New",
					TextColor = Colors.Red,
					TextType = TextType.Text
				});

				return Layout;
			}
		}

		/// <summary>
		/// Parses a string into simple XAML (for inclusion in tables, tooltips, etc.)
		/// </summary>
		/// <param name="Xaml">XAML</param>
		/// <returns>Parsed XAML</returns>
		public static object? ParseXaml(this string Xaml)
		{
			if (string.IsNullOrEmpty(Xaml))
				return null;

			object Result = new VerticalStackLayout().LoadFromXaml(Xaml);

			if (Result is VerticalStackLayout Panel && Panel.Children.Count == 1)
			{
				IView Child = Panel.Children[0];
				Panel.Children.RemoveAt(0);

				if (Child is ContentView ContentView)
				{
					Child = ContentView.Content;
					ContentView.Content = null;
				}

				if (Child is View View)
					View.Margin = new Thickness(0);

				return Child;
			}
			else
				return Result;
		}

	}
}
