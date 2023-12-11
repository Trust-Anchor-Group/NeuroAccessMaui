using Waher.Runtime.Inventory;

namespace NeuroAccessMaui.Links
{
	/// <summary>
	/// Interface for classes that can open links.
	/// </summary>
	public interface ILinkOpener : IProcessingSupport<Uri>
	{
		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		Task<bool> TryOpenLink(Uri Link);
	}
}
