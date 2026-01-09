namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Static class that gives access to app-specific themed colors.
	/// All colors are fetched directly from the current resources; there are no separate dark/light variants.
	/// </summary>
	public static class AppColors
	{
		private static Color? tnPSuccessBg;
		private static Color? tnPWarningBg;
		private static Color? tnPDangerBg;
		private static Color? tnPSuccessContent;
		private static Color? tnPWarningContent;
		private static Color? tnPDangerContent;
		private static Color? tnPSuccessFigure;
		private static Color? tnPInfoBg;
		private static Color? tnPInfoContent;
		private static Color? buttonNeutralNavButtonsOnContainerbgActive;
		private static Color? contentPrimary;
		private static Color? contentSecondary;
		private static Color? contentAccess;

		public static Color TnPSuccessBg
		{
			get
			{
				tnPSuccessBg ??= AppStyles.TryGetResource<Color>("TnPSuccessbgWL");
				return tnPSuccessBg!;
			}
		}

		public static Color TnPWarningBg
		{
			get
			{
				tnPWarningBg ??= AppStyles.TryGetResource<Color>("TnPWarningbgWL");
				return tnPWarningBg!;
			}
		}

		public static Color TnPDangerBg
		{
			get
			{
				tnPDangerBg ??= AppStyles.TryGetResource<Color>("TnPDangerbgWL");
				return tnPDangerBg!;
			}
		}

		public static Color TnPSuccessContent
		{
			get
			{
				tnPSuccessContent ??= AppStyles.TryGetResource<Color>("TnPSuccessContentWL");
				return tnPSuccessContent!;
			}
		}

		public static Color TnPSuccessFigure
		{
			get
			{
				tnPSuccessFigure ??= AppStyles.TryGetResource<Color>("TnPSuccessFigureWL");
				return tnPSuccessFigure!;
			}
		}

		public static Color TnPWarningContent
		{
			get
			{
				tnPWarningContent ??= AppStyles.TryGetResource<Color>("TnPWarningContentWL");
				return tnPWarningContent!;
			}
		}

		public static Color TnPDangerContent
		{
			get
			{
				tnPDangerContent ??= AppStyles.TryGetResource<Color>("TnPDangerContentWL");
				return tnPDangerContent!;
			}
		}

		public static Color TnPInfoBg
		{
			get
			{
				tnPInfoBg ??= AppStyles.TryGetResource<Color>("TnPInfobgWL");
				return tnPInfoBg!;
			}
		}

		public static Color TnPInfoContent
		{
			get
			{
				tnPInfoContent ??= AppStyles.TryGetResource<Color>("TnPInfoContentWL");
				return tnPInfoContent!;
			}
		}

		public static Color ButtonNeutralNavButtonsOnContainerbgActive
		{
			get
			{
				buttonNeutralNavButtonsOnContainerbgActive ??= AppStyles.TryGetResource<Color>("ButtonNeutralNavButtonsOnContainerbgActiveWL");
				return buttonNeutralNavButtonsOnContainerbgActive!;
			}
		}

		public static Color ContentPrimary
		{
			get
			{
				contentPrimary ??= AppStyles.TryGetResource<Color>("ContentPrimaryWL");
				return contentPrimary!;
			}
		}

		public static Color ContentSecondary
		{
			get
			{
				contentSecondary ??= AppStyles.TryGetResource<Color>("ContentSecondaryWL");
				return contentSecondary!;
			}
		}

		public static Color ContentAccess
		{
			get
			{
				contentAccess ??= AppStyles.TryGetResource<Color>("ContentAccessWL");
				return contentAccess!;
			}
		}

		/// <summary>
		/// Primary foreground color.
		/// </summary>
		public static Color PrimaryForeground =>
			GetRequiredColor("ContentPrimaryWL");

		/// <summary>
		/// Primary background color.
		/// </summary>
		public static Color PrimaryBackground =>
			GetRequiredColor("SurfaceBackgroundWL");

		/// <summary>
		/// Secondary Background Color.
		/// </summary>
		public static Color SecondaryBackground =>
			GetRequiredColor("SurfaceElevation1WL");

		/// <summary>
		/// EnabledFilledButton background color.
		/// </summary>
		public static Color ButtonAccessPrimarybg =>
			GetRequiredColor("ButtonAccessPrimarybgWL");

		/// <summary>
		/// DisabledFilledButton background color.
		/// </summary>
		public static Color ButtonUniversalbgInactiveWL =>
			GetRequiredColor("ButtonUniversalbgInactiveWL");

		/// <summary>
		/// Alert color.
		/// </summary>
		public static Color Alert =>
			GetRequiredColor("InputFieldsContentDangerv800");

		/// <summary>
		/// Error Background Color.
		/// </summary>
		public static Color ErrorBackground =>
			GetRequiredColor("InputFieldsContentDangerv800");

		/// <summary>
		/// Clickable color.
		/// </summary>
		public static Color Clickable =>
			GetRequiredColor("InputFieldsAccentContentAssets");

		/// <summary>
		/// Weak password foreground color.
		/// </summary>
		public static Color WeakPasswordForeground =>
			GetRequiredColor("TnPDangerFigureWL");

		/// <summary>
		/// Medium password foreground color.
		/// </summary>
		public static Color MediumPasswordForeground =>
			GetRequiredColor("TnPWarningContentWL");

		/// <summary>
		/// Strong password foreground color.
		/// </summary>
		public static Color StrongPasswordForeground =>
			GetRequiredColor("TnPSuccessFigureWL");

		/// <summary>
		/// Blue link color.
		/// </summary>
		public static Color BlueLink =>
			GetRequiredColor("ContentLinkWL");

		/// <summary>
		/// Purple color with 15% transparency.
		/// </summary>
		public static Color Purple15 =>
			GetRequiredColor("TnPAccent2bgWL");

		/// <summary>
		/// Inserted Border color.
		/// </summary>
		public static Color InsertedBorder =>
			GetRequiredColor("TnPSuccessbgWL");

		/// <summary>
		/// Purple color.
		/// </summary>
		public static Color Purple =>
			GetRequiredColor("TnPAccent2ContentWL");

		/// <summary>
		/// Deleted Border color.
		/// </summary>
		public static Color DeletedBorder =>
			GetRequiredColor("TnPDangerbgWL");

		/// <summary>
		/// Blue affirm color with 20% transparency.
		/// </summary>
		public static Color Blue20Affirm =>
			GetRequiredColor("TnPInfobgWL");

		/// <summary>
		/// Blue color.
		/// </summary>
		public static Color Blue =>
			GetRequiredColor("TnPInfoContentWL");

		private static Color GetRequiredColor(string Key)
		{
			Color? Color = AppStyles.TryGetResource<Color>(Key);
			if (Color is null)
				throw new InvalidOperationException($"Missing color resource: {Key}");

			return Color;
		}


	}
}
