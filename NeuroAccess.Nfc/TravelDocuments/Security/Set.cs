using System;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// A generic set class used if dedicated security objects cannot be found.
	/// </summary>
	/// <param name="SecurityInfo">Security information.</param>
	/// <param name="SubSection">Binary subsection encompassing the data.</param>
	public class Set(Array SecurityInfo, byte[] SubSection)
		: Vector(SecurityInfo, SubSection)
	{
	}
}
