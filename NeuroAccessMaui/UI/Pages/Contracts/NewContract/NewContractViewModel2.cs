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
using Waher.Content;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Persistence;
using Waher.Script;
using PropertyChangingEventArgs = System.ComponentModel.PropertyChangingEventArgs;

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

			foreach (Parameter P in this.template.Parameters)
			{
				if (P is BooleanParameter BP)
				{
					BooleanParameterInfo BPI = new(BP);
					this.Parameters.Add(BPI);
				}
				else if (P is DateParameter DP)
				{
					DateParameterInfo DPI = new(DP);
					this.Parameters.Add(DPI);
				}
				else if (P is DurationParameter DuraP)
				{
					DurationParameterInfo DPI = new(DuraP);
					this.Parameters.Add(DPI);
				}
				else if (P is StringParameter SP)
				{
					StringParameterInfo SPI = new(SP);
					this.Parameters.Add(SPI);
				}
				else if (P is NumericalParameter NP)
				{
					NumericalParameterInfo NPI = new(NP);
					this.Parameters.Add(NPI);
				}
				else if (P is TimeParameter TP)
				{
					TimeParameterInfo TPI = new(TP);
					this.Parameters.Add(TPI);
				}
				this.Parameters.Last().PropertyChanged += this.Parameter_PropertyChanged;
			}

		}

		async void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			Console.WriteLine(e.PropertyName);
			await this.ValidateParameters();
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{

			await base.OnDispose();
		}



		#region Properties



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

		/// <inheritdoc/>
		protected override void OnPropertyChanging(PropertyChangingEventArgs e)
		{
			base.OnPropertyChanging(e);

		}

		/// <inheritdoc/>
		protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			try
			{
				base.OnPropertyChanged(e);
				Console.WriteLine(e.PropertyName);
				await this.ValidateParameters();
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async Task ValidateParameters()
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
		/// Holds Xaml code for visually representing a contract's roles.
		/// </summary>
		[ObservableProperty]
		private VerticalStackLayout? roles;

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
