using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.Models
{
	/// <summary>
	/// A legal identity identifier read from a link, with the identity when it is accessible.
	/// </summary>
	public sealed class IdentityReference
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IdentityReference"/> class.
		/// </summary>
		/// <param name="Identifier">The legal identity identifier.</param>
		/// <param name="Identity">The resolved identity, or <c>null</c> when access must be requested from its owner.</param>
		public IdentityReference(string Identifier, LegalIdentity? Identity)
		{
			this.Identifier = Identifier;
			this.Identity = Identity;
		}

		/// <summary>
		/// Gets the legal identity identifier.
		/// </summary>
		public string Identifier { get; }

		/// <summary>
		/// Gets the resolved identity, or <c>null</c> when access must be requested from its owner.
		/// </summary>
		public LegalIdentity? Identity { get; }
	}
}
