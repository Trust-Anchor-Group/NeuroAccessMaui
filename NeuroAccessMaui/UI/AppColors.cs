namespace NeuroAccessMaui.UI
{
	/// <summary>
	/// Static class that gives access to app-specific themed colors.
	/// All colors are fetched directly from the current resources; there are no separate dark/light variants.
	/// </summary>
	public static class AppColors
	{
		/// <summary>
		/// Primary foreground color.
		/// </summary>
		public static Color PrimaryForeground =>
			AppStyles.TryGetResource<Color>("ContentPrimaryWL");

		/// <summary>
		/// Primary background color.
		/// </summary>
		public static Color PrimaryBackground =>
			AppStyles.TryGetResource<Color>("SurfaceBackgroundWL");

		/// <summary>
		/// Secondary Background Color.
		/// </summary>
		public static Color SecondaryBackground =>
			AppStyles.TryGetResource<Color>("SurfaceElevation1WL");

		/// <summary>
		/// EnabledFilledButton background color.
		/// </summary>
		public static Color ButtonAccessPrimarybg =>
			AppStyles.TryGetResource<Color>("ButtonAccessPrimarybgWL");

		/// <summary>
		/// DisabledFilledButton background color.
		/// </summary>
		public static Color ButtonUniversalbgInactiveWL =>
			AppStyles.TryGetResource<Color>("ButtonUniversalbgInactiveWL");

		/// <summary>
		/// Alert color.
		/// </summary>
		public static Color Alert =>
			AppStyles.TryGetResource<Color>("InputFieldsContentDangerv800");

		/// <summary>
		/// Error Background Color.
		/// </summary>
		public static Color ErrorBackground =>
			AppStyles.TryGetResource<Color>("InputFieldsContentDangerv800");

		/// <summary>
		/// Clickable color.
		/// </summary>
		public static Color Clickable =>
			AppStyles.TryGetResource<Color>("InputFieldsAccentContentAssets");

		/// <summary>
		/// Weak password foreground color.
		/// </summary>
		public static Color WeakPasswordForeground =>
			AppStyles.TryGetResource<Color>("TnPDangerFigureWL");

		/// <summary>
		/// Medium password foreground color.
		/// </summary>
		public static Color MediumPasswordForeground =>
			AppStyles.TryGetResource<Color>("TnPWarningContentWL");

		/// <summary>
		/// Strong password foreground color.
		/// </summary>
		public static Color StrongPasswordForeground =>
			AppStyles.TryGetResource<Color>("TnPSuccessFigureWL");

		/// <summary>
		/// Blue link color.
		/// </summary>
		public static Color BlueLink =>
			AppStyles.TryGetResource<Color>("ContentLinkWL");

		/// <summary>
		/// Purple color with 15% transparency.
		/// </summary>
		public static Color Purple15 =>
			AppStyles.TryGetResource<Color>("TnPAccent2bgWL");

		/// <summary>
		/// Inserted Border color.
		/// </summary>
		public static Color InsertedBorder =>
			AppStyles.TryGetResource<Color>("TnPSuccessbgWL");

		/// <summary>
		/// Purple color.
		/// </summary>
		public static Color Purple =>
			AppStyles.TryGetResource<Color>("TnPAccent2ContentWL");

		/// <summary>
		/// Deleted Border color.
		/// </summary>
		public static Color DeletedBorder =>
			AppStyles.TryGetResource<Color>("TnPDangerbgWL");

		/// <summary>
		/// Blue affirm color with 20% transparency.
		/// </summary>
		public static Color Blue20Affirm =>
			AppStyles.TryGetResource<Color>("TnPInfobgWL");

		/// <summary>
		/// Blue color.
		/// </summary>
		public static Color Blue =>
			AppStyles.TryGetResource<Color>("TnPInfoContentWL");
	}
}
