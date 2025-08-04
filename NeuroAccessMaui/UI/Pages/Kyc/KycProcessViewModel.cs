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
		private KycReference? kycReference;
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
			foreach (KycPage Page in this.process.Pages)
			{
				foreach (ObservableKycField Field in Page.AllFields)
				{
					this.kycReference?.ApplyFieldValue(Field);
				}
				foreach (KycSection Section in Page.AllSections)
				{
					foreach (ObservableKycField Field in Section.AllFields)
					{
						this.kycReference?.ApplyFieldValue(Field);
					}
				}
			}
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

			Dictionary<string, string> IdentityFields = new();
			List <string> Attachments = new();

			// Add Mapped Values to the model
			foreach (KeyValuePair<string, string> Kvp in MappedValues)
			{
				switch (Kvp.Key)
				{
					case "PASSPORT_FILE":
						if (MappedValues["DOC_TYPE"] == "passport") Attachments.Add(Kvp.Value);
						break; // TODO: Handle Passport file
					case "IDENTITY_CARD_FRONT":
						if (MappedValues["DOC_TYPE"] == "identityCard") Attachments.Add(Kvp.Value);
						break; // TODO: Handle Identity Card file
					case "IDENTITY_CARD_BACK":
						if (MappedValues["DOC_TYPE"] == "identityCard") Attachments.Add(Kvp.Value);
						break; // TODO: Handle Identity Card file
					case "DRIVERS_LICENSE_FRONT":
						if (MappedValues["DOC_TYPE"] == "driversLicense") Attachments.Add(Kvp.Value);
						break; // TODO: Handle Driver License file
					case "DRIVERS_LICENSE_BACK":
						if (MappedValues["DOC_TYPE"] == "driversLicense") Attachments.Add(Kvp.Value);
						break; // TODO: Handle Driver License file
					// Handle other special cases
					// case email:
					// case DeviceID
					// case Document type

					// Default case, unhandled case
					default:
						IdentityFields[Kvp.Key] = Kvp.Value;
						break;
				};

				Console.WriteLine($"Mapped: {Kvp.Key} = {Kvp.Value}");
			}

			// Submit the registration
			// Use IdentityFields and Attachments to submit the KYC process
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
					"year" => DateTime.TryParse(FieldValue, out DateTime Dt) ? Dt.Year.ToString(CultureInfo.InvariantCulture) : "",
					"month" => DateTime.TryParse(FieldValue, out DateTime Dt) ? Dt.Month.ToString(CultureInfo.InvariantCulture) : "",
					"day" => DateTime.TryParse(FieldValue, out DateTime Dt) ? Dt.Day.ToString(CultureInfo.InvariantCulture) : "",
					_ => FieldValue
				};
			}

			return Result;
		}
	}
}
