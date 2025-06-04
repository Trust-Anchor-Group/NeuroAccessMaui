namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Static class that gives access to app-specific themed colors
	/// </summary>
	public static class AppColors
	{
		private static Color? weakPasswordForeground;
		private static Color? mediumPasswordForeground;
		private static Color? strongPasswordForeground;
		private static Color? blueLink;
		private static Color? insertedBorder;
		private static Color? deletedBorder;

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
	}
}
