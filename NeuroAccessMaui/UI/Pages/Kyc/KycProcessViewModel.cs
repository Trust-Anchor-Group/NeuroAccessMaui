
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Extensions;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using NeuroAccessMaui.Services.UI.Photos;
using SkiaSharp;
using Waher.Networking.XMPP.Contracts;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	public partial class KycProcessViewModel : BaseViewModel
	{
		private KycProcess? process;
		private KycReference? kycReference;
		private int currentPageIndex = 0;
		private bool applicationSent;

		[ObservableProperty] private int currentPagePosition;
		[ObservableProperty] private KycPage? currentPage;
		[ObservableProperty] private string? currentPageTitle;
		[ObservableProperty] private string? currentPageDescription;
		[ObservableProperty] private bool hasCurrentPageDescription;
		[ObservableProperty] private ReadOnlyObservableCollection<KycSection>? currentPageSections;
		[ObservableProperty] private bool hasSections;
		[ObservableProperty] private string nextButtonText = "Next";

		public string BannerUriLight => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallLight);
		public string BannerUriDark => ServiceRef.ThemeService.GetImageUri(Constants.Branding.BannerSmallDark);

		public string BannerUri =>
			Application.Current.RequestedTheme switch
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
					return 0;
				}

				List<KycPage> VisiblePages = [.. this.Pages.Where(Page => Page.IsVisible(this.process.Values))];
				if (VisiblePages.Count == 0)
				{
					return 0;
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

		public KycProcessViewModel()
		{
			this.NextCommand = new AsyncRelayCommand(this.ExecuteNextAsync, this.CanExecuteNext);
			this.PreviousCommand = new AsyncRelayCommand(this.ExecutePrevious);
		}

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			// TODO: Load the KYC process as KycReference from serviceRef.KycService
			this.kycReference = await ServiceRef.KycService.LoadKycReferenceAsync(
					 "NeuroAccessMaui.Resources.Raw.TestKYCNeuro.xml",
					 "en"
			 );

			this.process = await this.kycReference.ToProcess();

			if (this.process is null) return; // TODO: Check and handle null process

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

			this.currentPageIndex = this.GetNextIndex(0);
			this.CurrentPagePosition = this.currentPageIndex;
			this.SetCurrentPage(this.currentPageIndex);

			this.NextCommand.NotifyCanExecuteChanged();
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
			if (E.PropertyName == nameof(ObservableKycField.StringValue))
			{
				this.SetCurrentPage(this.currentPageIndex);
				this.NextCommand.NotifyCanExecuteChanged();
			}
		}

		private void Section_PropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs E)
		{
			if (E.PropertyName == nameof(KycSection.IsVisible))
			{
				this.SetCurrentPage(this.currentPageIndex);
			}
		}

		private void Page_PropertyChanged(object? Sender, System.ComponentModel.PropertyChangedEventArgs E)
		{
			if (E.PropertyName == nameof(KycPage.IsVisible))
			{
				this.SetCurrentPage(this.currentPageIndex);
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

			int NextIndex = this.GetNextIndex(Index + 1);
			this.NextButtonText = NextIndex >= this.Pages.Count ? "Apply" : "Next";

			this.OnPropertyChanged(nameof(this.Progress));

			this.UpdateReference();
			this.SaveReferenceToStorage();
		}

		private void UpdateReference()
		{
			if (this.process is null)
			{
				return;
			}
			this.kycReference!.Fields = [.. this.process.Values.Select(p => new KycFieldValue(p.Key, p.Value))];
		}

		private void SaveReferenceToStorage()
		{
			if (this.kycReference is null)
			{
				return;
			}

			ServiceRef.KycService.SaveKycReference(this.kycReference);
		}

		private int GetNextIndex(int Start)
		{
			if (this.process is null)
			{
				return -1;
			}

			while (Start < this.Pages.Count && !this.Pages[Start].IsVisible(this.process.Values))
			{
				Start++;
			}
			return Start;
		}

		private int GetPreviousIndex(int Start)
		{
			if (this.process is null)
			{
				return -1;
			}

			while (Start >= 0 && !this.Pages[Start].IsVisible(this.process.Values))
			{
				Start--;
			}
			return Start;
		}

		private bool CanExecuteNext()
		{
			if (this.CurrentPage is null || this.CurrentPageSections is null)
			{
				return false;
			}

			IEnumerable<ObservableKycField> AllVisibleFields = this.CurrentPage.VisibleFields
				.Concat(this.CurrentPageSections.SelectMany(Section => Section.VisibleFields));
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

			int NextIndex = this.GetNextIndex(this.currentPageIndex + 1);
			if (NextIndex < this.Pages.Count)
			{
				this.currentPageIndex = NextIndex;
				this.CurrentPagePosition = NextIndex;
				this.SetCurrentPage(this.currentPageIndex);
			}
			else
			{
				await this.ExecuteApplyAsync();
			}
		}

		private async Task ExecutePrevious()
		{
			int PreviousIndex = this.GetPreviousIndex(this.currentPageIndex - 1);
			if (PreviousIndex >= 0)
			{
				this.currentPageIndex = PreviousIndex;
				this.CurrentPagePosition = PreviousIndex;
				this.SetCurrentPage(this.currentPageIndex);
			}
			else
			{
				await base.GoBack();
			}
		}

		public override async Task GoBack()
		{
			await this.ExecutePrevious();
		}

		private async Task<bool> ValidateCurrentPageAsync()
		{
			if (this.CurrentPage is null || this.CurrentPageSections is null)
			{
				return false;
			}

			bool IsOk = true;
			IEnumerable<ObservableKycField> Fields = this.CurrentPage.VisibleFields
				.Concat(this.CurrentPageSections.SelectMany(Section => Section.VisibleFields));

			foreach (ObservableKycField Field in Fields)
			{
				if (!Field.Validate("en"))
				{
					IsOk = false;
				}
			}

			if (IsOk && this.process is not null)
			{
				foreach (ObservableKycField Field in Fields)
				{
					this.process.Values[Field.Id] = Field.StringValue;
				}
			}

			this.NextCommand.NotifyCanExecuteChanged();

			return IsOk;
		}

		private async Task ExecuteApplyAsync()
		{
			if (this.applicationSent)
				return;

			if (!await AreYouSure(ServiceRef.Localizer[nameof(AppResources.AreYouSureYouWantToSendThisIdApplication)]))
				return;

			if (!await App.AuthenticateUserAsync(AuthenticationPurpose.SignApplication, true))
				return;

			List<Property> MappedValues = new();
			List<LegalIdentityAttachment> Attachments = new();

			// Map all values and submit
			if (this.process is null)
			{
				return;
			}

			//For each page
			foreach (KycPage Page in this.process.Pages)
			{
				bool PageValid = await this.ValidateCurrentPageAsync();
				Console.WriteLine($"Page: {Page.Id} Valid: {PageValid}");

				// For each field in the page
				foreach (ObservableKycField Field in Page.AllFields)
				{
					if (this.CheckAndHandleFile(Field, Attachments))
					{
						continue; // File handled, skip further processing
					}
					foreach (Property Prop in this.ApplyTansform(Field))
					{
						MappedValues.Add(Prop);
					}
				}
				// For each section in the page
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
					{
						if (this.CheckAndHandleFile(Field, Attachments))
						{
							continue; // File handled, skip further processing
						}
						foreach (Property Prop in this.ApplyTansform(Field))
						{
							MappedValues.Add(Prop);
						}
					}
				}
			}

			// Add special properties
			MappedValues.Add(new Property(Constants.XmppProperties.DeviceId, ServiceRef.PlatformSpecific.GetDeviceId()));
			MappedValues.Add(new Property(Constants.XmppProperties.Jid, ServiceRef.XmppService.BareJid));
			MappedValues.Add(new Property(Constants.XmppProperties.Phone, ServiceRef.TagProfile.PhoneNumber));
			MappedValues.Add(new Property(Constants.XmppProperties.EMail, ServiceRef.TagProfile.EMail));

			foreach (Property Prop in MappedValues)
			{
				Console.WriteLine($"Mapped: {Prop.Name} = {Prop.Value}");
			}

			foreach (LegalIdentityAttachment a in Attachments)
			{
				Console.WriteLine($"Attachment: {a.FileName} ({a.ContentType}) {a.Data}");
			}

			// Submit the registration
			// Use IdentityFields and Attachments to submit the KYC process

			bool HasIdWithPrivateKey = ServiceRef.TagProfile.LegalIdentity is not null &&
					  await ServiceRef.XmppService.HasPrivateKey(ServiceRef.TagProfile.LegalIdentity.Id);

			(bool Succeeded, LegalIdentity? AddedIdentity) = await ServiceRef.NetworkService.TryRequest(() =>
				 ServiceRef.XmppService.AddLegalIdentity(MappedValues.ToArray(), !HasIdWithPrivateKey, Attachments.ToArray()));

			if (Succeeded && AddedIdentity is not null)
			{
				await ServiceRef.TagProfile.SetIdentityApplication(AddedIdentity, true);
				this.applicationSent = true;

				// Loop through each local attachment and add it to the cache.
				// We assume the server returns attachments with the same FileName as those we built.
				foreach (LegalIdentityAttachment LocalAttachment in Attachments)
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

		/// <summary>
		/// Given a list of Mappings, retuns a dictionary with the mapping keys and transformed values.
		/// </summary>
		/// <param name="Mappings"></param>
		/// <param name="FieldValue"></param>
		/// <returns> Key value pair: Key = Field name in identity, Value = Transformed Value. For example: BYEAR: 1999</returns>
		private List<Property> ApplyTansform(ObservableKycField Field)
		{
			if (Field.Condition is not null)
			{
				if (Field is null || Field.Mappings is null || Field.Mappings.Count == 0 || !Field.Condition!.Evaluate(this.process!.Values))
				{
					return new List<Property>();
				}
			}

			List<Property> Result = new();

			string FieldValue = Field.StringValue?.Trim() ?? string.Empty;

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

				SKCodec Codec = SKCodec.Create(ImageData);
				SKBitmap SkBitmap = SKBitmap.Decode(ImageData);

				SkBitmap = HandleOrientation(SkBitmap, Codec.EncodedOrigin);

				bool Resize = false;
				int Height = SkBitmap.Height;
				int Width = SkBitmap.Width;

				// downdsample to FHD
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
					SkBitmap = SkBitmap.Resize(NewInfo, SKFilterQuality.High);
				}

				return SkBitmap.Encode(SKEncodedImageFormat.Jpeg, 80).ToArray();
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

	}
}
