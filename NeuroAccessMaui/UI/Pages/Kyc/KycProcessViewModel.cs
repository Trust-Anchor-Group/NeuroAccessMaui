using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Services.Kyc.ViewModels;
using NeuroAccessMaui.UI.Pages.Registration;

namespace NeuroAccessMaui.UI.Pages.Kyc
{
	public partial class KycProcessViewModel : BaseViewModel
	{
		private KycProcess? process;
		private int currentPageIndex = 0;

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

			this.process = await ServiceRef.KycService.LoadProcessAsync(
					 "NeuroAccessMaui.Resources.Raw.TestKYC.xml",
					 "en"
			 );

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

			this.process.LoadFieldsAsync();

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

			this.process?.SaveFieldsToStorage();
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
			Dictionary<string, string> MappedValues = new();

			// Map all values and submit
			if (this.process is null)
			{
				return;
			}

			//For each page
			foreach (KycPage Page in this.process.Pages)
			{
				// For each field in the page
				foreach (ObservableKycField Field in Page.AllFields)
				{
					foreach (KeyValuePair<string, string> Kvp in this.ApplyTansform(Field.Mappings, Field.StringValue ?? ""))
					{
						MappedValues[Kvp.Key] = Kvp.Value;
					}
				}
				// For each section in the page
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
					{
						foreach (KeyValuePair<string, string> Kvp in this.ApplyTansform(Field.Mappings, Field.StringValue ?? ""))
						{
							MappedValues[Kvp.Key] = Kvp.Value;
						}
					}
				}
			}

			// Create a register Identity Model object
			RegisterIdentityModel RegisterModel = new();

			bool IsOrg = false;
			string DocumentType = string.Empty;
			List <Attachment> Attachments = new();

			// Add Mapped Values to the model
			foreach (KeyValuePair<string, string> Kvp in MappedValues)
			{
				switch (Kvp.Key)
				{
					case "FIRST_NAME":  RegisterModel.FirstName = Kvp.Value; break;
					case "MIDDLE_NAME": RegisterModel.MiddleNames = Kvp.Value; break;
					case "LAST_NAME": RegisterModel.LastNames = Kvp.Value; break;
					case "PERSONAL_NUMBER": RegisterModel.PersonalNumber = Kvp.Value; break;
					case "ADDRESS": RegisterModel.Address = Kvp.Value; break;
					case "ADDRESS2": RegisterModel.Address2 = Kvp.Value; break;
					case "AREA": RegisterModel.Area = Kvp.Value; break;
					case "CITY": RegisterModel.City = Kvp.Value; break;
					case "ZIP": RegisterModel.ZipCode = Kvp.Value; break;
					case "REGION": RegisterModel.Region = Kvp.Value; break;
					case "COUNTRY": RegisterModel.CountryCode = Kvp.Value; break;
					case "NATIONALITY": RegisterModel.NationalityCode = Kvp.Value; break;
					case "GENDER": RegisterModel.GenderCode = Kvp.Value; break;
					case "BDATE": RegisterModel.BirthDate = DateTime.TryParse(Kvp.Value, out DateTime Dt) ? Dt : DateTime.Now; break;
					case "EMAIL": RegisterModel.EMail = Kvp.Value; break;
					case "DOC_TYPE": DocumentType = Kvp.Value; break; // TODO: Handle Documents
					case "PASSPORT_FILE": break; // TODO: Handle Passport file
					case "IDENTITY_CARD_FRONT": break; // TODO: Handle Identity Card file
					case "IDENTITY_CARD_BACK": break; // TODO: Handle Identity Card file
					case "DRIVERS_LICENSE_FRONT": break; // TODO: Handle Driver License file
					case "DRIVERS_LICENSE_BACK": break; // TODO: Handle Driver License file
					case "IS_ORG": IsOrg = true; break; // TODO: Handle Organizations?
					case "ORG_NAME": RegisterModel.OrgName = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_NUMBER": RegisterModel.OrgNumber = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_ADDRESS": RegisterModel.OrgAddress = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_ADDRESS2": RegisterModel.OrgAddress2 = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_AREA": RegisterModel.OrgArea = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_CITY": RegisterModel.OrgCity = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_ZIP": RegisterModel.OrgZipCode = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_REGION": RegisterModel.OrgRegion = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_COUNTRY": RegisterModel.OrgCountryCode = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_DEPARMENT": RegisterModel.OrgDepartment = IsOrg ? Kvp.Value : string.Empty; break;
					case "ORG_ROLE": RegisterModel.OrgRole = IsOrg ? Kvp.Value : string.Empty; break;
					case "PHONE": RegisterModel.PhoneNr = Kvp.Value; break;

					// Default case, unhandled case
					default: throw new Exception("Unhandled mapping key: " + Kvp.Key);
				};

				Console.WriteLine($"Mapped: {Kvp.Key} = {Kvp.Value}");
			}

			// Submit the registration
			// Example in ApplyIdViewModel.cs
			await Task.Run(() => { });
		}

		/// <summary>
		/// Given a list of Mappings, retuns a dictionary with the mapping keys and transformed values.
		/// </summary>
		/// <param name="Mappings"></param>
		/// <param name="FieldValue"></param>
		/// <returns> Key value pair: Key = Field name in identity, Value = Transformed Value. For example: BYEAR: 1999</returns>
		private Dictionary<string, string> ApplyTansform(ObservableCollection<KycMapping> Mappings, string FieldValue)
		{
			Dictionary<string, string> Result = new();

			foreach (KycMapping Map in Mappings)
			{
				if (string.IsNullOrEmpty(FieldValue))
				{
					continue;
				}

				Result[Map.Key] = Map.Transform switch
				{
					// Examples
					"uppercase" => FieldValue.ToUpperInvariant(),
					"lowercase" => FieldValue.ToLowerInvariant(),
					"trim" => FieldValue.Trim(),
					_ => FieldValue
				};
			}

			return Result;
		}
	}
}
