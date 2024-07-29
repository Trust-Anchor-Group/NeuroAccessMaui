using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI;
using NeuroAccessMaui.Services.UI.QR;
using NeuroAccessMaui.UI.Controls;
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
using Waher.Content.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Script;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// The view model to bind to when displaying a new contract view or page.
	/// </summary>
	public partial class NewContractViewModel2 : BaseViewModel, ILinkableView
	{
		private static readonly string partSettingsPrefix = typeof(NewContractViewModel2).FullName + ".Part_";

		[ObservableProperty]
		private ObservableCollection<ParameterInfo2> parameters = [];

		[ObservableProperty]
		private ObservableCollection<RoleInfo> roles = [];

		private readonly SortedDictionary<CaseInsensitiveString, ParameterInfo> parametersByName = [];
		private readonly LinkedList<ParameterInfo> parametersInOrder = new();
		private readonly Dictionary<CaseInsensitiveString, object> presetParameterValues = [];
		private readonly CaseInsensitiveString[]? suppressedProposalIds;
		private readonly string? templateId;
		private Contract? template;
		private bool saveStateWhileScanning;
		private Contract? stateTemplateWhileScanning;
		private readonly Dictionary<CaseInsensitiveString, string> partsToAdd = [];
		private readonly ContractVisibility? initialVisibility = null;

		/// <summary>
		/// The view model to bind to when displaying a new contract view or page.
		/// </summary>
		/// <param name="Page">Page displaying the view.</param>
		/// <param name="Args">Navigation arguments.</param>
		public NewContractViewModel2(NewContractPage2 Page, NewContractNavigationArgs? Args)
		{

			if (Args is not null)
			{
				this.template = Args.Template;
				this.suppressedProposalIds = Args.SuppressedProposalLegalIds;

				if (Args.ParameterValues is not null)
					this.presetParameterValues = Args.ParameterValues;

				if (Args.SetVisibility)
					this.initialVisibility = Args.Template?.Visibility;
			}
			else if (this.stateTemplateWhileScanning is not null)
			{
				this.template = this.stateTemplateWhileScanning;
				this.stateTemplateWhileScanning = null;
			}

			this.templateId = this.template?.ContractId ?? string.Empty;
			this.IsTemplate = this.template?.CanActAsTemplate ?? false;

			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.CreatorAndParts, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_CreatorAndParts)]));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.DomainAndParts, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_DomainAndParts)]));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.Public, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_Public)]));
			this.ContractVisibilityItems.Add(new ContractVisibilityModel(ContractVisibility.PublicSearchable, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_PublicSearchable)]));
		}


		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			try 
			{
				await this.InitializeParametersAsync();
				await this.ValidateParametersAsync();

				await this.InitializeRolesAsync();
				//Testing generating human readable text
				string hrt = await this.template.ToMauiXaml(this.template.DeviceLanguage());
				this.HumanReadableText = new VerticalStackLayout().LoadFromXaml(hrt);

			} catch (Exception ex) {
				ServiceRef.LogService.LogException(ex);
			}
		}


		async void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			await this.ValidateParametersAsync();
		}



		private async Task InitializeParametersAsync()
		{
			//Wrap the parameters in a ParameterInfo object
			foreach (Parameter P in this.template.Parameters)
			{
				ParameterInfo2? PI = P switch {
					BooleanParameter BoolP => new BooleanParameterInfo(BoolP),
					DateParameter DP => new DateParameterInfo(DP),
					DurationParameter DuraP => new DurationParameterInfo(DuraP),
					StringParameter SP => new StringParameterInfo(SP),
					NumericalParameter NP => new NumericalParameterInfo(NP),
					TimeParameter TP => new TimeParameterInfo(TP),
					_ => new ParameterInfo2(P)
				};

				if(PI is not null)
				{
					await PI.InitalizeWithContractAsync(this.template);
					if(this.presetParameterValues.TryGetValue(P.Name, out object? Value))
						PI.Value = Value;
					else
						this.presetParameterValues.Remove(P.Name);
					PI.PropertyChanged += this.Parameter_PropertyChanged;
					this.Parameters.Add(PI);
				}
			}
		}

		private async Task InitializeRolesAsync()
		{
			foreach (Role R in this.template.Roles)
			{
				RoleInfo RI = new(R);
				await RI.InitalizeWithContractAsync(this.template);
				this.Roles.Add(new RoleInfo(R));
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			await base.OnDispose();
		}
		#region Properties


		[ObservableProperty]
		private VerticalStackLayout? humanReadableText;
		/// <summary>
		/// Gets or sets whether this contract is a template or not.
		/// </summary>
		[ObservableProperty]
		private bool isTemplate;

		/// <summary>
		/// A list of valid visibility items to choose from for this contract.
		/// </summary>
		public ObservableCollection<ContractVisibilityModel> ContractVisibilityItems { get; } = [];

		/// <summary>
		/// The selected contract visibility item, if any.
		/// </summary>
		[ObservableProperty]
		private ContractVisibilityModel? selectedContractVisibilityItem;


		private async Task ValidateParametersAsync()
		{
			ContractsClient Client = ServiceRef.XmppService.ContractsClient;
			Variables v = [];
			foreach (ParameterInfo2 P in this.Parameters)
			{
				P.Parameter.Populate(v);
			}

			foreach (ParameterInfo2 P in this.Parameters)
			{
				P.Error = !await P.Parameter.IsParameterValid(v, Client);
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
		public ObservableCollection<string> AvailableRoles { get; } = [];

		/// <summary>
		/// The different parameter options available to choose from when creating a contract.
		/// </summary>
		public ObservableCollection<ContractOption> ParameterOptions { get; } = [];

		/// <summary>
		/// The role selected for the contract, if any.
		/// </summary>
		[ObservableProperty]
		private string? selectedRole;




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


		#endregion

	}
}
