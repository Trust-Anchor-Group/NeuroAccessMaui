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
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (ServiceRef.UiService.TryGetArgs(out ViewContractNavigationArgs? args))
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
		private string? localName;

		/// <summary>
		/// Namespace of machine-readable part
		/// </summary>
		[ObservableProperty]
		private string? @namespace;

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
		private VerticalStackLayout? roles;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's parts.
		/// </summary>
		[ObservableProperty]
		private VerticalStackLayout? parts;

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
		/// Holds Xaml code for visually representing a contract's client signatures.
		/// </summary>
		[ObservableProperty]
		private VerticalStackLayout? clientSignatures;

		/// <summary>
		/// Holds Xaml code for visually representing a contract's server signatures.
		/// </summary>
		[ObservableProperty]
		private VerticalStackLayout? serverSignatures;

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
			this.LocalName = null;
			this.Namespace = null;
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
				this.LocalName = this.Contract.ForMachinesLocalName;
				this.Namespace = this.Contract.ForMachinesNamespace;

				if (this.Contract.ClientSignatures is not null)
				{
					foreach (ClientSignature signature in this.Contract.ClientSignatures)
					{
						if (signature.LegalId == ServiceRef.TagProfile.LegalIdentity!.Id)
							HasSigned = true;

						if (!NrSignatures.TryGetValue(signature.Role, out int count))
							count = 0;

						NrSignatures[signature.Role] = count + 1;

						if (string.Equals(signature.BareJid, ServiceRef.XmppService.BareJid, StringComparison.OrdinalIgnoreCase))
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

				this.GenerateQrCode(Constants.UriSchemes.CreateSmartContractUri(this.Contract.ContractId));

				// Roles

				if (this.Contract.Roles is not null)
				{
					VerticalStackLayout RolesLayout = [];

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
								Text = ServiceRef.Localizer[nameof(AppResources.SignAsRole), Role.Name],
								StyleId = Role.Name
							};

							button.Clicked += this.SignButton_Clicked;
							RolesLayout.Children.Add(button);
						}
					}

					this.Roles = RolesLayout;
				}

				// Parts

				VerticalStackLayout PartsLayout = [];

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
					VerticalStackLayout ParametersLayout = [];

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
					VerticalStackLayout clientSignaturesLayout = [];
					TapGestureRecognizer openClientSignature = new();
					openClientSignature.Tapped += this.ClientSignature_Tapped;

					foreach (ClientSignature signature in this.Contract.ClientSignatures)
					{
						string Sign = Convert.ToBase64String(signature.DigitalSignature);
						StringBuilder sb = new();
						sb.Append(signature.LegalId);
						sb.Append(", ");
						sb.Append(signature.BareJid);
						sb.Append(", ");
						sb.Append(signature.Timestamp.ToString(CultureInfo.CurrentCulture));
						sb.Append(", ");
						sb.Append(Sign);

						AddKeyValueLabelPair(clientSignaturesLayout, signature.Role, sb.ToString(), false, Sign, openClientSignature);
					}

					this.ClientSignatures = clientSignaturesLayout;
				}

				// Server signature
				if (this.Contract.ServerSignature is not null)
				{
					VerticalStackLayout serverSignaturesLayout = [];

					TapGestureRecognizer openServerSignature = new();
					openServerSignature.Tapped += this.ServerSignature_Tapped;

					StringBuilder sb = new();
					sb.Append(this.Contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentCulture));
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

		private static void AddKeyValueLabelPair(VerticalStackLayout Container, string Key, string Value)
		{
			AddKeyValueLabelPair(Container, Key, Value, false, string.Empty, null);
		}

		private static void AddKeyValueLabelPair(VerticalStackLayout Container, string Key, string Value, bool IsHtml,
			TapGestureRecognizer TapGestureRecognizer)
		{
			AddKeyValueLabelPair(Container, Key, Value, IsHtml, Value, TapGestureRecognizer);
		}

		private static void AddKeyValueLabelPair(VerticalStackLayout Container, string Key,
			string Value, bool IsHtml, string StyleId, TapGestureRecognizer? TapGestureRecognizer)
		{
			HorizontalStackLayout layout = new()
			{
				StyleId = StyleId
			};

			Container.Children.Add(layout);

			layout.Children.Add(new Label
			{
				Text = Key + ":",
				Style = AppStyles.KeyLabel
			});

			layout.Children.Add(new Label
			{
				Text = Value,
				TextType = IsHtml ? TextType.Html : TextType.Text,
				Style = IsHtml ? AppStyles.FormattedValueLabel : TapGestureRecognizer is null ? AppStyles.ValueLabel : AppStyles.ClickableValueLabel
			});

			if (TapGestureRecognizer is not null)
				layout.GestureRecognizers.Add(TapGestureRecognizer);
		}

		private async void SignButton_Clicked(object? Sender, EventArgs e)
		{
			if (this.Contract is null)
				return;

			try
			{
				if (!await App.AuthenticateUser(true))
					return;

				if (Sender is Button button && !string.IsNullOrEmpty(button.StyleId))
				{
					this.skipContractEvent = DateTime.Now;

					Contract contract = await ServiceRef.XmppService.SignContract(this.Contract, button.StyleId, false);
					await this.ContractUpdated(contract);

					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.SuccessTitle)], ServiceRef.Localizer[nameof(AppResources.ContractSuccessfullySigned)]);
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
				if (Sender is VerticalStackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
				{
					await ServiceRef.ContractOrchestratorService.OpenLegalIdentity(Layout.StyleId,
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
				if (Sender is VerticalStackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
				{
					await ServiceRef.ContractOrchestratorService.OpenContract(Layout.StyleId,
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
				if (Sender is VerticalStackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
					await App.OpenUrlAsync(Layout.StyleId);
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
				if (Sender is VerticalStackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
				{
					await Clipboard.SetTextAsync(Layout.StyleId);
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
				if (Sender is VerticalStackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
				{
					string sign = layout.StyleId;
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
				if (Sender is VerticalStackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
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
				if (!await App.AuthenticateUser(true))
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
				if (!await App.AuthenticateUser(true))
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
					await App.OpenUrlAsync(e.GetUrl);
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
		private async Task OpenContract(object Item)
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
				await App.OpenUrlAsync(Url);
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
