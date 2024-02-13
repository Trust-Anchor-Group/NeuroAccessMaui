namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Static class that gives access to app-specific styles
	/// </summary>
	public static class AppStyles
	{
		private static Thickness? smallBottomOnlyMargins;
		private static Style? sectionTitleLabelStyle;
		private static Style? keyLabel;
		private static Style? valueLabel;
		private static Style? formattedValueLabel;
		private static Style? clickableValueLabel;
		private static Style? infoLabelStyle;

		/// <summary>
		/// Bottom-only small margins
		/// </summary>
		public static Thickness SmallBottomOnlyMargins
		{
			get
			{
				smallBottomOnlyMargins ??= (Thickness)Application.Current!.Resources["SmallBottomOnlyMargins"];
				return smallBottomOnlyMargins.Value;
			}
		}

		/// <summary>
		/// Style of section title labels
		/// </summary>
		public static Style SectionTitleLabelStyle
		{
			get
			{
				sectionTitleLabelStyle ??= (Style)Application.Current!.Resources["SectionTitleLabelStyle"];
				return sectionTitleLabelStyle!;
			}
		}

		/// <summary>
		/// Style of key labels
		/// </summary>
		public static Style KeyLabel
		{
			get
			{
				keyLabel ??= (Style)Application.Current!.Resources["KeyLabel"];
				return keyLabel!;
			}
		}

		/// <summary>
		/// Style of value labels
		/// </summary>
		public static Style ValueLabel
		{
			get
			{
				valueLabel ??= (Style)Application.Current!.Resources["ValueLabel"];
				return valueLabel!;
			}
		}

		/// <summary>
		/// Style of formatted value labels
		/// </summary>
		public static Style FormattedValueLabel
		{
			get
			{
				formattedValueLabel ??= (Style)Application.Current!.Resources["FormattedValueLabel"];
				return formattedValueLabel!;
			}
		}

		/// <summary>
		/// Style of clickable value labels
		/// </summary>
		public static Style ClickableValueLabel
		{
			get
			{
				clickableValueLabel ??= (Style)Application.Current!.Resources["ClickableValueLabel"];
				return clickableValueLabel!;
			}
		}

		/// <summary>
		/// Style of information labels
		/// </summary>
		public static Style InfoLabelStyle
		{
			get
			{
				infoLabelStyle ??= (Style)Application.Current!.Resources["InfoLabelStyle"];
				return infoLabelStyle!;
			}
		}
		
	}
}
