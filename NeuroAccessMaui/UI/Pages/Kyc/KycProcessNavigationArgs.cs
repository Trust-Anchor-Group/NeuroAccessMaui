using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// Navigation arguments for KYC application pages.
	/// </summary>
	public class KycProcessNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycProcessNavigationArgs"/> class.
		/// </summary>
		public KycProcessNavigationArgs()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="KycProcessNavigationArgs"/> class.
		/// </summary>
		/// <param name="Reference">The KYC reference to load.</param>
		public KycProcessNavigationArgs(KycReference Reference)
		{
			this.Reference = Reference;
		}

		/// <summary>
		/// Gets the KYC reference to load.
		/// </summary>
		public KycReference? Reference { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the KYC flow should resume in form mode, ignoring a saved summary state.
		/// </summary>
		public bool ForceFormResume { get; init; }

		/// <summary>
		/// Gets or sets a value indicating whether an incomplete travel-document chip attempt should be abandoned.
		/// </summary>
		public bool AbandonTravelDocumentAttempt { get; init; }
	}
}

