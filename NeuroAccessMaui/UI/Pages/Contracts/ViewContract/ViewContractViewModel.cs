using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NeuroAccessMaui.UI.Converters;
using IdApp.Extensions;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.ViewContract.ObjectModel;
using NeuroAccessMaui.UI.Pages.Signatures.ClientSignature;
using NeuroAccessMaui.UI.Pages.Signatures.ServerSignature;
using NeuroAccessMaui.Services.UI.Photos;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Extensions;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// The view model to bind to for when displaying contracts.
	/// </summary>
	public class ViewContractViewModel : QrXmppViewModel
	{
		private bool isReadOnly;
		private readonly PhotosLoader photosLoader;
		private DateTime skipContractEvent = DateTime.MinValue;

		/// <summary>
		/// Creates an instance of the <see cref="ViewContractViewModel"/> class.
		/// </summary>
		protected internal ViewContractViewModel()
		{
			this.Photos = [];
			this.photosLoader = new PhotosLoader(this.Photos);

			this.ObsoleteContractCommand = new Command(async _ => await this.ObsoleteContract());
			this.DeleteContractCommand = new Command(async _ => await this.DeleteContract());
			this.ShowDetailsCommand = new Command(async _ => await this.ShowDetails());

			this.GeneralInformation = [];
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryGetArgs(out ViewContractNavigationArgs args))
			{
				this.Contract = args.Contract;
				this.isReadOnly = args.IsReadOnly;
				this.Role = args.Role;
				this.IsProposal = !string.IsNullOrEmpty(this.Role);
				this.Proposal = string.IsNullOrEmpty(args.Proposal) ? ServiceRef.Localizer[nameof(AppResources.YouHaveReceivedAProposal)] : args.Proposal;
			}
			else
			{
				this.Contract = null;
				this.isReadOnly = true;
				this.Role = null;
				this.IsProposal = false;
			}

			ServiceRef.XmppService.ContractUpdated += this.ContractsClient_ContractUpdatedOrSigned;
			ServiceRef.XmppService.ContractSigned += this.ContractsClient_ContractUpdatedOrSigned;

			if (this.Contract is not null)
			{
				DateTime TP = ServiceRef.XmppService.GetTimeOfLastContractEvent(this.Contract.ContractId);
				if (DateTime.Now.Subtract(TP).TotalSeconds < 5)
					this.Contract = await ServiceRef.XmppService.GetContract(this.Contract.ContractId);

				this.Contract.FormatParameterDisplay += this.Contract_FormatParameterDisplay;

				await this.DisplayContract();
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			if (this.Contract is not null)
				this.Contract.FormatParameterDisplay -= this.Contract_FormatParameterDisplay;

			ServiceRef.XmppService.ContractUpdated -= this.ContractsClient_ContractUpdatedOrSigned;
			ServiceRef.XmppService.ContractSigned -= this.ContractsClient_ContractUpdatedOrSigned;

			this.ClearContract();

			await base.OnDispose();
		}

		private void Contract_FormatParameterDisplay(object Sender, Waher.Networking.XMPP.Contracts.EventArguments.ParameterValueFormattingEventArgs e)
		{
			if (e.Value is Duration Duration)
				e.Value = DurationToString.ToString(Duration);
		}

		private Task ContractsClient_ContractUpdatedOrSigned(object Sender, ContractReferenceEventArgs e)
		{
			if (e.ContractId == this.Contract.ContractId && DateTime.Now.Subtract(this.skipContractEvent).TotalSeconds > 5)
				this.ReloadContract(e.ContractId);

			return Task.CompletedTask;
		}

		private async void ReloadContract(string ContractId)
		{
			try
			{
				Contract Contract = await ServiceRef.XmppService.GetContract(ContractId);

				MainThread.BeginInvokeOnMainThread(async () => await this.ContractUpdated(Contract));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		private async Task ContractUpdated(Contract Contract)
		{
			this.ClearContract();

			this.Contract = Contract;

			if (this.Contract is not null)
				await this.DisplayContract();
		}

		#region Properties

		/// <summary>
		/// The command to bind to when marking a contract as obsolete.
		/// </summary>
		public ICommand ObsoleteContractCommand { get; }

		/// <summary>
		/// The command to bind to when deleting a contract.
		/// </summary>
		public ICommand DeleteContractCommand { get; }

		/// <summary>
		/// Command to show machine-readable details of contract.
		/// </summary>
		public ICommand ShowDetailsCommand { get; }

		/// <summary>
		/// See <see cref="Role"/>
		/// </summary>
		public static readonly BindableProperty RoleProperty =
			BindableProperty.Create(nameof(Role), typeof(string), typeof(ViewContractViewModel), default(string));

		/// <summary>
		/// Contains proposed role, if a proposal, null if not a proposal.
		/// </summary>
		public string Role
		{
			get => (string)this.GetValue(RoleProperty);
			set => this.SetValue(RoleProperty, value);
		}

		/// <summary>
		/// See <see cref="IsProposal"/>
		/// </summary>
		public static readonly BindableProperty IsProposalProperty =
			BindableProperty.Create(nameof(IsProposal), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// If the view represents a proposal to sign a contract.
		/// </summary>
		public bool IsProposal
		{
			get => (bool)this.GetValue(IsProposalProperty);
			set => this.SetValue(IsProposalProperty, value);
		}

		/// <summary>
		/// See <see cref="Proposal"/>
		/// </summary>
		public static readonly BindableProperty ProposalProperty =
			BindableProperty.Create(nameof(Proposal), typeof(string), typeof(ViewContractViewModel), default(string));

		/// <summary>
		/// If the contract is a proposal
		/// </summary>
		public string Proposal
		{
			get => (string)this.GetValue(ProposalProperty);
			set => this.SetValue(ProposalProperty, value);
		}

		/// <summary>
		/// Holds a list of general information sections for the contract.
		/// </summary>
		public ObservableCollection<PartModel> GeneralInformation { get; }

		/// <summary>
		/// See <see cref="Roles"/>
		/// </summary>
		public static readonly BindableProperty RolesProperty =
			BindableProperty.Create(nameof(Roles), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's roles.
		/// </summary>
		public StackLayout Roles
		{
			get => (StackLayout)this.GetValue(RolesProperty);
			set => this.SetValue(RolesProperty, value);
		}

		/// <summary>
		/// See <see cref="Parts"/>
		/// </summary>
		public static readonly BindableProperty PartsProperty =
			BindableProperty.Create(nameof(Parts), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parts.
		/// </summary>
		public StackLayout Parts
		{
			get => (StackLayout)this.GetValue(PartsProperty);
			set => this.SetValue(PartsProperty, value);
		}

		/// <summary>
		/// See <see cref="Parameters"/>
		/// </summary>
		public static readonly BindableProperty ParametersProperty =
			BindableProperty.Create(nameof(Parameters), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parameters.
		/// </summary>
		public StackLayout Parameters
		{
			get => (StackLayout)this.GetValue(ParametersProperty);
			set => this.SetValue(ParametersProperty, value);
		}

		/// <summary>
		/// See <see cref="HumanReadableText"/>
		/// </summary>
		public static readonly BindableProperty HumanReadableTextProperty =
			BindableProperty.Create(nameof(HumanReadableText), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's human readable text section.
		/// </summary>
		public StackLayout HumanReadableText
		{
			get => (StackLayout)this.GetValue(HumanReadableTextProperty);
			set => this.SetValue(HumanReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="MachineReadableText"/>
		/// </summary>
		public static readonly BindableProperty MachineReadableTextProperty =
			BindableProperty.Create(nameof(MachineReadableText), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's machine readable text section.
		/// </summary>
		public StackLayout MachineReadableText
		{
			get => (StackLayout)this.GetValue(MachineReadableTextProperty);
			set => this.SetValue(MachineReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="ClientSignatures"/>
		/// </summary>
		public static readonly BindableProperty ClientSignaturesProperty =
			BindableProperty.Create(nameof(ClientSignatures), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's client signatures.
		/// </summary>
		public StackLayout ClientSignatures
		{
			get => (StackLayout)this.GetValue(ClientSignaturesProperty);
			set => this.SetValue(ClientSignaturesProperty, value);
		}

		/// <summary>
		/// See <see cref="ServerSignatures"/>
		/// </summary>
		public static readonly BindableProperty ServerSignaturesProperty =
			BindableProperty.Create(nameof(ServerSignatures), typeof(StackLayout), typeof(ViewContractViewModel), default(StackLayout));

		/// <summary>
		/// Holds Xaml code for visually representing a contract's server signatures.
		/// </summary>
		public StackLayout ServerSignatures
		{
			get => (StackLayout)this.GetValue(ServerSignaturesProperty);
			set => this.SetValue(ServerSignaturesProperty, value);
		}

		/// <summary>
		/// Gets the list of photos associated with the contract.
		/// </summary>
		public ObservableCollection<Photo> Photos { get; }

		/// <summary>
		/// The contract to display.
		/// </summary>
		public Contract Contract { get; private set; }

		/// <summary>
		/// See <see cref="HasPhotos"/>
		/// </summary>
		public static readonly BindableProperty HasPhotosProperty =
			BindableProperty.Create(nameof(HasPhotos), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether photos are available.
		/// </summary>
		public bool HasPhotos
		{
			get => (bool)this.GetValue(HasPhotosProperty);
			set => this.SetValue(HasPhotosProperty, value);
		}

		/// <summary>
		/// See <see cref="HasRoles"/>
		/// </summary>
		public static readonly BindableProperty HasRolesProperty =
			BindableProperty.Create(nameof(HasRoles), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any roles to display.
		/// </summary>
		public bool HasRoles
		{
			get => (bool)this.GetValue(HasRolesProperty);
			set => this.SetValue(HasRolesProperty, value);
		}

		/// <summary>
		/// See <see cref="HasParts"/>
		/// </summary>
		public static readonly BindableProperty HasPartsProperty =
			BindableProperty.Create(nameof(HasParts), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any contract parts to display.
		/// </summary>
		public bool HasParts
		{
			get => (bool)this.GetValue(HasPartsProperty);
			set => this.SetValue(HasPartsProperty, value);
		}

		/// <summary>
		/// See <see cref="HasParameters"/>
		/// </summary>
		public static readonly BindableProperty HasParametersProperty =
			BindableProperty.Create(nameof(HasParameters), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any parameters to display.
		/// </summary>
		public bool HasParameters
		{
			get => (bool)this.GetValue(HasParametersProperty);
			set => this.SetValue(HasParametersProperty, value);
		}

		/// <summary>
		/// See <see cref="HasHumanReadableText"/>
		/// </summary>
		public static readonly BindableProperty HasHumanReadableTextProperty =
			BindableProperty.Create(nameof(HasHumanReadableText), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any human readable texts to display.
		/// </summary>
		public bool HasHumanReadableText
		{
			get => (bool)this.GetValue(HasHumanReadableTextProperty);
			set => this.SetValue(HasHumanReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="HasMachineReadableText"/>
		/// </summary>
		public static readonly BindableProperty HasMachineReadableTextProperty =
			BindableProperty.Create(nameof(HasMachineReadableText), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any machine readable texts to display.
		/// </summary>
		public bool HasMachineReadableText
		{
			get => (bool)this.GetValue(HasMachineReadableTextProperty);
			set => this.SetValue(HasMachineReadableTextProperty, value);
		}

		/// <summary>
		/// See <see cref="HasClientSignatures"/>
		/// </summary>
		public static readonly BindableProperty HasClientSignaturesProperty =
			BindableProperty.Create(nameof(HasClientSignatures), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any client signatures to display.
		/// </summary>
		public bool HasClientSignatures
		{
			get => (bool)this.GetValue(HasClientSignaturesProperty);
			set => this.SetValue(HasClientSignaturesProperty, value);
		}

		/// <summary>
		/// See <see cref="HasServerSignatures"/>
		/// </summary>
		public static readonly BindableProperty HasServerSignaturesProperty =
			BindableProperty.Create(nameof(HasServerSignatures), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the contract has any server signatures to display.
		/// </summary>
		public bool HasServerSignatures
		{
			get => (bool)this.GetValue(HasServerSignaturesProperty);
			set => this.SetValue(HasServerSignaturesProperty, value);
		}

		/// <summary>
		/// See <see cref="CanDeleteContract"/>
		/// </summary>
		public static readonly BindableProperty CanDeleteContractProperty =
			BindableProperty.Create(nameof(CanDeleteContract), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can delete or obsolete a contract.
		/// </summary>
		public bool CanDeleteContract
		{
			get => (bool)this.GetValue(CanDeleteContractProperty);
			set => this.SetValue(CanDeleteContractProperty, value);
		}

		/// <summary>
		/// See <see cref="CanObsoleteContract"/>
		/// </summary>
		public static readonly BindableProperty CanObsoleteContractProperty =
			BindableProperty.Create(nameof(CanObsoleteContract), typeof(bool), typeof(ViewContractViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether a user can delete or obsolete a contract.
		/// </summary>
		public bool CanObsoleteContract
		{
			get => (bool)this.GetValue(CanObsoleteContractProperty);
			set => this.SetValue(CanObsoleteContractProperty, value);
		}

		#endregion

		private void ClearContract()
		{
			this.photosLoader.CancelLoadPhotos();
			this.Contract = null;
			this.GeneralInformation.Clear();
			this.Roles = null;
			this.Parts = null;
			this.Parameters = null;
			this.HumanReadableText = null;
			this.MachineReadableText = null;
			this.ClientSignatures = null;
			this.ServerSignatures = null;
			this.HasPhotos = false;
			this.HasRoles = false;
			this.HasParts = false;
			this.HasParameters = false;
			this.HasHumanReadableText = false;
			this.HasMachineReadableText = false;
			this.HasClientSignatures = false;
			this.HasServerSignatures = false;
			this.CanDeleteContract = false;
			this.CanObsoleteContract = false;

			this.RemoveQrCode();
		}

		private async Task DisplayContract()
		{
			try
			{
				bool HasSigned = false;
				bool AcceptsSignatures =
					(this.Contract.State == ContractState.Approved || this.Contract.State == ContractState.BeingSigned) &&
					(!this.Contract.SignAfter.HasValue || this.Contract.SignAfter.Value < DateTime.Now) &&
					(!this.Contract.SignBefore.HasValue || this.Contract.SignBefore.Value > DateTime.Now);
				Dictionary<string, int> NrSignatures = [];
				bool CanObsolete = false;

				if (this.Contract.ClientSignatures is not null)
				{
					foreach (Waher.Networking.XMPP.Contracts.ClientSignature signature in this.Contract.ClientSignatures)
					{
						if (signature.LegalId == this.TagProfile.LegalIdentity.Id)
							HasSigned = true;

						if (!NrSignatures.TryGetValue(signature.Role, out int count))
							count = 0;

						NrSignatures[signature.Role] = count + 1;

						if (string.Compare(signature.BareJid, ServiceRef.XmppService.BareJid, true) == 0)
						{
							if (this.Contract.Roles is not null)
							{
								foreach (Role Role in this.Contract.Roles)
								{
									if (Role.Name == signature.Role)
									{
										if (Role.CanRevoke)
										{
											CanObsolete =
												this.Contract.State == ContractState.Approved ||
												this.Contract.State == ContractState.BeingSigned ||
												this.Contract.State == ContractState.Signed;
										}

										break;
									}
								}
							}
						}
					}
				}

				// General Information

				this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.Created)], this.Contract.Created.ToString(CultureInfo.CurrentUICulture)));

				if (this.Contract.Updated > DateTime.MinValue)
					this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.Updated)], this.Contract.Updated.ToString(CultureInfo.CurrentUICulture)));

				this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.State)], this.Contract.State.ToString(), ContractStateToColor.ToColor(this.Contract.State)));
				this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.Visibility)], this.Contract.Visibility.ToString()));
				this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.Duration)], this.Contract.Duration.ToString()));
				this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.From)], this.Contract.From.ToString(CultureInfo.CurrentUICulture)));
				this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.To)], this.Contract.To.ToString(CultureInfo.CurrentUICulture)));
				this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.Archiving_Optional)], this.Contract.ArchiveOptional.ToString()));
				this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.Archiving_Required)], this.Contract.ArchiveRequired.ToString()));
				this.GeneralInformation.Add(new PartModel(ServiceRef.Localizer[nameof(AppResources.CanActAsTemplate)], this.Contract.CanActAsTemplate.ToYesNo()));

				this.GenerateQrCode(Constants.UriSchemes.CreateSmartContractUri(this.Contract.ContractId));

				// Roles

				if (this.Contract.Roles is not null)
				{
					StackLayout RolesLayout = [];

					foreach (Role Role in this.Contract.Roles)
					{
						string Html = await Role.ToHTML(this.Contract.DeviceLanguage(), this.Contract);
						Html = Waher.Content.Html.HtmlDocument.GetBody(Html);

						AddKeyValueLabelPair(RolesLayout, Role.Name, Html + GenerateMinMaxCountString(Role.MinCount, Role.MaxCount), true, string.Empty, null);

						if (!this.isReadOnly && AcceptsSignatures && !HasSigned && this.Contract.PartsMode == ContractParts.Open &&
							(!NrSignatures.TryGetValue(Role.Name, out int count) || count < Role.MaxCount) &&
							(!this.IsProposal || Role.Name == this.Role))
						{
							Button button = new()
							{
								Text = string.Format(ServiceRef.Localizer[nameof(AppResources.SignAsRole)], Role.Name),
								StyleId = Role.Name
							};

							button.Clicked += this.SignButton_Clicked;
							RolesLayout.Children.Add(button);
						}
					}

					this.Roles = RolesLayout;
				}

				// Parts

				StackLayout PartsLayout = [];

				if (this.Contract.SignAfter.HasValue)
					AddKeyValueLabelPair(PartsLayout, ServiceRef.Localizer[nameof(AppResources.SignAfter)], this.Contract.SignAfter.Value.ToString(CultureInfo.CurrentUICulture));

				if (this.Contract.SignBefore.HasValue)
					AddKeyValueLabelPair(PartsLayout, ServiceRef.Localizer[nameof(AppResources.SignBefore)], this.Contract.SignBefore.Value.ToString(CultureInfo.CurrentUICulture));

				AddKeyValueLabelPair(PartsLayout, ServiceRef.Localizer[nameof(AppResources.Mode)], this.Contract.PartsMode.ToString());

				if (this.Contract.Parts is not null)
				{
					TapGestureRecognizer OpenLegalId = new();
					OpenLegalId.Tapped += this.Part_Tapped;

					foreach (Part Part in this.Contract.Parts)
					{
						AddKeyValueLabelPair(PartsLayout, Part.Role, Part.LegalId, false, OpenLegalId);

						if (!this.isReadOnly && AcceptsSignatures && !HasSigned && Part.LegalId == this.TagProfile.LegalIdentity.Id)
						{
							Button Button = new()
							{
								Text = string.Format(ServiceRef.Localizer[nameof(AppResources.SignAsRole)], Part.Role),
								StyleId = Part.Role
							};

							Button.Clicked += this.SignButton_Clicked;
							PartsLayout.Children.Add(Button);
						}
					}
				}

				this.Parts = PartsLayout;

				// Parameters

				if (this.Contract.Parameters is not null)
				{
					StackLayout ParametersLayout = [];

					foreach (Parameter Parameter in this.Contract.Parameters)
					{
						if (Parameter.ObjectValue is bool b)
							AddKeyValueLabelPair(ParametersLayout, Parameter.Name, b ? "✔" : "✗");
						else
							AddKeyValueLabelPair(ParametersLayout, Parameter.Name, Parameter.ObjectValue?.ToString());
					}

					this.Parameters = ParametersLayout;
				}

				// Human readable text

				StackLayout HumanReadableTextLayout = [];
				string Xaml = await this.Contract.ToXamarinForms(this.Contract.DeviceLanguage());
				StackLayout HumanReadableXaml = new StackLayout().LoadFromXaml(Xaml);

				List<View> Children = [.. HumanReadableXaml.Children];

				foreach (View View in Children)
				{
					if (View is ContentView)
					{
						foreach (Element InnView in (View as ContentView).Children)
						{
							if (InnView is Label)
							{
								(InnView as Label).TextColor = (Color)(Application.Current.RequestedTheme == OSAppTheme.Dark ?
								Application.Current.Resources["LabelTextColorDarkTheme"] : Application.Current.Resources["LabelTextColorLightTheme"]);
							}
						}
					}

					HumanReadableTextLayout.Children.Add(View);
				}

				this.HumanReadableText = HumanReadableTextLayout;

				// Machine readable text

				TapGestureRecognizer OpenContractId = new();
				OpenContractId.Tapped += this.ContractId_Tapped;

				TapGestureRecognizer OpenLink = new();
				OpenLink.Tapped += this.Link_Tapped;

				TapGestureRecognizer CopyToClipboard = new();
				CopyToClipboard.Tapped += this.CopyToClipboard_Tapped;

				StackLayout MachineReadableTextLayout = [];
				AddKeyValueLabelPair(MachineReadableTextLayout, ServiceRef.Localizer[nameof(AppResources.ContractId)],
					this.Contract.ContractId, false, Constants.UriSchemes.IotSc + ":" + this.Contract.ContractId,
					CopyToClipboard);

				if (!string.IsNullOrEmpty(this.Contract.TemplateId))
				{
					AddKeyValueLabelPair(MachineReadableTextLayout, ServiceRef.Localizer[nameof(AppResources.TemplateId)],
						this.Contract.TemplateId, false, OpenContractId);
				}

				AddKeyValueLabelPair(MachineReadableTextLayout, ServiceRef.Localizer[nameof(AppResources.Digest)], Convert.ToBase64String(this.Contract.ContentSchemaDigest), false, CopyToClipboard);
				AddKeyValueLabelPair(MachineReadableTextLayout, ServiceRef.Localizer[nameof(AppResources.HashFunction)], this.Contract.ContentSchemaHashFunction.ToString(), false, CopyToClipboard);
				AddKeyValueLabelPair(MachineReadableTextLayout, ServiceRef.Localizer[nameof(AppResources.LocalName)], this.Contract.ForMachinesLocalName, false, CopyToClipboard);
				AddKeyValueLabelPair(MachineReadableTextLayout, ServiceRef.Localizer[nameof(AppResources.Namespace)], this.Contract.ForMachinesNamespace, false, OpenLink);

				this.MachineReadableText = MachineReadableTextLayout;

				// Client signatures
				if (this.Contract.ClientSignatures is not null)
				{
					StackLayout clientSignaturesLayout = [];
					TapGestureRecognizer openClientSignature = new();
					openClientSignature.Tapped += this.ClientSignature_Tapped;

					foreach (Waher.Networking.XMPP.Contracts.ClientSignature signature in this.Contract.ClientSignatures)
					{
						string Sign = Convert.ToBase64String(signature.DigitalSignature);
						StringBuilder sb = new();
						sb.Append(signature.LegalId);
						sb.Append(", ");
						sb.Append(signature.BareJid);
						sb.Append(", ");
						sb.Append(signature.Timestamp.ToString(CultureInfo.CurrentUICulture));
						sb.Append(", ");
						sb.Append(Sign);

						AddKeyValueLabelPair(clientSignaturesLayout, signature.Role, sb.ToString(), false, Sign, openClientSignature);
					}

					this.ClientSignatures = clientSignaturesLayout;
				}

				// Server signature
				if (this.Contract.ServerSignature is not null)
				{
					StackLayout serverSignaturesLayout = [];

					TapGestureRecognizer openServerSignature = new();
					openServerSignature.Tapped += this.ServerSignature_Tapped;

					StringBuilder sb = new();
					sb.Append(this.Contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture));
					sb.Append(", ");
					sb.Append(Convert.ToBase64String(this.Contract.ServerSignature.DigitalSignature));

					AddKeyValueLabelPair(serverSignaturesLayout, this.Contract.Provider, sb.ToString(), false, this.Contract.ContractId, openServerSignature);
					this.ServerSignatures = serverSignaturesLayout;
				}

				this.CanDeleteContract = !this.isReadOnly && !this.Contract.IsLegallyBinding(true);
				this.CanObsoleteContract = this.CanDeleteContract || CanObsolete;

				this.HasRoles = this.Roles?.Children.Count > 0;
				this.HasParts = this.Parts?.Children.Count > 0;
				this.HasParameters = this.Parameters?.Children.Count > 0;
				this.HasHumanReadableText = this.HumanReadableText?.Children.Count > 0;
				this.HasMachineReadableText = this.MachineReadableText?.Children.Count > 0;
				this.HasClientSignatures = this.ClientSignatures?.Children.Count > 0;
				this.HasServerSignatures = this.ServerSignatures?.Children.Count > 0;

				if (this.Contract.Attachments is not null && this.Contract.Attachments.Length > 0)
				{
					_ = this.photosLoader.LoadPhotos(this.Contract.Attachments, SignWith.LatestApprovedId, () =>
						{
							MainThread.BeginInvokeOnMainThread(() => this.HasPhotos = this.Photos.Count > 0);
						});
				}
				else
					this.HasPhotos = false;
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex, this.GetClassAndMethod(MethodBase.GetCurrentMethod())
					.Append(new KeyValuePair<string, object>("ContractId", this.Contract?.ContractId))
					.ToArray());

				this.ClearContract();
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private static string GenerateMinMaxCountString(int min, int max)
		{
			if (min == max)
			{
				if (max == 1)
					return string.Empty;

				return " (" + max.ToString() + ")";
			}

			return " (" + min.ToString() + " - " + max.ToString() + ")";
		}

		private static void AddKeyValueLabelPair(StackLayout Container, string Key,
			string Value)
		{
			AddKeyValueLabelPair(Container, Key, Value, false, string.Empty, null);
		}

		private static void AddKeyValueLabelPair(StackLayout Container, string Key,
			string Value, bool IsHtml, TapGestureRecognizer TapGestureRecognizer)
		{
			AddKeyValueLabelPair(Container, Key, Value, IsHtml, Value, TapGestureRecognizer);
		}

		private static void AddKeyValueLabelPair(StackLayout Container, string Key,
			string Value, bool IsHtml, string StyleId, TapGestureRecognizer TapGestureRecognizer)
		{
			StackLayout layout = new()
			{
				Orientation = StackOrientation.Horizontal,
				StyleId = StyleId
			};

			Container.Children.Add(layout);

			layout.Children.Add(new Label
			{
				Text = Key + ":",
				Style = (Style)Application.Current.Resources["KeyLabel"]
			});

			layout.Children.Add(new Label
			{
				Text = Value,
				TextType = IsHtml ? TextType.Html : TextType.Text,
				Style = (Style)Application.Current.Resources[IsHtml ? "FormattedValueLabel" : TapGestureRecognizer is null ? "ValueLabel" : "ClickableValueLabel"]
			});

			if (TapGestureRecognizer is not null)
				layout.GestureRecognizers.Add(TapGestureRecognizer);
		}

		private async void SignButton_Clicked(object Sender, EventArgs e)
		{
			try
			{
				if (!await App.AuthenticateUser(true))
					return;

				if (Sender is Button button && !string.IsNullOrEmpty(button.StyleId))
				{
					this.skipContractEvent = DateTime.Now;

					Contract contract = await ServiceRef.XmppService.SignContract(this.Contract, button.StyleId, false);
					await this.ContractUpdated(contract);

					await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.ContractSuccessfullySigned)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void Part_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
					await this.ContractOrchestratorService.OpenLegalIdentity(Layout.StyleId, ServiceRef.Localizer[nameof(AppResources.PurposeReviewContract)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void ContractId_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
					await this.ContractOrchestratorService.OpenContract(Layout.StyleId, ServiceRef.Localizer[nameof(AppResources.PurposeReviewContract)], null);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void Link_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
					await App.OpenUrlAsync(Layout.StyleId);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void CopyToClipboard_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
				{
					await Clipboard.SetTextAsync(Layout.StyleId);
					await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void ClientSignature_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
				{
					string sign = layout.StyleId;
					Waher.Networking.XMPP.Contracts.ClientSignature signature = this.Contract.ClientSignatures.FirstOrDefault(x => sign == Convert.ToBase64String(x.DigitalSignature));
					if (signature is not null)
					{
						string legalId = signature.LegalId;
						LegalIdentity identity = await ServiceRef.XmppService.GetLegalIdentity(legalId);

						await this.NavigationService.GoToAsync(nameof(ClientSignaturePage),
							new ClientSignatureNavigationArgs(signature, identity));
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private async void ServerSignature_Tapped(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
				{
					await this.NavigationService.GoToAsync(nameof(ServerSignaturePage),
						  new ServerSignatureNavigationArgs(this.Contract));
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ObsoleteContract()
		{
			try
			{
				if (!await App.AuthenticateUser(true))
					return;

				this.skipContractEvent = DateTime.Now;

				Contract obsoletedContract = await ServiceRef.XmppService.ObsoleteContract(this.Contract.ContractId);
				await this.ContractUpdated(obsoletedContract);

				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.ContractHasBeenObsoleted)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task DeleteContract()
		{
			try
			{
				if (!await App.AuthenticateUser(true))
					return;

				this.skipContractEvent = DateTime.Now;

				Contract deletedContract = await ServiceRef.XmppService.DeleteContract(this.Contract.ContractId);
				await this.ContractUpdated(deletedContract);

				await ServiceRef.UiSerializer.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.ContractHasBeenDeleted)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ShowDetails()
		{
			try
			{
				byte[] Bin = Encoding.UTF8.GetBytes(this.Contract.ForMachines.OuterXml);
				HttpFileUploadEventArgs e = await ServiceRef.XmppService.RequestUploadSlotAsync(this.Contract.ContractId + ".xml", "text/xml; charset=utf-8", Bin.Length);

				if (e.Ok)
				{
					await e.PUT(Bin, "text/xml", (int)Constants.Timeouts.UploadFile.TotalMilliseconds);
					await App.OpenUrlAsync(e.GetUrl);
				}
				else
					await ServiceRef.UiSerializer.DisplayAlert(e.StanzaError ?? new Exception(e.ErrorText));
			}
			catch (Exception ex)
			{
				await ServiceRef.UiSerializer.DisplayAlert(ex);
			}
		}

		#region ILinkableView

		/// <summary>
		/// Link to the current view
		/// </summary>
		public override string Link { get; }

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => ContractModel.GetName(this.Contract, this);

		#endregion

	}
}
