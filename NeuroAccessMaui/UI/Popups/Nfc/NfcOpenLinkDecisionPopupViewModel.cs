using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Popups.Nfc
{
	/// <summary>
	/// Decision returned from <see cref="NfcOpenLinkDecisionPopup"/>.
	/// </summary>
	public enum NfcOpenLinkDecision
	{
		/// <summary>
		/// No decision (dismissed).
		/// </summary>
		None = 0,

		/// <summary>
		/// Open the link once.
		/// </summary>
		OpenOnce = 1,

		/// <summary>
		/// Trust the domain and open the link.
		/// </summary>
		TrustAndOpen = 2,

		/// <summary>
		/// Copy the link to clipboard.
		/// </summary>
		CopyLink = 3,

		/// <summary>
		/// Cancel.
		/// </summary>
		Cancel = 4
	}

	/// <summary>
	/// View model for <see cref="NfcOpenLinkDecisionPopup"/>.
	/// </summary>
	public partial class NfcOpenLinkDecisionPopupViewModel : ReturningPopupViewModel<NfcOpenLinkDecision>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NfcOpenLinkDecisionPopupViewModel"/> class.
		/// </summary>
		/// <param name="Uri">The URI being considered.</param>
		/// <param name="Warnings">Any Safe Scan warnings for this URI.</param>
		/// <param name="CanTrustDomain">If the domain can be trusted from this popup.</param>
		public NfcOpenLinkDecisionPopupViewModel(string Uri, IEnumerable<string>? Warnings, bool CanTrustDomain)
			: base()
		{
			this.Uri = Uri ?? string.Empty;
			this.CanTrustDomain = CanTrustDomain;
			this.Warnings = new ObservableCollection<string>(Warnings ?? Array.Empty<string>());
			this.HasWarnings = this.Warnings.Count > 0;
		}

		/// <summary>
		/// Gets the URI.
		/// </summary>
		[ObservableProperty]
		private string uri;

		/// <summary>
		/// Gets the warnings.
		/// </summary>
		[ObservableProperty]
		private ObservableCollection<string> warnings;

		/// <summary>
		/// Gets a value indicating whether warnings exist.
		/// </summary>
		[ObservableProperty]
		private bool hasWarnings;

		/// <summary>
		/// Gets a value indicating whether the domain can be trusted from this popup.
		/// </summary>
		[ObservableProperty]
		private bool canTrustDomain;

		/// <summary>
		/// Opens the link once.
		/// </summary>
		[RelayCommand]
		private async Task OpenOnceAsync()
		{
			this.TrySetResult(NfcOpenLinkDecision.OpenOnce);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Trusts the domain and opens the link.
		/// </summary>
		[RelayCommand]
		private async Task TrustAndOpenAsync()
		{
			this.TrySetResult(NfcOpenLinkDecision.TrustAndOpen);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Copies the link to clipboard.
		/// </summary>
		[RelayCommand]
		private async Task CopyUriAsync()
		{
			string Candidate = this.Uri?.Trim() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(Candidate))
				await Clipboard.Default.SetTextAsync(Candidate);

			this.TrySetResult(NfcOpenLinkDecision.CopyLink);
			await ServiceRef.PopupService.PopAsync();
		}

		/// <summary>
		/// Cancels the popup.
		/// </summary>
		[RelayCommand]
		private async Task CancelAsync()
		{
			this.TrySetResult(NfcOpenLinkDecision.Cancel);
			await ServiceRef.PopupService.PopAsync();
		}
	}
}
