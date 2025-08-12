using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
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
	/// <summary>
	/// The view model to bind to when displaying a new contract view or page.
	/// </summary>
	public partial class NewContractViewModel : BaseViewModel, ILinkableView, IDisposable
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NewContractViewModel"/> class.
		/// </summary>
		public NewContractViewModel()
		{
			this.args = ServiceRef.UiService.PopLatestArgs<NewContractNavigationArgs>();

			this.SelectedContractVisibilityItem = this.ContractVisibilityItems[0];
		}

		#endregion

		#region Fields

		private readonly NewContractNavigationArgs? args;

		private System.Timers.Timer? debounceValidationTimer;
		private readonly object debounceLock = new(); // To prevent race conditions
		private Task? latestValidationTask;


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

		[ObservableProperty]
		private bool isOnFinalState;

		private Contract? lastCreatedContract;

		// Future: detect attachment reference parameters; for now keep off by default.
		[ObservableProperty]
		private bool hasAttachmentRequirements;

		/// <summary>
		/// When enabled, bypasses step validations and allows creating with invalid parameters (for testing).
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanCreate))]
		[NotifyCanExecuteChangedFor(nameof(CreateCommand))]
		private bool isValidationDisabled = true;

		public string ProgressText => this.CurrentStep is null ? string.Empty : $"Step {this.CurrentStep.Index + 1} of {this.Steps.Count}";

		public string PrimaryActionText =>
			(this.CurrentStep is not null && this.Steps.Count > 0 && this.CurrentStep.Index >= this.Steps.Count - 1)
				? (ServiceRef.Localizer[nameof(AppResources.Create)] ?? "Create")
				: "Next";

		public bool CanGoBack => (this.CurrentStep?.Index ?? 0) > 0;

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
			}
		}

		[ObservableProperty]
		private string contractName = string.Empty;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasHumanReadableText))]
		private VerticalStackLayout? humanReadableText;

		public ObservableCollection<ObservableRole> AvailableRoles { get; set; } = [];

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(CanCreate))]
		private ObservableRole? selectedRole;

		private ObservableRole? persistingSelectedRole;

		public ObservableCollection<ObservableParameter> EditableParameters { get; set; } = [];




		[ObservableProperty]
		private bool canAddParts;

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
			(this.IsParametersOk && this.IsRolesOk && this.IsContractOk
			&& this.persistingSelectedRole is not null
			&& this.SelectedContractVisibilityItem is not null)
			|| this.IsValidationDisabled;

		partial void OnIsContractOkChanged(bool value)
		{
			if (this.CurrentStep?.Key == nameof(NewContractStep.Preview))
				_ = this.UpdateCurrentStepValidityAsync();
		}

		#endregion

		#region Methods
		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{

			await base.OnInitialize();

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
				this.Contract.ParameterChanged += this.Parameter_PropertyChanged;


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
									await RoleItem.AddPart(LegalID);
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
							|| Parameter.Parameter is ContractReferenceParameter)
						{
							Console.WriteLine("Adding parameter: +" + Parameter.Parameter.GetType().Name);
							this.EditableParameters.Add(Parameter);
						}
					}
					this.OnPropertyChanged(nameof(this.HasRoles));
					this.OnPropertyChanged(nameof(this.HasParameters));

					HasInitializedParameters.SetResult(true);
				});
				await HasInitializedParameters.Task;
				//await this.ValidateParametersAsync();
				this.InitializeSteps();

				// Detect attachment-related parameters (placeholder logic: any string parameter whose Label/Description contains 'attachment')
				bool HasAttach = false;
				foreach (ObservableParameter P in this.Contract.Parameters)
				{
					string Label = P.Label?.ToLowerInvariant() ?? string.Empty;
					string Desc = P.Description?.ToLowerInvariant() ?? string.Empty;
					if (Label.Contains("attachment") || Desc.Contains("attachment"))
					{
						HasAttach = true;
						break;
					}
				}
				this.HasAttachmentRequirements = HasAttach;
				// Auto-select single role if only one role exists
				if (this.Contract.Roles.Count == 1)
				{
					this.SelectedRole = this.Contract.Roles[0];
				}

				await this.GoToState(NewContractStep.Parameters);
				this.CurrentStep = this.Steps.FirstOrDefault(Step => Step.Key == nameof(NewContractStep.Parameters));
				//await GoToOverview();
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
		protected override async Task OnDispose()
		{
			if (this.Contract is not null)
			{
				this.Contract.ParameterChanged -= this.Parameter_PropertyChanged;
			}
			await base.OnDispose();
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

			string[] Order = [nameof(NewContractStep.Parameters), nameof(NewContractStep.Roles), nameof(NewContractStep.Preview)];
			int I = 0;
			foreach (string Key in Order)
			{
				Func<Task<bool>>? ValidateFunc = null;
				if (Key == nameof(NewContractStep.Parameters))
				{
					ValidateFunc = async () =>
					{
						if (this.IsValidationDisabled)
							return true;
						await this.FlushValidationAsync();
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
						return Ok;
					};
				}
				else if (Key == nameof(NewContractStep.Roles))
				{
					ValidateFunc = () => Task.FromResult(this.IsValidationDisabled || this.CheckRolesValid());
				}
				else if (Key == nameof(NewContractStep.Preview))
				{
					ValidateFunc = () => Task.FromResult(this.IsValidationDisabled || this.IsContractOk);
				}

				this.Steps.Add(new StepDescriptor
				{
					Key = Key,
					Title = Key,
					Index = I++,
					ValidateAsync = ValidateFunc
				});
			}
		}

		private bool CheckRolesValid()
		{
			if (this.Contract is null)
				return false;
			foreach (ObservableRole Role in this.Contract.Roles)
			{
				if (Role.Parts.Count < Role.MinCount)
					return false;
			}
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
				// attempt to create
				_ = this.CreateAsync();
			}
		}

		[RelayCommand]
		private void GoPreviousStep()
		{
			if (this.CurrentStep is null)
				return;
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

			bool ParametersOk = true;
			foreach (ObservableParameter ParamItem in this.EditableParameters)
			{
				if (ParamItem.Value is null || !ParamItem.IsValid)
				{
					ParametersOk = false;
					break;
				}
			}

			bool RolesOk = true;
			foreach (ObservableRole Role in this.Contract.Roles)
			{
				if (Role.Parts.Count < Role.MinCount)
				{
					RolesOk = false;
					break;
				}
			}

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

				Task<(ObservableParameter Param, bool IsValid, string ValidationText)>[] ValidationTasks = this.EditableParameters.Select(async ParamToValidate =>
				{
					bool IsValid = false;
					string ValidationText = string.Empty;
					try
					{
						if (ParamToValidate.Value is null)
						{
							IsValid = false;
							ValidationText = string.Empty;
						}
						else
						{
							IsValid = await ParamToValidate.Parameter.IsParameterValid(Variables, ServiceRef.XmppService.ContractsClient).ConfigureAwait(false);
							IsValid = IsValid || ParamToValidate.Parameter.ErrorText == ContractStatus.ClientIdentityInvalid.ToString();
							ValidationText = ParamToValidate.Parameter.ErrorText;
						}
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
				});
				_ = this.UpdateCurrentStepValidityAsync();
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

				this.debounceValidationTimer = new Timer(1500); // e.g., 1.5 seconds
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

			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.SignContract, true))
			{
				await this.GoToState(NewContractStep.Parameters);
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
				if (this.persistingSelectedRole is not null)
					CreatedContract = await ServiceRef.XmppService.SignContract(CreatedContract, this.persistingSelectedRole!.Name, false);

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
				}
			}
			catch (Waher.Networking.XMPP.XmppException Ex)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					Ex.Message,
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);

				//Todo: display contract errors

			}

			if (CreatedContract is null)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.Error)],
					ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)],
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoToState(NewContractStep.Parameters);
				return;
			}

			// Navigate to final state instead of opening the contract directly
			this.lastCreatedContract = CreatedContract;
			this.IsOnFinalState = true;
			await this.GoToState(NewContractStep.Final);

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
					case NewContractStep.Final:
						await base.GoBack();
						this.IsOnFinalState = false;
						break;
					default:
						this.persistingSelectedRole = this.SelectedRole;
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

		[RelayCommand]
		private async Task OpenCreatedContract()
		{
			if (this.lastCreatedContract is null)
				return;
			ViewContractNavigationArgs Args = new(this.lastCreatedContract, false);
			await ServiceRef.UiService.GoToAsync(nameof(ViewContractPage), Args, BackMethod.Pop3);
		}

		/// <summary>
		/// Navigates to the parameters view
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToParameters()
		{
			await this.GoToState(NewContractStep.Loading);
			await this.GoToState(NewContractStep.Parameters);
		}

		/// <summary>
		/// Navigates to the roles view and restores SelectedRole
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToRoles()
		{
			await this.GoToState(NewContractStep.Loading);
			//		this.FilterAvailableRoles();
			await this.GoToState(NewContractStep.Roles);
			if (this.persistingSelectedRole is not null)
				this.SelectedRole = this.persistingSelectedRole;
			else
			{
				ObservableRole? AvailableRole = null;
				foreach (ObservableRole Role in this.Contract?.Roles ?? [])
				{
					if (Role.Parts.Count < Role.MaxCount)
					{
						if (AvailableRole is null)
							AvailableRole = Role;
						else
							return;
					}
				}
				this.SelectedRole = AvailableRole;
			}

		}

		// Removed GoToOverview; flow starts at Parameters.

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

			VerticalStackLayout? HumanReadableText = await this.Contract.Contract.ToMaui(this.Contract.Contract.DeviceLanguage());

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.HumanReadableText = HumanReadableText;
			});

			await this.GoToState(NewContractStep.Preview);
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
				this.DebounceValidateParameters();
		}

		partial void OnSelectedRoleChanged(ObservableRole? oldValue, ObservableRole? newValue)
		{
			if (newValue is null)
				return;

			ObservableRole? MyRole = null;
			foreach (ObservableRole Role in this.Contract?.Roles ?? [])
			{
				foreach (ObservablePart Part in Role.Parts)
				{
					if (Part.IsMe)
						MyRole = Role;
				}
			}
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				string? MyLegalID = ServiceRef.TagProfile.LegalIdentity?.Id;
				if (string.IsNullOrEmpty(MyLegalID))
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.Error)],
						ServiceRef.Localizer[nameof(AppResources.NoLegalIdSelected)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					return;
				}

				if (newValue.Parts.Count < newValue.MaxCount)
				{
					MyRole?.RemovePart(MyLegalID, false);
					newValue?.AddPart(MyLegalID, false);
				}
				else if (MyRole != newValue)
				{
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.Error)],
						ServiceRef.Localizer[nameof(AppResources.SelectedRoleHasReachedMaximumNumberOfParts)],
						ServiceRef.Localizer[nameof(AppResources.Ok)]);
					this.SelectedRole = MyRole;
				}
			});
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
