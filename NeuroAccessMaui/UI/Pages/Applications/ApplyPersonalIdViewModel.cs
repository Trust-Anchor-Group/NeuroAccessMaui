using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Pages.Registration;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Applications
{
	/// <summary>
	/// The view model to bind to for when displaying the an application for a Personal ID.
	/// </summary>
	public partial class ApplyPersonalIdViewModel : RegisterIdentityModel
	{
		/// <summary>
		/// Creates an instance of the <see cref="ApplyPersonalIdViewModel"/> class.
		/// </summary>
		public ApplyPersonalIdViewModel()
			: base()
		{
		}

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			LegalIdentity? CurrentId = ServiceRef.TagProfile.LegalIdentity;
			if (CurrentId is not null)
				this.SetProperties(CurrentId.Properties, true);

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
			this.ApplyCommand.NotifyCanExecuteChanged();
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
		private async Task Apply()
		{
			try
			{
				if (!await App.AuthenticateUser(true))
					return;

				// TODO
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
