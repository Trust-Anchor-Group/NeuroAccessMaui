namespace NeuroAccessMaui.Services.Data.PersonalNumbers
{
	/// <summary>
	/// Personal number information
	/// </summary>
	public class NumberInformation
	{
		/// <summary>
		/// String representation of the personal number.
		/// </summary>
		public string? PersonalNumber;

		/// <summary>
		/// true = valid: PersonalNumber may be normalized.
		/// false = invalid
		/// null = scheme not applicable, or no registered schemes for country
		/// </summary>
		public bool? IsValid;

		/// <summary>
		/// A string that can be displayed to a user, informing the user about the approximate format expected.
		/// </summary>
		public string? DisplayString;
	}
}
