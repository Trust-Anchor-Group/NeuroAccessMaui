namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Static class that gives access to themed colors
	/// </summary>
	public static class ThemeColors
	{
		/// <summary>
		/// Primary foreground color.
		/// </summary>
		public static Color? PrimaryForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["PrimaryForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["PrimaryForegroundLight"];
			}
		}

		/// <summary>
		/// Primary background color.
		/// </summary>
		public static Color? PrimaryBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["PrimaryBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["PrimaryBackgroundLight"];
			}
		}

		/// <summary>
		/// Secondary foreground color.
		/// </summary>
		public static Color? SecondaryForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["SecondaryForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["SecondaryForegroundLight"];
			}
		}

		/// <summary>
		/// Secondary background color.
		/// </summary>
		public static Color? SecondaryBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["SecondaryBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["SecondaryBackgroundLight"];
			}
		}

		/// <summary>
		/// Accent foreground color.
		/// </summary>
		public static Color? AccentForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["AccentForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["AccentForegroundLight"];
			}
		}

		/// <summary>
		/// Normal foreground color.
		/// </summary>
		public static Color? NormalForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["NormalForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["NormalForegroundLight"];
			}
		}

		/// <summary>
		/// Normal background color.
		/// </summary>
		public static Color? NormalBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["NormalBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["NormalBackgroundLight"];
			}
		}

		/// <summary>
		/// Selected foreground color.
		/// </summary>
		public static Color? SelectedForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["SelectedForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["SelectedForegroundLight"];
			}
		}

		/// <summary>
		/// Selected background color.
		/// </summary>
		public static Color? SelectedBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["SelectedBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["SelectedBackgroundLight"];
			}
		}

		/// <summary>
		/// EnabledFilledButton foreground color.
		/// </summary>
		public static Color? EnabledFilledButtonForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["EnabledFilledButtonForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["EnabledFilledButtonForegroundLight"];
			}
		}

		/// <summary>
		/// EnabledFilledButton background color.
		/// </summary>
		public static Color? EnabledFilledButtonBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["EnabledFilledButtonBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["EnabledFilledButtonBackgroundLight"];
			}
		}

		/// <summary>
		/// DisabledFilledButton foreground color.
		/// </summary>
		public static Color? DisabledFilledButtonForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["DisabledFilledButtonForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["DisabledFilledButtonForegroundLight"];
			}
		}

		/// <summary>
		/// DisabledFilledButton background color.
		/// </summary>
		public static Color? DisabledFilledButtonBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["DisabledFilledButtonBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["DisabledFilledButtonBackgroundLight"];
			}
		}

		/// <summary>
		/// EnabledOutlinedButton foreground color.
		/// </summary>
		public static Color? EnabledOutlinedButtonForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["EnabledOutlinedButtonForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["EnabledOutlinedButtonForegroundLight"];
			}
		}

		/// <summary>
		/// EnabledOutlinedButton background color.
		/// </summary>
		public static Color? EnabledOutlinedButtonBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["EnabledOutlinedButtonBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["EnabledOutlinedButtonBackgroundLight"];
			}
		}

		/// <summary>
		/// DisabledOutlinedButton foreground color.
		/// </summary>
		public static Color? DisabledOutlinedButtonForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["DisabledOutlinedButtonForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["DisabledOutlinedButtonForegroundLight"];
			}
		}

		/// <summary>
		/// DisabledOutlinedButton background color.
		/// </summary>
		public static Color? DisabledOutlinedButtonBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["DisabledOutlinedButtonBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["DisabledOutlinedButtonBackgroundLight"];
			}
		}

		/// <summary>
		/// NormalEdit foreground color.
		/// </summary>
		public static Color? NormalEditForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["NormalEditForegroundDark"];
				else
					return (Color?)Application.Current?.Resources["NormalEditForegroundLight"];
			}
		}

		/// <summary>
		/// NormalEdit background color.
		/// </summary>
		public static Color? NormalEditBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["NormalEditBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["NormalEditBackgroundLight"];
			}
		}

		/// <summary>
		/// Alert color.
		/// </summary>
		public static Color? Alert
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["AlertDark"];
				else
					return (Color?)Application.Current?.Resources["AlertLight"];
			}
		}

		/// <summary>
		/// Error Background Color.
		/// </summary>
		public static Color? ErrorBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["ErrorBackgroundDark"];
				else
					return (Color?)Application.Current?.Resources["ErrorBackgroundLight"];
			}
		}

		/// <summary>
		/// Clickable color.
		/// </summary>
		public static Color? Clickable
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
					return (Color?)Application.Current?.Resources["ClickableDark"];
				else
					return (Color?)Application.Current?.Resources["ClickableLight"];
			}
		}

	}
}
