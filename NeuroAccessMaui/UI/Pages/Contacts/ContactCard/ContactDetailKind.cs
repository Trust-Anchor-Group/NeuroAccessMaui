namespace NeuroAccessMaui.UI.Pages.Contacts.ContactCard
{
	/// <summary>
	/// What a row of contact details holds, which decides what a tap on it does.
	/// </summary>
	public enum ContactDetailKind
	{
		/// <summary>A phone number. A tap calls it.</summary>
		Phone,

		/// <summary>An email address. A tap writes to it.</summary>
		Email,

		/// <summary>A postal address. A tap shows it on a map.</summary>
		Address,

		/// <summary>The organization and role.</summary>
		Organization,

		/// <summary>The birthday.</summary>
		Birthday
	}
}
