using System.Diagnostics.CodeAnalysis;

namespace NeuroAccess.Nfc.TravelDocuments
{
	/// <summary>
	/// Contains MRZ-related Extensions for Machine-Readable Travel Documents.
	/// </summary>
	public static class MrzExtensions
	{
		/// <summary>
		/// Derives Basic Access Control Keys from the second row of the
		/// Machine-Readable string in passport (MRZ).
		/// </summary>
		/// <param name="MRZ">Machine-Readable text.</param>
		/// <param name="Info">Parsed Document Information.</param>
		/// <returns>If the string could be parsed.</returns>
		public static bool ParseMrz(string MRZ, [NotNullWhen(true)] out DocumentInformation? Info)
		{
			return MrzValidator.TryParse(MRZ, out Info);
		}
	}
}
