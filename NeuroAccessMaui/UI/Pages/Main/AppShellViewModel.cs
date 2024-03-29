﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Main.Settings;

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
		protected override Task OnInitialize()
		{
			this.UpdateProperties();

			ServiceRef.TagProfile.OnPropertiesChanged += this.TagProfile_OnPropertiesChanged;
			ServiceRef.XmppService.OnRosterItemAdded += this.XmppService_OnRosterItemAdded;
			ServiceRef.XmppService.OnRosterItemRemoved += this.XmppService_OnRosterItemRemoved;
			ServiceRef.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;

			return base.OnInitialize();
		}

		/// <inheritdoc/>
		protected override Task OnDispose()
		{
			ServiceRef.TagProfile.OnPropertiesChanged -= this.TagProfile_OnPropertiesChanged;
			ServiceRef.XmppService.OnRosterItemAdded -= this.XmppService_OnRosterItemAdded;
			ServiceRef.XmppService.OnRosterItemRemoved -= this.XmppService_OnRosterItemRemoved;
			ServiceRef.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;

			return base.OnDispose();
		}

		private void TagProfile_OnPropertiesChanged(object? sender, EventArgs e)
		{
			this.UpdateProperties();
		}

		private Task XmppService_ConnectionStateChanged(object Sender, Waher.Networking.XMPP.XmppState NewState)
		{
			if (NewState == Waher.Networking.XMPP.XmppState.Connected)
				this.UpdateProperties();

			return Task.CompletedTask;
		}

		private Task XmppService_OnRosterItemRemoved(object Sender, Waher.Networking.XMPP.RosterItem Item)
		{
			this.UpdateProperties();

			return Task.CompletedTask;
		}

		private Task XmppService_OnRosterItemAdded(object Sender, Waher.Networking.XMPP.RosterItem Item)
		{
			this.UpdateProperties();

			return Task.CompletedTask;
		}

		private void UpdateProperties()
		{
			this.CanShowMyContractsCommand = ServiceRef.TagProfile.HasContractReferences;
			this.CanShowCreateContractCommand = ServiceRef.TagProfile.HasContractTemplateReferences;
			this.CanShowCreateTokenCommand = ServiceRef.TagProfile.HasContractTokenCreationTemplatesReferences;
			this.CanShowContactsCommand = ServiceRef.XmppService.Roster is not null && ServiceRef.XmppService.Roster.Length > 0;
		}

		/// <summary>
		/// Closes the app.
		/// </summary>
		/// <returns></returns>
		[RelayCommand]
		private static async Task Close()
		{
			try
			{
				AppShell.Current.FlyoutIsPresented = false;
				await App.Stop();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Views the user's ID.
		/// </summary>
		[RelayCommand]
		private static async Task ViewId()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(ViewIdentityPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Shows the settings page.
		/// </summary>
		[RelayCommand]
		private static async Task ShowSettings()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(SettingsPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Shows the applications page.
		/// </summary>
		[RelayCommand]
		private static async Task ShowApplications()
		{
			try
			{
				await ServiceRef.UiService.GoToAsync(nameof(ApplicationsPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// If the My Contracts command should be displayed.
		/// </summary>
		[ObservableProperty]
		private bool canShowMyContractsCommand;

		[RelayCommand]
		private static async Task ShowMyContracts()
		{
			try
			{
				MyContractsNavigationArgs Args = new(ContractsListMode.Contracts);
				await ServiceRef.UiService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// If the Create Contract command should be displayed.
		/// </summary>
		[ObservableProperty]
		private bool canShowCreateContractCommand;

		[RelayCommand]
		private static async Task CreateContract()
		{
			try
			{
				MyContractsNavigationArgs Args = new(ContractsListMode.ContractTemplates);
				await ServiceRef.UiService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// If the Create Token command should be displayed.
		/// </summary>
		[ObservableProperty]
		private bool canShowCreateTokenCommand;

		[RelayCommand]
		private static async Task CreateToken()
		{
			try
			{
				MyContractsNavigationArgs Args = new(ContractsListMode.TokenCreationTemplates);
				await ServiceRef.UiService.GoToAsync(nameof(MyContractsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// If the Contacts command should be displayed.
		/// </summary>
		[ObservableProperty]
		private bool canShowContactsCommand;

		[RelayCommand]
		private static async Task ShowContacts()
		{
			try
			{
				ContactListNavigationArgs Args = new();
				await ServiceRef.UiService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.Pop);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}
	}
}
