using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.UI.Photos;
using NeuroAccessMaui.UI.Converters;
using NeuroAccessMaui.UI.Pages.Contracts.MyContracts.ObjectModels;
using NeuroAccessMaui.UI.Pages.Contracts.NewContract;
using NeuroAccessMaui.UI.Pages.Contracts.ObjectModel;
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
		[ObservableProperty]
		private ObservableContract? contract;

		/// <summary>
		/// The state object containing all views. Is set by the view.
		/// </summary>
		public BindableObject? StateObject { get; set; }

		/// <summary>
		/// If the view can change state.
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(GoToParametersCommand))]
		[NotifyCanExecuteChangedFor(nameof(BackCommand))]
		private bool canStateChange;

		/// <summary>
		/// The current state of the view.
		/// </summary>
		[ObservableProperty]
		private string currentState = nameof(NewContractStep.Loading);

		/// <summary>
		/// The human readable text of the contract.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasHumanReadableText))]
		private VerticalStackLayout? humanReadableText;

		/// <summary>
		/// If HumanReadableText is not empty
		/// </summary>
		public bool HasHumanReadableText => this.HumanReadableText is not null;

		/// <summary>
		/// All parameters that can be displayed / Are supported.
		/// </summary>
		public ObservableCollection<ObservableParameter> DisplayableParameters { get; set; } = [];

		/// <summary>
		/// If the user has reviewed the contract and sees it as valid.
		/// </summary>
		[ObservableProperty]
		private bool isContractOk;

		/// <summary>
		/// If the user can sign the contract.
		/// </summary>
		[ObservableProperty]
		private bool canSign;

		/// <summary>
		/// The friednly name of the proposal sender.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasProposalFriendlyName))]
		[NotifyPropertyChangedFor(nameof(IsProposal))]
		private string? proposalFriendlyName;

		/// <summary>
		/// The role of the proposal.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasProposalRole))]
		private string? proposalRole;

		/// <summary>
		/// The message of the proposal.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasProposalMessage))]
		[NotifyPropertyChangedFor(nameof(IsProposal))]
		private string? proposalMessage;

		/// <summary>
		/// If we are viewing the contract as a proposal.
		/// </summary>
		public bool IsProposal => !string.IsNullOrEmpty(this.ProposalRole) || !string.IsNullOrEmpty(this.ProposalMessage) || !string.IsNullOrEmpty(this.ProposalFriendlyName);

		/// <summary>
		/// If the proposal sender has a friendly name.
		/// </summary>
		public bool HasProposalFriendlyName => !string.IsNullOrEmpty(this.ProposalFriendlyName);

		/// <summary>
		/// If the proposal has a role.
		/// </summary>
		public bool HasProposalRole => !string.IsNullOrEmpty(this.ProposalRole);

		/// <summary>
		/// If the proposal has a message.
		/// </summary>
		public bool HasProposalMessage => !string.IsNullOrEmpty(this.ProposalMessage);

		public string? Visibility => this.Contract?.Visibility switch
		{
			ContractVisibility.Public => ServiceRef.Localizer[nameof(AppResources.ContractVisibility_Public)],
			ContractVisibility.CreatorAndParts => ServiceRef.Localizer[nameof(AppResources.ContractVisibility_CreatorAndParts)],
			ContractVisibility.DomainAndParts => ServiceRef.Localizer[nameof(AppResources.ContractVisibility_DomainAndParts)],
			ContractVisibility.PublicSearchable => ServiceRef.Localizer[nameof(AppResources.ContractVisibility_PublicSearchable)],
			_ => string.Empty
		};


		private readonly ViewContractNavigationArgs? args;

		/// <summary>
		/// Creates an instance of the <see cref="ViewContractViewModel"/> class.
		/// </summary>
		/// <param name="Args">Navigation arguments.</param>
		public ViewContractViewModel()
		{
			this.args = ServiceRef.UiService.PopLatestArgs<ViewContractNavigationArgs>();
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			if (this.args is null || this.args.Contract is null)
			{
				// TODO: Handle error, perhaps change to an error state
				return;
			}

			try
			{
				ObservableContract Contract = await ObservableContract.CreateAsync(this.args.Contract);
				string ProposalFriendlyName = string.Empty;
				// If we have a proposal, try to find the friendly name of the sender and prepare observable properties
				if (!string.IsNullOrEmpty(this.args.Proposal) && !string.IsNullOrEmpty(this.args.FromJID))
				{
					try
					{
						ContactInfo? info = await ContactInfo.FindByBareJid(this.args.FromJID);
						if (!string.IsNullOrEmpty(info?.FriendlyName))
							ProposalFriendlyName = info.FriendlyName;
						else
							ProposalFriendlyName = this.args.FromJID;
					}
					catch (Exception ex)
					{
						//Ignore, normal opereration
					}
				}


				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.Contract = Contract;
					this.GenerateQrCode(Constants.UriSchemes.CreateSmartContractUri(this.Contract.ContractId));
					this.ProposalFriendlyName = ProposalFriendlyName;
					this.ProposalRole = this.args.Role ?? string.Empty;
					this.ProposalMessage = this.args.Proposal ?? string.Empty;
				});

				// Prepare displayable parameters
				foreach (ObservableParameter p in this.Contract!.Parameters)
				{
					if (p.Parameter is null)
						continue;

					if (p.Parameter is BooleanParameter
						|| p.Parameter is StringParameter
						|| p.Parameter is NumericalParameter
						|| p.Parameter is DateParameter
						|| p.Parameter is TimeParameter
						|| p.Parameter is DurationParameter
						|| p.Parameter is DateTimeParameter
						|| p.Parameter is CalcParameter)
					{
						this.DisplayableParameters.Add(p);
					}
				}

				await this.GoToState(ViewContractStep.Overview);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)]);
				await this.GoBack();
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{

			await base.OnDispose();
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
				ViewContractStep currentStep = (ViewContractStep)Enum.Parse(typeof(ViewContractStep), this.CurrentState);

				switch (currentStep)
				{
					case ViewContractStep.Loading:
					case ViewContractStep.Overview:
						await base.GoBack();
						break;
					default:
						await this.GoToState(ViewContractStep.Overview);
						break;
				}
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Navigates to the specified state.
		/// Can only navigate when <see cref="CanStateChange"/> is true.
		/// Otherwise it stalls until it can navigate.
		/// </summary>
		/// <param name="newStep">The new step to navigate to.</param>
		private async Task GoToState(ViewContractStep newStep)
		{
			if (this.StateObject is null)
				return;

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
		/// Navigates to the parameters view
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToParameters()
		{
			await this.GoToState(ViewContractStep.Loading);
			await this.GoToState(ViewContractStep.Parameters);
		}

		/// <summary>
		/// Navigates to the parameters view
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanStateChange))]
		private async Task GoToRoles()
		{
			await this.GoToState(ViewContractStep.Loading);
			await this.GoToState(ViewContractStep.Roles);
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

				Contract obsoletedContract = await ServiceRef.XmppService.ObsoleteContract(this.Contract.ContractId);

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

				Contract deletedContract = await ServiceRef.XmppService.DeleteContract(this.Contract.ContractId);

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
				byte[] Bin = Encoding.UTF8.GetBytes(this.Contract?.Contract.ForMachines.OuterXml);
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
					if (Label == this.Contract?.ContractId)
					{
						await Clipboard.SetTextAsync(Constants.UriSchemes.IotSc + ":" + this.Contract?.ContractId);
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
		public override Task<string> Title => ContractModel.GetName(this.Contract?.Contract);

		#endregion

	}
}
