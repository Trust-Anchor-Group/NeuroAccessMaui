namespace NeuroAccessMaui.Services.Contacts
{
	/// <summary>
	/// Parts of a contact, such as those that importing a card changes.
	/// </summary>
	[Flags]
	public enum ContactFields
	{
		/// <summary>Nothing.</summary>
		None = 0,

		/// <summary>The name.</summary>
		Name = 1,

		/// <summary>Phone numbers.</summary>
		Phone = 2,

		/// <summary>Email addresses.</summary>
		Email = 4,

		/// <summary>Postal addresses.</summary>
		Address = 8,

		/// <summary>Organization or role.</summary>
		Organization = 16,

		/// <summary>The photo.</summary>
		Photo = 32,

		/// <summary>Notes.</summary>
		Notes = 64,

		/// <summary>Labels.</summary>
		Labels = 128,

		/// <summary>Birthday.</summary>
		Birthday = 256,

		/// <summary>Chat address.</summary>
		ChatAddress = 512
	}
}
