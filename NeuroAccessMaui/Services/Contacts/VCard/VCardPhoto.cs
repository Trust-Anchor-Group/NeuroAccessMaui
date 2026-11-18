namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// The photo of a contact card: either embedded data with its media type, or a link to it.
	/// </summary>
	/// <param name="Data">The encoded image, if it is embedded in the card.</param>
	/// <param name="MediaType">Media type of the embedded image, such as image/jpeg, if known.</param>
	/// <param name="Uri">Link to the image, if it is not embedded.</param>
	public sealed record VCardPhoto(byte[]? Data, string? MediaType, string? Uri)
	{
		/// <summary>
		/// If the image is embedded in the card.
		/// </summary>
		public bool IsEmbedded => this.Data is not null && this.Data.Length > 0;
	}
}
