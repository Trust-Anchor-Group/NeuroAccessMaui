using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Contacts;
using NeuroAccessMaui.Services.Notification;
using NeuroAccessMaui.Services.Notification.Identities;
using NeuroAccessMaui.UI.MVVM;
using NeuroAccessMaui.UI.MVVM.Building;
using NeuroAccessMaui.UI.MVVM.Policies;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using NeuroAccessMaui.UI.Pages.Signatures.ClientSignature;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Persistence;
using Waher.Persistence.Filters;

namespace NeuroAccessMaui.UI.Pages.Contracts.ObjectModel
{
	public partial class ObservablePart : ObservableObject, IDisposable
	{
		private static readonly IAsyncPolicy IdentityResolutionBulkhead = Policies.Bulkhead(4, 20);
		private const int IdentityResolutionTimeoutSeconds = 10;
		private const int IdentityResolutionRetryAttempts = 2;

		private readonly ObservableTask<int> resolveIdentityTask;
		private bool shouldSendPetition = true;

		public ObservablePart(Part part)
		{
			this.Part = part;
			ServiceRef.XmppService.PetitionedIdentityResponseReceived += this.XmppService_OnPetitionedIdentityResponseReceived;
			this.resolveIdentityTask = this.BuildResolveIdentityTask();
		}

		public ObservablePart(Part part, ClientSignature signature)
		{
			this.Part = part;
			ServiceRef.XmppService.PetitionedIdentityResponseReceived += this.XmppService_OnPetitionedIdentityResponseReceived;
			this.Signature = signature;
			this.resolveIdentityTask = this.BuildResolveIdentityTask();

		}

		/// <summary>
		/// Gets the task that resolves identity information for this part.
		/// </summary>
		public ObservableTask<int> ResolveIdentityTask => this.resolveIdentityTask;
		/// <summary>
		/// Initializes the part, setting properties which needs to be set asynchronosly
		/// </summary>
		public Task InitializeAsync(bool sendPetition = true)
		{
			this.StartIdentityResolution(sendPetition);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Starts resolving identity information for this part in the background.
		/// </summary>
		/// <param name="sendPetition">If true, sends a petition when identity cannot be retrieved directly.</param>
		public void StartIdentityResolution(bool sendPetition = true)
		{
			this.shouldSendPetition = sendPetition;
			this.resolveIdentityTask.Run();
		}

		private ObservableTask<int> BuildResolveIdentityTask()
		{
			ObservableTaskBuilder<int> Builder = new ObservableTaskBuilder<int>()
				.Named(nameof(this.ResolveIdentityTask))
				.AutoStart(false)
				.WithPolicy(Policies.Timeout(TimeSpan.FromSeconds(IdentityResolutionTimeoutSeconds)))
				.WithPolicy(Policies.Retry(IdentityResolutionRetryAttempts, (Attempt, Ex) => TimeSpan.FromMilliseconds(500 * Attempt), Ex => Ex is not OperationCanceledException))
				.Run(this.ResolveIdentityAsync);

			return Builder.Build();
		}

		private async Task ResolveIdentityAsync(TaskContext<int> Context)
		{
			CancellationToken CancellationToken = Context.CancellationToken;
			IProgress<int> Progress = Context.Progress;

			Progress.Report(0);
			await IdentityResolutionBulkhead.ExecuteAsync(async InnerToken =>
			{
				await this.UpdateFriendlyNameAsync(InnerToken).ConfigureAwait(false);
				InnerToken.ThrowIfCancellationRequested();

				if (this.shouldSendPetition)
					await this.ResolveIdentityWithPetitionAsync(InnerToken).ConfigureAwait(false);
				else
					await this.ResolveIdentityWithoutPetitionAsync(InnerToken).ConfigureAwait(false);
			}, CancellationToken).ConfigureAwait(false);

			Progress.Report(100);
		}

		private async Task UpdateFriendlyNameAsync(CancellationToken cancellationToken)
		{
			string FriendlyName = await this.GetFriendlyNameAsync().ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.FriendlyName = FriendlyName;
			});
		}

