using CommunityToolkit.Mvvm.ComponentModel;
using NeuroAccessMaui.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using IServiceProvider = Waher.Networking.XMPP.Contracts.IServiceProvider;

namespace NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders
{
	/// <summary>
	/// The view model to bind to for when displaying a list of service providers.
	/// </summary>
	/// <param name="NavigationArgs">Navigation arguments.</param>
	public partial class ServiceProvidersViewModel(ServiceProvidersNavigationArgs? NavigationArgs) : XmppViewModel()
	{
		private const int defaultIconHeight = 150;

		private readonly bool useShellNavigationService = NavigationArgs is null;
		private ServiceProvidersNavigationArgs? navigationArgs = NavigationArgs;

		/// <summary>
		/// Creates an instance of the <see cref="ServiceProvidersViewModel"/> class.
		/// </summary>
		public ServiceProvidersViewModel()
			: this(ServiceRef.UiService.PopLatestArgs<ServiceProvidersNavigationArgs>())
		{
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.navigationArgs is null && ServiceRef.UiService.TryGetArgs(out ServiceProvidersNavigationArgs? Args))
				this.navigationArgs = Args;

			this.Title = this.navigationArgs?.Title;
			this.Description = this.navigationArgs?.Description;

			if (this.navigationArgs is not null)
			{
				foreach (IServiceProvider ServiceProvider in this.navigationArgs.ServiceProviders)
					this.ServiceProviders.Add(new ServiceProviderViewModel(ServiceProvider, defaultIconHeight, this));
			}
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			if (this.navigationArgs?.ServiceProvider is TaskCompletionSource<IServiceProvider> TaskSource)
				TaskSource.TrySetResult(null);

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// Title to show the user.
		/// </summary>
		[ObservableProperty]
		private string? title;

		/// <summary>
		/// Description to show the user.
		/// </summary>
		[ObservableProperty]
		private string? description;

		/// <summary>
		/// Holds a list of service providers
		/// </summary>
		public ObservableCollection<ServiceProviderViewModel> ServiceProviders { get; } = [];

		/// <summary>
		/// The currently selected contact, if any.
		/// </summary>
		[ObservableProperty]
		private ServiceProviderViewModel? selectedServiceProvider;

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			switch (e.PropertyName)
			{
				case nameof(this.SelectedServiceProvider):
					MainThread.BeginInvokeOnMainThread(async () =>
					{
						await this.TrySetResultAndClosePage(this.SelectedServiceProvider?.ServiceProvider);
					});
					break;
			}
		}

		#endregion

		/// <inheritdoc/>
		public override Task GoBack()
		{
			return this.TrySetResultAndClosePage(null);
		}

		private async Task TrySetResultAndClosePage(IServiceProvider? ServiceProvider)
		{
			TaskCompletionSource<IServiceProvider?>? TaskSource = null;

			if (this.navigationArgs is not null)
			{
				TaskSource = this.navigationArgs.ServiceProvider;
				this.navigationArgs.ServiceProvider = null;
			}

			await base.GoBack();

			TaskSource?.TrySetResult(ServiceProvider);
		}

		/// <summary>
		/// Selects a service provider.
		/// </summary>
		/// <param name="ServiceProvider">Service provider selected.</param>
		internal async Task SelectServiceProvider(ServiceProviderViewModel ServiceProvider)
		{
			await this.TrySetResultAndClosePage(ServiceProvider.ServiceProvider);
		}
	}
}
