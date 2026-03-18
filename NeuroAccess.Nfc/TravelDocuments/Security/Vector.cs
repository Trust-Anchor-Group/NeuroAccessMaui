using System;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Abstract base class for arrays of security objects.
	/// </summary>
	/// <param name="SecurityInfo">Security information.</param>
	/// <param name="SubSection">Binary subsection encompassing the data.</param>
	public abstract class Vector(Array SecurityInfo, byte[] SubSection)
	{
		/// <summary>
		/// Binary Value.
		/// </summary>
		public Array Elements { get; } = SecurityInfo;

		/// <summary>
		/// Subsection
		/// </summary>
		public byte[] SubSection { get; } = SubSection;
	}
}
