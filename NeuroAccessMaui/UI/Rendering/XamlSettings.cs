using System.Globalization;
using System.Text;

namespace NeuroAccessMaui.UI.Rendering
{
	/// <summary>
	/// Contains settings that the XAML export uses to customize XAML output.
	/// </summary>
	/// <remarks>
	/// Modified from original in Waher.Content.Markdown.Xamarin library, with permission.
	/// </remarks>
	public class XamlSettings
	{
		private readonly int[] headerFontSize = [28, 24, 22, 20, 18, 16, 15, 14, 13, 12];
		private readonly string[] headerForegroundColor = ["Navy", "Navy", "Navy", "Navy", "Navy", "Navy", "Navy", "Navy", "Navy", "Navy"];

		private string tableCellPadding = "5,2,5,2";
		private string tableCellBorderColor = "Gray";
		private string tableCellRowBackgroundColor1 = string.Empty;
		private string tableCellRowBackgroundColor2 = string.Empty;
		private int tableCellPaddingLeft = 5;
		private int tableCellPaddingTop = 2;
		private int tableCellPaddingRight = 5;
		private int tableCellPaddingBottom = 2;
		private double tableCellBorderThickness = 0.5;

		private int definitionSeparator = 10;
		private int definitionMargin = 20;

		private double superscriptScale = 0.75;
		private int superscriptOffset = -5;

		private int footnoteSeparator = 2;

		private int defaultGraphWidth = 480;
		private int defaultGraphHeight = 360;

		/// <summary>
		/// Contains settings that the XAML export uses to customize XAML output.
		/// </summary>
		public XamlSettings()
		{
		}

		private static void Parse(string s, out int Left, out int Top, out int Right, out int Bottom)
		{
			string[] Parts = s.Split(',');
			if (Parts.Length != 4)
				throw new ArgumentException("Invalid Margins.", nameof(s));

			int Left2 = int.Parse(Parts[0], NumberStyles.None, CultureInfo.InvariantCulture);
			int Top2 = int.Parse(Parts[1], NumberStyles.None, CultureInfo.InvariantCulture);
			int Right2 = int.Parse(Parts[2], NumberStyles.None, CultureInfo.InvariantCulture);
			int Bottom2 = int.Parse(Parts[3], NumberStyles.None, CultureInfo.InvariantCulture);

			Left = Left2;
			Top = Top2;
			Right = Right2;
			Bottom = Bottom2;
		}

		/// <summary>
		/// Header font sizes for different levels. Index corresponds to header level - 1.
		/// </summary>
		public int[] HeaderFontSize => this.headerFontSize;

		/// <summary>
		/// Header foreground colors for different levels. Index corresponds to header level - 1.
		/// 
		/// NOTE: Property is an array of strings, to allow generation of XAML where access to WPF libraries is not available.
		/// </summary>
		public string[] HeaderForegroundColor => this.headerForegroundColor;

		/// <summary>
		/// TableCell padding.
		/// </summary>
		public string TableCellPadding
		{
			get => this.tableCellPadding;
			set
			{
				Parse(value, out this.tableCellPaddingLeft, out this.tableCellPaddingTop, out this.tableCellPaddingRight, out this.tableCellPaddingBottom);
				this.tableCellPadding = value;
			}
		}

		private void UpdateTableCellPadding()
		{
			StringBuilder sb = new();

			sb.Append(this.tableCellPaddingLeft);
			sb.Append(',');
			sb.Append(this.tableCellPaddingTop);
			sb.Append(',');
			sb.Append(this.tableCellPaddingRight);
			sb.Append(',');
			sb.Append(this.tableCellPaddingBottom);

			this.tableCellPadding = sb.ToString();
		}

		/// <summary>
		/// Left padding for table cells.
		/// </summary>
		public int TableCellPaddingLeft
		{
			get => this.tableCellPaddingLeft;
			set
			{
				this.tableCellPaddingLeft = value;
				this.UpdateTableCellPadding();
			}
		}

		/// <summary>
		/// Top padding for table cells.
		/// </summary>
		public int TableCellPaddingTop
		{
			get => this.tableCellPaddingTop;
			set
			{
				this.tableCellPaddingTop = value;
				this.UpdateTableCellPadding();
			}
		}

		/// <summary>
		/// Right padding for table cells.
		/// </summary>
		public int TableCellPaddingRight
		{
			get => this.tableCellPaddingRight;
			set
			{
				this.tableCellPaddingRight = value;
				this.UpdateTableCellPadding();
			}
		}

		/// <summary>
		/// Bottom padding for table cells.
		/// </summary>
		public int TableCellPaddingBottom
		{
			get => this.tableCellPaddingBottom;
			set
			{
				this.tableCellPaddingBottom = value;
				this.UpdateTableCellPadding();
			}
		}

		/// <summary>
		/// Table cell border color.
		/// </summary>
		public string TableCellBorderColor
		{
			get => this.tableCellBorderColor;
			set => this.tableCellBorderColor = value;
		}

		/// <summary>
		/// Table cell border thickness.
		/// </summary>
		public double TableCellBorderThickness
		{
			get => this.tableCellBorderThickness;
			set => this.tableCellBorderThickness = value;
		}

		/// <summary>
		/// Optional background color for tables, odd row numbers.
		/// </summary>
		public string TableCellRowBackgroundColor1
		{
			get => this.tableCellRowBackgroundColor1;
			set => this.tableCellRowBackgroundColor1 = value;
		}

		/// <summary>
		/// Optional background color for tables, even row numbers.
		/// </summary>
		public string TableCellRowBackgroundColor2
		{
			get => this.tableCellRowBackgroundColor2;
			set => this.tableCellRowBackgroundColor2 = value;
		}

		/// <summary>
		/// Distance between definitions.
		/// </summary>
		public int DefinitionSeparator
		{
			get => this.definitionSeparator;
			set => this.definitionSeparator = value;
		}

		/// <summary>
		/// Left margin for definitions.
		/// </summary>
		public int DefinitionMargin
		{
			get => this.definitionMargin;
			set => this.definitionMargin = value;
		}

		/// <summary>
		/// Superscript scaling, compared to the normal font size.
		/// </summary>
		public double SuperscriptScale
		{
			get => this.superscriptScale;
			set => this.superscriptScale = value;
		}

		/// <summary>
		/// Superscript vertical offset.
		/// </summary>
		public int SuperscriptOffset
		{
			get => this.superscriptOffset;
			set => this.superscriptOffset = value;
		}

		/// <summary>
		/// Space between footnote and text in the footnote section.
		/// </summary>
		public int FootnoteSeparator
		{
			get => this.footnoteSeparator;
			set => this.footnoteSeparator = value;
		}

		/// <summary>
		/// Default graph width
		/// </summary>
		public int DefaultGraphWidth
		{
			get => this.defaultGraphWidth;
			set => this.defaultGraphWidth = value;
		}

		/// <summary>
		/// Default graph height
		/// </summary>
		public int DefaultGraphHeight
		{
			get => this.defaultGraphHeight;
			set => this.defaultGraphHeight = value;
		}

	}
}
