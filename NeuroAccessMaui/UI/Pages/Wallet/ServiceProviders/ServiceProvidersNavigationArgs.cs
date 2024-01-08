using NeuroAccessMaui.Services.Navigation;
using IServiceProvider = Waher.Networking.XMPP.Contracts.IServiceProvider;

namespace NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// Holds navigation parameters for selecting a service provider.
	/// </summary>
	/// <param name="ServiceProviders">Service Providers</param>
	/// <param name="Title">Title to show the user.</param>
	/// <param name="Description">Descriptive text to show the user.</param>
	public class ServiceProvidersNavigationArgs(IServiceProvider[] ServiceProviders, string Title, string Description) : NavigationArgs
	{
		private readonly IServiceProvider[] serviceProviders = ServiceProviders;
		private readonly string title = Title;
		private readonly string description = Description;

		/// <summary>
		/// Creates a new instance of the <see cref="ServiceProvidersNavigationArgs"/> class.
		/// </summary>
		public ServiceProvidersNavigationArgs()
			: this([], string.Empty, string.Empty)
		{
		}

		/// <summary>
		/// Available service providers.
		/// </summary>
		public IServiceProvider[] ServiceProviders => this.serviceProviders;

		/// <summary>
		/// Title to show the user.
		/// </summary>
		public string Title => this.title;

		/// <summary>
		/// Descriptive text to show the user.
		/// </summary>
		public string Description => this.description;

		/// <summary>
		/// Task completion source; can be used to wait for a result.
		/// </summary>
		public TaskCompletionSource<IServiceProvider?>? ServiceProvider { get; internal set; } = new();
	}
}
