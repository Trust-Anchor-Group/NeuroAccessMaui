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

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// View model for the read-only KYC application status page.
	/// </summary>
	/// <remarks>
	/// Pending applications show a Submitted, In review, Approved timeline. Rejected, approved, and reserved applications
	/// keep their own texts and actions.
	/// </remarks>
	public partial class KycApplicationStatusViewModel : BaseViewModel
	{
		private const string nfcVerificationMethod = "NFC";
		private const string photoVerificationMethod = "Photo";

		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycReference? reference;
		private KycApplicationStatus? status;

		/// <summary>
		/// Initializes a new instance of the <see cref="KycApplicationStatusViewModel"/> class.
		/// </summary>
		public KycApplicationStatusViewModel()
		{
			this.navigationArguments = ServiceRef.NavigationService.PopLatestArgs<KycProcessNavigationArgs>();
			this.TitleText = ServiceRef.Localizer[nameof(AppResources.IdentityApplication)];
			this.DescriptionText = ServiceRef.Localizer[nameof(AppResources.KycPendingManualReviewHint)];
			this.StageText = ServiceRef.Localizer[nameof(AppResources.InProgress)];
			this.LastUpdatedText = string.Empty;
			this.MethodText = string.Empty;
			this.DetailText = ServiceRef.Localizer[nameof(AppResources.KycManualReviewInfo)];
			this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.Open)];
			this.SecondaryActionText = ServiceRef.Localizer[nameof(AppResources.Applications)];
		}

		/// <summary>
		/// Gets the timeline rows shown for a pending application.
		/// </summary>
		public ObservableCollection<KycApplicationStatusRow> StatusRows { get; } = new ObservableCollection<KycApplicationStatusRow>();

		/// <summary>
		/// Gets or sets the page title.
		/// </summary>
		[ObservableProperty]
		private string titleText;

		/// <summary>
		/// Gets or sets the status description.
		/// </summary>
		[ObservableProperty]
		private string descriptionText;

		/// <summary>
		/// Gets or sets the current stage text.
		/// </summary>
		[ObservableProperty]
		private string stageText;

		/// <summary>
		/// Gets or sets the time of the last update, without a label.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasLastUpdated))]
		[NotifyPropertyChangedFor(nameof(ShowStatusDetails))]
		private string lastUpdatedText;

		/// <summary>
		/// Gets or sets how the application was verified when it was sent.
		/// </summary>
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasMethod))]
		[NotifyPropertyChangedFor(nameof(ShowStatusDetails))]
		private string methodText;

		/// <summary>
		/// Gets or sets supporting status details.
		/// </summary>
		[ObservableProperty]
		private string detailText;

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
		/// Gets or sets a value indicating whether the rejected state is active.
		/// </summary>
		[ObservableProperty]
		private bool isRejected;

		/// <summary>
		/// Gets or sets a value indicating whether the finalization state is active.
		/// </summary>
		[ObservableProperty]
		private bool isFinalizing;

		/// <summary>
		/// Gets or sets a value indicating whether the application is approved.
		/// </summary>
		[ObservableProperty]
		private bool isApproved;

		/// <summary>
		/// Gets or sets a value indicating whether the pending timeline is shown.
		/// </summary>
		[ObservableProperty]
		private bool showTimeline;

		/// <summary>
		/// Gets or sets the scene of the status badge.
		/// </summary>
		[ObservableProperty]
		private StatusVisualKind visualKind = StatusVisualKind.Pending;

		/// <summary>
		/// Gets a value indicating whether the verification method is known.
		/// </summary>
		public bool HasMethod => !string.IsNullOrWhiteSpace(this.MethodText);

		/// <summary>
		/// Gets a value indicating whether the time of the last update is known.
		/// </summary>
		public bool HasLastUpdated => !string.IsNullOrWhiteSpace(this.LastUpdatedText);

		/// <summary>
		/// Gets a value indicating whether the card with the verification method and last update is shown.
		/// </summary>
		public bool ShowStatusDetails => this.HasMethod || this.HasLastUpdated;

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			await this.LoadAsync(false);
		}

		[RelayCommand]
		private async Task Refresh()
		{
			await this.LoadAsync(true);
		}

		[RelayCommand]
		private async Task PrimaryAction()
		{
			if (this.status?.IsApproved == true)
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

		/// <summary>
		/// Loads the application status from the KYC reference and the profile identities.
		/// </summary>
		/// <param name="ReloadReference">Whether to read the latest saved copy of the reference, so updates received while the
		/// page was open are shown.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		private async Task LoadAsync(bool ReloadReference)
		{
			this.SetIsBusy(true);
			try
			{
				IdentityApplicationGateDecision? Decision = null;
				this.reference ??= this.navigationArguments?.Reference;

				if (ReloadReference && this.reference is not null)
					this.reference = await this.ReloadReferenceAsync(this.reference);

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
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
			finally
			{
				this.SetIsBusy(false);
			}
		}

		private async Task<KycReference> ReloadReferenceAsync(KycReference Current)
		{
			string? IdentityId = Current.GetActiveApplicationIdentityId();
			if (string.IsNullOrWhiteSpace(IdentityId))
				return Current;

			KycReference? Latest = await ServiceRef.KycService.FindReferenceByIdentityIdAsync(IdentityId);
			if (Latest is null || !string.Equals(Latest.ObjectId, Current.ObjectId, StringComparison.Ordinal))
				return Current;

			return Latest;
		}

		private void ApplyStatus(KycApplicationStatus Status, IdentityApplicationGateDecision? Decision)
		{
			this.LastUpdatedText = FormatTime(this.reference?.UpdatedUtc);
			this.MethodText = this.ResolveMethodText(this.reference);
			this.IsRejected = Status.IsRejectedOrInvalid;
			this.IsFinalizing = Status.IsFinalizing;
			this.IsApproved = false;
			this.ShowTimeline = false;
			this.StatusRows.Clear();

			if (Status.IsRejectedOrInvalid)
			{
				this.VisualKind = StatusVisualKind.Attention;
				this.TitleText = ServiceRef.Localizer[nameof(AppResources.KycRejectedHeader)];
				this.DescriptionText = ServiceRef.Localizer[nameof(AppResources.KycRejectedDescription)];
				this.StageText = ServiceRef.Localizer[nameof(AppResources.Rejected)];
				this.DetailText = ServiceRef.Localizer[nameof(AppResources.KycRejectedInvalidationHint)];
				this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.KycReapplyWithoutPendingButton)];
				return;
			}

			if (Status.IsFinalizing)
			{
				this.VisualKind = StatusVisualKind.Pending;
				this.TitleText = ServiceRef.Localizer[nameof(AppResources.IdentityApplication)];
				this.DescriptionText = Status.Kind == KycApplicationStatusKind.FinalizationInProgress
					? ServiceRef.Localizer[nameof(AppResources.BeforeFinalizingSummary)]
					: ServiceRef.Localizer[nameof(AppResources.KycManualReviewInfo)];
				this.StageText = this.ResolveStageText(Status);
				this.DetailText = Status.Kind == KycApplicationStatusKind.FinalizationInProgress
					? ServiceRef.Localizer[nameof(AppResources.BeforeFinalizingSummary)]
					: ServiceRef.Localizer[nameof(AppResources.KycPendingManualReviewHint)];
				this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.Open)];
				this.BuildTimeline(true);
				return;
			}

			if (Status.IsApproved || Decision?.Route == IdentityApplicationRoute.ShowApprovedIdentity)
			{
				this.VisualKind = StatusVisualKind.Success;
				this.IsApproved = true;
				this.TitleText = ServiceRef.Localizer[nameof(AppResources.IdentityState_Approved)];
				this.DescriptionText = ServiceRef.Localizer[nameof(AppResources.ApplyIdInfoMainPage)];
				this.StageText = ServiceRef.Localizer[nameof(AppResources.Approved)];
				this.DetailText = ServiceRef.Localizer[nameof(AppResources.ApplyIdInfoMainPage)];
				this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.Open)];
				return;
			}

			bool IsReservedPreview = Status.Kind == KycApplicationStatusKind.ReservedPreview;
			this.VisualKind = IsReservedPreview ? StatusVisualKind.Draft : StatusVisualKind.Pending;
			this.TitleText = IsReservedPreview
				? ServiceRef.Localizer[nameof(AppResources.IdentityApplication)]
				: ServiceRef.Localizer[nameof(AppResources.IdentityReviewRequest)];
			this.DescriptionText = IsReservedPreview
				? ServiceRef.Localizer[nameof(AppResources.BeforeFinalizingSummary)]
				: ServiceRef.Localizer[nameof(AppResources.KycPendingManualReviewHint)];
			this.StageText = this.ResolveStageText(Status);
			this.DetailText = IsReservedPreview
				? ServiceRef.Localizer[nameof(AppResources.BeforeFinalizingSummary)]
				: ServiceRef.Localizer[nameof(AppResources.KycManualReviewInfo)];
			this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.Open)];

			if (!IsReservedPreview)
				this.BuildTimeline(false);
		}

		/// <summary>
		/// Builds the Submitted, In review, Approved timeline of a pending application.
		/// </summary>
		/// <param name="IsFinalizing">Whether the review is done and the approved identity is being finalized.</param>
		private void BuildTimeline(bool IsFinalizing)
		{
			this.StatusRows.Add(new KycApplicationStatusRow(
				ServiceRef.Localizer[nameof(AppResources.KycApplicationStatusTimelineSubmitted)],
				ServiceRef.Localizer[nameof(AppResources.KycApplicationStatusTimelineSubmittedDescription)],
				IdentityFlowPhaseState.Complete));
			this.StatusRows.Add(new KycApplicationStatusRow(
				ServiceRef.Localizer[nameof(AppResources.InReview)],
				ServiceRef.Localizer[nameof(AppResources.KycApplicationStatusTimelineReviewDescription)],
				IsFinalizing ? IdentityFlowPhaseState.Complete : IdentityFlowPhaseState.Current));
			this.StatusRows.Add(new KycApplicationStatusRow(
				ServiceRef.Localizer[nameof(AppResources.Approved)],
				IsFinalizing
					? ServiceRef.Localizer[nameof(AppResources.KycApplicationStatusTimelineFinalizingDescription)]
					: ServiceRef.Localizer[nameof(AppResources.KycApplicationStatusTimelineApprovedDescription)],
				IsFinalizing ? IdentityFlowPhaseState.Current : IdentityFlowPhaseState.Upcoming));
			this.ShowTimeline = true;
		}

		private string ResolveStageText(KycApplicationStatus Status)
		{
			return Status.Kind switch
			{
				KycApplicationStatusKind.ReservedPreview => ServiceRef.Localizer[nameof(AppResources.ReservedNoColon)],
				KycApplicationStatusKind.PreviewRejected => ServiceRef.Localizer[nameof(AppResources.Rejected)],
				KycApplicationStatusKind.Rejected => ServiceRef.Localizer[nameof(AppResources.Rejected)],
				KycApplicationStatusKind.Approved => ServiceRef.Localizer[nameof(AppResources.Approved)],
				_ => ServiceRef.Localizer[nameof(AppResources.InProgress)]
			};
		}

		private string ResolveMethodText(KycReference? Reference)
		{
			string? Method = Reference?.SubmittedVerificationMethod;
			if (string.Equals(Method, nfcVerificationMethod, StringComparison.OrdinalIgnoreCase))
				return ServiceRef.Localizer[nameof(AppResources.KycApplicationStatusMethodChip)];

			if (string.Equals(Method, photoVerificationMethod, StringComparison.OrdinalIgnoreCase))
				return ServiceRef.Localizer[nameof(AppResources.KycApplicationStatusMethodPhotos)];

			return string.Empty;
		}

		private static string FormatTime(DateTime? UpdatedUtc)
		{
			if (!UpdatedUtc.HasValue || UpdatedUtc.Value == default)
				return string.Empty;

			DateTime LocalTime = UpdatedUtc.Value.Kind == DateTimeKind.Utc ? UpdatedUtc.Value.ToLocalTime() : UpdatedUtc.Value;
			return LocalTime.ToString("g", CultureInfo.CurrentCulture);
		}
	}
}
