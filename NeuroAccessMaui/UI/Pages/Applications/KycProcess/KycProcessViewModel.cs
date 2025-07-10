using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using System.ComponentModel;

namespace NeuroAccessMaui.UI.Pages.Applications.KycProcess
{
	public partial class KycProcessViewModel : BaseViewModel
	{
		private readonly ApplyIdViewModel applyViewModel = new();
		private ObservableCollection<KycPage> pages = new();
		private readonly Dictionary<string, string?> fieldValues = new();
		private int currentPageIndex = 0;
		private string currentLang = "en";
		private readonly HashSet<string> conditionControllerFields = new();

		public KycProcessViewModel() { }

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			await this.LoadProcess();
		}

		private async Task LoadProcess()
		{
			// Load your model (must return ObservableCollection<KycPage>)
			this.pages = new ObservableCollection<KycPage>(
				await KycProcessParser.LoadProcessAsync("NeuroAccessMaui.Resources.Raw.TestKYC.xml", this.currentLang)
			);

			// Attach model-level visibility change handlers
			foreach (var page in this.pages)
				page.InitVisibilityHandlers();

			this.GatherConditionFields();
			this.AttachFieldPropertyChangedHandlers();

			this.currentPageIndex = 0;
			this.UpdateCurrentPage();
		}

		private void GatherConditionFields()
		{
			this.conditionControllerFields.Clear();
			foreach (KycPage page in this.pages)
			{
				if (page.Condition is not null)
					this.conditionControllerFields.Add(page.Condition.FieldRef);

				foreach (KycField field in page.AllFields)
					if (field.Condition is not null)
						this.conditionControllerFields.Add(field.Condition.FieldRef);

				foreach (KycSection section in page.AllSections)
					foreach (KycField field in section.AllFields)
						if (field.Condition is not null)
							this.conditionControllerFields.Add(field.Condition.FieldRef);
			}
		}

		private void AttachFieldPropertyChangedHandlers()
		{
			foreach (KycPage page in this.pages)
			{
				foreach (KycField field in page.AllFields)
					field.PropertyChanged += this.Field_PropertyChanged;

				foreach (KycSection section in page.AllSections)
					foreach (KycField field in section.AllFields)
						field.PropertyChanged += this.Field_PropertyChanged;
			}
		}

		[ObservableProperty]
		private KycPage? currentPage;

		[ObservableProperty]
		private string? currentPageTitle;

		[ObservableProperty]
		private string? currentPageDescription;

		[ObservableProperty]
		private bool hasCurrentPageDescription;

		[ObservableProperty]
		private ObservableCollection<KycSection> currentPageSections = new();

		[ObservableProperty]
		private bool hasSections;

		[ObservableProperty]
		private string? nextButtonText;

		// Recompute visibility for all fields/sections in all pages
		private void UpdateAllFieldVisibilities()
		{
			foreach (KycPage page in this.pages)
				page.UpdateAllVisibilities(this.fieldValues);
		}

		private void UpdateCurrentPage()
		{
			this.UpdateAllFieldVisibilities();

			if (this.currentPageIndex >= this.pages.Count)
				return;

			KycPage page = this.pages[this.currentPageIndex];
			while (!page.IsVisible(this.fieldValues) && this.currentPageIndex < this.pages.Count - 1)
			{
				this.currentPageIndex++;
				page = this.pages[this.currentPageIndex];
			}

			this.CurrentPage = page;
			this.CurrentPageTitle = page.Title?.Text ?? page.Id;
			this.CurrentPageDescription = page.Description?.Text;
			this.HasCurrentPageDescription = !string.IsNullOrWhiteSpace(this.CurrentPageDescription);

			// **Bind directly to VisibleSections for the current page**
			this.CurrentPageSections = page.VisibleSections;
			this.HasSections = this.CurrentPageSections.Count > 0;

			int NextIndex = this.GetNextPageIndex(this.currentPageIndex + 1);
			this.NextButtonText = NextIndex >= this.pages.Count ? "Apply" : "Next";

			OnPropertyChanged(nameof(this.Progress));
			this.NextCommand.NotifyCanExecuteChanged();
		}

		private int GetNextPageIndex(int start)
		{
			int index = start;
			while (index < this.pages.Count && !this.pages[index].IsVisible(this.fieldValues))
				index++;
			return index;
		}

		private int GetPreviousPageIndex(int start)
		{
			int index = start;
			while (index >= 0 && !this.pages[index].IsVisible(this.fieldValues))
				index--;
			return index;
		}

		private async Task<bool> ValidateCurrentPage()
		{
			bool allValid = true;

			// Validate all visible fields on this page and visible sections
			foreach (KycField field in this.CurrentPage!.VisibleFields
				.Concat(this.CurrentPageSections.SelectMany(s => s.VisibleFields)))
			{
				if (!field.Validate(this.currentLang))
					allValid = false;
			}
			if (!allValid)
			{
				await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], "Some fields are invalid.");
				return false;
			}
			foreach (KycField? field in this.CurrentPage.VisibleFields.Concat(this.CurrentPageSections.SelectMany(s => s.VisibleFields)))
				this.fieldValues[field.Id] = field.Value;
			return true;
		}

		private void Field_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is KycField field && e.PropertyName == nameof(KycField.Value))
			{
				this.fieldValues[field.Id] = field.Value;

				this.UpdateAllFieldVisibilities();

				if (this.conditionControllerFields.Contains(field.Id))
				{
					this.UpdateCurrentPage();
				}

				this.NextCommand.NotifyCanExecuteChanged();
			}
		}

		// True if all fields in the current page and sections are valid
		public bool CanGoNext => (this.CurrentPage?.VisibleFields
			.Concat(this.CurrentPageSections.SelectMany(s => s.VisibleFields))
			.All(f => f.IsValid) ?? true);

		[RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanGoNext))]
		private async Task Next()
		{
			ServiceRef.PlatformSpecific.HideKeyboard();
			await Task.Delay(150);

			if (!await this.ValidateCurrentPage())
				return;

			int nextIndex = this.GetNextPageIndex(this.currentPageIndex + 1);
			if (nextIndex < this.pages.Count)
			{
				this.currentPageIndex = nextIndex;
				this.UpdateCurrentPage();
			}
			else
			{
				await this.Apply();
			}
		}

		[RelayCommand]
		private void Previous()
		{
			int prevIndex = this.GetPreviousPageIndex(this.currentPageIndex - 1);
			if (prevIndex >= 0)
			{
				this.currentPageIndex = prevIndex;
				this.UpdateCurrentPage();
			}
		}
		public double Progress
		{
			get
			{
				// Only count pages that are visible in the current state
				var visiblePages = pages.Where(p => p.IsVisible(this.fieldValues)).ToList();

				if (!visiblePages.Any())
					return 0;

				// Find the index of the current page among visible pages
				int visibleIndex = visiblePages.IndexOf(this.CurrentPage);

				// Progress is how many pages completed (including current), divided by total
				// E.g. on first page: (1/total), on last page: 1.0
				double progress = ((double)visibleIndex) / (double)visiblePages.Count;
				return Math.Min(1.0, Math.Max(0.0, progress));
			}
		}
		private async Task Apply()
		{
			foreach (KycPage page in this.pages)
			{
				foreach (KycField field in page.AllFields.Concat(page.AllSections.SelectMany(s => s.AllFields)))
				{
					field.MapToApplyModel(this.applyViewModel);
				}
			}
			await this.applyViewModel.ApplyCommand.ExecuteAsync(null);
		}
	}
}
