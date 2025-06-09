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
		private static Color? buttonAccessPrimarybgDark;
		private static Color? buttonAccessPrimarybgLight;

		private static Color? enabledFilledButtonBackgroundDark;
		private static Color? enabledFilledButtonBackgroundLight;
		private static Color? disabledFilledButtonForegroundDark;
		private static Color? disabledFilledButtonForegroundLight;
		private static Color? buttonUniversalbgInactiveWLDark;
		private static Color? buttonUniversalbgInactiveWLLight;

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
		private static Color? normalEditPlaceholderDark;
		private static Color? normalEditPlaceholderLight;
		private static Color? alertDark;
		private static Color? alertLight;
		private static Color? errorBackgroundDark;
		private static Color? errorBackgroundLight;
		private static Color? clickableDark;
		private static Color? clickableLight;
		private static Color? weakPasswordForeground;
		private static Color? mediumPasswordForeground;
		private static Color? strongPasswordForeground;
		private static Color? blueLink;
		private static Color? insertedBorder;
		private static Color? deletedBorder;
		private static Color? purple15Light;
		private static Color? purple15Dark;
		private static Color? purpleLight;
		private static Color? purpleDark;
		private static Color? blue20AffirmLight;
		private static Color? blue20AffirmDark;
		private static Color? blueLight;
		private static Color? blueDark;
		private static Color? strokeGrey;
		private static Color? offBlack;

		/// <summary>
		/// Primary foreground color.
		/// </summary>
		public static Color PrimaryForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					primaryForegroundDark ??= AppStyles.TryGetResource<Color>("ContentPrimaryWLDark");
					return primaryForegroundDark!;
				}
				else
				{
					primaryForegroundLight ??= AppStyles.TryGetResource<Color>("ContentPrimaryWLLight");
					return primaryForegroundLight!;
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
					primaryBackgroundDark ??= AppStyles.TryGetResource<Color>("SurfaceBackgroundWLDark");
					return primaryBackgroundDark!;
				}
				else
				{
					primaryBackgroundLight ??= AppStyles.TryGetResource<Color>("SurfaceBackgroundWLLight");
					return primaryBackgroundLight!;
				}
			}
		}

		/// <summary>
		/// Secondary Background Color
		/// </summary>
		public static Color SecondaryBackground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					secondaryBackgroundDark ??= AppStyles.TryGetResource<Color>("SurfaceElevation1WLDark");
					return secondaryBackgroundDark!;
				}
				else
				{
					secondaryBackgroundLight ??= AppStyles.TryGetResource<Color>("SurfaceElevation1WLLight");
					return secondaryBackgroundLight!;
				}
			}
		}

		/// <summary>
		/// EnabledFilledButton background color.
		/// </summary>
		public static Color ButtonAccessPrimarybg
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					buttonAccessPrimarybgDark ??= AppStyles.TryGetResource<Color>("ButtonAccessPrimarybgWLDark");
					return buttonAccessPrimarybgDark!;
				}
				else
				{
					buttonAccessPrimarybgLight ??= AppStyles.TryGetResource<Color>("ButtonAccessPrimarybgWLLight");
					return buttonAccessPrimarybgLight!;
				}
			}
		}

		/// <summary>
		/// DisabledFilledButton background color.
		/// </summary>
		public static Color ButtonUniversalbgInactiveWL
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					buttonUniversalbgInactiveWLDark ??= AppStyles.TryGetResource<Color>("ButtonUniversalbgInactiveWLDark");
					return buttonUniversalbgInactiveWLDark!;
				}
				else
				{
					buttonUniversalbgInactiveWLLight ??= AppStyles.TryGetResource<Color>("ButtonUniversalbgInactiveWLLight");
					return buttonUniversalbgInactiveWLLight!;
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
					alertDark ??= AppStyles.TryGetResource<Color>("InputFieldsContentDangerv800Dark");
					return alertDark!;
				}
				else
				{
					alertLight ??= AppStyles.TryGetResource<Color>("InputFieldsContentDangerv800Light");
					return alertLight!;
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
					errorBackgroundDark ??= AppStyles.TryGetResource<Color>("InputFieldsContentDangerv800Dark");
					return errorBackgroundDark!;
				}
				else
				{
					errorBackgroundLight ??= AppStyles.TryGetResource<Color>("InputFieldsContentDangerv800Light");
					return errorBackgroundLight!;
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
					clickableDark ??= AppStyles.TryGetResource<Color>("InputFieldsAccentContentAssetsDark");
					return clickableDark!;
				}
				else
				{
					clickableLight ??= AppStyles.TryGetResource<Color>("InputFieldsAccentContentAssetsLight");
					return clickableLight!;
				}
			}
		}

		/// <summary>
		/// Weak password foreground color.
		/// </summary>
		public static Color WeakPasswordForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					weakPasswordForeground ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsFigureDangerWLDark");
					return weakPasswordForeground!;
				}
				else
				{
					weakPasswordForeground ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsFigureDangerWLLight");
					return weakPasswordForeground!;
				}
			}
		}

		/// <summary>
		/// Medium password foreground color.
		/// </summary>
		public static Color MediumPasswordForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					mediumPasswordForeground ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsFigureWarningWLDark");
					return mediumPasswordForeground!;
				}
				else
				{
					mediumPasswordForeground ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsFigureWarningWLLight");
					return mediumPasswordForeground!;
				}
			}
		}

		/// <summary>
		/// Strong password foreground color.
		/// </summary>
		public static Color StrongPasswordForeground
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					strongPasswordForeground ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsFigureSuccessWLDark");
					return strongPasswordForeground!;
				}
				else
				{
					strongPasswordForeground ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsFigureSuccessWLLight");
					return strongPasswordForeground!;
				}
			}
		}

		/// <summary>
		/// Blue link color
		/// </summary>
		public static Color BlueLink
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					blueLink ??= AppStyles.TryGetResource<Color>("ContentLinkWLDark");
					return blueLink!;
				}
				else
				{
					blueLink ??= AppStyles.TryGetResource<Color>("ContentLinkWLLight");
					return blueLink!;
				}
			}
		}

		/// Purple color with 15% transparency.
		/// </summary>
		public static Color Purple15
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					purple15Dark ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsbgAccent2WLDark");
					return purple15Dark!;
				}
				else
				{
					purple15Light ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsbgAccent2WLLight");
					return purple15Light!;
				}
			}
		}

		/// <summary>
		/// Inserted Border color
		/// </summary>
		public static Color InsertedBorder
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Light)
				{
					insertedBorder ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsbgSuccessWLLight");
					return insertedBorder!;
				}
				else
				{
					insertedBorder ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsbgSuccessWLDark");
					return insertedBorder!;
				}
			}
		}


		/// Purple color
		/// </summary>
		public static Color Purple
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					purpleDark ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsContentAccent2WLDark");
					return purpleDark!;
				}
				else
				{
					purpleLight ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsContentAccent2WLLight");
					return purpleLight!;
				}
			}
		}

		/// <summary>
		/// Deleted Border color
		/// </summary>
		public static Color DeletedBorder
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Light)
				{
					deletedBorder ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsbgDangerWLLight");
					return deletedBorder!;
				}
				else
				{
					deletedBorder ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsbgDangerWLDark");
					return deletedBorder!;
				}
			}
		}

		/// Blue affirm color with 20% transparency.
		/// </summary>
		public static Color Blue20Affirm
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					blue20AffirmDark ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsbgInfoWLDark");
					return blue20AffirmDark!;
				}
				else
				{
					blue20AffirmLight ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsbgInfoWLLight");
					return blue20AffirmLight!;
				}
			}
		}

		/// <summary>
		/// Blue color
		/// </summary>
		public static Color Blue
		{
			get
			{
				if (Application.Current?.RequestedTheme == AppTheme.Dark)
				{
					blueDark ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsContentInfoWLDark");
					return blueDark!;
				}
				else
				{
					blueLight ??= AppStyles.TryGetResource<Color>("WLToastsAndPillsContentInfoWLLight");
					return blueLight!;
				}
			}
		}
	}
}
