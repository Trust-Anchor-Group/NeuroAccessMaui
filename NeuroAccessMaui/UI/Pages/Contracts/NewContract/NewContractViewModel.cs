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
			this.IsParametersOk
			&& this.IsRolesOk
			&& this.IsContractOk
			&& this.persistingSelectedRole is not null
			&& this.SelectedContractVisibilityItem is not null;

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
						foreach (ObservableParameter p in this.Contract.Parameters)
						{
							if (this.args.ParameterValues.TryGetValue(p.Parameter.Name, out object? Value))
								p.Value = Value;
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
						foreach (ObservableRole r in this.Contract.Roles)
						{
							if (this.args.ParameterValues.TryGetValue(r.Role.Name, out object? RoleValue))
							{
								if (RoleValue is string LegalID)
									await r.AddPart(LegalID);
							}
						}
					}
					foreach (ObservableParameter p in this.Contract.Parameters)
					{
						if (p.Parameter is BooleanParameter
							|| p.Parameter is StringParameter
							|| p.Parameter is NumericalParameter
							|| p.Parameter is DateParameter
							|| p.Parameter is DateTimeParameter
							|| p.Parameter is TimeParameter
							|| p.Parameter is DurationParameter
							|| p.Parameter is ContractReferenceParameter)
						{
							Console.WriteLine("Adding parameter: +" + p.Parameter.GetType().Name);
							this.EditableParameters.Add(p);
						}
					}
					this.OnPropertyChanged(nameof(this.HasRoles));
					this.OnPropertyChanged(nameof(this.HasParameters));

					HasInitializedParameters.SetResult(true);
				});
				await HasInitializedParameters.Task;
				//await this.ValidateParametersAsync();
				await this.GoToState(NewContractStep.Overview);
				//await GoToOverview();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
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

		/// <summary>
		/// Checks if the contract can be created based on the validity of parameters and roles.
		/// </summary>
		public async Task<bool> CheckCanCreateAsync()
		{
			if (this.Contract is null)
				return false;

			await this.FlushValidationAsync();

			bool ParametersOk = true;
			foreach (ObservableParameter p in this.EditableParameters)
			{
				if (p.Value is null || !p.IsValid)
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
				foreach (ObservableParameter p in this.Contract.Parameters)
					p.Parameter.Populate(Variables);

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

				Task<(ObservableParameter Param, bool IsValid, string ValidationText)>[] ValidationTasks = this.EditableParameters.Select(async p =>
				{
					bool IsValid = false;
					string ValidationText = string.Empty;
					try
					{
						if (p.Value is null)
						{
							IsValid = false;
							ValidationText = string.Empty;
						}
						else
						{
							IsValid = await p.Parameter.IsParameterValid(Variables, ServiceRef.XmppService.ContractsClient).ConfigureAwait(false);
							IsValid = IsValid || p.Parameter.ErrorText == ContractStatus.ClientIdentityInvalid.ToString();
							ValidationText = p.Parameter.ErrorText;
						}
						ServiceRef.LogService.LogDebug($"Parameter '{p.Parameter.Name}' validation result: {IsValid}, Error: {p.Parameter.ErrorReason} - {ValidationText}");
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
						IsValid = true;
					}
					return (Param: p, IsValid, ValidationText);
				}).ToArray();

				(ObservableParameter Param, bool IsValid, string ValidationText)[] Results = await Task.WhenAll(ValidationTasks);

				// Update UI in a batch on the main thread:
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					foreach (var Result in Results)
					{
						Result.Param.IsValid = Result.IsValid;
						Result.Param.ValidationText = Result.ValidationText;
					}
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
				await this.GoToOverview();
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
				await this.GoToOverview();
				return;
			}

			ViewContractNavigationArgs Args = new(CreatedContract, false);
			await ServiceRef.UiService.GoToAsync(nameof(ViewContractPage), Args, BackMethod.Pop3);

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
					case NewContractStep.Overview:
						await base.GoBack();
						break;
					case NewContractStep.Roles:
						this.persistingSelectedRole = this.SelectedRole;
						await this.GoToOverview();
						break;
					default:
						await this.GoToState(NewContractStep.Overview);
						await this.CheckCanCreateAsync();
						break;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
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

		/// <summary>
		/// Navigates to the overview view and performs logic to check if create conditions are met
		/// </summary>
		private async Task GoToOverview()
		{
			await this.GoToState(NewContractStep.Loading);
			await this.CheckCanCreateAsync();
			await this.GoToState(NewContractStep.Overview);
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
			if(e.PropertyName == nameof(ObservableParameter.Value))
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
				StringBuilder url = new();
				//bool first = true;

				url.Append(Constants.UriSchemes.IotSc);
				url.Append(':');
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

				return url.ToString();
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
