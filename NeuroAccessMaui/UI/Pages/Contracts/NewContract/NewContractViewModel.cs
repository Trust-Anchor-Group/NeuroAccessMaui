using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Script;
using Waher.Persistence;
using System.Linq;

using Timer = System.Timers.Timer;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	public partial class NewContractViewModel : BaseViewModel, ILinkableView, IDisposable
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NewContractViewModel"/> class.
		/// </summary>
		public NewContractViewModel()
		{
			this.args = ServiceRef.NavigationService.PopLatestArgs<NewContractNavigationArgs>();

			this.SelectedContractVisibilityItem = this.ContractVisibilityItems[0];
		}

		#endregion

		#region Fields

		private readonly NewContractNavigationArgs? args;
		private System.Timers.Timer? debounceValidationTimer;
		private readonly object debounceLock = new();
		private bool suppressParameterValidation;
		private bool hasAttemptedParameterStep;
		private int validationGeneration;
		private TaskCompletionSource<Contract?>? postCreateCompletion;


		#endregion

		#region Properties
		// If roles were preselected via args, the user cannot change their own role selections
		[ObservableProperty]
		private bool areRolesLockedForMe;

		partial void OnAreRolesLockedForMeChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.CanEditRoles));
		}

		[ObservableProperty]
		private ObservableContract? contract;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(BackCommand))]
		private bool canStateChange;

		[ObservableProperty]
		private string currentState = nameof(NewContractStep.Loading);

		partial void OnCurrentStateChanged(string value)
		{
			this.OnPropertyChanged(nameof(this.IsOnParametersStep));
			this.OnPropertyChanged(nameof(this.IsOnRolesStep));
			this.OnPropertyChanged(nameof(this.ShowNoRolesWarning));
			this.OnPropertyChanged(nameof(this.CanGoBack));
			this.OnPropertyChanged(nameof(this.CanAdvanceCurrentStep));
			this.OnPropertyChanged(nameof(this.CurrentStepTitle));
			this.OnPropertyChanged(nameof(this.ProgressText));
		}

		/// <summary>
		/// Gets the ordered steps shown by the contract-creation wizard.
		/// </summary>
		public ObservableCollection<StepDescriptor> Steps { get; } = new();

		[ObservableProperty]
		private StepDescriptor? currentStep;

		[ObservableProperty]
		private bool isCurrentStepValid;

		[ObservableProperty]
		private bool isOnPreviewStep;

		[ObservableProperty]
		private bool isValidatingParameters;

		partial void OnIsValidatingParametersChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.CanAdvanceCurrentStep));
		}

		[ObservableProperty]
		private bool isTransientPreview;

		partial void OnIsTransientPreviewChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.CanGoBack));
			this.OnPropertyChanged(nameof(this.CurrentStepTitle));
			this.OnPropertyChanged(nameof(this.ProgressText));
		}

		private Contract? lastCreatedContract;

		[ObservableProperty]
		private ObservableParameter? firstInvalidParameter;

		[ObservableProperty]
		private string rolesValidationContext = string.Empty;

		/// <summary>
		/// When enabled, bypasses step validations and allows creating with invalid parameters (for testing).
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanCreate))]
		[NotifyCanExecuteChangedFor(nameof(CreateCommand))]
		private bool isValidationDisabled = false;

		public string ProgressText =>
			this.IsTransientPreview && this.CurrentState == nameof(NewContractStep.Preview)
				? ServiceRef.Localizer[nameof(AppResources.ContractWizardReviewHumanReadable)]
				: this.CurrentStep is null
					? string.Empty
					: ServiceRef.Localizer[nameof(AppResources.ContractWizardStepFormat), this.CurrentStep.Index + 1, this.Steps.Count] ?? string.Empty;

		/// <summary>
		/// Gets the title of the step or transient review currently shown.
		/// </summary>
		public string CurrentStepTitle =>
			this.IsTransientPreview && this.CurrentState == nameof(NewContractStep.Preview)
				? ServiceRef.Localizer[nameof(AppResources.ReviewContractTitle)]
				: this.CurrentStep?.Title ?? string.Empty;

		public string PrimaryActionText =>
			(this.CurrentStep is not null && this.Steps.Count > 0 && this.CurrentStep.Index >= this.Steps.Count - 1)
				? (ServiceRef.Localizer[nameof(AppResources.Create)] ?? "Create")
				: (ServiceRef.Localizer[nameof(AppResources.ContractWizardNext)] ?? "Next");

		public bool CanGoBack =>
			(this.CurrentStep?.Index ?? 0) > 0 ||
			(this.IsTransientPreview && this.CurrentState == nameof(NewContractStep.Preview));

		/// <summary>
		/// Gets whether the primary wizard action can currently be invoked.
		/// </summary>
		public bool CanAdvanceCurrentStep =>
			!this.IsValidatingParameters &&
			(!this.IsOnPreviewStep || this.IsContractOk);

		partial void OnCurrentStepChanged(StepDescriptor? oldValue, StepDescriptor? newValue)
		{
			if (oldValue is not null)
				oldValue.IsCurrent = false;
			if (newValue is not null)
			{
				newValue.IsCurrent = true;
				newValue.IsVisited = true;
				this.IsOnPreviewStep = newValue.Key == nameof(NewContractStep.Preview);
				this.OnPropertyChanged(nameof(this.ProgressText));
				this.OnPropertyChanged(nameof(this.CurrentStepTitle));
				this.OnPropertyChanged(nameof(this.PrimaryActionText));
				this.OnPropertyChanged(nameof(this.CanGoBack));
				this.OnPropertyChanged(nameof(this.CanAdvanceCurrentStep));
				this.NavigateStateForStep(newValue);
				_ = this.UpdateCurrentStepValidityAsync();
				this.OnPropertyChanged(nameof(this.IsOnRolesStep));
				this.OnPropertyChanged(nameof(this.ShowNoRolesWarning));
			}
		}

		[ObservableProperty]
		private VerticalStackLayout? humanReadableText;

		/// <summary>
		/// True if the current user has selected at least one role to sign as.
		/// </summary>
		public bool HasSelectedRoles
		{
			get
			{
				string? MyId = ServiceRef.TagProfile.LegalIdentity?.Id;
				if (this.Contract is null || string.IsNullOrEmpty(MyId))
					return false;
				foreach (ObservableRole Role in this.Contract.Roles)
				{
					if (Role.Parts.Any(p => p.LegalId == MyId))
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the roles selected for the current legal identity.
		/// </summary>
		public IEnumerable<ObservableRole> SelectedRoles
		{
			get
			{
				string? MyId = ServiceRef.TagProfile.LegalIdentity?.Id;
				if (this.Contract is null || string.IsNullOrEmpty(MyId))
					return [];

				return this.Contract.Roles.Where(Role => Role.Parts.Any(Part => Part.LegalId == MyId));
			}
		}

		/// <summary>
		/// Gets a concise summary of the roles selected for the current identity.
		/// </summary>
		public string SelectedRolesSummary
		{
			get
			{
				string[] Roles = this.SelectedRoles.Select(Role => Role.Label).ToArray();
				return Roles.Length == 0
					? ServiceRef.Localizer[nameof(AppResources.None)]
					: string.Join(", ", Roles);
			}
		}

		/// <summary>
		/// Gets a localized summary of the number of selected contract parties.
		/// </summary>
		public string ParticipantCountText
		{
			get
			{
				int Count = this.Contract?.Roles.Sum(Role => Role.Parts.Count) ?? 0;
				return Count == 1
					? ServiceRef.Localizer[nameof(AppResources.ContractPartiesSingular)]
					: ServiceRef.Localizer[nameof(AppResources.ContractPartiesPluralFormat), Count];
			}
		}

		/// <summary>
		/// Gets the friendly name of the legal identity used to create and sign the contract.
		/// </summary>
		public string SelectedIdentityName
		{
			get
			{
				string Name = ContactInfo.GetFriendlyName(ServiceRef.TagProfile.LegalIdentity);
				return string.IsNullOrWhiteSpace(Name)
					? ServiceRef.Localizer[nameof(AppResources.NotAvailable)]
					: Name;
			}
		}

		/// <summary>
		/// Gets the localized state of the selected legal identity.
		/// </summary>
		public string SelectedIdentityStatus =>
			ServiceRef.TagProfile.LegalIdentity is null
				? ServiceRef.Localizer[nameof(AppResources.NotAvailable)]
				: ServiceRef.TagProfile.LegalIdentity.State.ToDisplayText();

		/// <summary>
		/// Gets the semantic tone used for the selected legal identity state.
		/// </summary>
		public StatusPillTone SelectedIdentityTone =>
			ServiceRef.TagProfile.LegalIdentity?.State switch
			{
				IdentityState.Approved => StatusPillTone.Success,
				IdentityState.Created => StatusPillTone.Warning,
				IdentityState.Compromised or IdentityState.Rejected => StatusPillTone.Danger,
				_ => StatusPillTone.Neutral
			};

		/// <summary>
		/// Gets whether the selected legal identity is eligible to create and sign the contract.
		/// </summary>
		public bool IsSelectedIdentityEligible => ServiceRef.TagProfile.LegalIdentity?.IsApproved() == true;

		/// <summary>
		/// Gets whether role and party selections can be edited for the current identity.
		/// </summary>
		public bool CanEditRoles => this.IsSelectedIdentityEligible && !this.AreRolesLockedForMe;

		/// <summary>
		/// Gets the localized name of the selected visibility option.
		/// </summary>
		public string SelectedVisibilityName =>
			this.SelectedContractVisibilityItem?.Name ?? ServiceRef.Localizer[nameof(AppResources.NotAvailable)];

		/// <summary>
		/// Gets whether the parameter step is currently visible.
		/// </summary>
		public bool IsOnParametersStep => this.CurrentState == nameof(NewContractStep.Parameters);

		/// <summary>
		/// Gets whether the roles and parties step is currently visible.
		/// </summary>
		public bool IsOnRolesStep => this.CurrentState == nameof(NewContractStep.Roles);

		/// <summary>
		/// Gets whether the roles step should show a validation warning.
		/// </summary>
		public bool ShowNoRolesWarning => this.IsOnRolesStep && !this.IsRolesOk;

		/// <summary>
		/// Gets the localized validation guidance for the roles step.
		/// </summary>
		public string RolesValidationMessage =>
			this.IsSelectedIdentityEligible
				? ServiceRef.Localizer[nameof(AppResources.ContractRoleMustBeSelected)]
				: ServiceRef.Localizer[nameof(AppResources.ContractCreationApprovedIdentityRequired)];

		/// <summary>
		/// Gets or sets the template-defined parameters that the user can edit.
		/// </summary>
		public ObservableCollection<ObservableParameter> EditableParameters { get; set; } = [];

		/// <summary>
		/// The state object containing all views. Is set by the view.
		/// </summary>
		public BindableObject? StateObject { get; set; }

		/// <summary>
		/// A list of valid visibility items to choose from for this contract.
		/// </summary>
		public ObservableCollection<ContractVisibilityModel> ContractVisibilityItems { get; } =
		  [
				new ContractVisibilityModel(ContractVisibility.CreatorAndParts, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_CreatorAndParts)]),
				new ContractVisibilityModel(ContractVisibility.DomainAndParts, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_DomainAndParts)]),
				new ContractVisibilityModel(ContractVisibility.Public, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_Public)]),
				new ContractVisibilityModel(ContractVisibility.PublicSearchable, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_PublicSearchable)])
		  ];

		/// <summary>
		/// The selected contract visibility item.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanCreate))]
		private ContractVisibilityModel? selectedContractVisibilityItem;


		/// <summary>
		/// If the parameters are valid.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanCreate))]
		[NotifyCanExecuteChangedFor(nameof(CreateCommand))]
		private bool isParametersOk;

		/// <summary>
		/// If the roles are valid.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanCreate))]
		[NotifyCanExecuteChangedFor(nameof(CreateCommand))]
		private bool isRolesOk;

		/// <summary>
		/// If the user has reviewed the contract and sees it as valid.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanCreate))]
		[NotifyCanExecuteChangedFor(nameof(CreateCommand))]
		private bool isContractOk;

		/// <summary>
		/// If Contract can be created
		/// </summary>

		public bool CanCreate =>
			(this.IsParametersOk && this.IsRolesOk && this.IsContractOk
			&& this.SelectedContractVisibilityItem is not null
			&& this.IsSelectedIdentityEligible)
			|| this.IsValidationDisabled;

		partial void OnIsContractOkChanged(bool value)
		{
			if (this.CurrentStep?.Key == nameof(NewContractStep.Preview))
				_ = this.UpdateCurrentStepValidityAsync();

			this.OnPropertyChanged(nameof(this.CanAdvanceCurrentStep));
		}

		partial void OnIsRolesOkChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.ShowNoRolesWarning));
		}

		#endregion

		#region Methods
		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{

			await base.OnInitializeAsync();

			if (this.args is null || this.args?.Template is null)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Error)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoBack();
				return;
			}

			try
			{
				this.Contract = await ObservableContract.CreateAsync(this.args.Template);
				if (this.args.SetVisibility)
				{
					this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(Item =>
						Item.Visibility == this.Contract.Visibility) ?? this.SelectedContractVisibilityItem;
				}

				this.Contract.ParameterChanged += this.Parameter_PropertyChanged;
				this.Contract.PartChanged += this.Contract_PartChanged;


				TaskCompletionSource<bool> HasInitializedParameters = new();

				MainThread.BeginInvokeOnMainThread(async () =>
				{
					if (this.args.ParameterValues is not null)
					{
						// Set the parameter values
						foreach (ObservableParameter Parameter in this.Contract.Parameters)
						{
							if (this.args.ParameterValues.TryGetValue(Parameter.Parameter.Name, out object? Value))
								Parameter.Value = Value;
						}

						lock (this.debounceLock)
						{
							if (this.debounceValidationTimer is not null)
							{
								this.debounceValidationTimer.Stop();
								this.debounceValidationTimer.Dispose();
								this.debounceValidationTimer = null;
							}
						}
						// Set Role values
						foreach (ObservableRole RoleItem in this.Contract.Roles)
						{
							if (this.args.ParameterValues.TryGetValue(RoleItem.Role.Name, out object? RoleValue))
							{
								if (RoleValue is string LegalID)
									await RoleItem.AddPart(LegalID, PresetFromArgs: true);
							}
						}
						// If my own ID was preselected for any role, lock role selection
						string? MyIdInit = ServiceRef.TagProfile.LegalIdentity?.Id;
						if (!string.IsNullOrEmpty(MyIdInit))
						{
							this.AreRolesLockedForMe = this.Contract.Roles.Any(r => r.Parts.Any(p => p.LegalId == MyIdInit));
						}
					}
					foreach (ObservableParameter Parameter in this.Contract.Parameters)
					{
						if (Parameter.Parameter is BooleanParameter
							|| Parameter.Parameter is StringParameter
							|| Parameter.Parameter is NumericalParameter
							|| Parameter.Parameter is DateParameter
							|| Parameter.Parameter is DateTimeParameter
							|| Parameter.Parameter is TimeParameter
							|| Parameter.Parameter is DurationParameter
							|| Parameter.Parameter is ContractReferenceParameter
							|| Parameter.Parameter is GeoParameter)
						{
							this.EditableParameters.Add(Parameter);
						}
					}

					HasInitializedParameters.SetResult(true);
				});
				await HasInitializedParameters.Task;
				// One-time validation after presets, no debounce
				await this.ValidateParametersAsync();
				this.InitializeSteps();

				// Multi-select: do not auto-select any role. Keep user in control.

				await this.GoToState(NewContractStep.Intro);
				this.CurrentStep = this.Steps.FirstOrDefault(Step => Step.Key == nameof(NewContractStep.Intro));
			}
			catch (Exception Ex4)
			{
				ServiceRef.LogService.LogException(Ex4);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Error)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoBack();
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			if (this.Contract is not null)
			{
				this.Contract.ParameterChanged -= this.Parameter_PropertyChanged;
				this.Contract.PartChanged -= this.Contract_PartChanged;
			}
			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Navigates to the specified state.
		/// Can only navigate when <see cref="CanStateChange"/> is true.
		/// Otherwise it stalls until it can navigate.
		/// </summary>
		/// <param name="NewStep">The new step to navigate to.</param>
		private async Task GoToState(NewContractStep NewStep)
		{
			if (this.StateObject is null)
				return;

			string NewState = NewStep.ToString();

			if (NewState == this.CurrentState)
				return;

			while (!this.CanStateChange)
				await Task.Delay(100);

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await StateContainer.ChangeStateWithAnimation(this.StateObject, NewState);
			});
		}

		private void InitializeSteps()
		{
			if (this.Steps.Count > 0)
				return;

			string[] Order = [nameof(NewContractStep.Intro), nameof(NewContractStep.Parameters), nameof(NewContractStep.Roles), nameof(NewContractStep.Preview)];
			int I = 0;
			foreach (string Key in Order)
			{
				Func<Task<bool>>? ValidateFunc = null;
				string Title;
				if (Key == nameof(NewContractStep.Intro))
				{
					Title = ServiceRef.Localizer[nameof(AppResources.Overview)];
					ValidateFunc = () => Task.FromResult(true);
				}
				else if (Key == nameof(NewContractStep.Parameters))
				{
					Title = ServiceRef.Localizer[nameof(AppResources.Parameters)];
					ValidateFunc = () =>
					{
						if (this.IsValidationDisabled)
							return Task.FromResult(true);
						bool Ok = true;
						foreach (ObservableParameter Param in this.EditableParameters)
						{
							if (Param.Value is null || !Param.IsValid)
							{
								Ok = false;
								break;
							}
						}
						this.IsParametersOk = Ok;
						return Task.FromResult(Ok);
					};
				}
				else if (Key == nameof(NewContractStep.Roles))
				{
					Title = ServiceRef.Localizer[nameof(AppResources.PeopleAndSignatures)];
					ValidateFunc = () => Task.FromResult(this.IsValidationDisabled || this.CheckRolesValid());
				}
				else
				{
					Title = ServiceRef.Localizer[nameof(AppResources.ReviewContractTitle)];
					ValidateFunc = () => Task.FromResult(this.IsValidationDisabled || this.IsContractOk);
				}

				this.Steps.Add(new StepDescriptor
				{
					Key = Key,
					Title = Title,
					Index = I++,
					ValidateAsync = ValidateFunc
				});
			}
		}

		private bool CheckRolesValid()
		{
			if (this.Contract is null)
				return false;

			if (!this.IsSelectedIdentityEligible)
			{
				this.RolesValidationContext = this.SelectedIdentityName + " — " + this.SelectedIdentityStatus;
				this.IsRolesOk = false;
				return false;
			}

			if (!this.HasSelectedRoles)
			{
				this.RolesValidationContext = string.Empty;
				this.IsRolesOk = false;
				return false;
			}

			ObservableRole? IncompleteRole = this.Contract.Roles.FirstOrDefault(Role => Role.Parts.Count < Role.MinCount);
			if (IncompleteRole is not null)
			{
				this.RolesValidationContext = IncompleteRole.Label;
				this.IsRolesOk = false;
				return false;
			}

			this.RolesValidationContext = string.Empty;
			this.IsRolesOk = true;
			return true;
		}

		private async Task UpdateCurrentStepValidityAsync()
		{
			if (this.CurrentStep?.ValidateAsync is null)
			{
				this.IsCurrentStepValid = true;
				return;
			}
			if (this.IsValidationDisabled)
			{
				this.IsCurrentStepValid = true;
				this.CurrentStep.IsComplete = true;
				return;
			}
			bool Ok = false;
			try
			{
				Ok = await this.CurrentStep.ValidateAsync();
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			this.IsCurrentStepValid = Ok;
			this.CurrentStep.IsComplete = Ok;
		}

		partial void OnIsValidationDisabledChanged(bool value)
		{
			_ = this.UpdateCurrentStepValidityAsync();
		}

		private async void NavigateStateForStep(StepDescriptor Step)
		{
			try
			{
				NewContractStep Target = (NewContractStep)Enum.Parse(typeof(NewContractStep), Step.Key);
				if (Target != NewContractStep.Loading && this.CurrentState != Step.Key)
				{
					if (Target == NewContractStep.Preview)
					{
						// Ensure preview is generated when entering preview step
						await this.GoToPreview();
					}
					else if (Target == NewContractStep.Parameters)
					{
						// Suppress validation noise caused by initial UI bindings when entering the Parameters step
						lock (this.debounceLock)
						{
							if (this.debounceValidationTimer is not null)
							{
								this.debounceValidationTimer.Stop();
								this.debounceValidationTimer.Dispose();
								this.debounceValidationTimer = null;
							}
						}
						this.suppressParameterValidation = true;

						await this.GoToState(Target);

						// Clear suppression on the next UI tick after controls have bound
						MainThread.BeginInvokeOnMainThread(async () =>
						{
							await Task.Delay(50);
							this.suppressParameterValidation = false;
						});
					}
					else if (Target == NewContractStep.Roles)
					{
						// Auto-select the only available role if exactly one can be chosen
						if (this.Contract is not null && !this.HasSelectedRoles)
						{
							string? MyId = ServiceRef.TagProfile.LegalIdentity?.Id;
							if (!string.IsNullOrEmpty(MyId))
							{
								List<ObservableRole> Joinable = this.Contract.Roles
									.Where(r => r.Parts.Count < r.MaxCount && !r.Parts.Any(p => p.LegalId == MyId))
									.ToList();
								if (Joinable.Count == 1)
								{
									try
									{
										await Joinable[0].AddPart(MyId, false);
									}
									catch (Exception Ex)
									{
										ServiceRef.LogService.LogException(Ex);
									}
								}
							}
						}

						await this.GoToState(Target);
					}
					else
					{
						await this.GoToState(Target);
					}
				}
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task GoNextStepAsync()
		{
			if (this.CurrentStep is null)
				return;

			if (this.CurrentStep.Key == nameof(NewContractStep.Parameters))
			{
				this.hasAttemptedParameterStep = true;
				await this.FlushValidationAsync();
			}

			bool IsValid = this.IsValidationDisabled ||
				this.CurrentStep.ValidateAsync is null ||
				await this.CurrentStep.ValidateAsync();

			this.IsCurrentStepValid = IsValid;
			this.CurrentStep.IsComplete = IsValid;
			if (!IsValid)
			{
				if (this.CurrentStep.Key == nameof(NewContractStep.Parameters))
					this.RequestFirstInvalidParameterFocus();

				return;
			}

			int NextIndex = this.CurrentStep.Index + 1;
			if (NextIndex < this.Steps.Count)
			{
				this.CurrentStep = this.Steps[NextIndex];
			}
			else if (this.CurrentStep.Key == nameof(NewContractStep.Preview))
			{
				await this.CreateAsync();
			}
		}

		private void RequestFirstInvalidParameterFocus()
		{
			ObservableParameter? InvalidParameter = this.EditableParameters.FirstOrDefault(Parameter =>
				Parameter.Value is null || !Parameter.IsValid);

			this.FirstInvalidParameter = null;
			this.FirstInvalidParameter = InvalidParameter;
		}

		[RelayCommand]
		private void GoPreviousStep()
		{
			if (this.CurrentStep is null)
				return;
			// Special case: transient preview -> always return to Intro regardless of CurrentStep pointer
			if (this.IsTransientPreview && this.CurrentState == nameof(NewContractStep.Preview))
			{
				this.IsTransientPreview = false;
				// Don't advance step progression; just go back to Intro state
				_ = this.GoToState(NewContractStep.Intro);
				return;
			}

			int PrevIndex = this.CurrentStep.Index - 1;
			if (PrevIndex >= 0)
				this.CurrentStep = this.Steps[PrevIndex];
		}

		[RelayCommand]
		private void GoToStepDescriptor(StepDescriptor? Step)
		{
			if (Step is null)
				return;

			StepDescriptor? FirstIncompleteStep = this.Steps.FirstOrDefault(Item => !Item.IsComplete);
			int LastReachableIndex = FirstIncompleteStep?.Index ?? this.Steps.Count - 1;
			int CurrentIndex = this.CurrentStep?.Index ?? 0;
			if (Step.Index <= CurrentIndex || Step.Index <= LastReachableIndex)
				this.CurrentStep = Step;
		}

		/// <summary>
		/// Checks if the contract can be created based on the validity of parameters and roles.
		/// </summary>
		public async Task<bool> CheckCanCreateAsync()
		{
			if (this.Contract is null)
				return false;

			this.hasAttemptedParameterStep = true;
			await this.FlushValidationAsync();

			bool ParametersOk = true;
			foreach (ObservableParameter ParamItem in this.EditableParameters)
			{
				if (ParamItem.Value is null || !ParamItem.IsValid)
				{
					ParametersOk = false;
					break;
				}
			}

			bool RolesOk = this.CheckRolesValid();

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.IsParametersOk = ParametersOk;
				this.IsRolesOk = RolesOk;
			});
			return this.CanCreate;
		}

		/// <summary>
		/// Validates the parameters of the contract and updates their error states.
		/// </summary>
		private async Task ValidateParametersAsync()
		{
			if (this.Contract is null)
				return;

			int ValidationGeneration = Interlocked.Increment(ref this.validationGeneration);
			try
			{
				Variables Variables = [];

				Variables["Duration"] = this.Contract.Contract.Duration;

				DateTime? FirstSignature = this.Contract.Contract.FirstSignatureAt;
				if (FirstSignature.HasValue)
				{
					Variables["Now"] = FirstSignature.Value.ToLocalTime();
					Variables["NowUtc"] = FirstSignature.Value.ToUniversalTime();
				}

				foreach (ObservableParameter ParamLoop in this.Contract.Parameters)
					ParamLoop.Parameter.Populate(Variables);

				ContractsClient? ContractsClient = null;
				try
				{
					ContractsClient = ServiceRef.XmppService.ContractsClient;
				}
				catch (Exception)
				{
					// Ignore, client might not be available currently
				}

				Task<(ObservableParameter Param, bool IsValid, string ValidationText)>[] ValidationTasks = this.Contract.Parameters.Select(async ParamToValidate =>
				{
					bool IsValid = false;
					string ValidationText = string.Empty;
					try
					{
						if (await ParamToValidate.Parameter.IsParameterValid(Variables, ContractsClient).ConfigureAwait(false))
						{
							IsValid = true;
							ValidationText = string.Empty;
						}
						else if(ParamToValidate.Value is null)
						{
							ValidationText = this.hasAttemptedParameterStep
								? ServiceRef.Localizer[nameof(AppResources.ContractFieldNeedsAttention)]
								: string.Empty;
						}
						else
						{
							IsValid = ParamToValidate.Parameter.ErrorText == ContractStatus.ClientIdentityInvalid.ToString();
							ValidationText = IsValid
								? string.Empty
								: ParamToValidate.Parameter.ErrorText;

							if (!IsValid && string.IsNullOrWhiteSpace(ValidationText) && this.hasAttemptedParameterStep)
								ValidationText = ServiceRef.Localizer[nameof(AppResources.ContractFieldNeedsAttention)];
						}

					}
					catch (Exception Ex2)
					{
						ServiceRef.LogService.LogException(Ex2);
						IsValid = false;
						ValidationText = this.hasAttemptedParameterStep
							? ServiceRef.Localizer[nameof(AppResources.ContractFieldNeedsAttention)]
							: string.Empty;
					}
					return (Param: ParamToValidate, IsValid, ValidationText);
				}).ToArray();

				(ObservableParameter Param, bool IsValid, string ValidationText)[] Results = await Task.WhenAll(ValidationTasks);
				if (ValidationGeneration != Volatile.Read(ref this.validationGeneration))
					return;

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach ((ObservableParameter Param, bool IsValid, string ValidationText) Result in Results)
					{
						Result.Param.IsValid = Result.IsValid;
						Result.Param.ValidationText = Result.ValidationText;
					}
					this.IsParametersOk = this.EditableParameters.All(Parameter => Parameter.Value is not null && Parameter.IsValid);
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private void DebounceValidateParameters()
		{
			lock (this.debounceLock)
			{
				if (this.debounceValidationTimer is not null)
				{
					this.debounceValidationTimer.Stop();
					this.debounceValidationTimer.Dispose();
					this.debounceValidationTimer = null;
				}

				this.debounceValidationTimer = new Timer(700);
				this.debounceValidationTimer.Elapsed += async (s, e) =>
				{
					lock (this.debounceLock)
					{
						this.debounceValidationTimer?.Stop();
						this.debounceValidationTimer?.Dispose();
						this.debounceValidationTimer = null;
					}

					Task ValidationTask = MainThread.InvokeOnMainThreadAsync(async () =>
					{
						await this.ValidateParametersAsync();
					});

					await ValidationTask;
					await MainThread.InvokeOnMainThreadAsync(async () =>
					{
						await this.UpdateCurrentStepValidityAsync();
					});
				};
				this.debounceValidationTimer.AutoReset = false;
				this.debounceValidationTimer.Start();
			}
		}
		private async Task FlushValidationAsync()
		{
			this.IsValidatingParameters = true;
			try
			{
				lock (this.debounceLock)
				{
					if (this.debounceValidationTimer is not null)
					{
						this.debounceValidationTimer.Stop();
						this.debounceValidationTimer.Dispose();
						this.debounceValidationTimer = null;
					}
				}

				Task ValidationTask = MainThread.InvokeOnMainThreadAsync(this.ValidateParametersAsync);
				await ValidationTask;
			}
			finally
			{
				this.IsValidatingParameters = false;
			}
		}



		#endregion

		#region Commands

		[RelayCommand(CanExecute = nameof(CanCreate), AllowConcurrentExecutions = false)]
		private async Task CreateAsync()
		{
			if (this.Contract is null)
				return;

			if (!await this.ValidateCreationAndNavigateAsync())
				return;

			await this.GoToState(NewContractStep.Loading);

			if (!await ServiceRef.AuthenticationService.AuthenticateUserAsync(AuthenticationPurpose.SignContract, true))
			{
				await this.GoToState(NewContractStep.Preview);
				return;
			}

			ContractsClient Client = ServiceRef.XmppService.ContractsClient;

			Contract? CreatedContract = null;
			List<Part> Parts = [];
			foreach (ObservableRole Role in this.Contract.Roles)
			{
				foreach (ObservablePart Part in Role.Parts)
				{
					Parts.Add(Part.Part);
				}
			}

			try
			{
				CreatedContract = await Client.CreateContractAsync(
					this.Contract.Contract.ContractId,
					[.. Parts],
					this.Contract.Contract.Parameters,
					this.SelectedContractVisibilityItem?.Visibility ?? this.Contract.Visibility,
					ContractParts.ExplicitlyDefined,
					this.Contract.Contract.Duration ?? Duration.FromYears(1),
					this.Contract.Contract.ArchiveRequired ?? Duration.FromYears(5),
					this.Contract.Contract.ArchiveOptional ?? Duration.FromYears(5),
					null, null, false);
				this.lastCreatedContract = CreatedContract;
				await this.OpenCreatedContract();
				// Sign for all selected roles (could be none)
				string? MyId = ServiceRef.TagProfile.LegalIdentity?.Id;
				if (!string.IsNullOrEmpty(MyId))
				{
					foreach (ObservableRole Role in this.Contract.Roles)
					{
						if (Role.Parts.Any(p => p.LegalId == MyId))
						{
							CreatedContract = await ServiceRef.XmppService.SignContract(CreatedContract, Role.Name, false);
						}
					}
				}

				foreach (Part Part in Parts)
				{
					if (this.args?.SuppressedProposalLegalIds is not null && Array.IndexOf<CaseInsensitiveString>(this.args.SuppressedProposalLegalIds, Part.LegalId) >= 0)
						continue;

					if (Part.LegalId == ServiceRef.TagProfile.LegalIdentity?.Id)
						continue;

					ContactInfo? Info = await ContactInfo.FindByLegalId(Part.LegalId);
					if (Info is null || string.IsNullOrEmpty(Info.BareJid))
						continue;
					await ServiceRef.XmppService.ContractsClient.AuthorizeAccessToContractAsync(CreatedContract.ContractId, Info.BareJid, true);

					string? Proposal = await ServiceRef.UiService.DisplayPrompt(ServiceRef.Localizer[nameof(AppResources.Proposal)],
						ServiceRef.Localizer[nameof(AppResources.EnterProposal), Info.FriendlyName],
						ServiceRef.Localizer[nameof(AppResources.Send)],
						ServiceRef.Localizer[nameof(AppResources.Cancel)]);

					if (!string.IsNullOrEmpty(Proposal))
						await ServiceRef.XmppService.SendContractProposal(CreatedContract, Part.Role, Info.BareJid, Proposal);
					else
						await ServiceRef.XmppService.SendContractProposal(CreatedContract, Part.Role, Info.BareJid, ServiceRef.Localizer[nameof(AppResources.ProposalDefaultMessage)]);
				}
			}
			catch (Waher.Networking.XMPP.XmppException)
			{
				// Protocol exception text can expose technical details; the localized failure below is retry-safe.
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}

			if (CreatedContract is null)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Error)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoToState(NewContractStep.Preview);
				return;
			}

			// Directly open created contract (no Final step)
			this.lastCreatedContract = CreatedContract;
			// Complete the post-create TCS so ViewContract can update signing UI
			this.postCreateCompletion?.TrySetResult(CreatedContract);
			this.postCreateCompletion = null;

		}

		private async Task<bool> ValidateCreationAndNavigateAsync()
		{
			if (this.IsValidationDisabled)
				return true;

			this.hasAttemptedParameterStep = true;
			await this.FlushValidationAsync();

			ObservableParameter? InvalidParameter = this.EditableParameters.FirstOrDefault(Parameter =>
				Parameter.Value is null || !Parameter.IsValid);
			if (InvalidParameter is not null)
			{
				this.IsParametersOk = false;
				this.CurrentStep = this.Steps.FirstOrDefault(Step => Step.Key == nameof(NewContractStep.Parameters));
				this.RequestFirstInvalidParameterFocus();
				return false;
			}

			this.IsParametersOk = true;
			if (!this.CheckRolesValid())
			{
				this.CurrentStep = this.Steps.FirstOrDefault(Step => Step.Key == nameof(NewContractStep.Roles));
				return false;
			}

			if (this.SelectedContractVisibilityItem is null)
			{
				this.CurrentStep = this.Steps.FirstOrDefault(Step => Step.Key == nameof(NewContractStep.Intro));
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Error)],
					ServiceRef.Localizer[nameof(AppResources.ContractVisibilityMustBeSelected)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return false;
			}

			if (!this.IsContractOk)
			{
				this.CurrentStep = this.Steps.FirstOrDefault(Step => Step.Key == nameof(NewContractStep.Preview));
				return false;
			}

			return this.IsSelectedIdentityEligible;
		}


		/// <summary>
		/// A custom back command, similar to inherited GoBack with Views in mind
		/// </summary>
		/// <returns></returns>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		public async Task Back()
		{
			try
			{
				NewContractStep CurrentStep = (NewContractStep)Enum.Parse(typeof(NewContractStep), this.CurrentState);

				switch (CurrentStep)
				{
					case NewContractStep.Loading:
						await base.GoBack();
						break;
					case NewContractStep.Preview when this.IsTransientPreview:
						this.IsTransientPreview = false;
						await this.GoToState(NewContractStep.Intro);
						break;
					default:
						if (this.CanGoBack)
						{
							this.GoPreviousStep();
						}
						else
						{
							await base.GoBack();
						}
						break;
				}
			}
			catch (Exception Ex3)
			{
				ServiceRef.LogService.LogException(Ex3);
			}
		}

		public override async Task GoBack()
		{
			await this.Back();
		}

		private async Task OpenCreatedContract()
		{
			if (this.lastCreatedContract is null)
				return;
			TaskCompletionSource<Contract?> Tcs = new TaskCompletionSource<Contract?>();
			ViewContractNavigationArgs Args = new(this.lastCreatedContract, false, null, string.Empty, null, Tcs);
			await ServiceRef.NavigationService.GoToAsync(nameof(ViewContractPage), Args, BackMethod.Pop3);
			this.postCreateCompletion = Tcs;
		}

		/// <summary>
		/// Loads the humand readable part of the contract and navigates to the Preview view
		/// </summary>
		/// <returns></returns>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToPreview()
		{
			if (this.Contract is null)
				return;

			await this.GoToState(NewContractStep.Loading);

			foreach (Parameter Param in this.Contract.Contract.Parameters)
			{
				try
				{
					if (string.IsNullOrEmpty(Param.StringValue))
						Param.StringValue = null;
				}
				catch (Exception Ex)
				{
					// Ignore
				}
			}

			await this.ValidateParametersAsync(); // Populate All Parameters

			foreach (Parameter Param in this.Contract.Contract.Parameters)
			{
				try
				{
					if (string.IsNullOrEmpty(Param.StringValue))
						Param.StringValue = null;
				}
				catch (Exception Ex)
				{
					// Ignore
				}
			}
			VerticalStackLayout? HumanReadableText = await this.Contract.Contract.ToMaui(this.Contract.Contract.DeviceLanguage());

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.HumanReadableText = HumanReadableText;
			});

			await this.GoToState(NewContractStep.Preview);
		}

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task ShowPreviewFromIntro()
		{
			this.IsTransientPreview = true;
			await this.GoToPreview();
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Event handler for when a parameter changes.
		/// Validates the parameter.
		/// </summary>
		private void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ObservableParameter.Value))
			{
				if (ReferenceEquals(sender, this.FirstInvalidParameter))
					this.FirstInvalidParameter = null;

				if (this.suppressParameterValidation)
					return;
				this.DebounceValidateParameters();
			}
		}

		partial void OnSelectedContractVisibilityItemChanged(ContractVisibilityModel? oldValue, ContractVisibilityModel? newValue)
		{
			//Fixes losing value when switching view
			if (newValue is null)
			{
				this.SelectedContractVisibilityItem = oldValue;
				return;
			}

			this.OnPropertyChanged(nameof(this.SelectedVisibilityName));
		}

		#endregion

		#region Interface Implementations

		/// <inheritdoc/>
		public bool IsLinkable => true;

		/// <inheritdoc/>
		public bool EncodeAppLinks => true;

		/// <inheritdoc/>
		public string Link
		{
			get
			{
				return Constants.UriSchemes.IotSc + ":";
			}
		}

		private void Contract_PartChanged(object? Sender, NotifyCollectionChangedEventArgs e)
		{
			this.OnPropertyChanged(nameof(this.HasSelectedRoles));
			this.OnPropertyChanged(nameof(this.SelectedRoles));
			this.OnPropertyChanged(nameof(this.SelectedRolesSummary));
			this.OnPropertyChanged(nameof(this.ParticipantCountText));
			this.OnPropertyChanged(nameof(this.ShowNoRolesWarning));
			_ = this.UpdateCurrentStepValidityAsync();
		}

		/// <inheritdoc/>
		public Task<string> Title => ContractModel.GetName(this.Contract?.Contract);

		/// <inheritdoc/>
		public bool HasMedia => false;

		/// <inheritdoc/>
		public byte[]? Media => null;

		/// <inheritdoc/>
		public string? MediaContentType => null;


		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					lock (this.debounceLock)
					{
						this.debounceValidationTimer?.Stop();
						this.debounceValidationTimer?.Dispose();
						this.debounceValidationTimer = null;
					}
				}

				this.disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
