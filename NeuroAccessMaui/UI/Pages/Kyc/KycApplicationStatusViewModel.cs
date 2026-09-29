using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.UI.Controls;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// View model for the read-only KYC application status page.
	/// </summary>
	/// <remarks>
	/// Maps the resolved application status to a visual, copy, timeline, and actions, and reloads when the provider
	/// changes the application while the page is open.
	/// </remarks>
	public partial class KycApplicationStatusViewModel : BaseViewModel
	{
		/// <summary>
		/// Timeline step shown when every step is complete.
		/// </summary>
		private const int timelineComplete = 3;

		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycReference? reference;
		private KycApplicationStatus? status;
		private bool isSubscribed;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycApplicationStatusViewModel"/> class.
		/// </summary>
		public KycApplicationStatusViewModel()
		{
			this.navigationArguments = ServiceRef.NavigationService.PopLatestArgs<KycProcessNavigationArgs>();
			this.TitleText = ServiceRef.Localizer[nameof(AppResources.KycStatusPendingTitle)];
			this.DescriptionText = ServiceRef.Localizer[nameof(AppResources.KycStatusPendingDescription)];
			this.StageText = ServiceRef.Localizer[nameof(AppResources.InReview)];
			this.UpdatedText = string.Empty;
			this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.KycViewApplicationButton)];
			this.SecondaryActionText = ServiceRef.Localizer[nameof(AppResources.KycAllApplicationsButton)];
		}

		/// <summary>
		/// Occurs when the application becomes approved while the page is open, so the view can celebrate it.
		/// </summary>
		public event EventHandler? ApplicationApproved;

		/// <summary>
		/// Gets or sets the status title.
		/// </summary>
		[ObservableProperty]
		private string titleText;

		/// <summary>
		/// Gets or sets the status description.
		/// </summary>
		[ObservableProperty]
		private string descriptionText;

		/// <summary>
		/// Gets or sets the short stage text shown in the status pill.
		/// </summary>
		[ObservableProperty]
		private string stageText;

		/// <summary>
		/// Gets or sets the last updated text.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasUpdatedText))]
		private string updatedText;

		/// <summary>
		/// Gets or sets the primary action text.
		/// </summary>
		[ObservableProperty]
		private string primaryActionText;

		/// <summary>
		/// Gets or sets the secondary action text.
		/// </summary>
		[ObservableProperty]
		private string secondaryActionText;

		/// <summary>
		/// Gets or sets a value indicating whether the rejected or invalid state is active.
		/// </summary>
		[ObservableProperty]
		private bool isRejected;

		/// <summary>
		/// Gets or sets a value indicating whether the finalization state is active.
		/// </summary>
		[ObservableProperty]
		private bool isFinalizing;

		/// <summary>
		/// Gets or sets a value indicating whether the application has been approved.
		/// </summary>
		[ObservableProperty]
		private bool isApproved;

		/// <summary>
		/// Gets or sets the scene shown by the status visual.
		/// </summary>
		[ObservableProperty]
		private StatusVisualKind visualKind = StatusVisualKind.Pending;

		/// <summary>
		/// Gets or sets the active timeline step (details, review, ID ready). Three means every step is complete.
		/// </summary>
		[ObservableProperty]
		private int timelineStep = 1;

		/// <summary>
		/// Gets or sets a value indicating whether the timeline is shown.
		/// </summary>
		[ObservableProperty]
		private bool showTimeline = true;

		/// <summary>
		/// Gets or sets the reviewer's message for a rejected application.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasReviewMessage))]
		[NotifyPropertyChangedFor(nameof(HasAttentionDetails))]
		private string reviewMessage = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether a pull-to-refresh is in progress.
		/// </summary>
		[ObservableProperty]
		private bool isRefreshing;

		/// <summary>
		/// Gets the items the reviewer marked as needing attention, with reasons when available.
		/// </summary>
		public ObservableCollection<string> AttentionItems { get; } = [];

		/// <summary>
		/// Gets a value indicating whether a reviewer message is available.
		/// </summary>
		public bool HasReviewMessage => !string.IsNullOrWhiteSpace(this.ReviewMessage);

		/// <summary>
		/// Gets a value indicating whether any items need attention.
		/// </summary>
		public bool HasAttentionItems => this.AttentionItems.Count > 0;

		/// <summary>
		/// Gets a value indicating whether the attention card should be shown.
		/// </summary>
		public bool HasAttentionDetails => this.IsRejected && (this.HasReviewMessage || this.HasAttentionItems);

		/// <summary>
		/// Gets a value indicating whether the last update time is known.
		/// </summary>
		public bool HasUpdatedText => !string.IsNullOrWhiteSpace(this.UpdatedText);

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			await this.LoadAsync();

			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;
			this.isSubscribed = true;
		}

		/// <inheritdoc/>
		public override async Task OnDisposeAsync()
		{
			if (this.isSubscribed)
			{
				ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;
				this.isSubscribed = false;
			}

			await base.OnDisposeAsync();
		}

		partial void OnIsRejectedChanged(bool value)
		{
			this.OnPropertyChanged(nameof(this.HasAttentionDetails));
		}

		[RelayCommand]
		private async Task Refresh()
		{
			try
			{
				await this.ReloadReferenceAsync(this.reference?.GetActiveApplicationIdentityId());
				await this.LoadAsync();
			}
			finally
			{
				this.IsRefreshing = false;
			}
		}

		/// <summary>
		/// Replaces the shown reference with the latest stored copy for the given identity, when it is the same application.
		/// </summary>
		/// <param name="IdentityId">An identity identifier belonging to the application.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		private async Task ReloadReferenceAsync(string? IdentityId)
		{
			if (this.reference is null || string.IsNullOrWhiteSpace(IdentityId))
				return;

			try
			{
				KycReference? Updated = await ServiceRef.KycService.FindReferenceByIdentityIdAsync(IdentityId);
				if (Updated is not null && string.Equals(Updated.ObjectId, this.reference.ObjectId, StringComparison.Ordinal))
					this.reference = Updated;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		[RelayCommand]
		private async Task PrimaryAction()
		{
			if (this.IsApproved)
			{
				await this.OpenApprovedIdentityAsync();
				return;
			}

			if (this.reference is not null)
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(KycProcessPage), new KycProcessNavigationArgs(this.reference));
				return;
			}

			await this.ReturnToApplicationsAsync();
		}

		private async Task OpenApprovedIdentityAsync()
		{
			LegalIdentity? Identity = ServiceRef.TagProfile.LegalIdentity;
			if (Identity?.State == IdentityState.Approved)
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
				return;
			}

			string? ActiveIdentityId = this.status?.ActiveIdentityId ?? this.reference?.GetActiveApplicationIdentityId();

			if (!string.IsNullOrWhiteSpace(ActiveIdentityId) &&
				(Identity is null || !string.Equals(Identity.Id, ActiveIdentityId, StringComparison.OrdinalIgnoreCase)))
			{
				try
				{
					Identity = await ServiceRef.XmppService.GetLegalIdentity(ActiveIdentityId);
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}
			}

			if (Identity is not null)
			{
				await ServiceRef.NavigationService.GoToAsync(nameof(ViewIdentityPage), new ViewIdentityNavigationArgs(Identity));
				return;
			}

			await this.ReturnToApplicationsAsync();
		}

		[RelayCommand]
		private async Task SecondaryAction()
		{
			await this.ReturnToApplicationsAsync();
		}

		private async Task ReturnToApplicationsAsync()
		{
			await ServiceRef.NavigationService.PopToRootAsync();

			if (ServiceRef.NavigationService.CurrentPage is not ApplicationsPage)
			{
				if (ServiceRef.NavigationService.CurrentPage is KycProcessPage or KycTravelDocumentPage or KycApplicationStatusPage)
				{
					await ServiceRef.NavigationService.SetRootAsync(nameof(ApplicationsPage));
					return;
				}

				await ServiceRef.NavigationService.GoToAsync(nameof(ApplicationsPage));
			}
		}

		private async Task LoadAsync()
		{
			this.SetIsBusy(true);
			try
			{
				this.reference ??= this.navigationArguments?.Reference;
				IdentityApplicationGateDecision? Decision = null;
				if (this.reference is null)
				{
					Decision = await ServiceRef.IdentityApplicationGateService.EvaluateAsync();
					this.reference = Decision.Reference;
				}

				this.status = KycApplicationStatusResolver.Resolve(
					this.reference,
					ServiceRef.TagProfile.LegalIdentity,
					ServiceRef.TagProfile.IdentityApplication);

				this.ApplyStatus(this.status, Decision);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		/// <summary>
		/// Reloads the status when the provider changes the application while the page is open.
		/// </summary>
		private async Task XmppService_IdentityApplicationChanged(object? Sender, LegalIdentityEventArgs E)
		{
			try
			{
				string? IdentityId = E.Identity?.Id;
				if (this.reference is null || !this.reference.MatchesIdentityId(IdentityId))
					return;

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					bool WasApproved = this.status?.IsApproved == true;
					await this.ReloadReferenceAsync(IdentityId);
					await this.LoadAsync();

					if (!WasApproved && this.status?.IsApproved == true)
						this.NotifyApproved();
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		/// <summary>
		/// Marks an approval that arrived while the page was open with haptic feedback, an announcement, and a celebration.
		/// </summary>
		private void NotifyApproved()
		{
			try
			{
				HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
			}
			catch (Exception)
			{
				// Devices without haptics still get the visual confirmation.
			}

			try
			{
				SemanticScreenReader.Default.Announce(this.TitleText);
			}
			catch (Exception)
			{
				// Announcements are best effort.
			}

			this.ApplicationApproved?.Invoke(this, EventArgs.Empty);
		}

		private void ApplyStatus(KycApplicationStatus Status, IdentityApplicationGateDecision? Decision)
		{
			this.UpdatedText = this.FormatUpdatedText(this.reference?.UpdatedUtc);
			this.IsRejected = Status.IsRejectedOrInvalid;
			this.IsFinalizing = Status.IsFinalizing;
			this.IsApproved = Status.IsApproved || Decision?.Route == IdentityApplicationRoute.ShowApprovedIdentity;
			this.ApplyReview(Status.IsRejectedOrInvalid ? this.reference?.ApplicationReview : null);
			this.SecondaryActionText = ServiceRef.Localizer[nameof(AppResources.KycAllApplicationsButton)];

			if (Status.IsRejectedOrInvalid)
			{
				this.SetPresentation(
					StatusVisualKind.Attention,
					null,
					nameof(AppResources.KycRejectedHeader),
					nameof(AppResources.KycRejectedDescription),
					this.ResolveStageText(Status),
					nameof(AppResources.KycFixInvalidClaimsButton));
				return;
			}

			if (Status.Kind == KycApplicationStatusKind.Expired)
			{
				this.SetPresentation(
					StatusVisualKind.Attention,
					null,
					nameof(AppResources.KycStatusExpiredTitle),
					nameof(AppResources.KycStatusExpiredDescription),
					ServiceRef.Localizer[nameof(AppResources.KycStatusExpiredStage)],
					nameof(AppResources.KycViewApplicationButton));
				return;
			}

			if (this.IsApproved)
			{
				this.SetPresentation(
					StatusVisualKind.Success,
					timelineComplete,
					nameof(AppResources.KycStatusApprovedTitle),
					nameof(AppResources.KycStatusApprovedDescription),
					ServiceRef.Localizer[nameof(AppResources.Approved)],
					nameof(AppResources.KycOpenIdButton));
				return;
			}

			if (Status.IsFinalizing)
			{
				this.SetPresentation(
					StatusVisualKind.Pending,
					2,
					nameof(AppResources.KycStatusFinalizingTitle),
					nameof(AppResources.KycStatusFinalizingDescription),
					ServiceRef.Localizer[nameof(AppResources.InProgress)],
					nameof(AppResources.KycViewApplicationButton));
				return;
			}

			if (Status.Kind == KycApplicationStatusKind.ReservedPreview)
			{
				this.SetPresentation(
					StatusVisualKind.Draft,
					0,
					nameof(AppResources.KycStatusDraftTitle),
					nameof(AppResources.KycStatusDraftDescription),
					ServiceRef.Localizer[nameof(AppResources.InProgress)],
					nameof(AppResources.KycContinueApplicationButton));
				return;
			}

			this.SetPresentation(
				StatusVisualKind.Pending,
				1,
				nameof(AppResources.KycStatusPendingTitle),
				nameof(AppResources.KycStatusPendingDescription),
				ServiceRef.Localizer[nameof(AppResources.InReview)],
				nameof(AppResources.KycViewApplicationButton));
		}

		/// <summary>
		/// Applies the visual, timeline, and copy for one status.
		/// </summary>
		/// <param name="Kind">The status visual scene.</param>
		/// <param name="Step">The active timeline step, or <c>null</c> to hide the timeline.</param>
		/// <param name="TitleKey">Resource key of the title.</param>
		/// <param name="DescriptionKey">Resource key of the description.</param>
		/// <param name="Stage">Localized text for the status pill.</param>
		/// <param name="PrimaryKey">Resource key of the primary action.</param>
		private void SetPresentation(StatusVisualKind Kind, int? Step, string TitleKey, string DescriptionKey, string Stage, string PrimaryKey)
		{
			this.VisualKind = Kind;
			this.ShowTimeline = Step.HasValue;
			this.TimelineStep = Step ?? 0;
			this.TitleText = ServiceRef.Localizer[TitleKey];
			this.DescriptionText = ServiceRef.Localizer[DescriptionKey];
			this.StageText = Stage;
			this.PrimaryActionText = ServiceRef.Localizer[PrimaryKey];
		}

		/// <summary>
		/// Shows the reviewer's message and the items marked invalid, or clears them when there is no rejection.
		/// </summary>
		/// <param name="Review">The stored application review, if any.</param>
		private void ApplyReview(ApplicationReview? Review)
		{
			this.AttentionItems.Clear();

			if (Review is not null)
			{
				foreach (ApplicationReviewClaimDetail Detail in Review.InvalidClaimDetails ?? Array.Empty<ApplicationReviewClaimDetail>())
				{
					if (Detail is not null)
						AddAttentionItem(this.AttentionItems, Detail.DisplayName, Detail.Claim, Detail.Reason);
				}

				foreach (ApplicationReviewPhotoDetail Detail in Review.InvalidPhotoDetails ?? Array.Empty<ApplicationReviewPhotoDetail>())
				{
					if (Detail is not null)
						AddAttentionItem(this.AttentionItems, Detail.DisplayName, Detail.FileName, Detail.Reason);
				}
			}

			this.ReviewMessage = Review?.Message?.Trim() ?? string.Empty;
			this.OnPropertyChanged(nameof(this.HasAttentionItems));
			this.OnPropertyChanged(nameof(this.HasAttentionDetails));
		}

		private static void AddAttentionItem(ObservableCollection<string> Items, string? DisplayName, string? Fallback, string? Reason)
		{
			string Label = !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName.Trim() : Fallback?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(Label))
				return;

			Items.Add(string.IsNullOrWhiteSpace(Reason) ? Label : $"{Label} — {Reason.Trim()}");
		}

		private string ResolveStageText(KycApplicationStatus Status)
		{
			return Status.Kind switch
			{
				KycApplicationStatusKind.ReservedPreview => ServiceRef.Localizer[nameof(AppResources.ReservedNoColon)],
				KycApplicationStatusKind.PreviewRejected => ServiceRef.Localizer[nameof(AppResources.Rejected)],
				KycApplicationStatusKind.Rejected => ServiceRef.Localizer[nameof(AppResources.Rejected)],
				KycApplicationStatusKind.Approved => ServiceRef.Localizer[nameof(AppResources.Approved)],
				KycApplicationStatusKind.Obsoleted => ServiceRef.Localizer[nameof(AppResources.IdentityState_Obsoleted)],
				KycApplicationStatusKind.Compromised => ServiceRef.Localizer[nameof(AppResources.IdentityState_Compromised)],
				_ => ServiceRef.Localizer[nameof(AppResources.InProgress)]
			};
		}

		private string FormatUpdatedText(DateTime? UpdatedUtc)
		{
			if (!UpdatedUtc.HasValue || UpdatedUtc.Value == default)
				return string.Empty;

			DateTime LocalTime = UpdatedUtc.Value.Kind == DateTimeKind.Utc ? UpdatedUtc.Value.ToLocalTime() : UpdatedUtc.Value;
			return string.Format(
				CultureInfo.CurrentCulture,
				ServiceRef.Localizer[nameof(AppResources.LastUpdatedFormat)],
				LocalTime.ToString("g", CultureInfo.CurrentCulture));
		}
	}
}
