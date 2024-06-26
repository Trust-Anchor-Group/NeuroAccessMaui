﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

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

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;

			await base.OnInitialize();
			this.NotifyCommandsCanExecuteChanged();
		}

		protected override Task OnDispose()
		{
			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;

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

		#endregion

		#region Commands

		[RelayCommand(CanExecute = nameof(CanExecuteCommands))]
		private async Task ViewIdApplication()
		{
			try
			{
				if (ServiceRef.TagProfile.IdentityApplication is null)
					return;

				if (!await App.AuthenticateUser(AuthenticationPurpose.ViewId))
					return;

				await ServiceRef.UiService.GoToAsync(nameof(ApplyIdPage));
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
				if (!await App.AuthenticateUser(AuthenticationPurpose.ApplyForPersonalId))
					return;

				await ServiceRef.UiService.GoToAsync(nameof(ApplyIdPage), new ApplyIdNavigationArgs(true, false));
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
				if (!await App.AuthenticateUser(AuthenticationPurpose.ApplyForOrganizationalId))
					return;

				await ServiceRef.UiService.GoToAsync(nameof(ApplyIdPage), new ApplyIdNavigationArgs(false, false));
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
