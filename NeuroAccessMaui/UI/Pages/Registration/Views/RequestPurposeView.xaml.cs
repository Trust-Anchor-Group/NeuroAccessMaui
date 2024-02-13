using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class RequestPurposeView
	{
		public static RequestPurposeView Create()
		{
			return Create<RequestPurposeView>();
		}

		private readonly RequestPurposeViewModel viewModel;

		public RequestPurposeView(RequestPurposeViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
			this.viewModel = ViewModel;
		}

		[RelayCommand]
		public void SelectPurpose(object Control)
		{
			if (Control is not PurposeUse Purpose)
				return;

			foreach (object Item in this.PurposesContainer)
			{
				if ((Item is VisualElement Element) &&
					(Element.BindingContext is PurposeInfo PurposeInfo))
				{
					if (Purpose == PurposeInfo.Purpose)
					{
						VisualStateManager.GoToState(Element, VisualStateManager.CommonStates.Selected);
						this.viewModel.SelectedPurpose = PurposeInfo;
					}
					else
						VisualStateManager.GoToState(Element, VisualStateManager.CommonStates.Normal);
				}
			}
		}

		[RelayCommand]
		public async Task ShowPurposeInfo(object Control)
		{
			if (Control is not PurposeUse Purpose)
				return;

			foreach (object Item in this.PurposesContainer)
			{
				if ((Item is VisualElement Element) &&
					(Element.BindingContext is PurposeInfo PurposeInfo) &&
					(Purpose == PurposeInfo.Purpose))
				{
					ShowInfoPopup Page = new(PurposeInfo.LocalizedName, PurposeInfo.LocalizedDescription);
					await MopupService.Instance.PushAsync(Page);
					break;
				}
			}
		}
	}
}
