using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EDaler;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.Wallet;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using NeuroAccessMaui.UI.Pages.Kyc;
using NeuroAccessMaui.UI.Pages.Contracts;
using NeuroAccessMaui.UI.Pages.Main;
using NeuroAccessMaui.UI.Pages.Main.Apps;
using NeuroAccessMaui.UI.Pages.Wallet;
using NeuroAccessMaui.UI.Pages.Wallet.BuyEDaler;
using NeuroAccessMaui.UI.Pages.Wallet.MyWallet;
using NeuroAccessMaui.UI.Pages.Wallet.RequestPayment;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroFeatures;
using NeuroFeatures.EventArguments;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Persistence;

namespace NeuroAccessMaui.UI.Pages.Applications.Applications
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
			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				if (ServiceRef.TagProfile.IdentityApplication.IsDiscarded())
					await ServiceRef.TagProfile.SetIdentityApplication(null, true);
			}

			this.IdentityApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;

			this.HasLegalIdentity = ServiceRef.TagProfile.LegalIdentity is not null &&
				ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Approved;

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged += this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged += this.TagProfile_OnPropertiesChanged;

			await base.OnInitialize();
			this.NotifyCommandsCanExecuteChanged();
		}

		protected override Task OnDispose()
		{
			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;
			ServiceRef.XmppService.LegalIdentityChanged -= this.XmppService_LegalIdentityChanged;
			ServiceRef.TagProfile.OnPropertiesChanged -= this.TagProfile_OnPropertiesChanged;

			return base.OnDispose();
		}

		private Task XmppService_IdentityApplicationChanged(object? Sender, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.IdentityApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;
			});

			return Task.CompletedTask;
		}

		private Task XmppService_LegalIdentityChanged(object Sender, LegalIdentityEventArgs e)
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.HasLegalIdentity = ServiceRef.TagProfile.LegalIdentity is not null &&
					ServiceRef.TagProfile.LegalIdentity.State == IdentityState.Approved;
			});

			return Task.CompletedTask;
		}

		private void TagProfile_OnPropertiesChanged(object? sender, EventArgs e)
		{

		}

		protected override async Task OnAppearing()
		{
			await base.OnAppearing();

			// Page is not correctly updated if changes has happened when viewing a sub-view. Fix by resending notification.

			bool IdApplicationSent = ServiceRef.TagProfile.IdentityApplication is not null;
			if (this.IdentityApplicationSent != IdApplicationSent)
				this.IdentityApplicationSent = IdApplicationSent;
			else
				this.OnPropertyChanged(nameof(this.IdentityApplicationSent));
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
		/// Used to find out if a command can execute
		/// </summary>
		public bool CanExecuteCommands => !this.IsBusy;

		/// <summary>
		/// If an identity application has been sent.
		/// </summary>
		[ObservableProperty]
		private bool identityApplicationSent;

		/// <summary>
		/// If the user has an approved legal identity.
		/// </summary>
		[ObservableProperty]
		private bool hasLegalIdentity;

		#endregion

		#region Commands

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task ViewIdApplication()
		{
			try
			{
				if (ServiceRef.TagProfile.IdentityApplication is null)
					return;

				if (!await App.AuthenticateUserAsync(AuthenticationPurpose.ViewId))
					return;

				await ServiceRef.UiService.GoToAsync(nameof(KycProcessPage));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task ApplyPersonalId()
		{
			try
			{
				if (!await App.AuthenticateUserAsync(AuthenticationPurpose.ApplyForPersonalId))
					return;

				await ServiceRef.UiService.GoToAsync(nameof(KycProcessPage), new ApplyIdNavigationArgs(true, false));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task ApplyOrganizationalId()
		{
			try
			{
				if (!await App.AuthenticateUserAsync(AuthenticationPurpose.ApplyForOrganizationalId))
					return;

				await ServiceRef.UiService.GoToAsync(nameof(KycProcessPage), new ApplyIdNavigationArgs(false, false));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}



		#endregion
	}
}
