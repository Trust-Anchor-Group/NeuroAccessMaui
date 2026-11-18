namespace NeuroAccessMaui.UI.Popups.Contacts
{
	/// <summary>
	/// What the user chose to do about someone they don't want to hear from.
	/// </summary>
	public enum BlockChoice
	{
		/// <summary>Block them on this phone.</summary>
		Block,

		/// <summary>Block them, and report them to the server as sending spam.</summary>
		ReportSpam,

		/// <summary>Block them, and report them to the server as abusive.</summary>
		ReportAbuse,

		/// <summary>Block them, and report them to the server for another reason.</summary>
		ReportOther
	}
}
