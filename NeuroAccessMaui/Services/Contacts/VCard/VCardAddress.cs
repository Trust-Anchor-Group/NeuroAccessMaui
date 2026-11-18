namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// A postal address, as in the ADR property of a contact card.
	/// </summary>
	/// <param name="PostOfficeBox">Post office box.</param>
	/// <param name="Extended">Extended address, such as an apartment number.</param>
	/// <param name="Street">Street address.</param>
	/// <param name="Locality">City or town.</param>
	/// <param name="Region">Region, state or province.</param>
	/// <param name="PostalCode">Postal code.</param>
	/// <param name="Country">Country.</param>
	/// <param name="Types">Types, such as "home" or "work", in lower case.</param>
	/// <param name="Label">The address as one formatted text, if the card gives one.</param>
	public sealed record VCardAddress(string PostOfficeBox, string Extended, string Street, string Locality, string Region,
		string PostalCode, string Country, IReadOnlyList<string> Types, string? Label)
	{
		/// <summary>
		/// If no part of the address is filled in.
		/// </summary>
		public bool IsEmpty => string.IsNullOrWhiteSpace(this.PostOfficeBox) && string.IsNullOrWhiteSpace(this.Extended) &&
			string.IsNullOrWhiteSpace(this.Street) && string.IsNullOrWhiteSpace(this.Locality) &&
			string.IsNullOrWhiteSpace(this.Region) && string.IsNullOrWhiteSpace(this.PostalCode) &&
			string.IsNullOrWhiteSpace(this.Country);

		/// <summary>
		/// The address on one line, with its parts separated by commas: the label if the card gives one, or else the parts
		/// that are filled in.
		/// </summary>
		/// <returns>The address.</returns>
		public string ToSingleLine()
		{
			if (!string.IsNullOrWhiteSpace(this.Label))
				return this.Label.Replace("\r\n", "\n").Replace("\n", ", ").Trim();

			string Place = string.Join(' ', new[] { this.PostalCode, this.Locality }.Where(Part => !string.IsNullOrWhiteSpace(Part)));
			IEnumerable<string> Parts = new[] { this.PostOfficeBox, this.Street, this.Extended, Place, this.Region, this.Country }
				.Where(Part => !string.IsNullOrWhiteSpace(Part))
				.Select(Part => Part.Trim());

			return string.Join(", ", Parts);
		}
	}
}
