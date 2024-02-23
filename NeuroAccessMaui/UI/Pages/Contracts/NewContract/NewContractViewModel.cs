using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.UI.Controls.Extended;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contacts.MyContacts;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract;
using NeuroAccessMaui.UI.Pages.Main.Calculator;
using NeuroAccessMaui.UI.Pages.Main.Duration;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Persistence;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// The view model to bind to when displaying a new contract view or page.
	/// </summary>
	public partial class NewContractViewModel : BaseViewModel, ILinkableView
	{
		private static readonly string partSettingsPrefix = typeof(NewContractViewModel).FullName + ".Part_";

		private readonly SortedDictionary<CaseInsensitiveString, ParameterInfo> parametersByName = [];
		private readonly LinkedList<ParameterInfo> parametersInOrder = new();
		private Dictionary<CaseInsensitiveString, object> presetParameterValues = [];
		private CaseInsensitiveString[]? suppressedProposalIds;
		private Contract? template;
		private string? templateId;
		private bool saveStateWhileScanning;
		private Contract? stateTemplateWhileScanning;
		private readonly Dictionary<CaseInsensitiveString, string> partsToAdd;

		/// <summary>
		/// Creates an instance of the <see cref="NewContractViewModel"/> class.
		/// </summary>
		protected internal NewContractViewModel()
		{
			this.ContractVisibilityItems = [];
			this.AvailableRoles = [];
			this.ParameterOptions = [];
			this.partsToAdd = [];
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			ContractVisibility? Visibility = null;

			if (ServiceRef.UiService.TryGetArgs(out NewContractNavigationArgs? Args))
			{
				this.template = Args.Template;
				this.suppressedProposalIds = Args.SuppressedProposalLegalIds;

				if (Args.ParameterValues is not null)
					this.presetParameterValues = Args.ParameterValues;

				if (Args.SetVisibility)
					Visibility = Args.Template?.Visibility;

				if (this.template is not null)
					this.template.FormatParameterDisplay += this.Template_FormatParameterDisplay;
			}
			else if (this.stateTemplateWhileScanning is not null)
			{
				this.template = this.stateTemplateWhileScanning;
				this.stateTemplateWhileScanning = null;

				this.template.FormatParameterDisplay += this.Template_FormatParameterDisplay;
			}

			this.templateId = this.template?.ContractId ?? string.Empty;
			this.IsTemplate = this.template?.CanActAsTemplate ?? false;

			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.CreatorAndParts, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_CreatorAndParts)]));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.DomainAndParts, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_DomainAndParts)]));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.Public, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_Public)]));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.PublicSearchable, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_PublicSearchable)]));

			await this.PopulateTemplateForm(Visibility);
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			if (this.template is not null)
				this.template.FormatParameterDisplay -= this.Template_FormatParameterDisplay;

			this.ContractVisibilityItems.Clear();

			this.ClearTemplate(false);

			if (!this.saveStateWhileScanning)
			{
				await ServiceRef.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)));
				await ServiceRef.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedRole)));
				await ServiceRef.SettingsService.RemoveStateWhereKeyStartsWith(partSettingsPrefix);
			}

			await base.OnDispose();
		}

		private void Template_FormatParameterDisplay(object? Sender, ParameterValueFormattingEventArgs e)
		{
			if (e.Value is Duration Duration)
				e.Value = DurationToString.ToString(Duration);
		}

		/// <inheritdoc/>
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();

			if (this.SelectedContractVisibilityItem is not null)
				await ServiceRef.SettingsService.SaveState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)), this.SelectedContractVisibilityItem.Visibility);
			else
				await ServiceRef.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)));

			if (this.SelectedRole is not null)
				await ServiceRef.SettingsService.SaveState(this.GetSettingsKey(nameof(this.SelectedRole)), this.SelectedRole);
			else
				await ServiceRef.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedRole)));

			if (this.HasPartsToAdd)
			{
				foreach (KeyValuePair<CaseInsensitiveString, string> part in this.GetPartsToAdd())
				{
					string settingsKey = partSettingsPrefix + part.Key;
					await ServiceRef.SettingsService.SaveState(settingsKey, part.Value);
				}
			}
			else
				await ServiceRef.SettingsService.RemoveStateWhereKeyStartsWith(partSettingsPrefix);

			this.partsToAdd.Clear();
		}

		private bool HasPartsToAdd => this.partsToAdd.Count > 0;

		private KeyValuePair<CaseInsensitiveString, string>[] GetPartsToAdd()
		{
			int i = 0;
			int c = this.partsToAdd.Count;
			KeyValuePair<CaseInsensitiveString, string>[] Result = new KeyValuePair<CaseInsensitiveString, string>[c];

			foreach (KeyValuePair<CaseInsensitiveString, string> Part in this.partsToAdd)
				Result[i++] = Part;

			return Result;
		}

		/// <inheritdoc/>
		protected override async Task DoRestoreState()
		{
			if (this.saveStateWhileScanning)
			{
				Enum? e = await ServiceRef.SettingsService.RestoreEnumState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)));
				if (e is not null)
				{
					ContractVisibility cv = (ContractVisibility)e;
					this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(x => x.Visibility == cv);
				}

				string? selectedRole = await ServiceRef.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.SelectedRole)));
				string? matchingRole = this.AvailableRoles.FirstOrDefault(x => x.Equals(selectedRole, StringComparison.Ordinal));

				if (!string.IsNullOrWhiteSpace(matchingRole))
					this.SelectedRole = matchingRole;

				List<(string key, string value)> settings = (await ServiceRef.SettingsService.RestoreStateWhereKeyStartsWith<string>(partSettingsPrefix)).ToList();
				if (settings.Count > 0)
				{
					this.partsToAdd.Clear();
					foreach ((string key, string value) in settings)
					{
						string part = key[partSettingsPrefix.Length..];
						this.partsToAdd[part] = value;
					}
				}

				if (this.HasPartsToAdd)
				{
					foreach (KeyValuePair<CaseInsensitiveString, string> part in this.GetPartsToAdd())
						await this.AddRole(part.Key, part.Value);
				}

				await this.DeleteState();
			}

			this.saveStateWhileScanning = false;
			await base.DoRestoreState();
		}

		private async Task DeleteState()
		{
			await ServiceRef.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedContractVisibilityItem)));
			await ServiceRef.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedRole)));
			await ServiceRef.SettingsService.RemoveStateWhereKeyStartsWith(partSettingsPrefix);
		}

		#region Properties

		/// <summary>
		/// Gets or sets whether the user is proposing the contract at the current time.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ProposeCommand))]
		private bool isProposing;

		/// <summary>
		/// Gets or sets whether this contract is a template or not.
		/// </summary>
		[ObservableProperty]
		private bool isTemplate;

		/// <summary>
		/// A list of valid visibility items to choose from for this contract.
		/// </summary>
		public ObservableCollection<ContractVisibilityModel> ContractVisibilityItems { get; }

		/// <summary>
		/// The selected contract visibility item, if any.
		/// </summary>
		[ObservableProperty]
		private ContractVisibilityModel? selectedContractVisibilityItem;

		/// <inheritdoc/>
		protected override void OnPropertyChanging(System.ComponentModel.PropertyChangingEventArgs e)
		{
			base.OnPropertyChanging(e);

			switch (e.PropertyName)
			{
				case nameof(this.SelectedRole):
					if (this.SelectedRole is not null)
						this.RemoveRole(this.SelectedRole, ServiceRef.TagProfile.LegalIdentity!.Id);
					break;
			}
		}

		/// <inheritdoc/>
		protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			try
			{
				base.OnPropertyChanged(e);

				switch (e.PropertyName)
				{
					case nameof(this.SelectedContractVisibilityItem):
						if (this.template is not null && this.SelectedContractVisibilityItem is not null)
							this.template.Visibility = this.SelectedContractVisibilityItem.Visibility;
						break;

					case nameof(this.SelectedRole):
						if (this.template is not null && !string.IsNullOrWhiteSpace(this.SelectedRole))
							await this.AddRole(this.SelectedRole, ServiceRef.TagProfile.LegalIdentity!.Id);
						break;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Gets or sets whether the visibility items should be shown to the user or not.
		/// </summary>
		[ObservableProperty]
		private bool visibilityIsEnabled;

		/// <summary>
		/// The different roles available to choose from when creating a contract.
		/// </summary>
		public ObservableCollection<string> AvailableRoles { get; }

		/// <summary>
		/// The different parameter options available to choose from when creating a contract.
		/// </summary>
		public ObservableCollection<ContractOption> ParameterOptions { get; }

		/// <summary>
		/// The role selected for the contract, if any.
		/// </summary>
		[ObservableProperty]
		private string? selectedRole;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's roles.
		/// </summary>
		[ObservableProperty]
		private VerticalStackLayout? roles;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parameters.
		/// </summary>
		[ObservableProperty]
		private VerticalStackLayout? parameters;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's human readable text section.
		/// </summary>
		[ObservableProperty]
		private VerticalStackLayout? humanReadableText;

		/// <summary>
		/// Gets or sets whether the contract has roles.
		/// </summary>
		[ObservableProperty]
		private bool hasRoles;

		/// <summary>
		/// Gets or sets whether the contract has parameters.
		/// </summary>
		[ObservableProperty]
		private bool hasParameters;

		/// <summary>
		/// Gets or sets whether the contract has parameters.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ProposeCommand))]
		private bool parametersOk;

		/// <summary>
		/// Gets or sets whether the contract is comprised of human readable text.
		/// </summary>
		[ObservableProperty]
		private bool hasHumanReadableText;

		/// <summary>
		/// Gets or sets whether a user can add parts to a contract.
		/// </summary>
		[ObservableProperty]
		private bool canAddParts;

		#endregion

		private void ClearTemplate(bool propertiesOnly)
		{
			if (!propertiesOnly)
				this.template = null;

			this.SelectedRole = null;
			this.AvailableRoles.Clear();

			this.Roles = null;
			this.HasRoles = false;

			this.Parameters = null;
			this.HasParameters = false;

			this.HumanReadableText = null;
			this.HasHumanReadableText = false;

			this.CanAddParts = false;
			this.VisibilityIsEnabled = false;
		}

		private void RemoveRole(string Role, string LegalId)
		{
			Label? ToRemove = null;
			int State = 0;

			if (this.Roles is null)
				return;

			if (this.template?.Parts is not null)
			{
				List<Part> Parts = [];

				foreach (Part Part in this.template.Parts)
				{
					if (Part.LegalId != LegalId || Part.Role != Role)
						Parts.Add(Part);
				}

				this.template.Parts = [.. Parts];
			}

			if (this.Roles is not null)
			{
				foreach (IView View in this.Roles.Children)
				{
					switch (State)
					{
						case 0:
							if (View is Label Label && Label.StyleId == Role)
								State++;
							break;

						case 1:
							if (View is Button Button)
							{
								if (ToRemove is not null)
								{
									this.Roles.Children.Remove(ToRemove);
									Button.IsEnabled = true;
								}
								return;
							}
							else if (View is Label Label2 && Label2.StyleId == LegalId)
								ToRemove = Label2;
							break;
					}
				}
			}
		}

		private async Task AddRole(string Role, string LegalId)
		{
			Contract? contractToUse = this.template ?? this.stateTemplateWhileScanning;

			if ((contractToUse is null) || (this.Roles is null))
				return;

			Role? RoleObj = null;

			foreach (Role R in contractToUse.Roles)
			{
				if (R.Name == Role)
				{
					RoleObj = R;
					break;
				}
			}

			if (RoleObj is null)
				return;

			if (this.template is not null)
			{
				List<Part> Parts = [];

				if (this.template.Parts is not null)
				{
					foreach (Part Part in this.template.Parts)
					{
						if (Part.LegalId != LegalId || Part.Role != Role)
							Parts.Add(Part);
					}
				}

				Parts.Add(new Part()
				{
					LegalId = LegalId,
					Role = Role
				});

				this.template.Parts = [.. Parts];
			}

			if (this.Roles is not null)
			{
				int NrParts = 0;
				int i = 0;
				bool CurrentRole = false;
				bool LegalIdAdded = false;

				foreach (IView View in this.Roles.Children)
				{
					if (View is Label Label)
					{
						if (Label.StyleId == Role)
						{
							CurrentRole = true;
							NrParts = 0;
						}
						else
						{
							if (Label.StyleId == LegalId)
								LegalIdAdded = true;

							NrParts++;
						}
					}
					else if (View is Button Button)
					{
						if (CurrentRole)
						{
							if (!LegalIdAdded)
							{
								Label = new Label
								{
									Text = await ContactInfo.GetFriendlyName(LegalId),
									StyleId = LegalId,
									HorizontalTextAlignment = TextAlignment.Center,
									FontAttributes = FontAttributes.Bold
								};

								TapGestureRecognizer OpenLegalId = new();
								OpenLegalId.Tapped += this.LegalId_Tapped;

								Label.GestureRecognizers.Add(OpenLegalId);

								this.Roles?.Children.Insert(i, Label);
								NrParts++;

								if (NrParts >= RoleObj.MaxCount)
									Button.IsEnabled = false;
							}

							return;
						}
						else
						{
							CurrentRole = false;
							LegalIdAdded = false;
							NrParts = 0;
						}
					}

					i++;
				}
			}
		}

		private async void LegalId_Tapped(object? Sender, EventArgs e)
		{
			try
			{
				if (Sender is Label label && !string.IsNullOrEmpty(label.StyleId))
				{
					await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(label.StyleId,
						ServiceRef.Localizer[nameof(AppResources.ForInclusionInContract)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async void AddPartButton_Clicked(object? Sender, EventArgs e)
		{
			try
			{
				if (Sender is Button button)
				{
					this.saveStateWhileScanning = true;
					this.stateTemplateWhileScanning = this.template;

					TaskCompletionSource<ContactInfoModel?> Selected = new();
					ContactListNavigationArgs Args = new(ServiceRef.Localizer[nameof(AppResources.AddContactToContract)], Selected)
					{
						CanScanQrCode = true
					};

					await ServiceRef.UiService.GoToAsync(nameof(MyContactsPage), Args, BackMethod.Pop);

					ContactInfoModel? Contact = await Selected.Task;
					if (Contact is null)
						return;

					if (string.IsNullOrEmpty(Contact.LegalId))
						await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ServiceRef.Localizer[nameof(AppResources.SelectedContactCannotBeAdded)]);
					else
					{
						this.partsToAdd[button.StyleId] = Contact.LegalId;
						string settingsKey = partSettingsPrefix + button.StyleId;
						await ServiceRef.SettingsService.SaveState(settingsKey, Contact.LegalId);

						foreach (KeyValuePair<CaseInsensitiveString, string> part in this.GetPartsToAdd())
							await this.AddRole(part.Key, part.Value);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async void Parameter_DateChanged(object? Sender, NullableDateChangedEventArgs e)
		{
			try
			{
				if (Sender is not ExtendedDatePicker Picker || !this.parametersByName.TryGetValue(Picker.StyleId, out ParameterInfo? ParameterInfo))
					return;

				if (ParameterInfo?.Parameter is DateParameter DP)
				{
					if (e.NewDate is not null)
					{
						DP.Value = e.NewDate;
						Picker.BackgroundColor = ControlBgColor.ToColor(true);
					}
					else
					{
						Picker.BackgroundColor = ControlBgColor.ToColor(false);
						return;
					}
				}

				await this.ValidateParameters();
				await this.PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async void Parameter_TextChanged(object? Sender, TextChangedEventArgs e)
		{
			try
			{
				if (Sender is not Entry Entry || !this.parametersByName.TryGetValue(Entry.StyleId, out ParameterInfo? ParameterInfo))
					return;

				if (ParameterInfo.Parameter is StringParameter SP)
				{
					SP.Value = e.NewTextValue;
					Entry.BackgroundColor = ControlBgColor.ToColor(true);
				}
				else if (ParameterInfo.Parameter is NumericalParameter NP)
				{
					if (decimal.TryParse(e.NewTextValue, out decimal d))
					{
						NP.Value = d;
						Entry.BackgroundColor = ControlBgColor.ToColor(true);
					}
					else
					{
						Entry.BackgroundColor = ControlBgColor.ToColor(false);
						return;
					}
				}
				else if (ParameterInfo.Parameter is BooleanParameter BP)
				{
					if (CommonTypes.TryParse(e.NewTextValue, out bool b))
					{
						BP.Value = b;
						Entry.BackgroundColor = ControlBgColor.ToColor(true);
					}
					else
					{
						Entry.BackgroundColor = ControlBgColor.ToColor(false);
						return;
					}
				}
				else if (ParameterInfo.Parameter is DateTimeParameter DTP)
				{
					if (DateTime.TryParse(e.NewTextValue, out DateTime TP))
					{
						DTP.Value = TP;
						Entry.BackgroundColor = ControlBgColor.ToColor(true);
					}
					else
					{
						Entry.BackgroundColor = ControlBgColor.ToColor(false);
						return;
					}
				}
				else if (ParameterInfo.Parameter is TimeParameter TSP)
				{
					if (TimeSpan.TryParse(e.NewTextValue, out TimeSpan TS))
					{
						TSP.Value = TS;
						Entry.BackgroundColor = ControlBgColor.ToColor(true);
					}
					else
					{
						Entry.BackgroundColor = ControlBgColor.ToColor(false);
						return;
					}
				}
				else if (ParameterInfo.Parameter is DurationParameter DP)
				{
					if (ParameterInfo.DurationValue != Duration.Zero)
					{
						DP.Value = ParameterInfo.DurationValue;
						Entry.BackgroundColor = ControlBgColor.ToColor(true);
					}
					else
					{
						Entry.BackgroundColor = ControlBgColor.ToColor(false);
						return;
					}

					/*
					if (Duration.TryParse(e.NewTextValue, out Duration D))
					{
						DP.Value = D;
						Entry.BackgroundColor = ControlBgColor.ToColor(true);
					}
					else
					{
						Entry.BackgroundColor = ControlBgColor.ToColor(false);
						return;
					}
					*/
				}

				await this.ValidateParameters();
				await this.PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async void Parameter_CheckedChanged(object? Sender, CheckedChangedEventArgs e)
		{
			try
			{
				if (Sender is not CheckBox CheckBox || !this.parametersByName.TryGetValue(CheckBox.StyleId, out ParameterInfo? ParameterInfo))
					return;

				if (ParameterInfo.Parameter is BooleanParameter BP)
					BP.Value = e.Value;

				await this.ValidateParameters();
				await this.PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async Task ValidateParameters()
		{
			Variables Variables = [];
			bool Ok = true;

			if (this.template is not null)
				Variables["Duration"] = this.template.Duration;

			foreach (ParameterInfo P in this.parametersInOrder)
				P.Parameter.Populate(Variables);

			foreach (ParameterInfo P in this.parametersInOrder)
			{
				bool Valid;

				try
				{
					// Calculation parameters might only execute on the server. So, if they fail in the client, allow user to propose contract anyway.

					Valid = await P.Parameter.IsParameterValid(Variables, ServiceRef.XmppService.ContractsClient) || P.Control is null;
				}
				catch (Exception)
				{
					Valid = false;
				}

				if (Valid)
				{
					if (P.Control is not null)
						P.Control.BackgroundColor = ControlBgColor.ToColor(true);
				}
				else
				{
					if (P.Control is not null)
						P.Control.BackgroundColor = ControlBgColor.ToColor(false);

					Ok = false;
				}
			}

			this.ParametersOk = Ok;
		}

		[RelayCommand(CanExecute = nameof(CanPropose))]
		private async Task Propose()
		{
			if (this.template is null)
				return;

			List<Part> Parts = [];
			Contract? Created = null;
			string Role = string.Empty;
			int State = 0;
			int Nr = 0;
			int Min = 0;
			int Max = 0;

			this.IsProposing = true;
			try
			{
				if (this.Roles is not null)
				{
					foreach (IView View in this.Roles.Children)
					{
						switch (State)
						{
							case 0:
								if (View is Label Label && !string.IsNullOrEmpty(Label.StyleId))
								{
									Role = Label.StyleId;
									State++;
									Nr = Min = Max = 0;

									foreach (Role R in this.template.Roles)
									{
										if (R.Name == Role)
										{
											Min = R.MinCount;
											Max = R.MaxCount;
											break;
										}
									}
								}
								break;

							case 1:
								if (View is Button)
								{
									if (Nr < Min)
									{
										await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
											ServiceRef.Localizer[nameof(AppResources.TheContractRequiresAtLeast_AddMoreParts), Min, Role]);
										return;
									}

									if (Nr > Min)
									{
										await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
											ServiceRef.Localizer[nameof(AppResources.TheContractRequiresAtMost_RemoveParts), Max, Role]);
										return;
									}

									State--;
									Role = string.Empty;
								}
								else if (View is Label Label2 && !string.IsNullOrEmpty(Role))
								{
									Parts.Add(new Part
									{
										Role = Role,
										LegalId = Label2.StyleId
									});

									Nr++;
								}
								break;
						}
					}
				}

				if (this.Parameters is not null)
				{
					foreach (IView View in this.Parameters.Children)
					{
						if (View is Entry Entry)
						{
							if (Entry.BackgroundColor == ControlBgColor.ToColor(false))
							{
								await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
									ServiceRef.Localizer[nameof(AppResources.YourContractContainsErrors)]);

								Entry.Focus();
								return;
							}
						}
					}
				}

				this.template.PartsMode = ContractParts.Open;

				int i = this.SelectedContractVisibilityItem is null ? -1 : this.ContractVisibilityItems.IndexOf(this.SelectedContractVisibilityItem);
				switch (i)
				{
					case 0:
						this.template.Visibility = ContractVisibility.CreatorAndParts;
						break;

					case 1:
						this.template.Visibility = ContractVisibility.DomainAndParts;
						break;

					case 2:
						this.template.Visibility = ContractVisibility.Public;
						break;

					case 3:
						this.template.Visibility = ContractVisibility.PublicSearchable;
						break;

					default:
						await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
							ServiceRef.Localizer[nameof(AppResources.ContractVisibilityMustBeSelected)]);
						return;
				}

				if (this.SelectedRole is null)
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
						ServiceRef.Localizer[nameof(AppResources.ContractRoleMustBeSelected)]);
					return;
				}

				if (!await App.AuthenticateUser(true))
					return;

				Created = await ServiceRef.XmppService.CreateContract(this.templateId, [.. Parts], this.template.Parameters,
					this.template.Visibility, ContractParts.ExplicitlyDefined, this.template.Duration ?? Duration.FromYears(1),
					this.template.ArchiveRequired ?? Duration.FromYears(1), this.template.ArchiveOptional ?? Duration.FromYears(1),
					null, null, false);

				Created = await ServiceRef.XmppService.SignContract(Created, this.SelectedRole, false);

				if (Created.Parts is not null)
				{
					foreach (Part Part in Created.Parts)
					{
						if (this.suppressedProposalIds is not null && Array.IndexOf<CaseInsensitiveString>(this.suppressedProposalIds, Part.LegalId) >= 0)
							continue;

						ContactInfo Info = await ContactInfo.FindByLegalId(Part.LegalId);
						if (Info is null || string.IsNullOrEmpty(Info.BareJid))
							continue;

						string? Proposal = await ServiceRef.UiService.DisplayPrompt(ServiceRef.Localizer[nameof(AppResources.Proposal)],
							ServiceRef.Localizer[nameof(AppResources.EnterProposal), Info.FriendlyName],
							ServiceRef.Localizer[nameof(AppResources.Send)],
							ServiceRef.Localizer[nameof(AppResources.Cancel)]);

						if (!string.IsNullOrEmpty(Proposal))
							ServiceRef.XmppService.SendContractProposal(Created.ContractId, Part.Role, Info.BareJid, Proposal);
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
			finally
			{
				this.IsProposing = false;

				if (Created is not null)
				{
					ViewContractNavigationArgs Args = new(Created, false);

					// Inherit the back method here. It will vary if created or viewed.
					await ServiceRef.UiService.GoToAsync(nameof(ViewContractPage), Args, BackMethod.Inherited);
				}
			}
		}

		internal static void Populate(VerticalStackLayout Layout, string Xaml)
		{
			VerticalStackLayout xaml = new VerticalStackLayout().LoadFromXaml(Xaml);

			List<IView> children = [.. xaml.Children];

			foreach (IView Element in children)
				Layout.Children.Add(Element);
		}

		internal static void Populate(HorizontalStackLayout Layout, string Xaml)
		{
			VerticalStackLayout xaml = new VerticalStackLayout().LoadFromXaml(Xaml);

			List<IView> children = [.. xaml.Children];

			foreach (IView Element in children)
				Layout.Children.Add(Element);
		}

		private async Task PopulateTemplateForm(ContractVisibility? Visibility)
		{
			this.ClearTemplate(true);

			if (this.template is null)
				return;

			await this.PopulateHumanReadableText();

			this.HasRoles = (this.template.Roles?.Length ?? 0) > 0;
			this.VisibilityIsEnabled = true;

			VerticalStackLayout rolesLayout = [];
			if (this.template.Roles is not null)
			{
				foreach (Role Role in this.template.Roles)
				{
					this.AvailableRoles.Add(Role.Name);

					rolesLayout.Children.Add(new Label
					{
						Text = Role.Name,
						Style = AppStyles.SectionTitleLabel,
						StyleId = Role.Name
					});

					Populate(rolesLayout, await Role.ToMauiXaml(this.template.DeviceLanguage(), this.template));

					if (Role.MinCount > 0)
					{
						Button button = new()
						{
							Text = ServiceRef.Localizer[nameof(AppResources.AddPart)],
							StyleId = Role.Name,
							Margin = AppStyles.SmallBottomMargins
						};
						button.Clicked += this.AddPartButton_Clicked;

						rolesLayout.Children.Add(button);
					}
				}
			}

			this.Roles = rolesLayout;

			VerticalStackLayout ParametersLayout = [];
			if (this.template.Parameters.Length > 0)
			{
				ParametersLayout.Children.Add(new Label
				{
					Text = ServiceRef.Localizer[nameof(AppResources.Parameters)],
					Style = AppStyles.SectionTitleLabel
				});
			}

			this.parametersByName.Clear();
			this.parametersInOrder.Clear();

			foreach (Parameter Parameter in this.template.Parameters)
			{
				if (Parameter is BooleanParameter BP)
				{
					CheckBox CheckBox = new()
					{
						StyleId = Parameter.Name,
						IsChecked = BP.Value.HasValue && BP.Value.Value,
						VerticalOptions = LayoutOptions.Center
					};

					HorizontalStackLayout Layout = [];

					Layout.Children.Add(CheckBox);
					Populate(Layout, await Parameter.ToMauiXaml(this.template.DeviceLanguage(), this.template));
					ParametersLayout.Children.Add(Layout);

					CheckBox.CheckedChanged += this.Parameter_CheckedChanged;

					ParameterInfo PI = new(Parameter, CheckBox);
					this.parametersByName[Parameter.Name] = PI;
					this.parametersInOrder.AddLast(PI);

					if (this.presetParameterValues.TryGetValue(Parameter.Name, out object? PresetValue))
					{
						this.presetParameterValues.Remove(Parameter.Name);

						if (PresetValue is bool b || CommonTypes.TryParse(PresetValue?.ToString() ?? string.Empty, out b))
							CheckBox.IsChecked = b;
					}
				}
				else if (Parameter is CalcParameter || Parameter is RoleParameter)
				{
					ParameterInfo PI = new(Parameter, null);
					this.parametersByName[Parameter.Name] = PI;
					this.parametersInOrder.AddLast(PI);

					this.presetParameterValues.Remove(Parameter.Name);
				}
				else if (Parameter is DateParameter DP)
				{
					Populate(ParametersLayout, await Parameter.ToMauiXaml(this.template.DeviceLanguage(), this.template));

					ExtendedDatePicker Picker = new()
					{
						StyleId = Parameter.Name,
						NullableDate = Parameter.ObjectValue as DateTime?,
						Placeholder = Parameter.Guide
					};

					ParametersLayout.Children.Add(Picker);

					Picker.NullableDateSelected += this.Parameter_DateChanged;

					ParameterInfo PI = new(Parameter, Picker);
					this.parametersByName[Parameter.Name] = PI;
					this.parametersInOrder.AddLast(PI);

					if (this.presetParameterValues.TryGetValue(Parameter.Name, out object? PresetValue))
					{
						this.presetParameterValues.Remove(Parameter.Name);

						if (PresetValue is DateTime TP || XML.TryParse(PresetValue?.ToString() ?? string.Empty, out TP))
							Picker.Date = TP;
					}
				}
				else
				{
					Populate(ParametersLayout, await Parameter.ToMauiXaml(this.template.DeviceLanguage(), this.template));

					Entry Entry = new()
					{
						StyleId = Parameter.Name,
						Text = Parameter.ObjectValue?.ToString(),
						Placeholder = Parameter.Guide
					};

					string? CalculatorButtonType = null;

					if (Parameter is NumericalParameter NumericalParameter)
						CalculatorButtonType = "🖩";  // TODO: SVG icon

					if (Parameter is DurationParameter DurationParameter)
						CalculatorButtonType = "🕑";  // TODO: SVG icon

					if (CalculatorButtonType is not null)
					{
						Grid Grid = new()
						{
							RowDefinitions =
							[
								new RowDefinition()
								{
									Height = GridLength.Auto
								}
							],
							ColumnDefinitions =
							[
								new ColumnDefinition()
								{
									Width = GridLength.Star
								},
								new ColumnDefinition()
								{
									Width = GridLength.Auto
								}
							],
							RowSpacing = 0,
							ColumnSpacing = 8,
							Padding = new Thickness(0),
							Margin = new Thickness(0)
						};

						Grid.SetColumn((IView)Entry, 0);
						Grid.SetRow((IView)Entry, 0);
						Grid.Children.Add(Entry);

						Button CalcButton = new()
						{
							TextColor = AppColors.PrimaryForeground,
							Text = CalculatorButtonType,
							HorizontalOptions = LayoutOptions.End,
							WidthRequest = 40,
							HeightRequest = 40,
							CornerRadius = 8,
							StyleId = Parameter.Name
						};

						CalcButton.Clicked += this.CalcButton_Clicked;

						Grid.SetColumn((IView)CalcButton, 1);
						Grid.SetRow((IView)CalcButton, 0);
						Grid.Children.Add(CalcButton);

						ParametersLayout.Children.Add(Grid);
					}
					else
					{
						ParametersLayout.Children.Add(Entry);
					}

					Entry.TextChanged += this.Parameter_TextChanged;

					ParameterInfo ParameterInfo = new(Parameter, Entry);

					if (Parameter is DurationParameter)
					{
						Entry.IsReadOnly = true;
						Entry.SetBinding(Entry.TextProperty, new Binding("DurationValue", BindingMode.OneWay, new DurationToString()));
						Entry.BindingContext = ParameterInfo;
					}

					this.parametersByName[Parameter.Name] = ParameterInfo;
					this.parametersInOrder.AddLast(ParameterInfo);

					if (this.presetParameterValues.TryGetValue(Parameter.Name, out object? PresetValue))
					{
						this.presetParameterValues.Remove(Parameter.Name);
						Entry.Text = PresetValue.ToString();
					}
				}
			}

			this.Parameters = ParametersLayout;
			this.HasParameters = this.Parameters.Children.Count > 0;

			if (this.template.Parts is not null)
			{
				foreach (Part Part in this.template.Parts)
				{
					if (ServiceRef.TagProfile.LegalIdentity?.Id == Part.LegalId)
						this.SelectedRole = Part.Role;
					else
						await this.AddRole(Part.Role, Part.LegalId);
				}
			}

			if (this.presetParameterValues.TryGetValue("Visibility", out object? Obj) &&
				(Obj is ContractVisibility Visibility2 || Enum.TryParse(Obj?.ToString() ?? string.Empty, out Visibility2)))
			{
				Visibility = Visibility2;
				this.presetParameterValues.Remove("Visibility");
			}

			if (Visibility.HasValue)
				this.SelectedContractVisibilityItem = this.ContractVisibilityItems.FirstOrDefault(x => x.Visibility == Visibility.Value);

			if (this.HasRoles)
			{
				foreach (string Role in this.AvailableRoles)
				{
					if (this.presetParameterValues.TryGetValue(Role, out Obj) && Obj is string LegalId)
					{
						int i = LegalId.IndexOf('@');
						if (i < 0 || !Guid.TryParse(LegalId[..i], out _))
							continue;

						await this.AddRole(Role, LegalId);
						this.presetParameterValues.Remove(Role);
					}
					else if (this.template.Parts is not null)
					{
						foreach (Part Part in this.template.Parts)
						{
							if (Part.Role == Role)
								await this.AddRole(Part.Role, Part.LegalId);
						}
					}
				}
			}

			if (this.presetParameterValues.TryGetValue("Role", out Obj) && Obj is string SelectedRole)
			{
				this.SelectedRole = SelectedRole;
				this.presetParameterValues.Remove("Role");
			}

			await this.ValidateParameters();
		}

		private async Task PopulateHumanReadableText()
		{
			this.HumanReadableText = null;

			VerticalStackLayout humanReadableTextLayout = [];

			if (this.template is not null)
				Populate(humanReadableTextLayout, await this.template.ToMauiXaml(this.template.DeviceLanguage()));

			this.HumanReadableText = humanReadableTextLayout;
			this.HasHumanReadableText = humanReadableTextLayout.Children.Count > 0;
		}

		private bool CanPropose()
		{
			return
				this.template is not null &&
				this.ParametersOk &&
				!this.IsProposing;
		}

		private async void CalcButton_Clicked(object? Sender, EventArgs e)
		{
			try
			{
				if (Sender is not Button CalcButton)
					return;

				if (!this.parametersByName.TryGetValue(CalcButton.StyleId, out ParameterInfo? ParameterInfo))
					return;

				if (ParameterInfo.Control is not Entry Entry)
					return;

				if (CalcButton.Text == "🕑")  // TODO: SVG icon
				{
					DurationNavigationArgs Args = new(Entry);
					await ServiceRef.UiService.GoToAsync(nameof(DurationPage), Args, BackMethod.Pop);
				}
				else
				{
					CalculatorNavigationArgs Args = new(Entry);
					await ServiceRef.UiService.GoToAsync(nameof(CalculatorPage), Args, BackMethod.Pop);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		#region ILinkableView

		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		public bool IsLinkable => true;

		/// <summary>
		/// If App links should be encoded with the link.
		/// </summary>
		public bool EncodeAppLinks => true;

		/// <summary>
		/// Link to the current view
		/// </summary>
		public string Link
		{
			get
			{
				StringBuilder Url = new();
				bool First = true;

				Url.Append(Constants.UriSchemes.IotSc);
				Url.Append(':');
				Url.Append(this.template?.ContractId);

				foreach (KeyValuePair<CaseInsensitiveString, ParameterInfo> P in this.parametersByName)
				{
					if (First)
					{
						First = false;
						Url.Append('&');
					}
					else
						Url.Append('?');

					Url.Append(P.Key);
					Url.Append('=');

					if (P.Value.Control is Entry Entry)
						Url.Append(Entry.Text);
					else if (P.Value.Control is CheckBox CheckBox)
						Url.Append(CheckBox.IsChecked ? '1' : '0');
					else if (P.Value.Control is ExtendedDatePicker Picker)
					{
						if (P.Value.Parameter is DateParameter)
							Url.Append(XML.Encode(Picker.Date, true));
						else
							Url.Append(XML.Encode(Picker.Date, false));
					}
					else
						P.Value.Parameter.ObjectValue?.ToString();
				}

				return Url.ToString();
			}
		}

		/// <summary>
		/// Title of the current view
		/// </summary>
		public Task<string> Title => ContractModel.GetName(this.template);

		/// <summary>
		/// If linkable view has media associated with link.
		/// </summary>
		public bool HasMedia => false;

		/// <summary>
		/// Encoded media, if available.
		/// </summary>
		public byte[]? Media => null;

		/// <summary>
		/// Content-Type of associated media.
		/// </summary>
		public string? MediaContentType => null;

		#endregion

		#region Contract Options

		/// <summary>
		/// Method called (from main thread) when contract options are made available.
		/// </summary>
		/// <param name="Options">Available options, as dictionaries with contract parameters.</param>
		public async Task ShowContractOptions(IDictionary<CaseInsensitiveString, object>[] Options)
		{
			if (Options.Length == 0)
				return;

			if (Options.Length == 1)
				this.ShowSingleContractOptions(Options[0]);
			else
				this.ShowMultipleContractOptions(Options);

			await this.ValidateParameters();
		}

		private void ShowSingleContractOptions(IDictionary<CaseInsensitiveString, object> Option)
		{
			foreach (KeyValuePair<CaseInsensitiveString, object> Parameter in Option)
			{
				string ParameterName = Parameter.Key;

				try
				{
					if (ParameterName.StartsWith("Max(", StringComparison.CurrentCultureIgnoreCase) && ParameterName.EndsWith(')'))
					{
						if (!this.parametersByName.TryGetValue(ParameterName[4..^1].Trim(), out ParameterInfo? Info))
							continue;

						Info.Parameter.SetMaxValue(Parameter.Value, true);
					}
					else if (ParameterName.StartsWith("Min(", StringComparison.CurrentCultureIgnoreCase) && ParameterName.EndsWith(')'))
					{
						if (!this.parametersByName.TryGetValue(ParameterName[4..^1].Trim(), out ParameterInfo? Info))
							continue;

						Info.Parameter.SetMinValue(Parameter.Value, true);
					}
					else
					{
						if (!this.parametersByName.TryGetValue(ParameterName, out ParameterInfo? Info))
							continue;

						Info.Parameter.SetValue(Parameter.Value);

						if (Info.Control is Entry Entry)
							Entry.Text = Parameter.Value?.ToString() ?? string.Empty;
						else if (Info.Control is CheckBox CheckBox)
						{
							if (Parameter.Value is bool b)
								CheckBox.IsChecked = b;
							else if (Parameter.Value is int i)
								CheckBox.IsChecked = i != 0;
							else if (Parameter.Value is double d)
								CheckBox.IsChecked = d != 0;
							else if (Parameter.Value is decimal d2)
								CheckBox.IsChecked = d2 != 0;
							else if (Parameter.Value is string s && CommonTypes.TryParse(s, out b))
								CheckBox.IsChecked = b;
							else
							{
								ServiceRef.LogService.LogWarning("Invalid option value.",
									new KeyValuePair<string, object?>("Parameter", ParameterName),
									new KeyValuePair<string, object?>("Value", Parameter.Value),
									new KeyValuePair<string, object?>("Type", Parameter.Value?.GetType().FullName ?? string.Empty));
							}
						}
						else if (Info.Control is ExtendedDatePicker Picker)
						{
							if (Parameter.Value is DateTime TP)
								Picker.NullableDate = TP;
							else if (Parameter.Value is string s && (DateTime.TryParse(s, out TP) || XML.TryParse(s, out TP)))
								Picker.NullableDate = TP;
							else
							{
								ServiceRef.LogService.LogWarning("Invalid option value.",
									new KeyValuePair<string, object?>("Parameter", ParameterName),
									new KeyValuePair<string, object?>("Value", Parameter.Value),
									new KeyValuePair<string, object?>("Type", Parameter.Value?.GetType().FullName ?? string.Empty));
							}
						}
					}
				}
				catch (Exception ex)
				{
					ServiceRef.LogService.LogWarning("Invalid option value. Exception: " + ex.Message,
						new KeyValuePair<string, object?>("Parameter", ParameterName),
						new KeyValuePair<string, object?>("Value", Parameter.Value),
						new KeyValuePair<string, object?>("Type", Parameter.Value?.GetType().FullName ?? string.Empty));

					continue;
				}
			}
		}

		private void ShowMultipleContractOptions(IDictionary<CaseInsensitiveString, object>[] Options)
		{
			CaseInsensitiveString PrimaryKey = this.GetPrimaryKey(Options);

			if (CaseInsensitiveString.IsNullOrEmpty(PrimaryKey))
			{
				ServiceRef.LogService.LogWarning("Options not displayed. No primary key could be established. Using only first option.");

				foreach (IDictionary<CaseInsensitiveString, object> Option in Options)
				{
					this.ShowSingleContractOptions(Option);
					break;
				}

				return;
			}

			if (!this.parametersByName.TryGetValue(PrimaryKey, out ParameterInfo? Info))
			{
				ServiceRef.LogService.LogWarning("Options not displayed. Primary key not available in contract.");
				return;
			}

			if (Info.Control is not Entry Entry)
			{
				ServiceRef.LogService.LogWarning("Options not displayed. Parameter control not of a type that allows a selection control to be created.");
				return;
			}

			int EntryIndex = this.Parameters?.Children.IndexOf(Entry) ?? -1;
			if (EntryIndex < 0)
			{
				ServiceRef.LogService.LogWarning("Options not displayed. Primary Key Entry not found.");
				return;
			}

			this.ParameterOptions.Clear();

			ContractOption? SelectedOption = null;

			foreach (IDictionary<CaseInsensitiveString, object> Option in Options)
			{
				string Name = Option[PrimaryKey]?.ToString() ?? string.Empty;
				ContractOption ContractOption = new(Name, Option);

				this.ParameterOptions.Add(ContractOption);

				if (Name == Entry.Text)
					SelectedOption = ContractOption;
			}

			Picker Picker = new()
			{
				StyleId = Info.Parameter.Name,
				ItemsSource = this.ParameterOptions,
				Title = Info.Parameter.Guide
			};

			this.Parameters?.Children.RemoveAt(EntryIndex);
			this.Parameters?.Children.Insert(EntryIndex, Picker);

			Picker.SelectedIndexChanged += this.Parameter_OptionSelectionChanged;
			Info.Control = Picker;

			if (SelectedOption is not null)
				Picker.SelectedItem = SelectedOption;
		}

		private async void Parameter_OptionSelectionChanged(object? Sender, EventArgs e)
		{
			if (Sender is not Picker Picker)
				return;

			if (Picker.SelectedItem is not ContractOption Option)
				return;

			try
			{
				foreach (KeyValuePair<CaseInsensitiveString, object> P in Option.Option)
				{
					string ParameterName = P.Key;

					try
					{
						if (ParameterName.StartsWith("Max(", StringComparison.CurrentCultureIgnoreCase) && ParameterName.EndsWith(')'))
						{
							if (!this.parametersByName.TryGetValue(ParameterName[4..^1].Trim(), out ParameterInfo? Info))
								continue;

							Info.Parameter.SetMaxValue(P.Value, true);
						}
						else if (ParameterName.StartsWith("Min(", StringComparison.CurrentCultureIgnoreCase) && ParameterName.EndsWith(')'))
						{
							if (!this.parametersByName.TryGetValue(ParameterName[4..^1].Trim(), out ParameterInfo? Info))
								continue;

							Info.Parameter.SetMinValue(P.Value, true);
						}
						else
						{
							if (!this.parametersByName.TryGetValue(ParameterName, out ParameterInfo? Info))
								continue;

							Entry? Entry = Info.Control as Entry;

							if (Info.Parameter is StringParameter SP)
							{
								string s = P.Value?.ToString() ?? string.Empty;

								SP.Value = s;

								if (Entry is not null)
								{
									Entry.Text = s;
									Entry.BackgroundColor = ControlBgColor.ToColor(true);
								}
							}
							else if (Info.Parameter is NumericalParameter NP)
							{
								try
								{
									NP.Value = Expression.ToDecimal(P.Value);

									if (Entry is not null)
										Entry.BackgroundColor = ControlBgColor.ToColor(true);
								}
								catch (Exception)
								{
									if (Entry is not null)
										Entry.BackgroundColor = ControlBgColor.ToColor(false);
								}
							}
							else if (Info.Parameter is BooleanParameter BP)
							{
								CheckBox? CheckBox = Info.Control as CheckBox;

								try
								{
									if (P.Value is bool b2)
										BP.Value = b2;
									else if (P.Value is string s && CommonTypes.TryParse(s, out b2))
										BP.Value = b2;
									else
									{
										if (CheckBox is not null)
											CheckBox.BackgroundColor = ControlBgColor.ToColor(false);

										continue;
									}

									if (CheckBox is not null)
										CheckBox.BackgroundColor = ControlBgColor.ToColor(true);
								}
								catch (Exception)
								{
									if (CheckBox is not null)
										CheckBox.BackgroundColor = ControlBgColor.ToColor(false);
								}
							}
							else if (Info.Parameter is DateTimeParameter DTP)
							{
								Picker? Picker2 = Info.Control as Picker;

								if (P.Value is DateTime TP ||
									(P.Value is string s && (DateTime.TryParse(s, out TP) || XML.TryParse(s, out TP))))
								{
									DTP.Value = TP;

									if (Picker2 is not null)
										Picker2.BackgroundColor = ControlBgColor.ToColor(true);
								}
								else
								{
									if (Picker2 is not null)
										Picker2.BackgroundColor = ControlBgColor.ToColor(false);
								}
							}
							else if (Info.Parameter is TimeParameter TSP)
							{
								if (P.Value is TimeSpan TS ||
									(P.Value is string s && TimeSpan.TryParse(s, out TS)))
								{
									TSP.Value = TS;

									if (Entry is not null)
										Entry.BackgroundColor = ControlBgColor.ToColor(true);
								}
								else
								{
									if (Entry is not null)
										Entry.BackgroundColor = ControlBgColor.ToColor(false);
								}
							}
							else if (Info.Parameter is DurationParameter DP)
							{
								if (P.Value is Duration D ||
									(P.Value is string s && Duration.TryParse(s, out D)))
								{
									DP.Value = D;

									if (Entry is not null)
										Entry.BackgroundColor = ControlBgColor.ToColor(true);
								}
								else
								{
									if (Entry is not null)
										Entry.BackgroundColor = ControlBgColor.ToColor(false);

									return;
								}
							}
						}
					}
					catch (Exception ex)
					{
						ServiceRef.LogService.LogException(ex);
					}
				}

				await this.ValidateParameters();
				await this.PopulateHumanReadableText();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private CaseInsensitiveString GetPrimaryKey(IDictionary<CaseInsensitiveString, object>[] Options)
		{
			Dictionary<CaseInsensitiveString, Dictionary<string, bool>> ByKeyAndValue = [];
			LinkedList<CaseInsensitiveString> Keys = new();
			int c = Options.Length;

			foreach (IDictionary<CaseInsensitiveString, object> Option in Options)
			{
				foreach (KeyValuePair<CaseInsensitiveString, object> P in Option)
				{
					if (!ByKeyAndValue.TryGetValue(P.Key, out Dictionary<string, bool>? Values))
					{
						Values = [];
						ByKeyAndValue[P.Key] = Values;
						Keys.AddLast(P.Key);
					}

					Values[P.Value?.ToString() ?? string.Empty] = true;
				}
			}

			foreach (CaseInsensitiveString Key in Keys)
			{
				if (ByKeyAndValue[Key].Count == c &&
					this.parametersByName.TryGetValue(Key, out ParameterInfo? Info) &&
					Info.Control is Entry)
				{
					return Key;
				}
			}

			return CaseInsensitiveString.Empty;
		}

		#endregion

	}
}
