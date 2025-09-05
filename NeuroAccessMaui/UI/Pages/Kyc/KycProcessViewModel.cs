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


namespace NeuroAccessMaui.UI.Pages.Kyc
{
	public partial class KycProcessViewModel : BaseViewModel
	{
		private readonly KycProcessNavigationArgs? navigationArguments;
		private KycProcess? process;
		private KycReference? kycReference;
		private int currentPageIndex = 0;
		private bool applicationSent;

		private List<Property> mappedValues;
		private List<LegalIdentityAttachment> attachments;
		[ObservableProperty] private bool shouldViewSummary = false;
		[ObservableProperty] private bool shouldReturnToSummary = false;

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
		}

		protected override async Task OnInitialize()
		{
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

			this.currentPageIndex = -1;
			this.currentPageIndex = this.GetNextIndex();
			this.CurrentPagePosition = this.currentPageIndex;
			this.SetCurrentPage(this.currentPageIndex);

			// Set initial localized label for the next/apply button
			this.NextButtonText = ServiceRef.Localizer["Kyc_Next"].Value;

			MainThread.BeginInvokeOnMainThread(
				this.NextCommand.NotifyCanExecuteChanged
			);
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

			this.UpdateReference();
			await this.SaveReferenceToStorageAsync();

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

					this.NextButtonText = ServiceRef.Localizer["Kyc_Apply"].Value;
				}
			}
		}

		public event EventHandler? ScrollToTop;

		private void ScrollUp()
		{
			ScrollToTop?.Invoke(this, EventArgs.Empty); // scroll to Y=0
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
				await this.GoToSummaryAsync();
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

			// Add special properties
			this.mappedValues.Add(new Property(Constants.XmppProperties.DeviceId, ServiceRef.PlatformSpecific.GetDeviceId()));
			this.mappedValues.Add(new Property(Constants.XmppProperties.Jid, ServiceRef.XmppService.BareJid));
			this.mappedValues.Add(new Property(Constants.XmppProperties.Phone, ServiceRef.TagProfile.PhoneNumber));
			this.mappedValues.Add(new Property(Constants.XmppProperties.EMail, ServiceRef.TagProfile.EMail));

			if (!this.process.HasMapping(Constants.XmppProperties.Country) && !string.IsNullOrEmpty(ServiceRef.TagProfile.SelectedCountry))
				this.mappedValues.Add(new Property(Constants.XmppProperties.Country, ISO_3166_1.ToName(ServiceRef.TagProfile.SelectedCountry) ?? ServiceRef.TagProfile.SelectedCountry));

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

			if (this.process is null)
			{
				return;
			}
			// Build summary via shared formatter to avoid duplicating logic with ViewIdentity
			IdentitySummaryFormatter.KycSummaryResult Summary = IdentitySummaryFormatter.BuildKycSummaryFromProperties(
				this.mappedValues,
				this.process,
				this.attachments.Select(a => new IdentitySummaryFormatter.AttachmentInfo(a.FileName ?? string.Empty, a.ContentType))
			);

			foreach (DisplayQuad Triple in Summary.Personal)
			{
				this.PersonalInformationSummary.Add(Triple);
			}

			foreach (DisplayQuad Triple in Summary.Address)
			{
				this.AddressInformationSummary.Add(Triple);
			}

			foreach (DisplayQuad Triple in Summary.Attachments)
			{
				this.AttachmentInformationSummary.Add(Triple);
			}
		}
	}
}
