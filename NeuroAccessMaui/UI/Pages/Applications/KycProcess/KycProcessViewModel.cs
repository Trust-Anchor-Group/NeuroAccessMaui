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
		private List<KycPage> pages = [];
		private readonly Dictionary<string, string?> fieldValues = [];
		private int currentPageIndex = 0;
		private string currentLang = "en";
		private readonly HashSet<string> conditionControllerFields = new();

		public KycProcessViewModel() { }

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			await LoadProcess();
		}

		private async Task LoadProcess()
		{
			this.pages = await KycProcessParser.LoadProcessAsync("NeuroAccessMaui.Resources.Raw.TestKYC.xml", this.currentLang);

			GatherConditionFields();
			AttachFieldPropertyChangedHandlers();

			this.currentPageIndex = 0;
			UpdateCurrentPage();
		}

		// Gather all field IDs used in KycCondition for fast lookup
		private void GatherConditionFields()
		{
			conditionControllerFields.Clear();
			foreach (var page in pages)
			{
				if (page.Condition is not null)
					conditionControllerFields.Add(page.Condition.FieldRef);

				foreach (var field in page.Fields)
					if (field.Condition is not null)
						conditionControllerFields.Add(field.Condition.FieldRef);

				foreach (var section in page.Sections)
					foreach (var field in section.Fields)
						if (field.Condition is not null)
							conditionControllerFields.Add(field.Condition.FieldRef);
			}
		}

		// Attach PropertyChanged ONCE, never inside UpdateCurrentPage!
		private void AttachFieldPropertyChangedHandlers()
		{
			foreach (var page in pages)
			{
				foreach (var field in page.Fields)
					field.PropertyChanged += Field_PropertyChanged;

				foreach (var section in page.Sections)
					foreach (var field in section.Fields)
						field.PropertyChanged += Field_PropertyChanged;
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
		private List<KycField> directFields = new();

		[ObservableProperty]
		private List<KycSection> currentPageSections = new();

		[ObservableProperty]
		private bool hasSections;

		[ObservableProperty]
		private string? nextButtonText;
		private void UpdateAllFieldVisibilities()
		{
			foreach (var page in pages)
			{
				foreach (var field in page.Fields)
				{
					bool newVisibility = field.Condition is null || field.Condition.Evaluate(fieldValues);
					if (field.IsVisible != newVisibility)
						field.IsVisible = newVisibility;
				}
				foreach (var section in page.Sections)
				{
					foreach (var field in section.Fields)
					{
						bool newVisibility = field.Condition is null || field.Condition.Evaluate(fieldValues);
						if (field.IsVisible != newVisibility)
							field.IsVisible = newVisibility;
					}
				}
			}
		}

		private void UpdateCurrentPage()
		{
			UpdateAllFieldVisibilities();

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

			// Direct fields
			this.DirectFields = page.Fields.Where(f => f.IsVisible).ToList();

			// Sections
			this.CurrentPageSections = page.Sections;
			this.HasSections = this.CurrentPageSections.Count > 0;

			int nextIndex = GetNextPageIndex(this.currentPageIndex + 1);
			this.NextButtonText = nextIndex >= this.pages.Count ? "Apply" : "Next";
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
			foreach (KycField field in this.DirectFields.Concat(this.CurrentPageSections.SelectMany(s => s.Fields)))
			{
				if (!field.Validate(out string error, this.currentLang))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], error);
					return false;
				}
				this.fieldValues[field.Id] = field.Value;
			}
			return true;
		}

		// The only place that triggers UpdateCurrentPage on value change!
		private void Field_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is KycField field && e.PropertyName == nameof(KycField.Value))
			{
				this.fieldValues[field.Id] = field.Value;

				// Always update all visibilities, not just when it's a condition controller field
				UpdateAllFieldVisibilities();

				// Refresh page if any field affects page/section visibility or the field list itself
				if (conditionControllerFields.Contains(field.Id))
				{
					UpdateCurrentPage();
				}
			}
		}

		[RelayCommand]
		private async Task Next()
		{
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

		private async Task Apply()
		{
			foreach (KycPage page in this.pages)
			{
				foreach (KycField field in page.Fields.Concat(page.Sections.SelectMany(s => s.Fields)))
				{
					field.MapToApplyModel(this.applyViewModel);
				}
			}
			await this.applyViewModel.ApplyCommand.ExecuteAsync(null);
		}
	}
}
