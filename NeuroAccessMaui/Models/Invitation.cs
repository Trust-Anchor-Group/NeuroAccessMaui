using System.Xml;

namespace NeuroAccessMaui.Models
{
	/// <summary>
	/// A validated onboarding invitation and the URI it was read from.
	/// </summary>
	/// <remarks>
	/// The URI contains the key material used to decrypt the invitation. Never log it or show it to the user.
	/// </remarks>
	public sealed class Invitation
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Invitation"/> class.
		/// </summary>
		/// <param name="Uri">The original invitation URI.</param>
		/// <param name="Definition">The decrypted and validated invitation definition.</param>
		public Invitation(Uri Uri, XmlDocument Definition)
		{
			ArgumentNullException.ThrowIfNull(Uri);
			ArgumentNullException.ThrowIfNull(Definition);

			this.Uri = Uri;
			this.Definition = Definition;
		}

		/// <summary>
		/// Gets the original invitation URI.
		/// </summary>
		public Uri Uri { get; }

		/// <summary>
		/// Gets the decrypted and validated invitation definition.
		/// </summary>
		public XmlDocument Definition { get; }
	}
}
