using System.Threading;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.UI;

namespace NeuroAccessMaui.Services.Kyc.Actions
{
	/// <summary>
	/// Provides the data and services needed to evaluate or execute a KYC page action.
	/// </summary>
	public class KycPageActionContext
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KycPageActionContext"/> class.
		/// </summary>
		/// <param name="Reference">The active KYC reference.</param>
		/// <param name="Process">The active KYC process.</param>
		/// <param name="Page">The page that declared the action.</param>
		/// <param name="KycService">The KYC service used for persistence and validation.</param>
		/// <param name="NavigationService">The navigation service used to open action-owned pages.</param>
		/// <param name="CancellationToken">The cancellation token for action work.</param>
		public KycPageActionContext(
			KycReference Reference,
			KycProcess Process,
			KycPage Page,
			IKycService KycService,
			INavigationService NavigationService,
			CancellationToken CancellationToken)
		{
			this.Reference = Reference;
			this.Process = Process;
			this.Page = Page;
			this.KycService = KycService;
			this.NavigationService = NavigationService;
			this.CancellationToken = CancellationToken;
		}

		/// <summary>
		/// Gets the active KYC reference.
		/// </summary>
		public KycReference Reference { get; }

		/// <summary>
		/// Gets the active KYC process.
		/// </summary>
		public KycProcess Process { get; }

		/// <summary>
		/// Gets the page that declared the action.
		/// </summary>
		public KycPage Page { get; }

		/// <summary>
		/// Gets the KYC service used for persistence and validation.
		/// </summary>
		public IKycService KycService { get; }

		/// <summary>
		/// Gets the navigation service used to open action-owned pages.
		/// </summary>
		public INavigationService NavigationService { get; }

		/// <summary>
		/// Gets the cancellation token for action work.
		/// </summary>
		public CancellationToken CancellationToken { get; }
	}
}
