namespace NeuroAccessMaui.Services.Profiles
{
	/// <summary>
	/// What the contacts backup in the account's private storage holds.
	/// </summary>
	/// <param name="Exists">If there is a backup.</param>
	/// <param name="Count">How many contacts the backup holds.</param>
	/// <param name="Updated">When the backup was made, in UTC, or <see cref="DateTime.MinValue"/> if unknown.</param>
	/// <param name="Text">The contacts, as one contact file.</param>
	public sealed record ContactBackupInfo(bool Exists, int Count, DateTime Updated, string Text)
	{
		/// <summary>
		/// No backup.
		/// </summary>
		public static ContactBackupInfo None { get; } = new(false, 0, DateTime.MinValue, string.Empty);
	}
}
