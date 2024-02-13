namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Static class that gives access to app-specific themed colors
	/// </summary>
	public static class AppColors
	{
		private static Color? primaryForegroundDark;
		private static Color? primaryForegroundLight;
		private static Color? primaryBackgroundDark;
		private static Color? primaryBackgroundLight;
		private static Color? secondaryForegroundDark;
		private static Color? secondaryForegroundLight;
		private static Color? secondaryBackgroundDark;
		private static Color? secondaryBackgroundLight;
		private static Color? accentForegroundDark;
		private static Color? accentForegroundLight;
		private static Color? normalForegroundDark;
		private static Color? normalForegroundLight;
		private static Color? normalBackgroundDark;
		private static Color? normalBackgroundLight;
		private static Color? selectedForegroundDark;
		private static Color? selectedForegroundLight;
		private static Color? selectedBackgroundDark;
		private static Color? selectedBackgroundLight;
		private static Color? enabledFilledButtonForegroundDark;
		private static Color? enabledFilledButtonForegroundLight;
		private static Color? enabledFilledButtonBackgroundDark;
		private static Color? enabledFilledButtonBackgroundLight;
		private static Color? disabledFilledButtonForegroundDark;
		private static Color? disabledFilledButtonForegroundLight;
		private static Color? disabledFilledButtonBackgroundDark;
		private static Color? disabledFilledButtonBackgroundLight;
		private static Color? enabledOutlinedButtonForegroundDark;
		private static Color? enabledOutlinedButtonForegroundLight;
		private static Color? enabledOutlinedButtonBackgroundDark;
		private static Color? enabledOutlinedButtonBackgroundLight;
		private static Color? disabledOutlinedButtonForegroundDark;
		private static Color? disabledOutlinedButtonForegroundLight;
		private static Color? disabledOutlinedButtonBackgroundDark;
		private static Color? disabledOutlinedButtonBackgroundLight;
		private static Color? normalEditForegroundDark;
		private static Color? normalEditForegroundLight;
		private static Color? normalEditBackgroundDark;
		private static Color? normalEditBackgroundLight;
		private static Color? alertDark;
		private static Color? alertLight;
		private static Color? errorBackgroundDark;
		private static Color? errorBackgroundLight;
		private static Color? clickableDark;
		private static Color? clickableLight;

		/// <summary>
		/// Primary foreground color.
		/// </summary>
		public static Color PrimaryForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					primaryForegroundDark ??= (Color)Application.Current!.Resources["PrimaryForegroundDark"];
					return primaryForegroundDark;
				}
				else
				{
					primaryForegroundLight ??= (Color)Application.Current!.Resources["PrimaryForegroundLight"];
					return primaryForegroundLight;
				}
			}
		}

		/// <summary>
		/// Primary background color.
		/// </summary>
		public static Color PrimaryBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					primaryBackgroundDark ??= (Color)Application.Current!.Resources["PrimaryBackgroundDark"];
					return primaryBackgroundDark;
				}
				else
				{
					primaryBackgroundLight ??= (Color)Application.Current!.Resources["PrimaryBackgroundLight"];
					return primaryBackgroundLight;
				}
			}
		}

		/// <summary>
		/// Secondary foreground color.
		/// </summary>
		public static Color SecondaryForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					secondaryForegroundDark ??= (Color)Application.Current!.Resources["SecondaryForegroundDark"];
					return secondaryForegroundDark;
				}
				else
				{
					secondaryForegroundLight ??= (Color)Application.Current!.Resources["SecondaryForegroundLight"];
					return secondaryForegroundLight;
				}
			}
		}

		/// <summary>
		/// Secondary background color.
		/// </summary>
		public static Color SecondaryBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					secondaryBackgroundDark ??= (Color)Application.Current!.Resources["SecondaryBackgroundDark"];
					return secondaryBackgroundDark;
				}
				else
				{
					secondaryBackgroundLight ??= (Color)Application.Current!.Resources["SecondaryBackgroundLight"];
					return secondaryBackgroundLight;
				}
			}
		}

		/// <summary>
		/// Accent foreground color.
		/// </summary>
		public static Color AccentForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					accentForegroundDark ??= (Color)Application.Current!.Resources["AccentForegroundDark"];
					return accentForegroundDark;
				}
				else
				{
					accentForegroundLight ??= (Color)Application.Current!.Resources["AccentForegroundLight"];
					return accentForegroundLight;
				}
			}
		}

		/// <summary>
		/// Normal foreground color.
		/// </summary>
		public static Color NormalForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					normalForegroundDark ??= (Color)Application.Current!.Resources["NormalForegroundDark"];
					return normalForegroundDark;
				}
				else
				{
					normalForegroundLight ??= (Color)Application.Current!.Resources["NormalForegroundLight"];
					return normalForegroundLight;
				}
			}
		}

		/// <summary>
		/// Normal background color.
		/// </summary>
		public static Color NormalBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					normalBackgroundDark ??= (Color)Application.Current!.Resources["NormalBackgroundDark"];
					return normalBackgroundDark;
				}
				else
				{
					normalBackgroundLight ??= (Color)Application.Current!.Resources["NormalBackgroundLight"];
					return normalBackgroundLight;
				}
			}
		}

		/// <summary>
		/// Selected foreground color.
		/// </summary>
		public static Color SelectedForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					selectedForegroundDark ??= (Color)Application.Current!.Resources["SelectedForegroundDark"];
					return selectedForegroundDark;
				}
				else
				{
					selectedForegroundLight ??= (Color)Application.Current!.Resources["SelectedForegroundLight"];
					return selectedForegroundLight;
				}
			}
		}

		/// <summary>
		/// Selected background color.
		/// </summary>
		public static Color SelectedBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					selectedBackgroundDark ??= (Color)Application.Current!.Resources["SelectedBackgroundDark"];
					return selectedBackgroundDark;
				}
				else
				{
					selectedBackgroundLight ??= (Color)Application.Current!.Resources["SelectedBackgroundLight"];
					return selectedBackgroundLight;
				}
			}
		}

		/// <summary>
		/// EnabledFilledButton foreground color.
		/// </summary>
		public static Color EnabledFilledButtonForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					enabledFilledButtonForegroundDark ??= (Color)Application.Current!.Resources["EnabledFilledButtonForegroundDark"];
					return enabledFilledButtonForegroundDark;
				}
				else
				{
					enabledFilledButtonForegroundLight ??= (Color)Application.Current!.Resources["EnabledFilledButtonForegroundLight"];
					return enabledFilledButtonForegroundLight;
				}
			}
		}

		/// <summary>
		/// EnabledFilledButton background color.
		/// </summary>
		public static Color EnabledFilledButtonBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					enabledFilledButtonBackgroundDark ??= (Color)Application.Current!.Resources["EnabledFilledButtonBackgroundDark"];
					return enabledFilledButtonBackgroundDark;
				}
				else
				{
					enabledFilledButtonBackgroundLight ??= (Color)Application.Current!.Resources["EnabledFilledButtonBackgroundLight"];
					return enabledFilledButtonBackgroundLight;
				}
			}
		}

		/// <summary>
		/// DisabledFilledButton foreground color.
		/// </summary>
		public static Color DisabledFilledButtonForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					disabledFilledButtonForegroundDark ??= (Color)Application.Current!.Resources["DisabledFilledButtonForegroundDark"];
					return disabledFilledButtonForegroundDark;
				}
				else
				{
					disabledFilledButtonForegroundLight ??= (Color)Application.Current!.Resources["DisabledFilledButtonForegroundLight"];
					return disabledFilledButtonForegroundLight;
				}
			}
		}

		/// <summary>
		/// DisabledFilledButton background color.
		/// </summary>
		public static Color DisabledFilledButtonBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					disabledFilledButtonBackgroundDark ??= (Color)Application.Current!.Resources["DisabledFilledButtonBackgroundDark"];
					return disabledFilledButtonBackgroundDark;
				}
				else
				{
					disabledFilledButtonBackgroundLight ??= (Color)Application.Current!.Resources["DisabledFilledButtonBackgroundLight"];
					return disabledFilledButtonBackgroundLight;
				}
			}
		}

		/// <summary>
		/// EnabledOutlinedButton foreground color.
		/// </summary>
		public static Color EnabledOutlinedButtonForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					enabledOutlinedButtonForegroundDark ??= (Color)Application.Current!.Resources["EnabledOutlinedButtonForegroundDark"];
					return enabledOutlinedButtonForegroundDark;
				}
				else
				{
					enabledOutlinedButtonForegroundLight ??= (Color)Application.Current!.Resources["EnabledOutlinedButtonForegroundLight"];
					return enabledOutlinedButtonForegroundLight;
				}
			}
		}

		/// <summary>
		/// EnabledOutlinedButton background color.
		/// </summary>
		public static Color EnabledOutlinedButtonBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					enabledOutlinedButtonBackgroundDark ??= (Color)Application.Current!.Resources["EnabledOutlinedButtonBackgroundDark"];
					return enabledOutlinedButtonBackgroundDark;
				}
				else
				{
					enabledOutlinedButtonBackgroundLight ??= (Color)Application.Current!.Resources["EnabledOutlinedButtonBackgroundLight"];
					return enabledOutlinedButtonBackgroundLight;
				}
			}
		}

		/// <summary>
		/// DisabledOutlinedButton foreground color.
		/// </summary>
		public static Color DisabledOutlinedButtonForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					disabledOutlinedButtonForegroundDark ??= (Color)Application.Current!.Resources["DisabledOutlinedButtonForegroundDark"];
					return disabledOutlinedButtonForegroundDark;
				}
				else
				{
					disabledOutlinedButtonForegroundLight ??= (Color)Application.Current!.Resources["DisabledOutlinedButtonForegroundLight"];
					return disabledOutlinedButtonForegroundLight;
				}
			}
		}

		/// <summary>
		/// DisabledOutlinedButton background color.
		/// </summary>
		public static Color DisabledOutlinedButtonBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					disabledOutlinedButtonBackgroundDark ??= (Color)Application.Current!.Resources["DisabledOutlinedButtonBackgroundDark"];
					return disabledOutlinedButtonBackgroundDark;
				}
				else
				{
					disabledOutlinedButtonBackgroundLight ??= (Color)Application.Current!.Resources["DisabledOutlinedButtonBackgroundLight"];
					return disabledOutlinedButtonBackgroundLight;
				}
			}
		}

		/// <summary>
		/// NormalEdit foreground color.
		/// </summary>
		public static Color NormalEditForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					normalEditForegroundDark ??= (Color)Application.Current!.Resources["NormalEditForegroundDark"];
					return normalEditForegroundDark;
				}
				else
				{
					normalEditForegroundLight ??= (Color)Application.Current!.Resources["NormalEditForegroundLight"];
					return normalEditForegroundLight;
				}
			}
		}

		/// <summary>
		/// NormalEdit background color.
		/// </summary>
		public static Color NormalEditBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					normalEditBackgroundDark ??= (Color)Application.Current!.Resources["NormalEditBackgroundDark"];
					return normalEditBackgroundDark;
				}
				else
				{
					normalEditBackgroundLight ??= (Color)Application.Current!.Resources["NormalEditBackgroundLight"];
					return normalEditBackgroundLight;
				}
			}
		}

		/// <summary>
		/// Alert color.
		/// </summary>
		public static Color Alert
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					alertDark ??= (Color)Application.Current!.Resources["AlertDark"];
					return alertDark;
				}
				else
				{
					alertLight ??= (Color)Application.Current!.Resources["AlertLight"];
					return alertLight;
				}
			}
		}

		/// <summary>
		/// Error Background Color.
		/// </summary>
		public static Color ErrorBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					errorBackgroundDark ??= (Color)Application.Current!.Resources["ErrorBackgroundDark"];
					return errorBackgroundDark;
				}
				else
				{
					errorBackgroundLight ??= (Color)Application.Current!.Resources["ErrorBackgroundLight"];
					return errorBackgroundLight;
				}
			}
		}

		/// <summary>
		/// Clickable color.
		/// </summary>
		public static Color Clickable
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					clickableDark ??= (Color)Application.Current!.Resources["ClickableDark"];
					return clickableDark;
				}
				else
				{
					clickableLight ??= (Color)Application.Current!.Resources["ClickableLight"];
					return clickableLight;
				}
			}
		}

	}
}
