using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.UI.Pages.Applications.Applications;
using NeuroAccessMaui.UI.Pages.Identity.ViewIdentity;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	/// <summary>
	/// View model for the read-only KYC application status page.
	/// </summary>
	public partial class KycApplicationStatusViewModel : BaseViewModel
	{
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
			this.UpdatedText = string.Empty;
			this.DetailText = ServiceRef.Localizer[nameof(AppResources.KycManualReviewInfo)];
			this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.Open)];
			this.SecondaryActionText = ServiceRef.Localizer[nameof(AppResources.Applications)];
		}

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
		/// Gets or sets the last updated text.
		/// </summary>
		[ObservableProperty]
		private string updatedText;

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

		/// <inheritdoc/>
		public override async Task OnInitializeAsync()
		{
			await base.OnInitializeAsync();
			await this.LoadAsync();
		}

		[RelayCommand]
		private async Task Refresh()
		{
			await this.LoadAsync();
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

		private async Task LoadAsync()
		{
			this.SetIsBusy(true);
			try
			{
				this.reference = this.navigationArguments?.Reference;
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

		private void ApplyStatus(KycApplicationStatus Status, IdentityApplicationGateDecision? Decision)
		{
			this.UpdatedText = this.FormatUpdatedText(this.reference?.UpdatedUtc);
			this.IsRejected = Status.IsRejectedOrInvalid;
			this.IsFinalizing = Status.IsFinalizing;

			if (Status.IsRejectedOrInvalid)
			{
				this.TitleText = ServiceRef.Localizer[nameof(AppResources.KycRejectedHeader)];
				this.DescriptionText = ServiceRef.Localizer[nameof(AppResources.KycRejectedDescription)];
				this.StageText = ServiceRef.Localizer[nameof(AppResources.Rejected)];
				this.DetailText = ServiceRef.Localizer[nameof(AppResources.KycRejectedInvalidationHint)];
				this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.KycReapplyWithoutPendingButton)];
				return;
			}

			if (Status.IsFinalizing)
			{
				this.TitleText = ServiceRef.Localizer[nameof(AppResources.IdentityApplication)];
				this.DescriptionText = Status.Kind == KycApplicationStatusKind.FinalizationInProgress
					? ServiceRef.Localizer[nameof(AppResources.BeforeFinalizingSummary)]
					: ServiceRef.Localizer[nameof(AppResources.KycManualReviewInfo)];
				this.StageText = this.ResolveStageText(Status);
				this.DetailText = Status.Kind == KycApplicationStatusKind.FinalizationInProgress
					? ServiceRef.Localizer[nameof(AppResources.BeforeFinalizingSummary)]
					: ServiceRef.Localizer[nameof(AppResources.KycPendingManualReviewHint)];
				this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.Open)];
				return;
			}

			if (Status.IsApproved || Decision?.Route == IdentityApplicationRoute.ShowApprovedIdentity)
			{
				this.TitleText = ServiceRef.Localizer[nameof(AppResources.IdentityState_Approved)];
				this.DescriptionText = ServiceRef.Localizer[nameof(AppResources.ApplyIdInfoMainPage)];
				this.StageText = ServiceRef.Localizer[nameof(AppResources.Approved)];
				this.DetailText = ServiceRef.Localizer[nameof(AppResources.ApplyIdInfoMainPage)];
				this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.Open)];
				return;
			}

			this.TitleText = Status.Kind == KycApplicationStatusKind.ReservedPreview
				? ServiceRef.Localizer[nameof(AppResources.IdentityApplication)]
				: ServiceRef.Localizer[nameof(AppResources.IdentityReviewRequest)];
			this.DescriptionText = Status.Kind == KycApplicationStatusKind.ReservedPreview
				? ServiceRef.Localizer[nameof(AppResources.BeforeFinalizingSummary)]
				: ServiceRef.Localizer[nameof(AppResources.KycPendingManualReviewHint)];
			this.StageText = this.ResolveStageText(Status);
			this.DetailText = Status.Kind == KycApplicationStatusKind.ReservedPreview
				? ServiceRef.Localizer[nameof(AppResources.BeforeFinalizingSummary)]
				: ServiceRef.Localizer[nameof(AppResources.KycManualReviewInfo)];
			this.PrimaryActionText = ServiceRef.Localizer[nameof(AppResources.Open)];
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
