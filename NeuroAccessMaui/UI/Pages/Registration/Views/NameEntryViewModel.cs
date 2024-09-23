﻿using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Popups.Info;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class NameEntryViewModel : BaseRegistrationViewModel
	{
		public NameEntryViewModel()
			  : base(RegistrationStep.NameEntry)
		{
			// Set default selection if needed
			this.SelectedNameOption = NameOption.RealName;
		}

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private NameOption selectedNameOption;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private string? firstName;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private string? lastName;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private string? nickname;

		partial void OnSelectedNameOptionChanged(NameOption value)
		{
			// Clear values when switching between options
			this.FirstName = this.LastName = this.Nickname = string.Empty;
		}

		public bool CanCreateAccount()
		{
			switch (this.SelectedNameOption)
			{
				case NameOption.RealName:
					return !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);
				case NameOption.Nickname:
					return !string.IsNullOrWhiteSpace(Nickname);
				case NameOption.Anonymous:
					return true; // No input needed
				default:
					return false;
			}
		}

		[RelayCommand(CanExecute = nameof(CanCreateAccount))]
		private void CreateAccount()
		{
			switch (this.SelectedNameOption)
			{
				case NameOption.RealName:
					ServiceRef.TagProfile.FirstName = this.FirstName;
					ServiceRef.TagProfile.LastName = this.LastName;
					break;
				case NameOption.Nickname:
					ServiceRef.TagProfile.FriendlyName = this.Nickname;
					break;
				case NameOption.Anonymous:
					// Do nothing
					break;
			}

			GoToRegistrationStep(RegistrationStep.ValidatePhone);
		}

		[RelayCommand]
		private async Task ShowDataInfo()
		{
			string title = ServiceRef.Localizer[nameof(AppResources.WhyIsThisDataCollected)];
			string message = ServiceRef.Localizer[nameof(AppResources.WhyIsThisDataCollectedInfo)];
			ShowInfoPopup infoPage = new(title, message);
			await ServiceRef.UiService.PushAsync(infoPage);
		}
	}
}
