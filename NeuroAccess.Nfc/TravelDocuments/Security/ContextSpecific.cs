using System;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// A context-specific object (or set of objects).
	/// </summary>
	/// <param name="Tag">Context-specific tag.</param>
	/// <param name="Elements">Elements in sequence.</param>
	/// <param name="SubSection">Binary subsection encompassing the data.</param>
	public class ContextSpecific(int Tag, Array Elements, byte[] SubSection)
		: Vector(Elements, SubSection)
	{
		/// <summary>
		/// Context-specific tag.
		/// </summary>
		public int Tag { get; } = Tag;
	}
}
