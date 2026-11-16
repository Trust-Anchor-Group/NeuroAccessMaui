using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contracts.ChooseSigner;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// The last step of creating a contract, where the user checks what will be created, and the sheets it opens: who signs
	/// a role, and the options the template fills in.
	/// </summary>
	public partial class NewContractViewModel
	{
		/// <summary>
		/// How many values the check step shows.
		/// </summary>
		private const int checkDetailCount = 3;

		/// <summary>
		/// The first sentences of the contract's text that have all their values, as filled in.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasCheckSummary))]
		private string checkSummaryText = string.Empty;

		/// <summary>
		/// The contract's key values, as filled in.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasCheckDetails))]
		private IReadOnlyList<ObservableParameter> checkDetails = [];

		/// <summary>
		/// If the check step has a summary.
		/// </summary>
		public bool HasCheckSummary => this.CheckSummaryText.Length > 0;

		/// <summary>
		/// If the check step has values to show.
		/// </summary>
		public bool HasCheckDetails => this.CheckDetails.Count > 0;

		/// <summary>
		/// If the bar with the main button is shown: on the steps, but not on the whole text or the options.
		/// </summary>
		public bool ShowsWizardBar => this.Steps.Any(Step => Step.Key == this.CurrentState);

		/// <summary>
		/// If the check step is shown, which explains what happens after creating.
		/// </summary>
		public bool IsOnCheckStep => this.CurrentState == nameof(NewContractStep.Check);

		/// <summary>
		/// If the values are filled in on a step of their own, which the check step links back to.
		/// </summary>
		public bool HasFillStep => this.Steps.Any(Step => Step.Key == nameof(NewContractStep.Parameters));

		/// <summary>
		/// If who signs is chosen on a step of its own, which the check step links back to.
		/// </summary>
		public bool HasRolesStep => this.Steps.Any(Step => Step.Key == nameof(NewContractStep.Roles));

		/// <summary>
		/// The template's name, shown above the steps.
		/// </summary>
		public string TemplateName => string.IsNullOrEmpty(this.Contract?.Category)
			? ServiceRef.Localizer[nameof(AppResources.NewContract)]
			: this.Contract.Category;

		/// <summary>
		/// The roles the user signs as, such as "Seller", or empty.
		/// </summary>
		private string MyRoleNames
		{
			get
			{
				string? MyId = ServiceRef.TagProfile.LegalIdentity?.Id;
				if (this.Contract is null || string.IsNullOrEmpty(MyId))
					return string.Empty;

				return string.Join(", ", this.Contract.Roles
					.Where(Role => Role.Parts.Any(Part => Part.LegalId == MyId))
					.Select(Role => Role.Name));
			}
		}

		/// <summary>
		/// Who can see the contract, such as "Creator and parts".
		/// </summary>
		public string VisibilityText => this.SelectedContractVisibilityItem?.Name ?? string.Empty;

		/// <summary>
		/// When signing ends, or that it doesn't.
		/// </summary>
		public string SignByText => this.SignBeforeDate.HasValue
			? (this.SignBeforeDate.Value.Date + (this.SignBeforeTime ?? TimeSpan.Zero)).ToString("g", CultureInfo.CurrentCulture)
			: ServiceRef.Localizer[nameof(AppResources.ContractNoDeadline)];

		/// <summary>
		/// How long the contract lasts once signed, such as "1 year".
		/// </summary>
		public string LastsText => DurationToString.ToString(this.ContractDuration);

		/// <summary>
		/// Checks if every role has the people it needs from the start, such as those the caller set, so there is nothing to
		/// choose.
		/// </summary>
		/// <returns>If the step for choosing who signs can be left out.</returns>
		private bool AreRolesFilledAtStart()
		{
			if (this.Contract is null || this.Contract.Roles.Count == 0)
				return true;

			return this.Contract.Roles.All(Role => Role.Parts.Count > 0 && Role.Parts.Count >= Role.MinCount);
		}

		/// <summary>
		/// Notifies the summary of the options on the check step.
		/// </summary>
		private void OnOptionsChanged()
		{
			this.OnPropertyChanged(nameof(this.VisibilityText));
			this.OnPropertyChanged(nameof(this.SignByText));
			this.OnPropertyChanged(nameof(this.LastsText));
		}

		/// <summary>
		/// Shows the check step: works out the values, then picks the summary and the key values.
		/// </summary>
		private async Task GoToCheckAsync()
		{
			if (this.Contract is null)
				return;

			await this.GoToState(NewContractStep.Loading);
			await this.ValidateParametersAsync();

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				try
				{
					this.CheckSummaryText = ContractDigest.GetSummary(this.Contract.Contract, this.Contract.Parameters);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
					this.CheckSummaryText = string.Empty;
				}

				this.CheckDetails = ContractDigest.GetKeyDetails(this.Contract.Contract, [.. this.EditableParameters], checkDetailCount);
				this.OnPropertyChanged(nameof(this.HasFillStep));
				this.OnPropertyChanged(nameof(this.HasRolesStep));
				this.OnPropertyChanged(nameof(this.TemplateName));
				this.OnPropertyChanged(nameof(this.PrimaryActionText));
				this.OnOptionsChanged();
			});

			await this.GoToState(NewContractStep.Check);
		}

		/// <summary>
		/// Opens the options the template fills in.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private Task ShowOptionsAsync()
		{
			return this.GoToState(NewContractStep.Settings);
		}

		/// <summary>
		/// Closes the options and goes back to the check step, when the server would accept them.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task SaveOptionsAsync()
		{
			if (!this.CheckSettingsValid())
				return;

			this.OnOptionsChanged();
			await this.UpdateCurrentStepValidityAsync();
			await this.GoToState(NewContractStep.Check);
		}

		/// <summary>
		/// Asks who signs a role, and adds them: the user, a contact, someone whose ID code is scanned, or anyone with the
		/// link, which opens the contract to anyone who gets it.
		/// </summary>
		/// <param name="Role">The role.</param>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task ChooseSignerAsync(ObservableRole? Role)
		{
			if (Role is null || this.Contract is null)
				return;

			try
			{
				string? MyId = ServiceRef.TagProfile.LegalIdentity?.Id;
				bool CanChooseMe = !string.IsNullOrEmpty(MyId) && !Role.IsSelectedByMe && !Role.HasReachedMaxCount;

				SignerChoice? Choice = await ChooseSignerViewModel.ShowAsync(Role.Name, CanChooseMe, !this.IsOpenParts);

				switch (Choice)
				{
					case SignerChoice.Me:
						Role.IsSelectedByMe = true;
						break;

					case SignerChoice.Contact:
						this.UseNamedParts();
						await Role.AddPartFromContactsCommand.ExecuteAsync(null);
						break;

					case SignerChoice.QrCode:
						this.UseNamedParts();
						await Role.AddPartFromQrCommand.ExecuteAsync(null);
						break;

					case SignerChoice.AnyoneWithLink:
						this.SelectedPartsMode = this.PartsModeItems.FirstOrDefault(Item => Item.PartsMode == ContractParts.Open)
							?? this.SelectedPartsMode;
						break;
				}

				this.OnPropertyChanged(nameof(this.PrimaryActionText));
				await this.UpdateCurrentStepValidityAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayException(Ex);
			}
		}

		/// <summary>
		/// Lets only the people named in the contract sign it, when the user names someone.
		/// </summary>
		private void UseNamedParts()
		{
			if (this.IsOpenParts)
			{
				this.SelectedPartsMode = this.PartsModeItems.FirstOrDefault(Item => Item.PartsMode == ContractParts.ExplicitlyDefined)
					?? this.SelectedPartsMode;
			}
		}
	}
}
