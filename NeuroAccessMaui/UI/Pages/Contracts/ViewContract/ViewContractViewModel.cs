﻿using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
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

		[ObservableProperty]
		private bool canStateChange;

		[ObservableProperty]
		private string currentState = nameof(NewContractStep.Loading);

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasHumanReadableText))]
		private VerticalStackLayout? humanReadableText;

		/// <summary>
		/// If HumanReadableText is not empty
		/// </summary>
		public bool HasHumanReadableText => this.HumanReadableText is not null;

		public ObservableCollection<ObservableParameter> DisplayableParameters { get; set; } = [];

		/// <summary>
		/// If the user has reviewed the contract and sees it as valid.
		/// </summary>
		[ObservableProperty]
		private bool isContractOk;


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
			if (this.args is null)
			{
				// TODO: Handle error, perhaps change to an error state
				return;
			}



			await base.OnInitialize();
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
