namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// The parts of a person's name, as in the N property of a contact card.
	/// </summary>
	/// <param name="Family">Family name.</param>
	/// <param name="Given">Given name.</param>
	/// <param name="Additional">Additional names, such as middle names.</param>
	/// <param name="Prefixes">Honorific prefixes, such as "Dr.".</param>
	/// <param name="Suffixes">Honorific suffixes, such as "Jr.".</param>
	public sealed record VCardName(string Family, string Given, string Additional, string Prefixes, string Suffixes)
	{
		/// <summary>
		/// If no part of the name is filled in.
		/// </summary>
		public bool IsEmpty => string.IsNullOrWhiteSpace(this.Family) && string.IsNullOrWhiteSpace(this.Given) &&
			string.IsNullOrWhiteSpace(this.Additional) && string.IsNullOrWhiteSpace(this.Prefixes) &&
			string.IsNullOrWhiteSpace(this.Suffixes);

		/// <summary>
		/// The name as it is usually written: prefixes, given, additional and family names, then suffixes.
		/// </summary>
		/// <returns>The full name.</returns>
		public string ToDisplayName()
		{
			IEnumerable<string> Parts = new[] { this.Prefixes, this.Given, this.Additional, this.Family, this.Suffixes }
				.Where(Part => !string.IsNullOrWhiteSpace(Part))
				.Select(Part => Part.Replace(',', ' ').Trim());

			return string.Join(' ', Parts);
		}
	}
}
