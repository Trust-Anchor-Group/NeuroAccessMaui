using System;

namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// A generic sequence class used if dedicated security objects cannot be found.
	/// </summary>
	/// <param name="SecurityInfo">Security information.</param>
	/// <param name="SubSection">Binary subsection encompassing the data.</param>
	public class Sequence(Array SecurityInfo, byte[] SubSection)
		: Vector(SecurityInfo, SubSection)
	{
	}
}
