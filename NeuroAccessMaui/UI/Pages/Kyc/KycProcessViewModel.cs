using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Data;
using NeuroAccessMaui.Services.Identity;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using NeuroAccessMaui.Services.UI.Photos;
using SkiaSharp;
using Waher.Content.Html.Elements;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Contracts.EventArguments;
using Waher.Networking.XMPP;
using NeuroAccessMaui.UI.Pages.Wallet.ServiceProviders;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using IServiceProvider = Waher.Networking.XMPP.Contracts.IServiceProvider;
namespace NeuroAccessMaui.UI.Pages.Kyc
{
	public partial class KycProcessViewModel : BaseViewModel
	{
		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycProcess? process;
		private KycReference? kycReference;
		private int currentPageIndex = 0;
		private bool applicationSent;
		private string? applicationId;

		private List<Property> mappedValues;
		private List<LegalIdentityAttachment> attachments;
		private ServiceProviderWithLegalId[]? peerReviewServices = null;
		[ObservableProperty] private bool shouldViewSummary = false;
		[ObservableProperty] private bool shouldReturnToSummary = false;
		[ObservableProperty] private bool isLoading = false;

		// Peer review integration
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(ScanQrCodeCommand))]
		[NotifyCanExecuteChangedFor(nameof(RequestReviewCommand))]
		[NotifyCanExecuteChangedFor(nameof(RevokeApplicationCommand))]
		private bool applicationSentPublic;

		[ObservableProperty]
		private bool peerReview;

		[ObservableProperty]
		private int nrReviews;

		[ObservableProperty]
		private int nrReviewers;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(RequestReviewCommand))]
		private bool hasFeaturedPeerReviewers;

		public bool CanRequestFeaturedPeerReviewer => this.ApplicationSentPublic && this.HasFeaturedPeerReviewers;

		public bool FeaturedPeerReviewers => this.CanRequestFeaturedPeerReviewer && this.PeerReview;

        public bool ApplicationSent => this.ApplicationSentPublic;

		[ObservableProperty] private int currentPagePosition;
		[ObservableProperty] private KycPage? currentPage;
		[ObservableProperty] private string? currentPageTitle;
		[ObservableProperty] private string? currentPageDescription;
		[ObservableProperty] private bool hasCurrentPageDescription;
		[ObservableProperty] private ReadOnlyObservableCollection<KycSection>? currentPageSections;
		[ObservableProperty] private bool hasSections;
		[ObservableProperty] private string nextButtonText = "Next";

		[ObservableProperty] private ObservableCollection<DisplayQuad> personalInformationSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> addressInformationSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> attachmentInformationSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> companyInformationSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> companyAddressSummary;
		[ObservableProperty] private ObservableCollection<DisplayQuad> companyRepresentativeSummary;

		[ObservableProperty] private string? errorDescription;
		[ObservableProperty] private bool hasErrorDescription = false;

		[ObservableProperty] private bool hasPersonalInformation = false;
		[ObservableProperty] private bool hasAddressInformation = false;
		[ObservableProperty] private bool hasAttachments = false;
		[ObservableProperty] private bool hasCompanyInformation = false;
		[ObservableProperty] private bool hasCompanyAddress = false;
		[ObservableProperty] private bool hasCompanyRepresentative = false;

		public string BannerUriLight => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallLight);
		public string BannerUriDark => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallDark);

		public string BannerUri =>
			(Application.Current?.RequestedTheme ?? AppTheme.Light) switch
			{
				AppTheme.Dark => this.BannerUriDark,
				AppTheme.Light => this.BannerUriLight,
				_ => this.BannerUriLight
			};

		public ObservableCollection<KycPage> Pages
		{
			get
			{
				if (this.process is not null)
				{
					return this.process.Pages;
				}
				return [];
			}
		}

		public double Progress
		{
			get
			{
				if (this.process is null || this.CurrentPage is null)
				{
					this.ProgressPercent = "0%";
					return 0;
				}

				ObservableCollection<KycPage> VisiblePages = [.. this.Pages.Where(Page => Page.IsVisible(this.process.Values))];

				if (VisiblePages.Count == 0)
				{
					this.ProgressPercent = "0%";
					return 0;
				}

				if (this.ShouldViewSummary)
				{
					this.ProgressPercent = "100%";
					return 1;
				}

				int Index = VisiblePages.IndexOf(this.CurrentPage);
				double Progress = Math.Clamp((double)Index / VisiblePages.Count, 0, 1);

				this.ProgressPercent = $"{(Progress * 100):0}%";

				return Progress;
			}
		}

		[ObservableProperty]
		private string progressPercent = "0%";

		public IAsyncRelayCommand NextCommand { get; }
		public IRelayCommand PreviousCommand { get; }

		/// <summary>
		/// Creates a new instance of the KycProcessViewModel.
		/// </summary>
		/// <param name="Args">Navigation arguments carrying the KycReference.</param>
		public KycProcessViewModel(KycProcessNavigationArgs? Args)
		{
			this.navigationArguments = Args;
			this.NextCommand = new AsyncRelayCommand(this.ExecuteNextAsync, this.CanExecuteNext);
			this.PreviousCommand = new AsyncRelayCommand(this.ExecutePrevious);
			this.PersonalInformationSummary = new ObservableCollection<DisplayQuad>();
			this.AddressInformationSummary = new ObservableCollection<DisplayQuad>();
			this.AttachmentInformationSummary = new ObservableCollection<DisplayQuad>();
			this.mappedValues = new List<Property>();
			this.attachments = new List<LegalIdentityAttachment>();
			// Initialize peer review state
			this.ApplicationSentPublic = ServiceRef.TagProfile.IdentityApplication is not null;
			this.NrReviews = ServiceRef.TagProfile.NrReviews;
		}

		protected override async Task OnInitialize()
		{
			this.IsLoading = true;

			await base.OnInitialize();

			string LanguageInit = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

			// Obtain KYC reference from navigation arguments; handle nulls gracefully.
			this.kycReference = this.navigationArguments?.Reference;
			if (this.kycReference is null)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					"Missing KYC reference.",
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoBack();
				return;
			}

			this.process = await this.kycReference.ToProcess(LanguageInit);
			this.applicationId = this.kycReference?.CreatedIdentityId;
			this.OnPropertyChanged(nameof(this.Pages));

			if (this.process is null)
			{
				await ServiceRef.UiService.DisplayAlert(
					ServiceRef.Localizer[nameof(AppResources.ErrorTitle)],
					"Unable to load KYC process.",
					ServiceRef.Localizer[nameof(AppResources.Ok)]);
				await this.GoBack();
				return;
			}

			this.process.Initialize();

			// Peer review: hook events and load attributes if an application exists
			ServiceRef.XmppService.IdentityApplicationChanged += this.XmppService_IdentityApplicationChanged;
			if (ServiceRef.TagProfile.IdentityApplication is not null)
			{
				await this.LoadApplicationAttributes();
				await this.LoadFeaturedPeerReviewers();
			}

			// Load any rejection details stored on the reference and apply
			if (this.kycReference is not null)
			{
				this.ErrorDescription = this.kycReference.RejectionMessage;
				this.HasErrorDescription = !string.IsNullOrWhiteSpace(this.ErrorDescription);
				this.ApplyInvalidationsToFieldsFromReference();
			}

			this.process.ClearValidation();

			foreach (KycPage Page in this.process.Pages)
			{
				Page.PropertyChanged += this.Page_PropertyChanged;
				foreach (ObservableKycField Field in Page.AllFields)
				{
					Field.PropertyChanged += this.Field_PropertyChanged;
				}
				foreach (KycSection Section in Page.AllSections)
				{
					Section.PropertyChanged += this.Section_PropertyChanged;
					foreach (ObservableKycField SectionField in Section.AllFields)
					{
						SectionField.PropertyChanged += this.Field_PropertyChanged;
					}
				}
			}

			foreach (KycPage Page in this.process.Pages)
			{
				Page.UpdateVisibilities(this.process.Values);
			}

			// Resume logic: decide where to open the process
			int ResumeIndex = -1;
			bool ResumeSummary = false;
			if (!string.IsNullOrEmpty(this.kycReference.LastVisitedMode) &&
				string.Equals(this.kycReference.LastVisitedMode, "Summary", StringComparison.OrdinalIgnoreCase))
			{
				ResumeSummary = true;
			}

			if (!ResumeSummary && !string.IsNullOrEmpty(this.kycReference.LastVisitedPageId))
			{
				for (int i = 0; i < this.Pages.Count; i++)
				{
					KycPage P = this.Pages[i];
					if (string.Equals(P.Id, this.kycReference.LastVisitedPageId, StringComparison.Ordinal) && P.IsVisible(this.process.Values))
					{
						ResumeIndex = i;
						break;
					}
				}
			}

			if (ResumeSummary)
			{
				// Validate all and decide: if any invalid page exists, navigate there instead of summary.
				int firstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				if (firstInvalid >= 0)
				{
					this.ShouldReturnToSummary = true; // after fixing, return to summary on continue
					this.ShouldViewSummary = false;
					this.currentPageIndex = firstInvalid;
					this.CurrentPagePosition = this.currentPageIndex;
					this.SetCurrentPage(this.currentPageIndex);
					this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
				}
                else
                {
                    // Prepare summary view and keep an underlying current page (previous visible)
                    await this.ProcessData();
                    this.currentPageIndex = this.Pages.Count; // sentinel after last
                    this.currentPageIndex = this.GetPreviousIndex();
					if (this.currentPageIndex >= 0)
					{
						this.CurrentPagePosition = this.currentPageIndex;
						this.SetCurrentPage(this.currentPageIndex);
					}
                    this.ShouldReturnToSummary = false;
                    this.ShouldViewSummary = true;
                    this.OnPropertyChanged(nameof(this.Progress));
                    this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;

                    // Persist resume mode as Summary to avoid landing on last page on next open
                    if (this.kycReference is not null)
                    {
                        this.kycReference.LastVisitedMode = "Summary";
                        this.UpdateReference();
                        await this.SaveReferenceToStorageAsync();
                    }
                }
            }
			else
			{
				// When starting fresh (no resume), open the FIRST visible page (index 0 if visible).
				// Previous logic used GetNextIndex() from default index 0, which always skipped the first page.
				int InitialIndex;
				if (ResumeIndex >= 0)
				{
					InitialIndex = ResumeIndex;
				}
				else
				{
					// Force next-index calculation to start from before the first page
					this.currentPageIndex = -1;
					InitialIndex = this.GetNextIndex();
				}

				this.currentPageIndex = InitialIndex;
				this.CurrentPagePosition = this.currentPageIndex;
				this.SetCurrentPage(this.currentPageIndex);
			}


			// Set initial localized label for the next/apply button depending on current mode
			this.NextButtonText = this.ShouldViewSummary
				? ServiceRef.Localizer["Kyc_Apply"].Value
				: ServiceRef.Localizer["Kyc_Next"].Value;

			MainThread.BeginInvokeOnMainThread(
				this.NextCommand.NotifyCanExecuteChanged
			);

			// If there's a pending/sent application, prefer summary with post-submission panel
			if (this.ApplicationSentPublic)
			{
				await this.ProcessData();
				this.ShouldViewSummary = true;
				this.OnPropertyChanged(nameof(this.Progress));
				this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
			}

				this.IsLoading = false;
			}

			partial void OnApplicationSentPublicChanged(bool value)
			{
				if (value)
				{
					this.ShouldViewSummary = true;
					this.OnPropertyChanged(nameof(this.Progress));
				}
			}

		partial void OnCurrentPagePositionChanged(int value)
		{
			// This is called by MAUI CarouselView when position changes (user swipes)
			if (value >= 0 && value < this.Pages.Count)
			{
				this.currentPageIndex = value;
				this.SetCurrentPage(this.currentPageIndex);
			}
		}

		private void Field_PropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs E)
		{
			if (E.PropertyName == nameof(ObservableKycField.RawValue))
			{
				// Raw value changed: update visibilities and page bindings.
				MainThread.BeginInvokeOnMainThread(() => this.SetCurrentPage(this.currentPageIndex));
			}

			// When a field's validation result changes, re-evaluate the Next button.
			if (E.PropertyName == nameof(ObservableKycField.IsValid))
			{
				MainThread.BeginInvokeOnMainThread(this.NextCommand.NotifyCanExecuteChanged);
			}
		}

		private void Section_PropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs E)
		{
			if (E.PropertyName == nameof(KycSection.IsVisible))
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.SetCurrentPage(this.currentPageIndex);
					this.NextCommand.NotifyCanExecuteChanged();
				});
			}
		}

		private void Page_PropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs E)
		{
			if (E.PropertyName == nameof(KycPage.IsVisible))
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.SetCurrentPage(this.currentPageIndex);
					this.NextCommand.NotifyCanExecuteChanged();
				});
			}
		}

		private void SetCurrentPage(int Index)
		{
			if (this.process is null)
			{
				return;
			}
			if (Index < 0 || Index >= this.Pages.Count)
			{
				return;
			}

			this.currentPageIndex = Index;
			this.CurrentPagePosition = Index;

			KycPage Page = this.Pages[Index];
			this.CurrentPage = Page;
			this.CurrentPageTitle = Page.Title is not null ? Page.Title.Text : Page.Id;
			this.CurrentPageDescription = Page.Description?.Text;
			this.HasCurrentPageDescription = !string.IsNullOrWhiteSpace(this.CurrentPageDescription);

			this.CurrentPageSections = Page.VisibleSections;
			this.HasSections = this.CurrentPageSections is not null && this.CurrentPageSections.Count > 0;

			this.OnPropertyChanged(nameof(this.Progress));

			// Scroll to top of page when changing pages

			// Re-evaluate Next button when page/section content changes.
			this.NextCommand.NotifyCanExecuteChanged();
		}

		/// <summary>
		/// Go to the first page that contains a field with the specified mapping (Should be a unique mapping).
		/// </summary>
		[RelayCommand]
		private void GoToPageWithMapping(string SoughtMapping)
		{
			if (this.process is null)
			{
				return;
			}

			string[] Mappings;

			if (SoughtMapping == "BDATE")
			{
				Mappings = ["BDAY", "BMONTH", "BYEAR"];
			}
			else if (SoughtMapping == "ORGREPBDATE")
			{
				Mappings = ["ORGREPBDAY", "ORGREPBMONTH", "ORGREPBYEAR"];
			}
			else
			{
				Mappings = [SoughtMapping];
			}

			for (int i = 0; i < this.Pages.Count; i++)
			{
				KycPage Page = this.Pages[i];

				foreach (string Mapping in Mappings)
				{
					if (Page.AllFields.Any(f => f.Mappings.Any(m => m.Key == Mapping)) ||
						Page.AllSections.Any(s => s.AllFields.Any(f => f.Mappings.Any(m => m.Key == Mapping))))
					{
						if (i >= 0 && i < this.Pages.Count)
						{
							this.currentPageIndex = i;
							this.CurrentPagePosition = i;

							this.ShouldViewSummary = false;

							this.ShouldReturnToSummary = true;
							this.NextButtonText = ServiceRef.Localizer["Kyc_Return"].Value;

							this.SetCurrentPage(this.currentPageIndex);

							this.ScrollUp();
						}
						return;
					}
				}
			}
		}

		private void UpdateReference()
		{
			if (this.process is null)
			{
				return;
			}
			this.kycReference!.Fields = [.. this.process.Values.Select(p => new KycFieldValue(p.Key, p.Value))];
			this.kycReference.Progress = this.Progress;
			this.kycReference.UpdatedUtc = DateTime.UtcNow;
		}

		private async Task SaveReferenceToStorageAsync()
		{
			if (this.kycReference is null)
			{
				return;
			}

			await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
		}

		private int GetNextIndex()
		{
			if (this.process is null)
			{
				return -1;
			}

			int Start = this.currentPageIndex + 1;

			while (Start < this.Pages.Count && !this.Pages[Start].IsVisible(this.process.Values))
			{
				Start++;
			}
			return Start;
		}

		private int GetPreviousIndex()
		{
			if (this.process is null)
			{
				return -1;
			}

			int Start = this.currentPageIndex-1;

			while (Start >= 0 && !this.Pages[Start].IsVisible(this.process.Values))
			{
				Start--;
			}
			return Start;
		}

		private bool CanExecuteNext()
		{
			if (this.CurrentPage is null)
			{
				return false;
			}

			IEnumerable<ObservableKycField> AllVisibleFields = this.CurrentPage.VisibleFields;

			if (this.CurrentPageSections is not null && this.CurrentPageSections.All(Section => Section is not null))
				AllVisibleFields = AllVisibleFields.Concat(this.CurrentPageSections.SelectMany(Section => Section.VisibleFields));

			return AllVisibleFields.All(Field => Field.IsValid);
		}

        private async Task ExecuteNextAsync()
        {
            ServiceRef.PlatformSpecific.HideKeyboard();

            bool IsValid = await this.ValidateCurrentPageAsync();
            if (!IsValid)
            {
                return;
            }

			if (this.kycReference is not null)
			{
				this.kycReference.LastVisitedPageId = this.CurrentPage?.Id;
				this.kycReference.LastVisitedMode = "Form";
			}

			this.UpdateReference();
			await this.SaveReferenceToStorageAsync();

            // If we came here from a quick edit initiated on Summary, prefer returning to Summary
            if (this.ShouldReturnToSummary)
            {
                int firstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
                if (firstInvalid >= 0)
                {
                    // Still something to fix: jump to the first invalid page
                    this.currentPageIndex = firstInvalid;
                    this.CurrentPagePosition = this.currentPageIndex;
                    this.SetCurrentPage(this.currentPageIndex);
                    this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
                    this.ScrollUp();
                    return;
                }
                else
                {
                    // All valid: return to Summary directly
                    await this.ProcessData();
                    this.ScrollUp();
                    this.ShouldViewSummary = true;
                    this.ShouldReturnToSummary = false;
                    this.OnPropertyChanged(nameof(this.Progress));
                    this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
                    // Persist state for resume
                    this.UpdateReference();
                    await this.SaveReferenceToStorageAsync();
                    return;
                }
            }

            int NextIndex = this.GetNextIndex();
            if (NextIndex < this.Pages.Count)
            {
                this.currentPageIndex = NextIndex;
                this.CurrentPagePosition = NextIndex;
                this.SetCurrentPage(this.currentPageIndex);
                this.ScrollUp();
            }
            else
            {
                if (this.ShouldViewSummary)
                {
                    await this.ExecuteApplyAsync();
                }
                else
                {
                    await this.ProcessData();

                    this.ScrollUp();
                    // Go to Summary Page
                    this.ShouldViewSummary = true;

                    this.OnPropertyChanged(nameof(this.Progress));

                    // Persist 100% progress and summary mode when entering summary via Next
                    if (this.kycReference is not null)
                    {
                        this.kycReference.LastVisitedMode = "Summary";
                    }
                    this.UpdateReference();
                    await this.SaveReferenceToStorageAsync();

                    this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
                }
            }
        }

		public event EventHandler? ScrollToTop;

		private void ScrollUp()
		{
			ScrollToTop?.Invoke(this, EventArgs.Empty); // scroll to Y=0
		}

		protected override Task OnDispose()
		{
			ServiceRef.XmppService.IdentityApplicationChanged -= this.XmppService_IdentityApplicationChanged;
			return base.OnDispose();
		}

		[RelayCommand]
		private async Task GoToSummaryAsync()
		{
			bool IsValid = await this.ValidateCurrentPageAsync();
			if (!IsValid)
			{
				return;
			}

			this.UpdateReference();
			await this.SaveReferenceToStorageAsync();
			await this.ProcessData();

			this.currentPageIndex = this.Pages.Count;
			this.currentPageIndex = this.GetPreviousIndex();
			this.CurrentPagePosition = this.currentPageIndex;
			this.SetCurrentPage(this.currentPageIndex);

			this.ScrollUp();
			this.ShouldViewSummary = true;
			this.ShouldReturnToSummary = false;

			// Persist last visited mode: Summary
			if (this.kycReference is not null)
			{
				this.kycReference.LastVisitedMode = "Summary";
				this.UpdateReference();
				await this.SaveReferenceToStorageAsync();
			}

			this.OnPropertyChanged(nameof(this.Progress));
			this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
		}

		private async Task ExecutePrevious()
		{
			if (this.ShouldViewSummary)
			{
				this.ScrollUp();
				this.ShouldViewSummary = false;
				this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
				this.OnPropertyChanged(nameof(this.Progress));
				return;
			}

			this.UpdateReference();
			await this.SaveReferenceToStorageAsync();

			int PreviousIndex = this.GetPreviousIndex();
			if (PreviousIndex >= 0)
			{
				this.ScrollUp();

				this.currentPageIndex = PreviousIndex;
				this.CurrentPagePosition = PreviousIndex;
				this.SetCurrentPage(this.currentPageIndex);
				await this.ValidateCurrentPageAsync();
			}
			else
			{
				await base.GoBack();
			}
		}

		public override async Task GoBack()
		{
			if (this.ShouldReturnToSummary)
			{
				// Only go to summary if all visible pages are valid; otherwise navigate to first invalid.
				int firstInvalid = await this.GetFirstInvalidVisiblePageIndexAsync();
				if (firstInvalid >= 0)
				{
					this.ShouldViewSummary = false;
					this.currentPageIndex = firstInvalid;
					this.CurrentPagePosition = this.currentPageIndex;
					this.SetCurrentPage(this.currentPageIndex);
					this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;
				}
				else
				{
					await this.GoToSummaryAsync();
				}
				return;
			}

			await this.ExecutePrevious();
		}

		[RelayCommand]
		public async Task Exit()
		{
			this.UpdateReference();
			await this.SaveReferenceToStorageAsync();

			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.Kyc_Exit)]))
				return;

			await base.GoBack();
		}

		private async Task<bool> ValidateCurrentPageAsync()
		{
			if (this.CurrentPage is null)
			{
				return false;
			}

			bool IsOk = true;
			IEnumerable<ObservableKycField> Fields = this.CurrentPage.VisibleFields;

			if (this.CurrentPageSections is not null && this.CurrentPageSections.All(Section => Section is not null))
				Fields = Fields.Concat(this.CurrentPageSections.SelectMany(Section => Section.VisibleFields));

			string Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

			List<Task> ValidationTasks = new();

			foreach (ObservableKycField Field in Fields)
			{
				Field.ValidationTask.Run();
				Task ValidationTask = MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await Field.ValidationTask.WaitAllAsync();
					if (!Field.IsValid)
						IsOk = false;
				});
				ValidationTasks.Add(ValidationTask);
			}

			await Task.WhenAll(ValidationTasks);

			if (IsOk && this.process is not null)
			{
				foreach (ObservableKycField Field in Fields)
				{
					this.process.Values[Field.Id] = Field.StringValue;
				}
			}

			MainThread.BeginInvokeOnMainThread(
				this.NextCommand.NotifyCanExecuteChanged
			);

			return IsOk;
		}

		private async Task<int> GetFirstInvalidVisiblePageIndexAsync()
		{
			if (this.process is null)
			{
				return -1;
			}

			for (int i = 0; i < this.Pages.Count; i++)
			{
				KycPage page = this.Pages[i];
				if (!page.IsVisible(this.process.Values))
				{
					continue;
				}

				IEnumerable<ObservableKycField> fields = page.VisibleFields;
				ReadOnlyObservableCollection<KycSection> sections = page.VisibleSections;
				if (sections is not null)
					fields = fields.Concat(sections.SelectMany(s => s.VisibleFields));

				bool ok = true;
				List<Task> validationTasks = new();
				foreach (ObservableKycField field in fields)
				{
					field.ValidationTask.Run();
					Task t = MainThread.InvokeOnMainThreadAsync(async () =>
					{
						await field.ValidationTask.WaitAllAsync();
						if (!field.IsValid)
							ok = false;
					});
					validationTasks.Add(t);
				}
				await Task.WhenAll(validationTasks);
				if (!ok)
				{
					return i;
				}
			}

			return -1;
		}

		[RelayCommand]
		private async Task ExecuteApplyAsync()
		{
			if (this.applicationSent)
				return;

			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToSendThisIdApplication)]))
				return;

			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.SignApplication, true))
				return;

			if (this.attachments is null || this.mappedValues is null)
				return;

			// Do not log PII or attachment data.

			// Submit the registration
			// Use IdentityFields and Attachments to submit the KYC process

			bool HasIdWithPrivateKey = ServiceRef.TagProfile.LegalIdentity is not null &&
					  await ServiceRef.XmppService.HasPrivateKey(ServiceRef.TagProfile.LegalIdentity.Id);

			(bool Succeeded, LegalIdentity? AddedIdentity) = await ServiceRef.NetworkService.TryRequest(() =>
				 ServiceRef.XmppService.AddLegalIdentity(this.mappedValues.ToArray(), !HasIdWithPrivateKey, this.attachments.ToArray()));

			if (Succeeded && AddedIdentity is not null)
			{
				await ServiceRef.TagProfile.SetIdentityApplication(AddedIdentity, true);
				this.applicationSent = true;

				// Persist reference to created identity on the draft KYC reference, if available
				try
				{
					if (this.kycReference is not null)
					{
						this.kycReference.CreatedIdentityId = AddedIdentity.Id;
						this.kycReference.CreatedIdentityState = AddedIdentity.State;
						this.kycReference.UpdatedUtc = DateTime.UtcNow;
						await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
					}
				}
				catch (Exception Ex)
				{
					ServiceRef.LogService.LogException(Ex);
				}

				// Loop through each local attachment and add it to the cache.
				// We assume the server returns attachments with the same FileName as those we built.
				foreach (LegalIdentityAttachment LocalAttachment in this.attachments)
				{
					// Find the matching attachment in the returned identity by filename.
					Attachment? MatchingAttachment = AddedIdentity.Attachments
						 .FirstOrDefault(a => string.Equals(a.FileName, LocalAttachment.FileName, StringComparison.OrdinalIgnoreCase));
					if (MatchingAttachment != null && LocalAttachment.Data is not null && LocalAttachment.ContentType is not null)
					{
						await ServiceRef.AttachmentCacheService.Add(
							 MatchingAttachment.Url,
							 AddedIdentity.Id,
							 true,
							 LocalAttachment.Data, // from our local attachment
							 LocalAttachment.ContentType);
					}
				}

				// Stay on page, switch to post-submission state and load peer review attributes
				this.applicationSent = true;
				this.applicationId = AddedIdentity.Id;
				this.ApplicationSentPublic = true;
				this.NrReviews = ServiceRef.TagProfile.NrReviews;
				await this.LoadApplicationAttributes();
				await this.LoadFeaturedPeerReviewers();
				this.ShouldViewSummary = true;
				this.OnPropertyChanged(nameof(this.Progress));
			}
		}

		private async Task LoadApplicationAttributes()
		{
			try
			{
				IdApplicationAttributesEventArgs e = await ServiceRef.XmppService.GetIdApplicationAttributes();
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.PeerReview = e.PeerReview;
					this.NrReviewers = e.NrReviewers;
				});
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
			}
		}

		private async Task LoadFeaturedPeerReviewers()
		{
			await ServiceRef.NetworkService.TryRequest(async () =>
			{
				this.peerReviewServices = await ServiceRef.XmppService.GetServiceProvidersForPeerReviewAsync();
				MainThread.BeginInvokeOnMainThread(() =>
				{
					this.HasFeaturedPeerReviewers = (this.peerReviewServices?.Length ?? 0) > 0;
				});
			});
		}

		private async Task XmppService_IdentityApplicationChanged(object? Sender, LegalIdentityEventArgs e)
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				this.ApplicationSentPublic = ServiceRef.TagProfile.IdentityApplication is not null;
				this.NrReviews = ServiceRef.TagProfile.NrReviews;
				if (this.ApplicationSentPublic && this.peerReviewServices is null)
				{
					await this.LoadFeaturedPeerReviewers();
				}
			});
		}

		[RelayCommand(CanExecute = nameof(ApplicationSentPublic))]
		private async Task ScanQrCode()
		{
			string? Url = await Services.UI.QR.QrCode.ScanQrCode(nameof(AppResources.QrPageTitleScanPeerId),
				[
					Constants.UriSchemes.IotId
				]);

			if (string.IsNullOrEmpty(Url) || !Constants.UriSchemes.StartsWithIdScheme(Url))
				return;

			await this.SendPeerReviewRequest(Constants.UriSchemes.RemoveScheme(Url));
		}

		private async Task SendPeerReviewRequest(string? ReviewerId)
		{
			LegalIdentity? ToReview = ServiceRef.TagProfile.IdentityApplication;
			if (ToReview is null || string.IsNullOrEmpty(ReviewerId))
				return;

			try
			{
				await ServiceRef.XmppService.PetitionPeerReviewId(ReviewerId, ToReview, Guid.NewGuid().ToString(),
					ServiceRef.Localizer[nameof(AppResources.CouldYouPleaseReviewMyIdentityInformation)]);

				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.PetitionSent)],
					ServiceRef.Localizer[nameof(AppResources.APetitionHasBeenSentToYourPeer)]);
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		[RelayCommand(CanExecute = nameof(CanRequestFeaturedPeerReviewer))]
		private async Task RequestReview()
		{
			if (this.peerReviewServices is null)
				await this.LoadFeaturedPeerReviewers();

			if ((this.peerReviewServices?.Length ?? 0) > 0)
			{
				List<ServiceProviderWithLegalId> ServiceProviders = [.. this.peerReviewServices, new RequestFromPeer()];

				ServiceProvidersNavigationArgs e = new([.. ServiceProviders],
					ServiceRef.Localizer[nameof(AppResources.RequestReview)],
					ServiceRef.Localizer[nameof(AppResources.SelectServiceProviderPeerReview)]);

				await ServiceRef.UiService.GoToAsync(nameof(ServiceProvidersPage), e, Services.UI.BackMethod.Pop);

				if (e.ServiceProvider is not null)
				{
					IServiceProvider? ServiceProvider = await e.ServiceProvider.Task;

					if (ServiceProvider is ServiceProviderWithLegalId ServiceProviderWithLegalId &&
						!string.IsNullOrEmpty(ServiceProviderWithLegalId.LegalId))
					{
						if (!ServiceProviderWithLegalId.External)
						{
							if (!await ServiceRef.NetworkService.TryRequest(async () =>
								await ServiceRef.XmppService.SelectPeerReviewService(ServiceProvider.Id, ServiceProvider.Type)))
							{
								return;
							}
						}

						await this.SendPeerReviewRequest(ServiceProviderWithLegalId.LegalId);
						return;
					}
					else if (ServiceProvider is null)
						return;
				}
			}

			await this.ScanQrCode();
		}

		[RelayCommand(CanExecute = nameof(ApplicationSentPublic))]
		private async Task RevokeApplication()
		{
			LegalIdentity? Application = ServiceRef.TagProfile.IdentityApplication;
			if (Application is null)
			{
				this.ApplicationSentPublic = false;
				this.peerReviewServices = null;
				this.HasFeaturedPeerReviewers = false;
				// Revert local reference to draft state if available
				if (this.kycReference is not null)
				{
					this.kycReference.CreatedIdentityId = null;
					this.kycReference.CreatedIdentityState = null;
					this.kycReference.UpdatedUtc = DateTime.UtcNow;
					await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
				}
				return;
			}

			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToRevokeTheCurrentIdApplication)]))
				return;

			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.RevokeApplication, true))
				return;

			try
			{
				await ServiceRef.XmppService.ObsoleteLegalIdentity(Application.Id);
				await ServiceRef.TagProfile.SetIdentityApplication(null, true);
				this.ApplicationSentPublic = false;
				this.peerReviewServices = null;
				this.HasFeaturedPeerReviewers = false;
				// Clear identity pointers on the local KYC reference so Applications page does not show "Pending..."
				if (this.kycReference is not null)
				{
					this.kycReference.CreatedIdentityId = null;
					this.kycReference.CreatedIdentityState = null;
					this.kycReference.UpdatedUtc = DateTime.UtcNow;
					await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
				}

				// Stay on Summary after revoke to keep context clear
				await this.ProcessData();
				this.ShouldViewSummary = true;
				this.OnPropertyChanged(nameof(this.Progress));
			}
			catch (Exception ex)
			{
				ServiceRef.LogService.LogException(ex);
				await ServiceRef.UiService.DisplayException(ex);
			}
		}

		private Task ProcessData()
		{
			this.mappedValues = new();
			this.attachments = new();

			// Map all values and submit
			if (this.process is null)
			{
				return Task.CompletedTask;
			}

			//For each page
			foreach (KycPage Page in this.process.Pages)
			{
				if (!Page.IsVisible(this.process.Values))
				{
					continue; // Skip invisible pages
				}

				// For each field in the page
				foreach (ObservableKycField Field in Page.VisibleFields)
				{
					if (this.CheckAndHandleFile(Field, this.attachments))
					{
						continue; // File handled, skip further processing
					}
					foreach (Property Prop in this.ApplyTransform(Field))
					{
						this.mappedValues.Add(Prop);
					}
				}
				// For each section in the page
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.VisibleFields)
					{
						if (this.CheckAndHandleFile(Field, this.attachments))
						{
							continue; // File handled, skip further processing
						}
						foreach (Property Prop in this.ApplyTransform(Field))
						{
							this.mappedValues.Add(Prop);
						}
					}
				}
			}

			/// Get values from TagProfile if not already mapped else get from current id, if any.
			string? JID = null;
			string? Phone = null;
			string? EMail = null;
			if (ServiceRef.TagProfile.LegalIdentity is not null)
			{
				JID = ServiceRef.TagProfile.LegalIdentity?.Properties.Where((P) => P.Name == Constants.XmppProperties.Jid).FirstOrDefault()?.Value ?? string.Empty;
				Phone = ServiceRef.TagProfile.LegalIdentity?.Properties.Where((P) => P.Name == Constants.XmppProperties.Phone).FirstOrDefault()?.Value ?? string.Empty;
				EMail = ServiceRef.TagProfile.LegalIdentity?.Properties.Where((P) => P.Name == Constants.XmppProperties.EMail).FirstOrDefault()?.Value ?? string.Empty;
			}

			if(string.IsNullOrEmpty(JID))
				JID = ServiceRef.XmppService.BareJid ?? string.Empty;
			if(string.IsNullOrEmpty(Phone))
				Phone = ServiceRef.TagProfile.PhoneNumber ?? string.Empty;
			if(string.IsNullOrEmpty(EMail))
				EMail = ServiceRef.TagProfile.EMail ?? string.Empty;

			// Add special properties
			this.mappedValues.Add(new Property(Constants.XmppProperties.DeviceId, ServiceRef.PlatformSpecific.GetDeviceId()));

			if(!this.process.HasMapping(Constants.XmppProperties.Jid))
				this.mappedValues.Add(new Property(Constants.XmppProperties.Jid, JID));
			if(!this.process.HasMapping(Constants.XmppProperties.Phone))
				this.mappedValues.Add(new Property(Constants.XmppProperties.Phone, Phone));
			if(!this.process.HasMapping(Constants.XmppProperties.EMail))
				this.mappedValues.Add(new Property(Constants.XmppProperties.EMail, EMail));

			if (!this.process.HasMapping(Constants.XmppProperties.Country) && !string.IsNullOrEmpty(ServiceRef.TagProfile.SelectedCountry))
				this.mappedValues.Add(new Property(Constants.XmppProperties.Country, ServiceRef.TagProfile.SelectedCountry));

			this.GenerateSummaryCollection();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Applies mapping transforms for a given field and returns a list of identity properties.
		/// </summary>
		/// <returns>List of properties like Key=identity field name, Value=transformed value.</returns>
		private List<Property> ApplyTransform(ObservableKycField Field)
		{
			if (Field is null)
			{
				return new List<Property>();
			}

			if (Field.Condition is not null)
			{
				if (Field is null || Field.Mappings is null || Field.Mappings.Count == 0 || !Field.Condition!.Evaluate(this.process!.Values))
				{
					return new List<Property>();
				}
			}

			List<Property> Result = new();

			string FieldValue = Field.StringValue?.Trim() ?? string.Empty;

			if (Field.Mappings is null || Field.Mappings.Count == 0)
			{
				return Result;
			}

			foreach (KycMapping Map in Field.Mappings)
			{
				if (string.IsNullOrEmpty(FieldValue))
				{
					continue;
				}

				Property Property = new Property { Name = Map.Key };

				Property.Value = Map.Transform switch
				{
					// Examples
					"uppercase" => FieldValue.ToUpperInvariant(),
					"lowercase" => FieldValue.ToLowerInvariant(),
					"trim" => FieldValue.Trim(),
					"year" => DateTime.TryParse(FieldValue, out DateTime Dt) ? Dt.Year.ToString(CultureInfo.InvariantCulture) : "",
					"month" => DateTime.TryParse(FieldValue, out DateTime Dt) ? Dt.Month.ToString(CultureInfo.InvariantCulture) : "",
					"day" => DateTime.TryParse(FieldValue, out DateTime Dt) ? Dt.Day.ToString(CultureInfo.InvariantCulture) : "",
					_ => FieldValue
				};

				Result.Add(Property);
			}

			return Result;
		}

		private bool CheckAndHandleFile(ObservableKycField Field, List<LegalIdentityAttachment> Attachments)
		{
			if (Field.Condition is not null)
			{
				if (Field is null || Field.Mappings is null || Field.Mappings.Count == 0 || !Field.Condition!.Evaluate(this.process!.Values))
				{
					return false;
				}
			}

			if (Field.StringValue is not null && Field.StringValue.Length > 0)
			{
				if (Field is ObservableImageField ImageField)
				{
					Attachments.Add(
						new LegalIdentityAttachment(
							ImageField.Mappings.First().Key + ".jpg",
							Constants.MimeTypes.Jpeg,
							CompressImage(
								new MemoryStream(
									Convert.FromBase64String(ImageField.StringValue!)
								)
							)!
						)
					);
					return true;
				}
			}

			return false;
		}

		private static byte[]? CompressImage(Stream inputStream)
		{
			try
			{
				using SKManagedStream ManagedStream = new(inputStream);
				using SKData ImageData = SKData.Create(ManagedStream);

				using SKCodec Codec = SKCodec.Create(ImageData);
				SKBitmap SkBitmap = SKBitmap.Decode(ImageData);

				SkBitmap = HandleOrientation(SkBitmap, Codec.EncodedOrigin);

				bool Resize = false;
				int Height = SkBitmap.Height;
				int Width = SkBitmap.Width;

				// downsample to FHD
				if ((Width >= Height) && (Width > 1920))
				{
					Height = (int)(Height * (1920.0 / Width) + 0.5);
					Width = 1920;
					Resize = true;
				}
				else if ((Height > Width) && (Height > 1920))
				{
					Width = (int)(Width * (1920.0 / Height) + 0.5);
					Height = 1920;
					Resize = true;
				}

				if (Resize)
				{
					SKImageInfo Info = SkBitmap.Info;
					SKImageInfo NewInfo = new(Width, Height, Info.ColorType, Info.AlphaType, Info.ColorSpace);
					SKBitmap? Resized = SkBitmap.Resize(NewInfo, SKFilterQuality.High);
					if (Resized is not null)
					{
						SkBitmap.Dispose();
						SkBitmap = Resized;
					}
				}

				byte[] Bytes;
				using (SKData Encoded = SkBitmap.Encode(SKEncodedImageFormat.Jpeg, 80))
				{
					Bytes = Encoded.ToArray();
				}

				SkBitmap.Dispose();
				return Bytes;
			}
			catch (Exception Ex)
			{
				ServiceRef.LogService.LogException(Ex);
				return null;
			}
		}

		private static SKBitmap HandleOrientation(SKBitmap Bitmap, SKEncodedOrigin Orientation)
		{
			SKBitmap Rotated;

			switch (Orientation)
			{
				case SKEncodedOrigin.BottomRight:
					Rotated = new SKBitmap(Bitmap.Width, Bitmap.Height);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.RotateDegrees(180, Bitmap.Width / 2, Bitmap.Height / 2);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.RightTop:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(Rotated.Width, 0);
						Surface.RotateDegrees(90);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.LeftBottom:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(0, Rotated.Height);
						Surface.RotateDegrees(270);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				default:
					return Bitmap;
			}

			return Rotated;
		}

		private void GenerateSummaryCollection()
		{
			this.PersonalInformationSummary = new ObservableCollection<DisplayQuad>();
			this.AddressInformationSummary = new ObservableCollection<DisplayQuad>();
			this.AttachmentInformationSummary = new ObservableCollection<DisplayQuad>();
			this.CompanyInformationSummary = new ObservableCollection<DisplayQuad>();
			this.CompanyAddressSummary = new ObservableCollection<DisplayQuad>();
			this.CompanyRepresentativeSummary = new ObservableCollection<DisplayQuad>();

			if (this.process is null)
			{
				return;
			}
			// Build summary via shared formatter; mark invalid items, if any
			ISet<string> InvalidMappings = this.BuildInvalidMappingSetFromReference();
			IdentitySummaryFormatter.KycSummaryResult Summary = IdentitySummaryFormatter.BuildKycSummaryFromProperties(
				this.mappedValues,
				this.process,
				this.attachments.Select(a => new IdentitySummaryFormatter.AttachmentInfo(a.FileName ?? string.Empty, a.ContentType)),
				CultureInfo.CurrentCulture,
				InvalidMappings
			);

			foreach (DisplayQuad Quad in Summary.Personal)
			{
				this.PersonalInformationSummary.Add(Quad);
			}
			this.HasPersonalInformation = this.PersonalInformationSummary.Count > 0;

			foreach (DisplayQuad Quad in Summary.Address)
			{
				this.AddressInformationSummary.Add(Quad);
			}
			this.HasAddressInformation = this.AddressInformationSummary.Count > 0;

			foreach (DisplayQuad Quad in Summary.Attachments)
			{
				this.AttachmentInformationSummary.Add(Quad);
			}
			this.HasAttachments = this.AttachmentInformationSummary.Count > 0;

			foreach (DisplayQuad Quad in Summary.CompanyInfo)
			{
				this.CompanyInformationSummary.Add(Quad);
			}
			this.HasCompanyInformation = this.CompanyInformationSummary.Count > 0;

			foreach (DisplayQuad Quad in Summary.CompanyAddress)
			{
				this.CompanyAddressSummary.Add(Quad);
			}
			this.HasCompanyAddress = this.CompanyAddressSummary.Count > 0;

			foreach (DisplayQuad Quad in Summary.CompanyRepresentative)
			{
				this.CompanyRepresentativeSummary.Add(Quad);
			}
			this.HasCompanyRepresentative = this.CompanyRepresentativeSummary.Count > 0;
		}

		private ISet<string> BuildInvalidMappingSetFromReference()
		{
			HashSet<string> Invalid = new(StringComparer.OrdinalIgnoreCase);
			if (this.kycReference is null)
				return Invalid;

			foreach (string Claim in this.kycReference.InvalidClaims ?? Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(Claim))
					continue;
				string Key = Claim.Trim();
				Invalid.Add(Key);

				// Grouped date: map parts to composed summary mapping
				if (Key.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) ||
					Key.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) ||
					Key.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase) ||
					Key.Equals(Constants.CustomXmppProperties.BirthDate, StringComparison.OrdinalIgnoreCase))
				{
					Invalid.Add(Constants.CustomXmppProperties.BirthDate);
				}

				if (Key.Equals("ORGREPBDAY", StringComparison.OrdinalIgnoreCase) ||
					Key.Equals("ORGREPBMONTH", StringComparison.OrdinalIgnoreCase) ||
					Key.Equals("ORGREPBYEAR", StringComparison.OrdinalIgnoreCase) ||
					Key.Equals("ORGREPBDATE", StringComparison.OrdinalIgnoreCase))
				{
					Invalid.Add("ORGREPBDATE");
				}
			}

			foreach (string Photo in this.kycReference.InvalidPhotos ?? Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(Photo))
					continue;
				Invalid.Add(Photo.Trim());
			}

			return Invalid;
		}

		private void ApplyInvalidationsToFieldsFromReference()
		{
			if (this.process is null || this.kycReference is null)
				return;

			IEnumerable<string> InvalidClaims = this.kycReference.InvalidClaims ?? Array.Empty<string>();
			if (!InvalidClaims.Any())
				return;

			HashSet<string> InvalidSet = new(InvalidClaims, StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> ReasonsByMapping = this.BuildInvalidReasonsByMappingFromReference();

			foreach (KycPage Page in this.process.Pages)
			{
				foreach (ObservableKycField Field in Page.AllFields)
				{
					if (Field.Mappings.Any(m => InvalidSet.Contains(m.Key) || this.IsGroupedDateMatch(m.Key, InvalidSet)))
					{
						Field.IsValid = false;
						// Use specific reason if available; else fall back to general description
						string? Reason = null;
						foreach (KycMapping Map in Field.Mappings)
						{
							if (ReasonsByMapping.TryGetValue(Map.Key, out string R))
							{
								Reason = R;
								break;
							}
						}
						Field.ValidationText = !string.IsNullOrWhiteSpace(Reason) ? Reason : (this.ErrorDescription ?? string.Empty);
					}
				}
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
					{
						if (Field.Mappings.Any(m => InvalidSet.Contains(m.Key) || this.IsGroupedDateMatch(m.Key, InvalidSet)))
						{
							Field.IsValid = false;
							string? Reason = null;
							foreach (KycMapping Map in Field.Mappings)
							{
								if (ReasonsByMapping.TryGetValue(Map.Key, out string R))
								{
									Reason = R;
									break;
								}
							}
							Field.ValidationText = !string.IsNullOrWhiteSpace(Reason) ? Reason : (this.ErrorDescription ?? string.Empty);
						}
					}
				}
			}
		}

		private Dictionary<string, string> BuildInvalidReasonsByMappingFromReference()
		{
			Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase);
			if (this.kycReference is null)
				return Map;

			foreach (KycInvalidClaim c in this.kycReference.InvalidClaimDetails ?? Array.Empty<KycInvalidClaim>())
			{
				if (c is null || string.IsNullOrWhiteSpace(c.Claim))
					continue;
				string Key = c.Claim.Trim();
				string Reason = string.IsNullOrWhiteSpace(c.Reason) ? this.ErrorDescription ?? string.Empty : c.Reason;
				Map[Key] = Reason;
				// grouped date support: if any part has a reason, map composed keys as well
				if (Key.Equals(Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) ||
					Key.Equals(Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) ||
					Key.Equals(Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase) ||
					Key.Equals("BDATE", StringComparison.OrdinalIgnoreCase))
				{
					Map[Constants.CustomXmppProperties.BirthDate] = Reason;
				}
				if (Key.Equals("ORGREPBDAY", StringComparison.OrdinalIgnoreCase) ||
					Key.Equals("ORGREPBMONTH", StringComparison.OrdinalIgnoreCase) ||
					Key.Equals("ORGREPBYEAR", StringComparison.OrdinalIgnoreCase) ||
					Key.Equals("ORGREPBDATE", StringComparison.OrdinalIgnoreCase))
				{
					Map["ORGREPBDATE"] = Reason;
				}
			}

			foreach (KycInvalidPhoto p in this.kycReference.InvalidPhotoDetails ?? Array.Empty<KycInvalidPhoto>())
			{
				if (p is null)
					continue;
				string Key = string.IsNullOrWhiteSpace(p.Mapping) ? (p.FileName ?? string.Empty) : p.Mapping;
				if (string.IsNullOrWhiteSpace(Key))
					continue;
				string Reason = string.IsNullOrWhiteSpace(p.Reason) ? this.ErrorDescription ?? string.Empty : p.Reason;
				Map[Key.Trim()] = Reason;
			}

			return Map;
		}

		private bool IsGroupedDateMatch(string MappingKey, ISet<string> InvalidSet)
		{
			if (string.Equals(MappingKey, Constants.XmppProperties.BirthDay, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(MappingKey, Constants.XmppProperties.BirthMonth, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(MappingKey, Constants.XmppProperties.BirthYear, StringComparison.OrdinalIgnoreCase))
			{
				return InvalidSet.Contains("BDATE") ||
					InvalidSet.Contains(Constants.XmppProperties.BirthDay) ||
					InvalidSet.Contains(Constants.XmppProperties.BirthMonth) ||
					InvalidSet.Contains(Constants.XmppProperties.BirthYear);
			}

			if (MappingKey.StartsWith("ORGREP", StringComparison.OrdinalIgnoreCase))
			{
				return InvalidSet.Contains("ORGREPBDATE") ||
					InvalidSet.Contains("ORGREPBDAY") ||
					InvalidSet.Contains("ORGREPBMONTH") ||
					InvalidSet.Contains("ORGREPBYEAR");
			}

			return false;
		}

		public async Task ApplyRejectionAsync(string Message, string[] InvalidClaims, string[] InvalidPhotos, string? Code)
		{
			this.ErrorDescription = Message;
			this.HasErrorDescription = !string.IsNullOrWhiteSpace(Message);

			if (this.kycReference is not null)
			{
				this.kycReference.RejectionMessage = Message;
				this.kycReference.RejectionCode = Code;
				this.kycReference.InvalidClaims = InvalidClaims;
				this.kycReference.InvalidPhotos = InvalidPhotos;
				this.kycReference.UpdatedUtc = DateTime.UtcNow;
				await ServiceRef.KycService.SaveKycReferenceAsync(this.kycReference);
			}

			this.ApplyInvalidationsToFieldsFromReference();
			await MainThread.InvokeOnMainThreadAsync(this.GenerateSummaryCollection);
		}
	}
}
