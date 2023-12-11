using NeuroAccessMaui.Services.Navigation;

namespace NeuroAccessMaui.UI.Pages.Identity
{
	/// <summary>
	/// Holds navigation parameters specific to views displaying an identity transfer code.
	/// </summary>
	/// <param name="Uri">Transfer URI</param>
	public class TransferIdentityNavigationArgs(string? Uri) : NavigationArgs
	{
		/// <summary>
		/// Creates a new instance of the <see cref="TransferIdentityNavigationArgs"/> class.
		/// </summary>
		public TransferIdentityNavigationArgs() : this(null) { }

		/// <summary>
		/// Transfer URI
		/// </summary>
		public string? Uri { get; } = Uri;
	}
}
