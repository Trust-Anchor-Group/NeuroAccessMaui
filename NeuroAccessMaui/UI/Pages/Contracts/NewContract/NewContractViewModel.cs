using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Controls.Extended;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract.ObjectModel;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Waher.Content.Xml;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Script;
using CommunityToolkit.Maui.Layouts;

namespace NeuroAccessMaui.UI.Pages.Contracts.NewContract
{
	/// <summary>
	/// The view model to bind to when displaying a new contract view or page.
	/// </summary>
	public partial class NewContractViewModel : BaseViewModel, ILinkableView
	{
		#region Fields

		private static readonly string partSettingsPrefix = typeof(NewContractViewModel).FullName + ".Part_";

		private readonly NewContractNavigationArgs? args;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NewContractViewModel"/> class.
		/// </summary>
		public NewContractViewModel()
		{
			this.args = ServiceRef.UiService.PopLatestArgs<NewContractNavigationArgs>();
		}

		#endregion

		#region Properties

		[ObservableProperty]
		private ObservableContract? contract;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(TestCommand))]
		[NotifyCanExecuteChangedFor(nameof(BackCommand))]
		private bool canStateChange;

		[ObservableProperty]
		private string currentState = "Loading";

		[ObservableProperty]
		private string contractName = string.Empty;

		[ObservableProperty]
		private VerticalStackLayout? humanReadableText;



		[ObservableProperty]
		private bool visibilityIsEnabled;

		[ObservableProperty]
		private string? selectedRole;

		[ObservableProperty]
		private bool hasRoles;

		[ObservableProperty]
		private bool hasParameters;

		[ObservableProperty]
		private bool hasHumanReadableText;

		[ObservableProperty]
		private bool canAddParts;

		/// <summary>
		/// The state object containing all views. Is set by the view.
		/// </summary>
		public BindableObject? StateObject { get; set; }

		/// <summary>
		/// A list of valid visibility items to choose from for this contract.
		/// </summary>
		public ObservableCollection<ContractVisibilityModel> ContractVisibilityItems { get; } = new()
		  {
				new ContractVisibilityModel(ContractVisibility.CreatorAndParts, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_CreatorAndParts)]),
				new ContractVisibilityModel(ContractVisibility.DomainAndParts, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_DomainAndParts)]),
				new ContractVisibilityModel(ContractVisibility.Public, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_Public)]),
				new ContractVisibilityModel(ContractVisibility.PublicSearchable, ServiceRef.Localizer[nameof(AppResources.ContractVisibility_PublicSearchable)])
		  };

		/// <summary>
		/// The selected contract visibility item.
		/// </summary>
		[ObservableProperty]
		private ContractVisibilityModel? selectedContractVisibilityItem;

		#endregion

		#region Methods

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.args is null || this.args?.Template is null)
			{
				// TODO: Handle error, perhaps change to an error state
				return;
			}


			try
			{
				this.Contract = await ObservableContract.CreateAsync(this.args.Template);

				string hrt = await this.Contract.Contract.ToMauiXaml(this.Contract.Contract.DeviceLanguage());
				this.HumanReadableText = new VerticalStackLayout().LoadFromXaml(hrt);
				await this.GoToState(NewContractStep.Overview);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				ServiceRef.LogService.LogException(ex);

				// TODO: Handle error, perhaps change to an error state
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			await base.OnDispose();
		}

		/// <summary>
		/// Navigates to the specified state.
		/// </summary>
		/// <param name="newStep">The new step to navigate to.</param>
		private async Task GoToState(NewContractStep newStep)
		{
			if (this.StateObject is null)
			{
				return;
			}

			string newState = newStep.ToString();

			if (newState == this.CurrentState)
				return;

			while (!this.CanStateChange)
				await Task.Delay(100);

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await StateContainer.ChangeStateWithAnimation(this.StateObject, newState);
			});
		}

		/// <summary>
		/// Validates the parameters of the contract and updates their error states.
		/// </summary>
		private async Task ValidateParametersAsync()
		{
			ContractsClient client = ServiceRef.XmppService.ContractsClient;

			// Populate the parameters
			Variables v = new();
			foreach (ObservableParameter p in this.Parameters)
			{
				p.Parameter.Populate(v);
			}

			foreach (ObservableParameter p in this.Parameters)
			{
				p.IsValid = await p.Parameter.IsParameterValid(v, client);
				p.ValidationText = p.Parameter.ErrorText;
			}
		}

		#endregion

		#region Commands

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		public async Task Back()
		{
			try
			{
				NewContractStep currentStep = (NewContractStep)Enum.Parse(typeof(NewContractStep), this.CurrentState);

				switch (currentStep)
				{
					case NewContractStep.Overview:
						await base.GoBack();
						break;
					default:
						await this.GoToState(NewContractStep.Overview);
						break;
				}
			}
			catch (Exception e)
			{
				ServiceRef.LogService.LogException(e);
			}
		}

		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task Test()
		{
			await this.GoToState(NewContractStep.Loading);
			await this.GoToState(NewContractStep.Parameters);
		}

		#endregion

		#region Event Handlers

		private async void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			await this.ValidateParametersAsync();
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
				bool first = true;

				url.Append(Constants.UriSchemes.IotSc);
				url.Append(':');
				url.Append(this.template?.ContractId);

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
		public Task<string> Title => ContractModel.GetName(this.template);

		/// <inheritdoc/>
		public bool HasMedia => false;

		/// <inheritdoc/>
		public byte[]? Media => null;

		/// <inheritdoc/>
		public string? MediaContentType => null;

		#endregion
	}
}
