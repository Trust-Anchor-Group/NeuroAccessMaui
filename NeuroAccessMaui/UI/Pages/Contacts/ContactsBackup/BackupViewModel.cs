using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Profiles;
using NeuroAccessMaui.Services.UI.Toasts;
using NeuroAccessMaui.UI.Pages.Contacts.Chat;

namespace NeuroAccessMaui.UI.Pages.Contacts.ContactsBackup
{
	/// <summary>
	/// The view model of the page where the user turns the contacts backup on or off, backs up now, restores the backup, or
	/// deletes it.
	/// </summary>
	public partial class BackupViewModel : BaseViewModel
	{
		private bool showing;
		private bool initialized;

		#region Properties

		/// <summary>
		/// If the backup is on.
		/// </summary>
		[ObservableProperty]
		private bool isEnabled;

		/// <summary>
		/// When the contacts were last backed up, as a sentence.
		/// </summary>
		[ObservableProperty]
		private string lastBackupText = string.Empty;

		/// <summary>
		/// If the backup is being read, made, restored or deleted.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(BackupNowCommand))]
		[NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
		[NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
		private bool isWorking;

		/// <summary>
		/// If there is a backup in the account's private storage.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
		[NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
		private bool hasBackup;

		/// <summary>
		/// What the backup holds, or that there is none, or that it can't be checked now.
		/// </summary>
		[ObservableProperty]
		private string backupText = string.Empty;

		/// <summary>
		/// If the app is connected, so the backup can be made, read and deleted.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(BackupNowCommand))]
		[NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
		[NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
		private bool isOnline;

		#endregion

		#region Life cycle

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();

			if (this.initialized)
				return;

			this.initialized = true;
			ContactBackup.Changed += this.ContactBackup_Changed;
		}

		/// <inheritdoc/>
		public override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();
			await this.RefreshAsync();
		}

		/// <inheritdoc/>
		public override Task OnDisposeAsync()
		{
			if (this.initialized)
			{
				this.initialized = false;
				ContactBackup.Changed -= this.ContactBackup_Changed;
			}

			return base.OnDisposeAsync();
		}

		private void ContactBackup_Changed(object? Sender, EventArgs e)
		{
			_ = this.RefreshAsync();
		}

		#endregion

		#region Showing

		/// <summary>
		/// Reads the switch, when the contacts were last backed up, and what the backup holds.
		/// </summary>
		private async Task RefreshAsync()
		{
			try
			{
				bool Enabled = await ContactBackup.IsEnabledAsync();
				DateTime Last = await ContactBackup.GetLastBackupAsync();
				bool Online = AccountConnection.IsConnected;
				ContactBackupInfo? Info = null;

				if (Online)
				{
					try
					{
						Info = await ContactBackup.GetInfoAsync();
					}
					catch (Exception Ex)
					{
						ServiceRef.LogService.LogException(Ex);
					}
				}

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					this.showing = true;

					try
					{
						this.IsEnabled = Enabled;
					}
					finally
					{
						this.showing = false;
					}

					this.IsOnline = Online;
					this.LastBackupText = Last == DateTime.MinValue ?
						ServiceRef.Localizer[nameof(AppResources.ContactBackupNever)] :
						ServiceRef.Localizer[nameof(AppResources.ContactBackupLast), ChatMessageItem.FormatMoment(Last)];

					this.HasBackup = Info?.Exists ?? false;

					if (Info is null)
						this.BackupText = ServiceRef.Localizer[nameof(AppResources.GroupsOffline)];
					else if (!Info.Exists)
						this.BackupText = ServiceRef.Localizer[nameof(AppResources.ContactBackupNone)];
					else if (Info.Count == 1)
						this.BackupText = ServiceRef.Localizer[nameof(AppResources.ContactBackupFoundOne), MomentOf(Info.Updated)];
					else
						this.BackupText = ServiceRef.Localizer[nameof(AppResources.ContactBackupFoundMany), Info.Count, MomentOf(Info.Updated)];
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private static string MomentOf(DateTime Utc)
		{
			return Utc == DateTime.MinValue ? string.Empty : ChatMessageItem.FormatMoment(Utc);
		}

		#endregion

		#region Actions

		/// <summary>
		/// Turns the backup on or off when the switch is flipped. Turning it on backs up at once.
		/// </summary>
		partial void OnIsEnabledChanged(bool value)
		{
			if (this.showing)
				return;

			_ = this.ChangeEnabledAsync(value);
		}

		private async Task ChangeEnabledAsync(bool Enabled)
		{
			this.IsWorking = true;

			try
			{
				await ContactBackup.SetEnabledAsync(Enabled);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				// The switch stays on; the backup is made when the app connects.
				if (Enabled && !AccountConnection.IsConnected)
					await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactBackupLater)]);
				else
					await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsWorking = false;
			}

			await this.RefreshAsync();
		}

		private bool CanBackupNow()
		{
			return this.IsEnabled && this.IsOnline && !this.IsWorking;
		}

		/// <summary>
		/// Backs up the contacts now.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanBackupNow))]
		private async Task BackupNowAsync()
		{
			this.IsWorking = true;

			try
			{
				int Count = await ContactBackup.BackupAsync();

				await ServiceRef.ToastService.ShowTextAsync(Count == 1 ?
					ServiceRef.Localizer[nameof(AppResources.ContactBackupDoneOne)] :
					ServiceRef.Localizer[nameof(AppResources.ContactBackupDoneMany), Count]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsWorking = false;
			}
		}

		private bool CanUseBackup()
		{
			return this.HasBackup && this.IsOnline && !this.IsWorking;
		}

		/// <summary>
		/// Restores the contacts in the backup, after asking.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanUseBackup))]
		private async Task RestoreAsync()
		{
			if (!await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactBackupRestoreTitle)],
				ServiceRef.Localizer[nameof(AppResources.ContactBackupRestoreQuestion)],
				ServiceRef.Localizer[nameof(AppResources.ContactBackupRestore)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]))
			{
				return;
			}

			this.IsWorking = true;

			try
			{
				int Count = await ContactBackup.RestoreAsync();

				string Message = Count switch
				{
					0 => ServiceRef.Localizer[nameof(AppResources.ContactBackupRestoredNone)],
					1 => ServiceRef.Localizer[nameof(AppResources.ContactBackupRestoredOne)],
					_ => ServiceRef.Localizer[nameof(AppResources.ContactBackupRestoredMany), Count]
				};

				await ServiceRef.ToastService.ShowTextAsync(Message);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsWorking = false;
			}
		}

		/// <summary>
		/// Deletes the backup, after asking. Backing up is turned off.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanUseBackup))]
		private async Task DeleteAsync()
		{
			if (!await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ContactBackupDelete)],
				ServiceRef.Localizer[nameof(AppResources.ContactBackupDeleteQuestion)],
				ServiceRef.Localizer[nameof(AppResources.ChatsDeleteConfirm)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]))
			{
				return;
			}

			this.IsWorking = true;

			try
			{
				await ContactBackup.DeleteAsync();
				await ServiceRef.ToastService.ShowTextAsync(ServiceRef.Localizer[nameof(AppResources.ContactBackupDeleted)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
			finally
			{
				this.IsWorking = false;
			}
		}

		#endregion
	}
}