		private async Task XmppService_OnPetitionedIdentityResponseReceived(object? Sender, LegalIdentityPetitionResponseEventArgs e)
		{
			if (e.RequestedIdentity is not null && e.RequestedIdentity.Id == this.LegalId)
			{
				this.identity = e.RequestedIdentity;
				string FriendlyName = await this.GetFriendlyNameAsync();

				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.FriendlyName = FriendlyName;
					this.HasSentPetition = false;
					this.OnPropertyChanged(nameof(this.HasIdentity));
					this.OnPropertyChanged(nameof(this.CanSendProposal));
					this.OnPropertyChanged(nameof(this.HasSentPetition));
				});
			}
		}

		private async Task ResolveIdentityWithPetitionAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.identity = await ServiceRef.ContractOrchestratorService.TryGetLegalIdentity(this.LegalId,
							ServiceRef.Localizer[nameof(AppResources.ForInclusionInContract)]).ConfigureAwait(false);

			string FriendlyName = await this.GetFriendlyNameAsync().ConfigureAwait(false);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.HasSentPetition = this.identity is null;
				this.OnPropertyChanged(nameof(this.HasIdentity));
				this.OnPropertyChanged(nameof(this.CanSendProposal));
				this.OnPropertyChanged(nameof(this.HasSentPetition));
				this.FriendlyName = FriendlyName;
			});
		}

		private async Task ResolveIdentityWithoutPetitionAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				LegalIdentity Identity = await ServiceRef.XmppService.GetLegalIdentity(this.LegalId).ConfigureAwait(false);
				this.identity = Identity;
			}
			catch (Exception)
			{
				// Ignore, OK if forbidden or network error
			}

			string FriendlyName = await this.GetFriendlyNameAsync().ConfigureAwait(false);

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				this.HasSentPetition = false;
				this.OnPropertyChanged(nameof(this.HasIdentity));
				this.OnPropertyChanged(nameof(this.CanSendProposal));
				this.OnPropertyChanged(nameof(this.HasSentPetition));
				this.FriendlyName = FriendlyName;
			});
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
				this.OnPropertyChanged(nameof(this.CanSendProposal));
				this.OpenSignatureCommand.NotifyCanExecuteChanged();
			}
		}

		public string LegalId => this.Part.LegalId;


		public string Role => this.Part.Role;

		/// <summary>
		/// The friendly name for the part
		/// Has to be initialized with <see cref="InitializeAsync"/> or <see cref="StartIdentityResolution"/>
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

		public bool HasSentPetition
		{
			get => this.hasSentPetition && !this.HasIdentity;
			private set
			{
				this.SetProperty(ref this.hasSentPetition, value);
			}
		}
		private bool hasSentPetition = false;

		public bool IsMe => this.Part.LegalId == ServiceRef.TagProfile.LegalIdentity?.Id;
		public bool IsThirdParty => !this.IsMe;

		public bool HasIdentity => this.identity is not null;

		public bool HasSigned => this.Signature is not null;

		public bool CanSendProposal => this.IsThirdParty && this.HasIdentity && !this.HasSigned && this.Role != "TrustProvider";

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


		#region Commands
		[RelayCommand(AllowConcurrentExecutions = false)]
		private async Task OpenIdentityAsync()
		{
			if (this.identity is null)
			{
				try
				{
					await this.ResolveIdentityWithPetitionAsync(CancellationToken.None);
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.Petition)], ServiceRef.Localizer[nameof(AppResources.PetitionIdentitySent)], ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
				catch (Exception)
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.Error)], ServiceRef.Localizer[nameof(AppResources.SomethingWentWrong)], ServiceRef.Localizer[nameof(AppResources.Ok)]);
				}
			}
			else
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(this.identity), Services.UI.BackMethod.Pop);
			}
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

			if (disposing)
			{
				this.resolveIdentityTask.Dispose();
				// Unsubscribe from the event to prevent memory leaks
				ServiceRef.XmppService.PetitionedIdentityResponseReceived -= this.XmppService_OnPetitionedIdentityResponseReceived;
			}

			this.disposed = true;
		}
		#endregion
	}
}
