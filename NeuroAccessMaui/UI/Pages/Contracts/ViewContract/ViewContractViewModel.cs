using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Signatures.ClientSignature;
using NeuroAccessMaui.UI.Pages.Signatures.ServerSignature;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using Waher.Content;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.HttpFileUpload;

namespace NeuroAccessMaui.UI.Pages.Contracts.ViewContract
{
	/// <summary>
	/// The view model to bind to for when displaying contracts.
	/// </summary>
	public partial class ViewContractViewModel : QrXmppViewModel
	{
		private readonly bool isReadOnly;
		private readonly PhotosLoader photosLoader;
		private DateTime skipContractEvent = DateTime.MinValue;

		/// <summary>
		/// Creates an instance of the <see cref="ViewContractViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public ViewContractViewModel(ViewContractNavigationArgs? Args)
		{
			this.Photos = [];
			this.photosLoader = new PhotosLoader(this.Photos);

			if (Args is not null)
			{
				this.Contract = Args.Contract;
				this.isReadOnly = Args.IsReadOnly;
				this.Role = Args.Role;
				this.IsProposal = !string.IsNullOrEmpty(this.Role);
				this.Proposal = string.IsNullOrEmpty(Args.Proposal) ? ServiceRef.Localizer[nameof(AppResources.YouHaveReceivedAProposal)] : Args.Proposal;
			}
			else
			{
				this.Contract = null;
				this.isReadOnly = true;
				this.Role = null;
				this.IsProposal = false;
			}
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

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

		private void Contract_FormatParameterDisplay(object? Sender, Waher.Networking.XMPP.Contracts.EventArguments.ParameterValueFormattingEventArgs e)
		{
			if (e.Value is Duration Duration)
				e.Value = DurationToString.ToString(Duration);
		}

		private Task ContractsClient_ContractUpdatedOrSigned(object? Sender, ContractReferenceEventArgs e)
		{
			if (e.ContractId == this.Contract?.ContractId && DateTime.Now.Subtract(this.skipContractEvent).TotalSeconds > 5)
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
		/// Contract Id
		/// </summary>
		[ObservableProperty]
		private string? contractId;

		/// <summary>
		/// Contract state
		/// </summary>
		[ObservableProperty]
		private ContractState? state;

		/// <summary>
		/// When contract was created
		/// </summary>
		[ObservableProperty]
		private DateTime? created;

		/// <summary>
		/// When contract was updated
		/// </summary>
		[ObservableProperty]
		private DateTime? updated;

		/// <summary>
		/// Contract visibility
		/// </summary>
		[ObservableProperty]
		private ContractVisibility? visibility;

		/// <summary>
		/// Duration of contract
		/// </summary>
		[ObservableProperty]
		private Duration? duration;

		/// <summary>
		/// From when contract is valid
		/// </summary>
		[ObservableProperty]
		private DateTime? from;

		/// <summary>
		/// To when contract is valid
		/// </summary>
		[ObservableProperty]
		private DateTime? to;

		/// <summary>
		/// Optional archiving time
		/// </summary>
		[ObservableProperty]
		private Duration? archivingOptional;

		/// <summary>
		/// Required archiving time
		/// </summary>
		[ObservableProperty]
		private Duration? archivingRequired;

		/// <summary>
		/// If contract can act as template
		/// </summary>
		[ObservableProperty]
		private bool? canActAsTemplate;

		/// <summary>
		/// ID of template used to create contract
		/// </summary>
		[ObservableProperty]
		private string? templateId;

		/// <summary>
		/// Content schema digest
		/// </summary>
		[ObservableProperty]
		private byte[]? contentSchemaDigest;

		/// <summary>
		/// Content schema Hash Function
		/// </summary>
		[ObservableProperty]
		private string? contentSchemaHashFunction;

		/// <summary>
		/// Local name of machine-readable part
		/// </summary>
		[ObservableProperty]
		private string? machineLocalName;

		/// <summary>
		/// Namespace of machine-readable part
		/// </summary>
		[ObservableProperty]
		private string? machineNamespace;

		/// <summary>
		/// Contains proposed role, if a proposal, null if not a proposal.
		/// </summary>
		[ObservableProperty]
		private string? role;

		/// <summary>
		/// If the view represents a proposal to sign a contract.
		/// </summary>
		[ObservableProperty]
		private bool isProposal;

		/// <summary>
		/// If the contract is a proposal
		/// </summary>
		[ObservableProperty]
		private string? proposal;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's roles.
		/// </summary>
		[ObservableProperty]
		private Grid? roles;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parts.
		/// </summary>
		[ObservableProperty]
		private Grid? parts;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parameters.
		/// </summary>
		[ObservableProperty]
		private Grid? parameters;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's human readable text section.
		/// </summary>
		[ObservableProperty]
		private VerticalStackLayout? humanReadableText;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's client signatures.
		/// </summary>
		[ObservableProperty]
		private Grid? clientSignatures;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's server signatures.
		/// </summary>
		[ObservableProperty]
		private Grid? serverSignatures;

		/// <summary>
		/// Gets the list of photos associated with the contract.
		/// </summary>
		public ObservableCollection<Photo> Photos { get; }

		/// <summary>
		/// The contract to display.
		/// </summary>
		public Contract? Contract { get; private set; }

		/// <summary>
		/// Gets or sets whether photos are available.
		/// </summary>
		[ObservableProperty]
		private bool hasPhotos;

		/// <summary>
		/// Gets or sets whether the contract has any roles to display.
		/// </summary>
		[ObservableProperty]
		private bool hasRoles;

		/// <summary>
		/// Gets or sets whether the contract has any contract parts to display.
		/// </summary>
		[ObservableProperty]
		private bool hasParts;

		/// <summary>
		/// Gets or sets whether the contract has any parameters to display.
		/// </summary>
		[ObservableProperty]
		private bool hasParameters;

		/// <summary>
		/// Gets or sets whether the contract has any human readable texts to display.
		/// </summary>
		[ObservableProperty]
		private bool hasHumanReadableText;

		/// <summary>
		/// Gets or sets whether the contract has any client signatures to display.
		/// </summary>
		[ObservableProperty]
		private bool hasClientSignatures;

		/// <summary>
		/// Gets or sets whether the contract has any server signatures to display.
		/// </summary>
		[ObservableProperty]
		private bool hasServerSignatures;

		/// <summary>
		/// Gets or sets whether a user can delete or obsolete a contract.
		/// </summary>
		[ObservableProperty]
		private bool canDeleteContract;

		/// <summary>
		/// Gets or sets whether a user can delete or obsolete a contract.
		/// </summary>
		[ObservableProperty]
		private bool canObsoleteContract;

		#endregion

		private void ClearContract()
		{
			this.photosLoader.CancelLoadPhotos();
			this.Contract = null;
			this.ContractId = null;
			this.State = null;
			this.Created = null;
			this.Updated = null;
			this.Visibility = null;
			this.Duration = null;
			this.From = null;
			this.To = null;
			this.ArchivingOptional = null;
			this.ArchivingRequired = null;
			this.CanActAsTemplate = null;
			this.TemplateId = null;
			this.ContentSchemaDigest = null;
			this.ContentSchemaHashFunction = null;
			this.MachineLocalName = null;
			this.MachineNamespace = null;
			this.Roles = null;
			this.Parts = null;
			this.Parameters = null;
			this.HumanReadableText = null;
			this.ClientSignatures = null;
			this.ServerSignatures = null;
			this.HasPhotos = false;
			this.HasRoles = false;
			this.HasParts = false;
			this.HasParameters = false;
			this.HasHumanReadableText = false;
			this.HasClientSignatures = false;
			this.HasServerSignatures = false;
			this.CanDeleteContract = false;
			this.CanObsoleteContract = false;

			this.RemoveQrCode();
		}

		private async Task DisplayContract()
		{
			if (this.Contract is null)
				return;

			try
			{
				bool HasSigned = false;
				bool AcceptsSignatures =
					(this.Contract is not null) &&
					(this.Contract.State == ContractState.Approved || this.Contract.State == ContractState.BeingSigned) &&
					(!this.Contract.SignAfter.HasValue || this.Contract.SignAfter.Value < DateTime.Now) &&
					(!this.Contract.SignBefore.HasValue || this.Contract.SignBefore.Value > DateTime.Now);
				Dictionary<string, int> NrSignatures = [];
				bool CanObsolete = false;

				this.ContractId = this.Contract!.ContractId;
				this.State = this.Contract.State;
				this.Created = this.Contract.Created;
				this.Updated = this.Contract.Updated == DateTime.MinValue ? null : this.Contract.Updated;
				this.Visibility = this.Contract.Visibility;
				this.Duration = this.Contract.Duration;
				this.From = this.Contract.From;
				this.To = this.Contract.To;
				this.ArchivingOptional = this.Contract.ArchiveOptional;
				this.ArchivingRequired = this.Contract.ArchiveRequired;
				this.CanActAsTemplate = this.Contract.CanActAsTemplate;
				this.TemplateId = this.Contract.TemplateId;
				this.ContentSchemaDigest = this.Contract.ContentSchemaDigest;
				this.ContentSchemaHashFunction = this.Contract.ContentSchemaHashFunction.ToString();
				this.MachineLocalName = this.Contract.ForMachinesLocalName;
				this.MachineNamespace = this.Contract.ForMachinesNamespace;

				if (this.Contract.ClientSignatures is not null)
				{
					foreach (ClientSignature Signature in this.Contract.ClientSignatures)
					{
						if (Signature.LegalId == ServiceRef.TagProfile.LegalIdentity!.Id)
							HasSigned = true;

						if (!NrSignatures.TryGetValue(Signature.Role, out int count))
							count = 0;

						NrSignatures[Signature.Role] = count + 1;

						if (string.Equals(Signature.BareJid, ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase))
						{
							if (this.Contract.Roles is not null)
							{
								foreach (Role Role in this.Contract.Roles)
								{
									if (Role.Name == Signature.Role)
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

				this.GenerateQrCode(Constants.UriSchemes.CreateSmartContractUri(this.Contract.ContractId));

				// Roles

				if (this.Contract.Roles is not null)
				{
					Grid RolesLayout = new()
					{
						ColumnDefinitions =
						[
							new ColumnDefinition(GridLength.Auto),
							new ColumnDefinition(GridLength.Star)
						]
					};

					foreach (Role Role in this.Contract.Roles)
					{
						string Html = await Role.ToHTML(this.Contract.DeviceLanguage(), this.Contract);
						Html = Waher.Content.Html.HtmlDocument.GetBody(Html);

						AddKeyValueLabelPair(RolesLayout, Role.Name, Html + GenerateMinMaxCountString(Role.MinCount, Role.MaxCount), true, string.Empty, null);

						if (!this.isReadOnly && AcceptsSignatures && !HasSigned && this.Contract.PartsMode == ContractParts.Open &&
							(!NrSignatures.TryGetValue(Role.Name, out int count) || count < Role.MaxCount) &&
							(!this.IsProposal || Role.Name == this.Role))
						{
							Button Button = new()
							{
								Text = ServiceRef.Localizer[nameof(AppResources.SignAsRole), Role.Name],
								StyleId = Role.Name
							};

							Button.Clicked += this.SignButton_Clicked;
							RolesLayout.Add(Button, 0, RolesLayout.RowDefinitions.Count);

							Grid.SetColumnSpan(Button, 2);
						}
					}

					this.Roles = RolesLayout;
				}

				// Parts

				Grid PartsLayout = new()
				{
					ColumnDefinitions =
					[
						new ColumnDefinition(GridLength.Auto),
						new ColumnDefinition(GridLength.Star)
					]
				};

				if (this.Contract.SignAfter.HasValue)
					AddKeyValueLabelPair(PartsLayout, ServiceRef.Localizer[nameof(AppResources.SignAfter)], this.Contract.SignAfter.Value.ToString(CultureInfo.CurrentCulture));

				if (this.Contract.SignBefore.HasValue)
					AddKeyValueLabelPair(PartsLayout, ServiceRef.Localizer[nameof(AppResources.SignBefore)], this.Contract.SignBefore.Value.ToString(CultureInfo.CurrentCulture));

				AddKeyValueLabelPair(PartsLayout, ServiceRef.Localizer[nameof(AppResources.Mode)], this.Contract.PartsMode.ToString());

				if (this.Contract.Parts is not null)
				{
					TapGestureRecognizer OpenLegalId = new();
					OpenLegalId.Tapped += this.Part_Tapped;

					foreach (Part Part in this.Contract.Parts)
					{
						AddKeyValueLabelPair(PartsLayout, Part.Role, Part.LegalId, false, OpenLegalId);

						if (!this.isReadOnly && AcceptsSignatures && !HasSigned && Part.LegalId == ServiceRef.TagProfile.LegalIdentity?.Id)
						{
							Button Button = new()
							{
								Text = ServiceRef.Localizer[nameof(AppResources.SignAsRole), Part.Role],
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
					Grid ParametersLayout = new()
					{
						ColumnDefinitions =
						[
							new ColumnDefinition(GridLength.Auto),
							new ColumnDefinition(GridLength.Star)
						]
					};

					foreach (Parameter Parameter in this.Contract.Parameters)
					{
						if (Parameter.ObjectValue is bool b)
							AddKeyValueLabelPair(ParametersLayout, Parameter.Name, b ? "✔" : "✗");
						else
							AddKeyValueLabelPair(ParametersLayout, Parameter.Name, Parameter.ObjectValue?.ToString() ?? string.Empty);
					}

					this.Parameters = ParametersLayout;
				}

				// Human readable text

				VerticalStackLayout HumanReadableTextLayout = [];
				string Xaml = await this.Contract.ToMauiXaml(this.Contract.DeviceLanguage());
				VerticalStackLayout HumanReadableXaml = new VerticalStackLayout().LoadFromXaml(Xaml);

				List<IView> Children = [.. HumanReadableXaml.Children];

				foreach (IView View in Children)
				{
					if (View is ContentView ContentView)
					{
						foreach (Element InnerView in ContentView.Children)
						{
							if (InnerView is Label Label)
								Label.TextColor = AppColors.PrimaryForeground;
						}
					}

					HumanReadableTextLayout.Children.Add(View);
				}

				this.HumanReadableText = HumanReadableTextLayout;

				// Client signatures
				if (this.Contract.ClientSignatures is not null)
				{
					Grid ClientSignaturesLayout = new()
					{
						ColumnDefinitions =
						[
							new ColumnDefinition(GridLength.Auto),
							new ColumnDefinition(GridLength.Star)
						]
					};
					TapGestureRecognizer openClientSignature = new();
					openClientSignature.Tapped += this.ClientSignature_Tapped;

					foreach (ClientSignature Signature in this.Contract.ClientSignatures)
					{
						string Sign = Convert.ToBase64String(Signature.DigitalSignature);
						StringBuilder sb = new();
						sb.Append(Signature.LegalId);
						sb.Append(", ");
						sb.Append(Signature.BareJid);
						sb.Append(", ");
						sb.Append(Signature.Timestamp.ToString(CultureInfo.CurrentCulture));
						sb.Append(", ");
						sb.Append(Sign);

						AddKeyValueLabelPair(ClientSignaturesLayout, Signature.Role, sb.ToString(), false, Sign, openClientSignature);
					}

					this.ClientSignatures = ClientSignaturesLayout;
				}

				// Server signature
				if (this.Contract.ServerSignature is not null)
				{
					Grid ServerSignaturesLayout = new()
					{
						ColumnDefinitions =
						[
							new ColumnDefinition(GridLength.Auto),
							new ColumnDefinition(GridLength.Star)
						]
					};

					TapGestureRecognizer openServerSignature = new();
					openServerSignature.Tapped += this.ServerSignature_Tapped;

					StringBuilder sb = new();
					sb.Append(this.Contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentCulture));
					sb.Append(", ");
					sb.Append(Convert.ToBase64String(this.Contract.ServerSignature.DigitalSignature));

					AddKeyValueLabelPair(ServerSignaturesLayout, this.Contract.Provider, sb.ToString(), false, this.Contract.ContractId, openServerSignature);
					this.ServerSignatures = ServerSignaturesLayout;
				}

				this.CanDeleteContract = !this.isReadOnly && !await this.Contract.IsLegallyBinding(true, ServiceRef.XmppService.ContractsClient);
				this.CanObsoleteContract = this.CanDeleteContract || CanObsolete;

				this.HasRoles = this.Roles?.Children.Count > 0;
				this.HasParts = this.Parts?.Children.Count > 0;
				this.HasParameters = this.Parameters?.Children.Count > 0;
				this.HasHumanReadableText = this.HumanReadableText?.Children.Count > 0;
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
				IEnumerable<KeyValuePair<string, object?>> Tags = this.GetClassAndMethod(MethodBase.GetCurrentMethod());
				Tags = Tags.Append(new KeyValuePair<string, object?>("ContractId", this.Contract?.ContractId ?? string.Empty));

				ServiceRef.LogService.LogException(ex, Tags.ToArray());

				this.ClearContract();
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private static string GenerateMinMaxCountString(int min, int max)
		{
			if (min == max)
			{
				if (max == 1)
					return string.Empty;

				return " (" + max.ToString(CultureInfo.InvariantCulture) + ")";
			}

			return " (" + min.ToString(CultureInfo.InvariantCulture) + " - " + max.ToString(CultureInfo.InvariantCulture) + ")";
		}

		private static void AddKeyValueLabelPair(Grid Container, string Key, string Value)
		{
			AddKeyValueLabelPair(Container, Key, Value, false, string.Empty, null);
		}

		private static void AddKeyValueLabelPair(Grid Container, string Key, string Value, bool IsHtml,
			TapGestureRecognizer TapGestureRecognizer)
		{
			AddKeyValueLabelPair(Container, Key, Value, IsHtml, Value, TapGestureRecognizer);
		}

		private static void AddKeyValueLabelPair(Grid Container, string Key,
			string Value, bool IsHtml, string StyleId, TapGestureRecognizer? TapGestureRecognizer)
		{
			int Row = Container.RowDefinitions.Count;

			Container.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

			Label KeyLabel = new()
			{
				Text = Key,
				Style = AppStyles.KeyLabel
			};

			Label ValueLabel = new()
			{
				Text = Value,
				TextType = IsHtml ? TextType.Html : TextType.Text,
				StyleId = StyleId,
				Style = IsHtml ? AppStyles.FormattedValueLabel : TapGestureRecognizer is null ? AppStyles.ValueLabel : AppStyles.ClickableValueLabel
			};

			Container.Add(KeyLabel, 0, Row);
			Container.Add(ValueLabel, 1, Row);

			if (TapGestureRecognizer is not null)
				ValueLabel.GestureRecognizers.Add(TapGestureRecognizer);
		}

		private async void SignButton_Clicked(object? Sender, EventArgs e)
		{
			if (this.Contract is null)
				return;

			try
			{
				if (Sender is Button Button && !string.IsNullOrEmpty(Button.StyleId))
				{
					string Role = Button.StyleId;

					if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToSignAs), Role]))
						return;

					if (!await App.AuthenticateUser(AuthenticationPurpose.SignContract, true))
						return;

					this.skipContractEvent = DateTime.Now;

					Contract Contract = await ServiceRef.XmppService.SignContract(this.Contract, Role, false);
					await this.ContractUpdated(Contract);

					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
						ServiceRef.Localizer[nameof(AppResources.ContractSuccessfullySigned)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async void Part_Tapped(object? Sender, EventArgs e)
		{
			try
			{
				if (Sender is Label Label && !string.IsNullOrEmpty(Label.StyleId))
				{
					await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(Label.StyleId,
						ServiceRef.Localizer[nameof(AppResources.PurposeReviewContract)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async void ContractId_Tapped(object? Sender, EventArgs e)
		{
			try
			{
				if (Sender is Label Label && !string.IsNullOrEmpty(Label.StyleId))
				{
					await ServiceRef.ContractOrchestratorService.OpenContract(Label.StyleId,
						ServiceRef.Localizer[nameof(AppResources.PurposeReviewContract)], null);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async void Link_Tapped(object? Sender, EventArgs e)
		{
			try
			{
				if (Sender is Label Label && !string.IsNullOrEmpty(Label.StyleId))
					await App.OpenUrlAsync(Label.StyleId);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async void CopyToClipboard_Tapped(object? Sender, EventArgs e)
		{
			try
			{
				if (Sender is Label Label && !string.IsNullOrEmpty(Label.StyleId))
				{
					await Clipboard.SetTextAsync(Label.StyleId);
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async void ClientSignature_Tapped(object? Sender, EventArgs e)
		{
			try
			{
				if (Sender is Label Label && !string.IsNullOrEmpty(Label.StyleId))
				{
					string sign = Label.StyleId;
					ClientSignature? signature = this.Contract?.ClientSignatures.FirstOrDefault(x => sign == Convert.ToBase64String(x.DigitalSignature));
					if (signature is not null)
					{
						string legalId = signature.LegalId;
						LegalIdentity identity = await ServiceRef.XmppService.GetLegalIdentity(legalId);

						await ServiceRef.UiService.GoToAsync(nameof(ClientSignaturePage),
							new ClientSignatureNavigationArgs(signature, identity));
					}
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private async void ServerSignature_Tapped(object? Sender, EventArgs e)
		{
			try
			{
				if (Sender is Label Label && !string.IsNullOrEmpty(Label.StyleId))
				{
					await ServiceRef.UiService.GoToAsync(nameof(ServerSignaturePage),
						  new ServerSignatureNavigationArgs(this.Contract));
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// The command to bind to when marking a contract as obsolete.
		/// </summary>
		[RelayCommand]
		private async Task ObsoleteContract()
		{
			if (this.Contract is null)
				return;

			try
			{
				if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToObsoleteContract)]))
					return;

				if (!await App.AuthenticateUser(AuthenticationPurpose.ObsoleteContract, true))
					return;

				this.skipContractEvent = DateTime.Now;

				Contract obsoletedContract = await ServiceRef.XmppService.ObsoleteContract(this.Contract.ContractId);
				await this.ContractUpdated(obsoletedContract);

				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.ContractHasBeenObsoleted)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// The command to bind to when deleting a contract.
		/// </summary>
		[RelayCommand]
		private async Task DeleteContract()
		{
			if (this.Contract is null)
				return;

			try
			{
				if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToDeleteContract)]))
					return;

				if (!await App.AuthenticateUser(AuthenticationPurpose.DeleteContract, true))
					return;

				this.skipContractEvent = DateTime.Now;

				Contract deletedContract = await ServiceRef.XmppService.DeleteContract(this.Contract.ContractId);
				await this.ContractUpdated(deletedContract);

				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.ContractHasBeenDeleted)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Command to show machine-readable details of contract.
		/// </summary>
		[RelayCommand]
		private async Task ShowDetails()
		{
			if (this.Contract is null)
				return;

			try
			{
				byte[] Bin = Encoding.UTF8.GetBytes(this.Contract.ForMachines.OuterXml);
				HttpFileUploadEventArgs e = await ServiceRef.XmppService.RequestUploadSlotAsync(this.Contract.ContractId + ".xml", "text/xml; charset=utf-8", Bin.Length);

				if (e.Ok)
				{
					await e.PUT(Bin, "text/xml", (int)Constants.Timeouts.UploadFile.TotalMilliseconds);
					if (!await App.OpenUrlAsync(e.GetUrl, false))
						await this.Copy(e.GetUrl);
				}
				else
					await ServiceRef.UiService.DisplayException(e.StanzaError ?? new Exception(e.ErrorText));
			}
			catch (Exception ex)
			{
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		/// <summary>
		/// Copies Item to clipboard
		/// </summary>
		[RelayCommand]
		private async Task Copy(object Item)
		{
			try
			{
				this.SetIsBusy(true);

				if (Item is string Label)
				{
					if (Label == this.ContractId)
					{
						await Clipboard.SetTextAsync(Constants.UriSchemes.IotSc + ":" + this.ContractId);
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
							ServiceRef.Localizer[nameof(AppResources.ContractIdCopiedSuccessfully)]);
					}
					else
					{
						await Clipboard.SetTextAsync(Label);
						await ServiceRef.UiService.DisplayAlert(
							ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
							ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
					}
				}
				else
				{
					await Clipboard.SetTextAsync(Item?.ToString() ?? string.Empty);
					await ServiceRef.UiService.DisplayAlert(
						ServiceRef.Localizer[nameof(AppResources.SuccessTitle)],
						ServiceRef.Localizer[nameof(AppResources.TagValueCopiedToClipboard)]);
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		/// <summary>
		/// Opens a contract.
		/// </summary>
		/// <param name="Item">Item clicked.</param>
		[RelayCommand]
		private static async Task OpenContract(object Item)
		{
			if (Item is string ContractId)
				await App.OpenUrlAsync(Constants.UriSchemes.IotSc + ":" + ContractId);
		}

		/// <summary>
		/// Opens a link.
		/// </summary>
		/// <param name="Item">Item clicked.</param>
		[RelayCommand]
		private async Task OpenLink(object Item)
		{
			if (Item is string Url)
			{
				if (!await App.OpenUrlAsync(Url, false))
					await this.Copy(Url);
			}
			else
				await this.Copy(Item);
		}

		#region ILinkableView

		/// <summary>
		/// Link to the current view
		/// </summary>
		public override string? Link { get; }

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => ContractModel.GetName(this.Contract);

		#endregion

	}
}
