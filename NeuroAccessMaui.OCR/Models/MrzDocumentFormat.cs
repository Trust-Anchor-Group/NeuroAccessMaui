namespace NeuroAccessMaui.OCR.Models
{
	/// <summary>
	/// Identifies the supported MRZ document layouts.
	/// </summary>
	public enum MrzDocumentFormat
	{
		/// <summary>
		/// TD1 identity-card layout with three 30-character lines.
		/// </summary>
		Td1 = 0,

		/// <summary>
		/// TD2 visa or card layout with two 36-character lines.
		/// </summary>
		Td2 = 1,

		/// <summary>
		/// TD3 passport layout with two 44-character lines.
		/// </summary>
		Td3 = 2
	}
}
