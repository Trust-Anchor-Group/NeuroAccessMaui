using System;
using System.Collections.Generic;
namespace NeuroAccess.Nfc.TravelDocuments.Security
{
	/// <summary>
	/// Contents of EF.SOD. Reference: §4.6.2, ICAO Doc 9303-10.
	/// </summary>
	public class SignedData
	{
		/// <summary>
		/// Contents of EF.SOD. Reference: §4.6.2, ICAO Doc 9303-10.
		/// </summary>
		public SignedData()
		{
		}

		public static bool TryParse(object Decoded, out SignedData? Result)
		{
			Result = null;

			if (Decoded is not Array A)
				return false;

			return false;
		}


	}
}
