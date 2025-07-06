using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Main.Settings;
using NeuroAccessMaui.UI.Pages.Things.MyThings;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Main
{
	/// <summary>
	/// View model for the <see cref="AppShell"/>.
	/// </summary>
	public partial class AppShellViewModel : BaseViewModel
	{
		/// <summary>
		/// View model for the <see cref="AppShell"/>.
		/// </summary>
		public AppShellViewModel()
		{
		}

		/// <inheritdoc/>
		public override Task OnInitializeAsync()
		{
			return base.OnInitializeAsync();
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			return base.OnDisposeAsync();
		}
	}
}
