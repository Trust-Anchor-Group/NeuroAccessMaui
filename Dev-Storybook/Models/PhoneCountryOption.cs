namespace DevStorybook.Models
{
	/// <summary>
	/// Contains the controlled country data needed by the production country-code picker.
	/// </summary>
	/// <param name="Name">The display name.</param>
	/// <param name="DialCode">The calling code without a leading plus sign.</param>
	public sealed record PhoneCountryOption(string Name, string DialCode);
}
