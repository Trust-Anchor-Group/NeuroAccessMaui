using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Waher.Content.Xml;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using NeuroAccessMaui.UI.Pages.Identity;
using Mopups.Services;
using NeuroAccessMaui.UI.Popups;
using NeuroAccessMaui.Services.Tag;

namespace NeuroAccessMaui.UI.Pages.Main.Applications
{
	/// <summary>
	/// The view model to bind to for when displaying the applications page.
	/// </summary>
	public partial class ApplicationsViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="ApplicationsViewModel"/> class.
		/// </summary>
		public ApplicationsViewModel()
			: base()
		{
		}

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			this.NotifyCommandsCanExecuteChanged();
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object? Sender, XmppState NewState)
		{
			return MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await base.XmppService_ConnectionStateChanged(Sender, NewState);

				this.NotifyCommandsCanExecuteChanged();
			});
		}

		/// <inheritdoc/>
		public override void SetIsBusy(bool IsBusy)
		{
			base.SetIsBusy(IsBusy);
			this.NotifyCommandsCanExecuteChanged();
		}

		private void NotifyCommandsCanExecuteChanged()
		{
			this.ApplyPersonalIdCommand.NotifyCanExecuteChanged();
			this.ApplyOrganizationalIdCommand.NotifyCanExecuteChanged();
		}

		#region Properties

		/// <summary>
		/// Used to find out if an ICommand can execute
		/// </summary>
		public bool CanExecuteCommands => !this.IsBusy && this.IsConnected;

		#endregion

		#region Commands

		[RelayCommand]
		private static async Task GoBack()
		{
			await ServiceRef.NavigationService.GoBackAsync();
		}

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task ApplyPersonalId()
		{
			try
			{
				if (!await App.AuthenticateUser())
					return;

				//await ServiceRef.NavigationService.GoToAsync(nameof(ApplyPersonalIdPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task ApplyOrganizationalId()
		{
			try
			{
				if (!await App.AuthenticateUser())
					return;

				//await ServiceRef.NavigationService.GoToAsync(nameof(ApplyOrganizationalIdPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayException(ex);
			}
		}

		#endregion
	}
}
