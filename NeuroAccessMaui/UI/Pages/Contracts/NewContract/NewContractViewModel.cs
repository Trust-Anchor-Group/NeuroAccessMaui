using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Pages.Contracts.InviteParts;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
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
		private Task? latestValidationTask;
		private bool suppressParameterValidation;
		private TaskCompletionSource<Contract?>? postCreateCompletion;


		#endregion

		#region Properties
		[ObservableProperty]
		private ObservableContract? contract;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(GoToParametersCommand))]
		[NotifyCanExecuteChangedFor(nameof(BackCommand))]
		private bool canStateChange;

		[ObservableProperty]
		private string currentState = nameof(NewContractStep.Loading);

		partial void OnCurrentStateChanged(string value)
		{
			this.OnPropertyChanged(nameof(this.IsOnRolesStep));
			this.OnPropertyChanged(nameof(this.ShowNoRolesWarning));
			this.OnPropertyChanged(nameof(this.CanGoBack));
		}

		// Wizard steps (Phase 1 skeleton)
		public ObservableCollection<StepDescriptor> Steps { get; } = new();

		[ObservableProperty]
		private StepDescriptor? currentStep;

		[ObservableProperty]
		private bool isCurrentStepValid;

		[ObservableProperty]
		private bool isOnPreviewStep;

		[ObservableProperty]
		private bool isValidatingParameters;

		// Removed final state concept

		[ObservableProperty]
		private bool isTransientPreview;

		partial void OnIsTransientPreviewChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.CanGoBack));
		}

		private Contract? lastCreatedContract;

		public string ProgressText => this.CurrentStep is null ? string.Empty : ServiceRef.Localizer[nameof(AppResources.ContractWizardStepFormat), this.CurrentStep.Index + 1, this.Steps.Count] ?? string.Empty;

		public string PrimaryActionText =>
			(this.CurrentStep is not null && this.Steps.Count > 0 && this.CurrentStep.Index >= this.Steps.Count - 1)
				? (ServiceRef.Localizer[nameof(AppResources.Create)] ?? "Create")
				: (ServiceRef.Localizer[nameof(AppResources.ContractWizardNext)] ?? "Next");

		public bool CanGoBack =>
			(this.CurrentStep?.Index ?? 0) > 0 ||
			(this.IsTransientPreview && this.CurrentState == nameof(NewContractStep.Preview));

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
				this.OnPropertyChanged(nameof(this.PrimaryActionText));
				this.OnPropertyChanged(nameof(this.CanGoBack));
				this.NavigateStateForStep(newValue);
				_ = this.UpdateCurrentStepValidityAsync();
				this.OnPropertyChanged(nameof(this.IsOnRolesStep));
				this.OnPropertyChanged(nameof(this.ShowNoRolesWarning));
			}
		}

		[ObservableProperty]
		private string contractName = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasHumanReadableText))]
		private VerticalStackLayout? humanReadableText;

		public ObservableCollection<ObservableRole> AvailableRoles { get; set; } = [];

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

		public bool IsOnRolesStep => this.CurrentState == nameof(NewContractStep.Roles);

		public bool ShowNoRolesWarning => this.IsOnRolesStep && !this.HasSelectedRoles;

		public ObservableCollection<ObservableParameter> EditableParameters { get; set; } = [];

		/// <summary>
		/// If HumanReadableText is not empty
		/// </summary>
		public bool HasHumanReadableText => this.HumanReadableText is not null;

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


		public bool HasRoles => this.Contract is not null && this.Contract.Roles.Count > 0;

		public bool HasParameters => this.Contract is not null && this.EditableParameters.Count > 0;

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
			this.IsParametersOk && this.IsRolesOk && this.IsContractOk && this.IsSettingsOk
			&& this.SelectedContractVisibilityItem is not null;

		partial void OnIsContractOkChanged(bool value)
		{
			if (this.CurrentStep?.Key == nameof(NewContractStep.Preview))
				_ = this.UpdateCurrentStepValidityAsync();
		}

		#endregion

		#region Settings
		/// <summary>
		/// The choices of who can sign the new contract.
		/// </summary>
		public ObservableCollection<ContractPartsModeModel> PartsModeItems { get; } =
		  [
				new ContractPartsModeModel(ContractParts.ExplicitlyDefined, ServiceRef.Localizer[nameof(AppResources.ContractPartsNamed)]),
				new ContractPartsModeModel(ContractParts.Open, ServiceRef.Localizer[nameof(AppResources.ContractPartsOpen)])
		  ];

		/// <summary>
		/// The chosen parts mode.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsNamedParts))]
		[NotifyPropertyChangedFor(nameof(IsOpenParts))]
		private ContractPartsModeModel? selectedPartsMode;

		/// <summary>
		/// If only the people named in the contract can sign it.
		/// </summary>
		public bool IsNamedParts => !this.IsOpenParts;

		/// <summary>
		/// If anyone who can see the contract can sign it.
		/// </summary>
		public bool IsOpenParts => this.SelectedPartsMode?.PartsMode == ContractParts.Open;

		/// <summary>
		/// The local date from which signatures are accepted, or null for no start.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSignAfter))]
		private DateTime? signAfterDate;

		/// <summary>
		/// The local time of day from which signatures are accepted.
		/// </summary>
		[ObservableProperty]
		private TimeSpan? signAfterTime;

		/// <summary>
		/// The local date until which signatures are accepted, or null for no end.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSignBefore))]
		private DateTime? signBeforeDate;

		/// <summary>
		/// The local time of day until which signatures are accepted.
		/// </summary>
		[ObservableProperty]
		private TimeSpan? signBeforeTime;

		/// <summary>
		/// If the signing window has a start.
		/// </summary>
		public bool HasSignAfter => this.SignAfterDate.HasValue;

		/// <summary>
		/// If the signing window has an end.
		/// </summary>
		public bool HasSignBefore => this.SignBeforeDate.HasValue;

		/// <summary>
		/// How long the contract is valid, counted from when it is created.
		/// </summary>
		[ObservableProperty]
		private Duration contractDuration = Duration.FromYears(1);

		/// <summary>
		/// How long the contract must be archived after it ends.
		/// </summary>
		[ObservableProperty]
		private Duration archiveRequiredTime = Duration.FromYears(5);

		/// <summary>
		/// How long the contract may be archived after the required time.
		/// </summary>
		[ObservableProperty]
		private Duration archiveOptionalTime = Duration.FromYears(5);

		/// <summary>
		/// If the settings are values the server accepts.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanCreate))]
		[NotifyCanExecuteChangedFor(nameof(CreateCommand))]
		private bool isSettingsOk = true;

		/// <summary>
		/// Why the settings can't be used, or empty.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasSettingsError))]
		private string settingsErrorText = string.Empty;

		/// <summary>
		/// If there is a settings error to show.
		/// </summary>
		public bool HasSettingsError => !string.IsNullOrEmpty(this.SettingsErrorText);

		/// <summary>
		/// The start of the signing window in UTC, or null.
		/// </summary>
		private DateTime? SignAfterUtc => ToUtc(this.SignAfterDate, this.SignAfterTime);

		/// <summary>
		/// The end of the signing window in UTC, or null.
		/// </summary>
		private DateTime? SignBeforeUtc => ToUtc(this.SignBeforeDate, this.SignBeforeTime);

		partial void OnSelectedPartsModeChanged(ContractPartsModeModel? value)
		{
			this.OnSettingsChanged();
		}

		partial void OnSignAfterDateChanged(DateTime? value)
		{
			if (value.HasValue && !this.SignAfterTime.HasValue)
				this.SignAfterTime = TimeSpan.Zero;
			else if (!value.HasValue)
				this.SignAfterTime = null;

			this.OnSettingsChanged();
		}

		partial void OnSignAfterTimeChanged(TimeSpan? value)
		{
			this.OnSettingsChanged();
		}

		partial void OnSignBeforeDateChanged(DateTime? value)
		{
			if (value.HasValue && !this.SignBeforeTime.HasValue)
				this.SignBeforeTime = new TimeSpan(23, 59, 0);
			else if (!value.HasValue)
				this.SignBeforeTime = null;

			this.OnSettingsChanged();
		}

		partial void OnSignBeforeTimeChanged(TimeSpan? value)
		{
			this.OnSettingsChanged();
		}

		partial void OnContractDurationChanged(Duration value)
		{
			this.OnSettingsChanged();
		}

		partial void OnArchiveRequiredTimeChanged(Duration value)
		{
			this.OnSettingsChanged();
		}

		partial void OnArchiveOptionalTimeChanged(Duration value)
		{
			this.OnSettingsChanged();
		}

		/// <summary>
		/// Removes the start of the signing window.
		/// </summary>
		[RelayCommand]
		private void ClearSignAfter()
		{
			this.SignAfterDate = null;
		}

		/// <summary>
		/// Removes the end of the signing window.
		/// </summary>
		[RelayCommand]
		private void ClearSignBefore()
		{
			this.SignBeforeDate = null;
		}

		/// <summary>
		/// Prefills the settings from the template.
		/// </summary>
		/// <param name="Template">The template the contract is created from.</param>
		private void InitializeSettings(Contract Template)
		{
			ContractParts PartsMode = Template.PartsMode == ContractParts.Open ? ContractParts.Open : ContractParts.ExplicitlyDefined;
			this.SelectedPartsMode = this.PartsModeItems.FirstOrDefault(Item => Item.PartsMode == PartsMode);

			this.ContractDuration = Template.Duration ?? Duration.FromYears(1);
			this.ArchiveRequiredTime = Template.ArchiveRequired ?? Duration.FromYears(5);
			this.ArchiveOptionalTime = Template.ArchiveOptional ?? Duration.FromYears(5);

			if (Template.SignAfter is DateTime SignAfter && SignAfter.ToUniversalTime() > DateTime.UtcNow)
			{
				DateTime Local = SignAfter.ToLocalTime();
				this.SignAfterDate = Local.Date;
				this.SignAfterTime = new TimeSpan(Local.Hour, Local.Minute, 0);
			}

			if (Template.SignBefore is DateTime SignBefore && SignBefore < DateTime.MaxValue)
			{
				DateTime Local = SignBefore.ToLocalTime();
				this.SignBeforeDate = Local.Date;
				this.SignBeforeTime = new TimeSpan(Local.Hour, Local.Minute, 0);
			}

			// Callers that preset the visibility, such as the token flows, set it on the template.
			if (this.args?.SetVisibility == true)
			{
				this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(Item => Item.Visibility == Template.Visibility)
					?? this.SelectedContractVisibilityItem;
			}

			this.CheckSettingsValid();
		}

		/// <summary>
		/// Revalidates the settings, and the step when it is showing.
		/// </summary>
		private void OnSettingsChanged()
		{
			if (this.CurrentStep?.Key == nameof(NewContractStep.Settings))
				_ = this.UpdateCurrentStepValidityAsync();
			else
				this.CheckSettingsValid();
		}

		/// <summary>
		/// Checks the settings against what the server accepts, and sets the error text.
		/// </summary>
		/// <returns>If the settings can be used.</returns>
		private bool CheckSettingsValid()
		{
			string Error = string.Empty;
			DateTime? SignAfter = this.SignAfterUtc;
			DateTime? SignBefore = this.SignBeforeUtc;

			if (!IsLongerThanZero(this.ContractDuration))
				Error = ServiceRef.Localizer[nameof(AppResources.ContractSettingsDurationInvalid)];
			else if (this.ArchiveRequiredTime.Negation || this.ArchiveOptionalTime.Negation)
				Error = ServiceRef.Localizer[nameof(AppResources.ContractSettingsArchiveInvalid)];
			else if (SignAfter.HasValue && SignBefore.HasValue && SignBefore.Value <= SignAfter.Value)
				Error = ServiceRef.Localizer[nameof(AppResources.ContractSettingsWindowOrder)];
			else if (SignBefore.HasValue && SignBefore.Value <= DateTime.UtcNow)
				Error = ServiceRef.Localizer[nameof(AppResources.ContractSettingsWindowPassed)];

			this.SettingsErrorText = Error;
			this.IsSettingsOk = string.IsNullOrEmpty(Error);
			return this.IsSettingsOk;
		}

		/// <summary>
		/// Checks if a duration is longer than zero.
		/// </summary>
		/// <param name="Value">The duration.</param>
		/// <returns>If it is longer than zero.</returns>
		private static bool IsLongerThanZero(Duration Value)
		{
			if (Value.Negation)
				return false;

			DateTime Start = DateTime.UtcNow;
			try
			{
				return Start + Value > Start;
			}
			catch (ArgumentOutOfRangeException)
			{
				return true;
			}
		}

		/// <summary>
		/// Combines a local date and time of day into UTC.
		/// </summary>
		/// <param name="Date">The local date, or null.</param>
		/// <param name="Time">The local time of day, or null for midnight.</param>
		/// <returns>The point in time in UTC, or null without a date.</returns>
		private static DateTime? ToUtc(DateTime? Date, TimeSpan? Time)
		{
			if (!Date.HasValue)
				return null;

			DateTime Local = DateTime.SpecifyKind(Date.Value.Date + (Time ?? TimeSpan.Zero), DateTimeKind.Local);
			return Local.ToUniversalTime();
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
				this.InitializeSettings(this.args.Template);
				this.Contract.ParameterChanged += this.Parameter_PropertyChanged;
				ServiceRef.ContractOrchestratorService.IdentityPetitionAnswered += this.OrchestratorService_IdentityPetitionAnswered;
				this.Contract.PartChanged += (_, __) =>
				{
					this.OnPropertyChanged(nameof(this.HasSelectedRoles));
					this.OnPropertyChanged(nameof(this.ShowNoRolesWarning));
					_ = this.UpdateCurrentStepValidityAsync();
				};


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
					this.OnPropertyChanged(nameof(this.HasRoles));
					this.OnPropertyChanged(nameof(this.HasParameters));

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
				// TODO: Handle error, perhaps change to an error state
			}
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			if (this.Contract is not null)
			{
				this.Contract.ParameterChanged -= this.Parameter_PropertyChanged;
			}
			ServiceRef.ContractOrchestratorService.IdentityPetitionAnswered -= this.OrchestratorService_IdentityPetitionAnswered;
			await base.OnDisposeAsync();
		}

		/// <summary>
		/// Shows the answer to an identity request on the part it was sent for.
		/// </summary>
		private async Task OrchestratorService_IdentityPetitionAnswered(object? Sender, IdentityPetitionAnsweredEventArgs e)
		{
			try
			{
				if (this.Contract is not null)
					await this.Contract.ApplyIdentityAnswerAsync(e);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Navigates to the specified state.
		/// Can only navigate when <see cref="CanStateChange"/> is true.
		/// Otherwise it stalls until it can navigate.
		/// </summary>
		/// <param name="newStep">The new step to navigate to.</param>
		private async Task GoToState(NewContractStep newStep)
		{
			if (this.StateObject is null)
				return;

			string NewState = newStep.ToString();

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

			string[] Order = [nameof(NewContractStep.Intro), nameof(NewContractStep.Parameters), nameof(NewContractStep.Settings),
				nameof(NewContractStep.Roles), nameof(NewContractStep.Preview)];
			int I = 0;
			foreach (string Key in Order)
			{
				Func<Task<bool>>? ValidateFunc = null;
				if (Key == nameof(NewContractStep.Intro))
				{
					// Intro is informational; always valid
					ValidateFunc = () => Task.FromResult(true);
				}
				else if (Key == nameof(NewContractStep.Parameters))
				{
					ValidateFunc = () =>
					{
						bool Ok = this.EditableParameters.All(IsParameterComplete);
						this.IsParametersOk = Ok;
						return Task.FromResult(Ok);
					};
				}
				else if (Key == nameof(NewContractStep.Settings))
				{
					ValidateFunc = () => Task.FromResult(this.CheckSettingsValid());
				}
				else if (Key == nameof(NewContractStep.Roles))
				{
					ValidateFunc = () => Task.FromResult(this.CheckRolesValid());
				}
				else if (Key == nameof(NewContractStep.Preview))
				{
					ValidateFunc = () => Task.FromResult(this.IsContractOk);
				}

				this.Steps.Add(new StepDescriptor
				{
					Key = Key,
					Title = GetStepTitle(Key),
					Index = I++,
					ValidateAsync = ValidateFunc
				});
			}
		}

		/// <summary>
		/// Gets the localized title of a wizard step.
		/// </summary>
		/// <param name="Key">The step's key, the name of a <see cref="NewContractStep"/>.</param>
		/// <returns>The localized title.</returns>
		private static string GetStepTitle(string Key)
		{
			return Key switch
			{
				nameof(NewContractStep.Intro) => ServiceRef.Localizer[nameof(AppResources.ContractWizardStepIntro)],
				nameof(NewContractStep.Parameters) => ServiceRef.Localizer[nameof(AppResources.Parameters)],
				nameof(NewContractStep.Settings) => ServiceRef.Localizer[nameof(AppResources.Settings)],
				nameof(NewContractStep.Roles) => ServiceRef.Localizer[nameof(AppResources.Roles)],
				nameof(NewContractStep.Preview) => ServiceRef.Localizer[nameof(AppResources.ContractWizardStepReview)],
				_ => Key
			};
		}

		/// <summary>
		/// Checks the roles. The user doesn't have to take a role: they can create a contract for others to sign.
		/// </summary>
		/// <returns>If every role has the parts it needs.</returns>
		private bool CheckRolesValid()
		{
			bool Ok = this.AreRoleCountsMet();
			this.IsRolesOk = Ok;
			return Ok;
		}

		/// <summary>
		/// Checks that every role has at least the parts it requires. In an open contract, anyone can sign later.
		/// </summary>
		/// <returns>If every role meets its minimum count.</returns>
		private bool AreRoleCountsMet()
		{
			if (this.Contract is null)
				return false;

			if (this.IsOpenParts)
				return true;

			foreach (ObservableRole Role in this.Contract.Roles)
			{
				if (Role.Parts.Count < Role.MinCount)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if a parameter is ready for creation: it is valid, and has a value unless the template lets it be empty.
		/// </summary>
		/// <param name="Parameter">The parameter to check.</param>
		/// <returns>If the parameter is complete.</returns>
		private static bool IsParameterComplete(ObservableParameter Parameter)
		{
			if (!Parameter.IsValid)
				return false;

			return !(Parameter.IsValueMissing && Parameter.Parameter.ErrorReason == ParameterErrorReason.LacksValue);
		}

		private async Task UpdateCurrentStepValidityAsync()
		{
			if (this.CurrentStep?.ValidateAsync is null)
			{
				this.IsCurrentStepValid = true;
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

		[RelayCommand]
		private void GoNextStep()
		{
			if (this.CurrentStep is null)
				return;
			int NextIndex = this.CurrentStep.Index + 1;
			if (NextIndex < this.Steps.Count)
			{
				this.CurrentStep = this.Steps[NextIndex];
			}
			else if (this.CurrentStep.Key == nameof(NewContractStep.Preview))
			{
				_ = this.CreateAsync();
			}
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
		private void GoToStep(string? stepKey)
		{
			if (string.IsNullOrEmpty(stepKey))
				return;
			StepDescriptor? Target = this.Steps.FirstOrDefault(S => S.Key == stepKey);
			if (Target is null)
				return;

			// Only allow navigating to steps up to the first incomplete one
			int FirstIncomplete = this.Steps.TakeWhile(S => S.IsComplete || S.IsCurrent).Count();
			if (Target.Index <= FirstIncomplete)
				this.CurrentStep = Target;
		}

		[RelayCommand]
		private void GoToStepDescriptor(StepDescriptor? step)
		{
			if (step is null)
				return;
			int FirstIncomplete = this.Steps.TakeWhile(S => S.IsComplete || S.IsCurrent).Count();
			if (step.Index <= FirstIncomplete)
				this.CurrentStep = step;
		}

		/// <summary>
		/// Checks if the contract can be created based on the validity of parameters and roles.
		/// </summary>
		public async Task<bool> CheckCanCreateAsync()
		{
			if (this.Contract is null)
				return false;

			await this.FlushValidationAsync();

			bool ParametersOk = this.EditableParameters.All(IsParameterComplete);
			bool RolesOk = this.AreRoleCountsMet();

			MainThread.BeginInvokeOnMainThread(() =>
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

			try
			{
				// Step 1: Get the variables and prepare the parameters to validate
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

				// Step 2: Prepare to collect validation results
				List<(ObservableParameter Param, bool IsValid, string ValidationText)> ValidationResults = [];

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
						if (await ParamToValidate.Parameter.IsParameterValid(Variables, ServiceRef.XmppService.ContractsClient).ConfigureAwait(false))
						{
							IsValid = true;
							ValidationText = string.Empty;
						}
						else if (ParamToValidate.Value is null)
						{
							// A value nobody has entered yet isn't an error to show; it only keeps the step incomplete.
							IsValid = ParamToValidate.Parameter.ErrorReason == ParameterErrorReason.LacksValue;
							ValidationText = string.Empty;
						}
						else
						{
							IsValid = IsValid || ParamToValidate.Parameter.ErrorText == ContractStatus.ClientIdentityInvalid.ToString();
							ValidationText = ParamToValidate.Parameter.ErrorText;
						}

						// Optional: keep debug noise low when simply entering the Parameters step
						if (!this.suppressParameterValidation)
							ServiceRef.LogService.LogDebug($"Parameter '{ParamToValidate.Parameter.Name}' validation result: {IsValid}, Error: {ParamToValidate.Parameter.ErrorReason} - {ValidationText}");
					}
					catch (Exception Ex2)
					{
						ServiceRef.LogService.LogException(Ex2);
						IsValid = true;
					}
					return (Param: ParamToValidate, IsValid, ValidationText);
				}).ToArray();

				(ObservableParameter Param, bool IsValid, string ValidationText)[] Results = await Task.WhenAll(ValidationTasks);

				// Update UI in a batch on the main thread:
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach ((ObservableParameter Param, bool IsValid, string ValidationText) Result in Results)
					{
						Result.Param.IsValid = Result.IsValid;
						Result.Param.ValidationText = Result.ValidationText;
					}
					// Reflect aggregate state for step gating without triggering another validation run
					this.IsParametersOk = this.EditableParameters.All(IsParameterComplete);
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

				this.debounceValidationTimer = new Timer(700); // e.g., 1.5 seconds
				this.debounceValidationTimer.Elapsed += async (s, e) =>
				{
					lock (this.debounceLock)
					{
						this.debounceValidationTimer?.Stop();
						this.debounceValidationTimer?.Dispose();
						this.debounceValidationTimer = null;
					}

					// Run and store validation task
					Task ValidationTask = MainThread.InvokeOnMainThreadAsync(async () =>
					{
						await this.ValidateParametersAsync();
					});

					// Store the task for possible awaiting later
					this.latestValidationTask = ValidationTask;
					await ValidationTask;
					// After validation finishes, update step validity based on current flags (no re-validation)
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
			Task? ValidationTask = null;
			this.IsValidatingParameters = true;

			lock (this.debounceLock)
			{
				if (this.debounceValidationTimer is not null)
				{
					this.debounceValidationTimer.Stop();
					this.debounceValidationTimer.Dispose();
					this.debounceValidationTimer = null;
					ValidationTask = MainThread.InvokeOnMainThreadAsync(this.ValidateParametersAsync);
					this.latestValidationTask = ValidationTask;
				}
				else if (this.latestValidationTask is not null)
				{
					ValidationTask = this.latestValidationTask;
				}
				else
				{
					ValidationTask = MainThread.InvokeOnMainThreadAsync(this.ValidateParametersAsync);
					this.latestValidationTask = ValidationTask;
				}
			}

			if (ValidationTask is not null)
				await ValidationTask;
			this.IsValidatingParameters = false;
		}



		#endregion

		#region Commands

		[RelayCommand(CanExecute = nameof(CanCreate), AllowConcurrentExecutions = false)]
		private async Task CreateAsync()
		{
			if (this.Contract is null)
				return;

			await this.GoToState(NewContractStep.Loading);

			if (!await ServiceRef.AuthenticationService.AuthenticateUserAsync(AuthenticationPurpose.SignContract, true))
			{
				await this.GoToState(NewContractStep.Preview);
				return;
			}

			List<Part> Parts = [];
			foreach (ObservableRole Role in this.Contract.Roles)
			{
				foreach (ObservablePart Part in Role.Parts)
				{
					Parts.Add(Part.Part);
				}
			}

			// Create through the XMPP service, which saves the contract before anything else can fail.
			Contract CreatedContract;
			try
			{
				CreatedContract = await ServiceRef.XmppService.CreateContract(
					this.Contract.Contract.ContractId,
					[.. Parts],
					this.Contract.Contract.Parameters,
					this.SelectedContractVisibilityItem?.Visibility ?? this.Contract.Visibility,
					this.IsOpenParts ? ContractParts.Open : ContractParts.ExplicitlyDefined,
					this.ContractDuration,
					this.ArchiveRequiredTime,
					this.ArchiveOptionalTime,
					this.SignAfterUtc,
					this.SignBeforeUtc,
					this.Contract.Contract.CanActAsTemplate);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ContractErrorMessages.GetMessage(Ex),
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoToState(NewContractStep.Preview);
				return;
			}

			// Directly open created contract (no Final step)
			this.lastCreatedContract = CreatedContract;

			try
			{
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

				// One sheet invites everyone, with one message. Access is only granted to those the user sends to.
				List<ObservablePart> Unreachable = [];
				IReadOnlyList<InviteRecipient> Recipients = await InvitePartsViewModel.GetRecipientsAsync(this.Contract,
					this.args?.SuppressedProposalLegalIds, false, Unreachable);

				if (Recipients.Count > 0)
					await InvitePartsViewModel.ShowAsync(CreatedContract, Recipients);

				if (Unreachable.Count > 0)
					await OfferIdentityRequestsAsync(Unreachable);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					ContractErrorMessages.GetMessage(Ex, CreatedContract),
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			finally
			{
				// Always complete the post-create TCS, with the latest version of the contract, so ViewContract never waits forever.
				this.lastCreatedContract = CreatedContract;
				this.postCreateCompletion?.TrySetResult(CreatedContract);
				this.postCreateCompletion = null;
			}
		}


		/// <summary>
		/// Tells the user who wasn't invited because their identity isn't visible, so the app can't reach them, and offers to
		/// ask them to show it. Once they do, the contract page offers to invite them.
		/// </summary>
		/// <param name="Parts">The parts who couldn't be reached.</param>
		private static async Task OfferIdentityRequestsAsync(IReadOnlyList<ObservablePart> Parts)
		{
			ObservablePart[] Waiting = [.. Parts
				.Where(Part => Part.CanRequestIdentityToInvite || Part.IsIdentityRequested)
				.DistinctBy(Part => Part.LegalId)];

			if (Waiting.Length == 0)
				return;

			string Names = string.Join(", ", Waiting.Select(Part => string.IsNullOrEmpty(Part.FriendlyName) ? Part.LegalId : Part.FriendlyName));
			ObservablePart[] Requestable = [.. Waiting.Where(Part => Part.CanRequestIdentityToInvite)];

			if (Requestable.Length == 0)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.InviteNotSentTitle)],
					ServiceRef.Localizer[nameof(AppResources.InviteWaitingForIdentity), Names],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				return;
			}

			bool Request = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.InviteNotSentTitle)],
				ServiceRef.Localizer[nameof(AppResources.InviteNeedsIdentity), Names],
				ServiceRef.Localizer[nameof(AppResources.RequestIdentity)],
				ServiceRef.Localizer[nameof(AppResources.Later)]);

			if (!Request)
				return;

			foreach (ObservablePart Part in Requestable)
				await Part.SendIdentityRequestAsync();
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

		[RelayCommand]
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
		/// Navigates to the parameters view
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToParameters()
		{
			await this.GoToState(NewContractStep.Loading);

			// Suppress initial validation during entry
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

			await this.GoToState(NewContractStep.Parameters);

			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await Task.Delay(50);
				this.suppressParameterValidation = false;
			});
		}

		/// <summary>
		/// Navigates to the roles view and restores SelectedRole
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToRoles()
		{
			await this.GoToState(NewContractStep.Loading);

			// If there's exactly one available role (not at MaxCount) for me, select it by default
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

			await this.GoToState(NewContractStep.Roles);

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
				StringBuilder Url = new();
				//bool first = true;

				Url.Append(Constants.UriSchemes.IotSc);
				Url.Append(':');
				//	url.Append(this.template?.ContractId);

				// TODO: Define and initialize 'parametersByName' if necessary
				// foreach (KeyValuePair<CaseInsensitiveString, ParameterInfo> p in this.parametersByName)
				// {
				//     if (first)
				//     {
				//         first = false;
				//         url.Append('&');
				//     }
				//     else
				//     {
				//         url.Append('?');
				//     }

				//     url.Append(p.Key);
				//     url.Append('=');

				//     if (p.Value.Control is Entry entry)
				//         url.Append(entry.Text);
				//     else if (p.Value.Control is CheckBox checkBox)
				//         url.Append(checkBox.IsChecked ? '1' : '0');
				//     else if (p.Value.Control is ExtendedDatePicker picker)
				//     {
				//         if (p.Value.Parameter is DateParameter)
				//             url.Append(XML.Encode(picker.Date, true));
				//         else
				//             url.Append(XML.Encode(picker.Date, false));
				//     }
				//     else
				//     {
				//         url.Append(p.Value.Parameter.ObjectValue?.ToString());
				//     }
				// }

				return Url.ToString();
			}
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
