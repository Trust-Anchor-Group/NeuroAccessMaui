namespace NeuroAccessMaui.Services.Data
{
	/// <summary>
	/// Contains one record of the ISO 5218 data set.
	/// </summary>
	/// <param name="Gender">Gender name</param>
	/// <param name="Code">ISO 5218 designated Code</param>
	/// <param name="Letter">Letter used in Legal IDs GENDER property.</param>
	/// <param name="LocalizedNameId">String ID for localized string.</param>
	/// <param name="Unicode">Unicode character.</param>
	public class ISO_5218_Gender(string Gender, int Code, string Letter, string LocalizedNameId, char Unicode)
	{
		/// <summary>
		/// Gender
		/// </summary>
		public string Gender { get; } = Gender;

		/// <summary>
		/// ISO 5218 gender code
		/// </summary>
		public int Code { get; } = Code;

		/// <summary>
		/// Character used in Legal IDs GENDER property.
		/// </summary>
		public string Letter { get; } = Letter;

		/// <summary>
		/// String ID for localized string.
		/// </summary>
		public string LocalizedNameId { get; } = LocalizedNameId;

		/// <summary>
		/// Unicode character.
		/// </summary>
		public char Unicode { get; } = Unicode;
	}
}
