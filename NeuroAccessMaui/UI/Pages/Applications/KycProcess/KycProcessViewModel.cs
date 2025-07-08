using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeuroAccessMaui.Services;
using NeuroAccessMaui.Services.Kyc;
using NeuroAccessMaui.Services.Kyc.Models;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.UI.Pages.Applications.ApplyId;
using System.Linq;
using System.ComponentModel;

// Add a using alias for KycField and related types from the new models namespace
using KycField = NeuroAccessMaui.Services.Kyc.Models.KycField;
using KycPage = NeuroAccessMaui.Services.Kyc.Models.KycPage;
using KycOption = NeuroAccessMaui.Services.Kyc.Models.KycOption;

namespace NeuroAccessMaui.UI.Pages.Applications.KycProcess
{
	public partial class KycProcessViewModel : BaseViewModel
	{
		private readonly ApplyIdViewModel applyViewModel;
		private List<KycPage> pages = new();
		private readonly Dictionary<string, string?> fieldValues = new();
		private int currentPageIndex;

		public KycProcessViewModel() : base()
		{
			this.applyViewModel = new ApplyIdViewModel();
		}

		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			await this.LoadProcess();
		}

		private async Task LoadProcess()
		{
			this.pages = await KycProcessParser.LoadProcessAsync("NeuroAccessMaui.Resources.Raw.TestKYC.xml");
			this.currentPageIndex = 0;
			this.UpdateCurrentPage();
		}

		private void UpdateCurrentPage()
		{
			if (this.currentPageIndex >= this.pages.Count)
				return;

			KycPage page = this.pages[this.currentPageIndex];
			while (!page.IsVisible(this.fieldValues) && this.currentPageIndex < this.pages.Count - 1)
			{
				this.currentPageIndex++;
				page = this.pages[this.currentPageIndex];
			}

			this.PageTitle = page.Title;

			int nextIndex = this.GetNextPageIndex(this.currentPageIndex + 1);
			this.NextButtonText = nextIndex >= this.pages.Count ? "Apply" : "Next";

			List<KycField> visible = page.GetVisibleFields(this.fieldValues).ToList();
			this.Fields = visible;
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
			foreach (KycField field in this.Fields)
			{
				if (!field.Validate(out string error))
				{
					await ServiceRef.UiService.DisplayAlert(ServiceRef.Localizer[nameof(AppResources.ErrorTitle)], error);
					return false;
				}
				this.fieldValues[field.Id] = field.Value;
			}
			return true;
		}

		[ObservableProperty]
		private string? pageTitle;

		[ObservableProperty]
		private string? nextButtonText;

		[ObservableProperty]
		private List<KycField> fields = new();

		partial void OnFieldsChanged(List<KycField> value)
		{
			foreach (KycField field in value)
				field.PropertyChanged += this.Field_PropertyChanged;
		}

		private void Field_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is KycField field && e.PropertyName == nameof(KycField.Value))
			{
				this.fieldValues[field.Id] = field.Value;
				this.UpdateCurrentPage();
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
				foreach (KycField field in page.Fields)
					field.MapToApplyModel(this.applyViewModel);

			await this.applyViewModel.ApplyCommand.ExecuteAsync(null);
		}
	}
}
