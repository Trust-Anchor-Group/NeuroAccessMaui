using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Contracts;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Signatures.ClientSignature;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence;
using Waher.Persistence.Filters;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	public partial class ObservablePart : ObservableObject, IDisposable
	{
		public ObservablePart(Part part)
		{
			this.Part = part;
		}

		public ObservablePart(Part part, ClientSignature signature)
		{
			this.Part = part;
			this.Signature = signature;
		}

		/// <summary>
		/// Initializes the part, setting properties which needs to be set asynchronosly. No identity petition is sent. When the
		/// identity is protected, the user can request it with <see cref="RequestIdentityCommand"/>.
		/// </summary>
		/// <param name="PreparedIdentity">The identity already looked up by the scanner, or <c>null</c> to look it up here.</param>
		/// <returns>A task representing part initialization.</returns>
		public async Task InitializeAsync(IdentityReference? PreparedIdentity = null)
		{
			try
			{
				// Set Friendly name before anything can go wrong
				this.FriendlyName = await this.GetFriendlyNameAsync();

				LegalIdentity? Identity = this.identity;

				if (Identity is null)
				{
					if (this.IsMe)
						Identity = ServiceRef.TagProfile.LegalIdentity;
					else if (PreparedIdentity is not null)
						Identity = PreparedIdentity.Identity;
					else
						Identity = await ServiceRef.ContractOrchestratorService.TryGetLegalIdentity(this.LegalId, string.Empty);
				}

				// A protected identity may still be known from the contact list, or at least the contact's address.
				bool HasContactAddress = false;
				if (Identity is null)
				{
					ContactInfo? Info = await ContactInfo.FindByLegalId(this.LegalId);
					Identity = Info?.LegalIdentity;
					HasContactAddress = !string.IsNullOrWhiteSpace(Info?.BareJid);
				}

				this.identity = Identity;
				string FriendlyName = await this.GetFriendlyNameAsync();

				// A request sent from another page, such as the one that created the contract, is still shown as sent.
				bool Requested = Identity is null && this.IsThirdParty &&
					ServiceRef.ContractOrchestratorService.IsIdentityRequestPending(this.LegalId);

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.FriendlyName = FriendlyName;
					this.hasContactAddress = HasContactAddress;

					if (this.identity is not null)
						this.IdentityState = PartIdentityState.Visible;
					else if (Requested)
						this.IdentityState = PartIdentityState.Requested;
					else if (this.IdentityState == PartIdentityState.Visible)
						this.IdentityState = PartIdentityState.NotVisible;
					else
						this.NotifyIdentityChanged();
				});
			}
			catch (Exception E)
			{
				ServiceRef.LogService.LogException(E);
			}
		}

		/// <summary>
		/// Shows the answer to an identity request on this part.
		/// </summary>
		/// <param name="e">The answer.</param>
		/// <returns>If the answer was for this part.</returns>
		public async Task<bool> ApplyIdentityAnswerAsync(IdentityPetitionAnsweredEventArgs e)
		{
			if (!string.Equals(e.LegalId, this.LegalId, StringComparison.OrdinalIgnoreCase))
				return false;

			if (e.Accepted && e.Identity is not null)
				this.identity = e.Identity;

			string FriendlyName = await this.GetFriendlyNameAsync();

			MainThread.BeginInvokeOnMainThread(() =>
			{
				this.FriendlyName = FriendlyName;

				if (this.identity is not null)
					this.IdentityState = PartIdentityState.Visible;
				else
					this.IdentityState = e.Accepted ? PartIdentityState.NotVisible : PartIdentityState.Declined;
			});

			return true;
		}

		private void NotifyIdentityChanged()
		{
			this.OnPropertyChanged(nameof(this.HasIdentity));
			this.OnPropertyChanged(nameof(this.HasSentPetition));
			this.OnPropertyChanged(nameof(this.IsIdentityRequested));
			this.OnPropertyChanged(nameof(this.IsIdentityDeclined));
			this.OnPropertyChanged(nameof(this.CanRequestIdentity));
			this.OnPropertyChanged(nameof(this.CanRequestIdentityToInvite));
			this.OnPropertyChanged(nameof(this.CanSendProposal));
		}

		private async Task<string> GetFriendlyNameAsync()
		{
			try
			{
				if (this.IsMe)
					return ServiceRef.Localizer[nameof(AppResources.Me)];

				if (this.Part.LegalId == ServiceRef.TagProfile.TrustProviderId && !string.IsNullOrEmpty(ServiceRef.TagProfile.Domain))
					return ServiceRef.TagProfile.Domain;

				ContactInfo Info = await Database.FindFirstIgnoreRest<ContactInfo>(new FilterFieldEqualTo("LegalId", this.Part.LegalId));
				if (Info is not null && !string.IsNullOrEmpty(Info.FriendlyName))
					return Info.FriendlyName;

				if (this.identity is not null)
					return ContactInfo.GetFriendlyName(this.identity);

				if (Info is not null && !string.IsNullOrEmpty(Info.BareJid))
					return Info.BareJid;

				if (this.Signature is not null)
					return this.Signature.BareJid;
			}
			catch (Exception E)
			{
				ServiceRef.LogService.LogException(E);
				//log and use fallback
			}

			return this.LegalId;
		}
		#region Properties
		private LegalIdentity? identity;
		private bool hasContactAddress;

		/// <summary>
		/// The wrapped Part object
		/// </summary>
		public Part Part { get; }

		private ClientSignature? signature = null;
		public ClientSignature? Signature
		{
			get => this.signature;
			set
			{
				this.SetProperty(ref this.signature, value);
				this.OnPropertyChanged(nameof(this.HasSigned));
				this.OnPropertyChanged(nameof(this.CanViewSignature));
				this.OnPropertyChanged(nameof(this.CanSendProposal));
				this.OnPropertyChanged(nameof(this.CanRequestIdentityToInvite));
				this.OpenSignatureCommand.NotifyCanExecuteChanged();
			}
		}

		public string LegalId => this.Part.LegalId;


		public string Role => this.Part.Role;

		/// <summary>
		/// The friendly name for the part
		/// Has to be initialized with <see cref="InitializeAsync"/>
		/// </summary>
		public string? FriendlyName
		{
			get => this.friendlyName;
			private set
			{
				this.SetProperty(ref this.friendlyName, value);
			}
		}
		private string? friendlyName = string.Empty;

		/// <summary>
		/// If the user can see the part's identity, has asked to see it, or was refused.
		/// </summary>
		public PartIdentityState IdentityState
		{
			get => this.identityState;
			private set
			{
				if (this.SetProperty(ref this.identityState, value))
					this.NotifyIdentityChanged();
			}
		}
		private PartIdentityState identityState = PartIdentityState.NotVisible;

		/// <summary>
		/// The name of the contract, used in the purpose of an identity request.
		/// </summary>
		public string? ContractName { get; set; }

		/// <summary>
		/// If an identity request has been sent and not yet answered.
		/// </summary>
		public bool HasSentPetition => this.IsIdentityRequested;

		/// <summary>
		/// If an identity request has been sent and not yet answered.
		/// </summary>
		public bool IsIdentityRequested => this.IdentityState == PartIdentityState.Requested && !this.HasIdentity;

		/// <summary>
		/// If the owner declined to show their identity.
		/// </summary>
		public bool IsIdentityDeclined => this.IdentityState == PartIdentityState.Declined && !this.HasIdentity;

		/// <summary>
		/// If "Request identity" is offered for this part.
		/// </summary>
		public bool CanRequestIdentity => this.IsThirdParty && !this.HasIdentity && this.IdentityState != PartIdentityState.Requested;

		public bool IsMe => this.Part.LegalId == ServiceRef.TagProfile.LegalIdentity?.Id;
		public bool IsThirdParty => !this.IsMe;

		public bool HasIdentity => this.identity is not null;

		public bool HasSigned => this.Signature is not null;

		/// <summary>
		/// If "View signature" is offered for this part. Follows <see cref="ContractActionRules.CanViewSignature"/>.
		/// </summary>
		public bool CanViewSignature => ContractActionRules.CanViewSignature(this.Signature);

		public bool CanSendProposal => this.IsThirdParty && this.HasIdentity && !this.HasSigned && this.Role != "TrustProvider";

		/// <summary>
		/// If the user is offered to request the identity, since the part can't be invited without it. Contacts can be invited
		/// at their address, and parts set by the caller and the trust provider aren't invited this way.
		/// </summary>
		public bool CanRequestIdentityToInvite => this.CanRequestIdentity && !this.hasContactAddress && !this.HasSigned &&
			!this.IsPresetFromArgs && this.Role != "TrustProvider";

		/// <summary>
		/// True if this part was preselected via navigation args and therefore should not be removable in the Roles view.
		/// </summary>
		public bool IsPresetFromArgs
		{
			get => this.isPresetFromArgs;
			set
			{
				if (this.SetProperty(ref this.isPresetFromArgs, value))
				{
					this.OnPropertyChanged(nameof(this.ShowRemoveAsMe));
					this.OnPropertyChanged(nameof(this.ShowRemoveAsThirdParty));
					this.OnPropertyChanged(nameof(this.CanRequestIdentityToInvite));
				}
			}
		}
		private bool isPresetFromArgs = false;

		/// <summary>
		/// Helper property for XAML: show the remove cross for "me" parts if not preset from args.
		/// </summary>
		public bool ShowRemoveAsMe => this.IsMe && !this.IsPresetFromArgs;

		/// <summary>
		/// Helper property for XAML: show the remove cross for third-party parts if not preset from args.
		/// </summary>
		public bool ShowRemoveAsThirdParty => this.IsThirdParty && !this.IsPresetFromArgs;

		#endregion

		/// <summary>
		/// Gets the messaging address from the participant's loaded legal identity.
		/// </summary>
		/// <returns>The identity's JID, or an empty string if it is unavailable.</returns>
		public string GetJid()
		{
			return this.identity?.GetJid() ?? string.Empty;
		}


		#region Commands
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenIdentityAsync()
		{
			if (this.identity is not null)
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(this.identity), Services.UI.BackMethod.Pop);
				return;
			}

			if (!this.CanRequestIdentity)
				return;

			bool Request = await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.IdentityNotVisible)],
				ServiceRef.Localizer[nameof(AppResources.IdentityNotVisibleAskRequest), this.FriendlyName ?? this.LegalId],
				ServiceRef.Localizer[nameof(AppResources.RequestIdentity)],
				ServiceRef.Localizer[nameof(AppResources.Cancel)]);

			if (Request)
				await this.RequestIdentityAsync();
		}

		/// <summary>
		/// Asks the owner of the part's identity to show it, and tells the user it was sent. The answer updates the part when it arrives.
		/// </summary>
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task RequestIdentityAsync()
		{
			if (!await this.SendIdentityRequestAsync())
				return;

			await ServiceRef.UiService.DisplayAlert(
				ServiceRef.Localizer[nameof(AppResources.Petition)],
				ServiceRef.Localizer[nameof(AppResources.PetitionIdentitySent)],
				ServiceRef.Localizer[nameof(AppResources.Ok)]);
		}

		/// <summary>
		/// Asks the owner of the part's identity to show it. The answer updates the part when it arrives.
		/// </summary>
		/// <returns>If the request was sent. If not, the user has been told why.</returns>
		public async Task<bool> SendIdentityRequestAsync()
		{
			if (!this.CanRequestIdentity)
				return false;

			string Purpose = string.IsNullOrEmpty(this.ContractName)
				? ServiceRef.Localizer[nameof(AppResources.ForInclusionInContract)]
				: ServiceRef.Localizer[nameof(AppResources.IdentityRequestPurpose), this.ContractName];

			string? PetitionId = await ServiceRef.ContractOrchestratorService.RequestIdentityAsync(this.LegalId, Purpose);
			if (PetitionId is null)
				return false;

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.IdentityState = PartIdentityState.Requested;
			});

			return true;
		}

		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(HasSigned))]
		private async Task OpenSignatureAsync()
		{
			if (this.Signature is not null)
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ClientSignaturePage),
					new ClientSignatureNavigationArgs(this.Signature, this.identity),
					Services.UI.BackMethod.Pop);
			}
		}
		#endregion

		private bool disposed = false;

		#region IDisposable Support
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
				return;

			this.disposed = true;
		}
		#endregion
	}
}
