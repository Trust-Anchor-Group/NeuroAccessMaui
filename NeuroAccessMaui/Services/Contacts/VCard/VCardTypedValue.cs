namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// A value of a contact card with its types, such as a phone number marked "cell" or an email address marked "work".
	/// </summary>
	/// <param name="Value">The value.</param>
	/// <param name="Types">Types, in lower case.</param>
	/// <param name="IsPreferred">If the value is preferred over others of its kind.</param>
	public sealed record VCardTypedValue(string Value, IReadOnlyList<string> Types, bool IsPreferred);
}
