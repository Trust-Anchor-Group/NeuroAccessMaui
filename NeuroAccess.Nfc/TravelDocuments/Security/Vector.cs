using System;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Abstract base class for arrays of security objects.
	/// </summary>
	/// <param name="Elements">Elements in sequence.</param>
	/// <param name="SubSection">Binary subsection encompassing the data.</param>
	public abstract class Vector(Array Elements, byte[] SubSection)
	{
		/// <summary>
		/// Binary Value.
		/// </summary>
		public Array Elements { get; } = Elements;

		/// <summary>
		/// Subsection
		/// </summary>
		public byte[] SubSection { get; } = SubSection;
	}
}
