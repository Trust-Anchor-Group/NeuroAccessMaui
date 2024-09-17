using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Tag;
using Waher.Networking.XMPP;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class NameEntryViewModel : BaseRegistrationViewModel
	{
		public NameEntryViewModel()
			 : base(RegistrationStep.NameEntry) // Ensure this step is defined
		{
		}

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
		}

		protected override async Task OnDispose()
		{
			await base.OnDispose();
		}

		public override async Task DoAssignProperties()
		{
			await base.DoAssignProperties();
		}


		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private bool isUsingNickname;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private string firstName;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private string lastName;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(CreateAccountCommand))]
		private string nickname;

		public bool CanCreateAccount()
		{
			if (this.IsUsingNickname)
			{
				return !string.IsNullOrWhiteSpace(this.Nickname);
			}
			else
			{
				return !string.IsNullOrWhiteSpace(this.FirstName) && !string.IsNullOrWhiteSpace(this.LastName);
			}
		}


		[RelayCommand(CanExecute = nameof(CanCreateAccount))]
		private void CreateAccount()
		{
			if (this.IsUsingNickname)
			{
				ServiceRef.TagProfile.FriendlyName = this.Nickname;
			}
			else
			{
				ServiceRef.TagProfile.FirstName = this.FirstName;
				ServiceRef.TagProfile.LastName = this.LastName;
			}

			GoToRegistrationStep(RegistrationStep.ValidatePhone);
		}



	}
}

