namespace NeuroAccessMaui.UI.Controls.Markdown
{
	/// <summary>
	/// Sizes and gaps of a density. All font sizes follow the system text size.
	/// </summary>
	internal sealed class MarkdownMetrics
	{
		/// <summary>
		/// For reading.
		/// </summary>
		public static readonly MarkdownMetrics Comfortable = new()
		{
			BodySize = 16,
			SmallSize = 13,
			CodeSize = 14,
			LineHeight = 1.15,
			BlockSpacing = 12,
			ListSpacing = 6,
			MarkerGap = 10,
			HeadingSizes = [24, 20, 18, 16, 16, 16, 16, 16, 16],
			HeadingTopMargins = [4, 12, 8, 4, 4, 4, 4, 4, 4],
			CardPadding = new Thickness(16, 12)
		};

		/// <summary>
		/// For small places such as chat bubbles.
		/// </summary>
		public static readonly MarkdownMetrics Compact = new()
		{
			BodySize = 15,
			SmallSize = 12,
			CodeSize = 13,
			LineHeight = 1.1,
			BlockSpacing = 8,
			ListSpacing = 4,
			MarkerGap = 8,
			HeadingSizes = [18, 16, 15, 15, 15, 15, 15, 15, 15],
			HeadingTopMargins = [2, 4, 2, 2, 2, 2, 2, 2, 2],
			CardPadding = new Thickness(12, 10)
		};

		/// <summary>
		/// Gets the metrics of a density.
		/// </summary>
		public static MarkdownMetrics For(MarkdownDensity Density)
		{
			return Density == MarkdownDensity.Compact ? Compact : Comfortable;
		}

		/// <summary>Paragraph text size.</summary>
		public double BodySize { get; init; }

		/// <summary>Size of captions, footnotes and code labels.</summary>
		public double SmallSize { get; init; }

		/// <summary>Size of code in code blocks.</summary>
		public double CodeSize { get; init; }

		/// <summary>Line height, as a multiple of the font's own.</summary>
		public double LineHeight { get; init; }

		/// <summary>Gap between blocks.</summary>
		public double BlockSpacing { get; init; }

		/// <summary>Gap between list items.</summary>
		public double ListSpacing { get; init; }

		/// <summary>Gap between a list marker and the item's text.</summary>
		public double MarkerGap { get; init; }

		/// <summary>Heading sizes by level, from 1.</summary>
		public required double[] HeadingSizes { get; init; }

		/// <summary>Extra room above headings by level, from 1.</summary>
		public required double[] HeadingTopMargins { get; init; }

		/// <summary>Padding in quotes.</summary>
		public Thickness CardPadding { get; init; }
	}

	/// <summary>
	/// The theme colors the text uses. They are dynamic resources, so the text follows the light and dark themes.
	/// </summary>
	internal static class MarkdownColors
	{
		public const string Text = "ContentPrimaryWL";
		public const string Muted = "ContentSecondaryWL";
		public const string Accent = "ContentAccessWL";
		public const string AccentBackground = "TnPSuccessbgWL";
		public const string Card = "SurfaceElevation1WL";
		public const string Line = "SurfaceDividerWL";
		public const string Chip = "MarkdownChipWL";
		public const string Surface = "SurfaceBackgroundWL";
		public const string Warning = "TnPWarningContentWL";
		public const string WarningBackground = "TnPWarningbgWL";
		public const string Danger = "TnPDangerContentWL";
		public const string Inserted = "TnPSuccessFigureWL";
		public const string Deleted = "TnPDangerFigureWL";
	}

	/// <summary>
	/// The fonts the text uses.
	/// </summary>
	internal static class MarkdownFonts
	{
		public const string Body = "HaasGroteskRegular";
		public const string Medium = "HaasGroteskMedium";
		public const string Bold = "HaasGroteskBold";
		public const string Heading = "SpaceGroteskBold";
		public const string InlineCode = "SpaceGroteskRegular";

		/// <summary>
		/// The app bundles no monospaced font, so code blocks use the system's.
		/// </summary>
		public static string Monospace => DeviceInfo.Platform == DevicePlatform.Android ? "monospace" :
			DeviceInfo.Platform == DevicePlatform.WinUI ? "Consolas" : "Menlo";
	}
}
