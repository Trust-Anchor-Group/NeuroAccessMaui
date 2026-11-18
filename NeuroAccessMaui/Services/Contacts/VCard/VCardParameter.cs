namespace NeuroAccessMaui.Services.Contacts.VCard
{
	/// <summary>
	/// A parameter of a contact card property, such as <c>TYPE=cell,voice</c>. Its name is kept in upper case, and it can
	/// have several values.
	/// </summary>
	public sealed class VCardParameter
	{
		/// <summary>
		/// A parameter of a contact card property.
		/// </summary>
		/// <param name="Name">Name of the parameter. It is stored in upper case.</param>
		/// <param name="Values">Values of the parameter.</param>
		public VCardParameter(string Name, IEnumerable<string> Values)
		{
			this.Name = Name.Trim().ToUpperInvariant();
			this.Values = [.. Values];
		}

		/// <summary>
		/// Name of the parameter, in upper case.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Values of the parameter, as read.
		/// </summary>
		public List<string> Values { get; }
	}
}
