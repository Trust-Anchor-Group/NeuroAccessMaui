namespace NeuroAccess.Nfc.TravelDocuments.Security.MaskGenerationFunctions
{
	/// <summary>
	/// Abstract base class of Mask Generation Functions.
	/// </summary>
	/// <param name="HashFunction">Hash function to use.</param>
	public abstract class MaskGenerationFunction : SecurityObject
	{
		/// <summary>
		/// Calcaultes a mask of a specific length, given a seed.
		/// </summary>
		/// <param name="Seed">Seed value.</param>
		/// <param name="Length">Length of mask.</param>
		/// <returns>Generated mask.</returns>
		public abstract byte[] CalculateMask(byte[] Seed, int Length);
	}
}
