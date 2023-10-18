using CommunityToolkit.Mvvm.Input;
using Mopups.Services;
using NeuroAccessMaui.Services.Tag;
using NeuroAccessMaui.UI.Popups;

namespace NeuroAccessMaui.UI.Pages.Registration.Views;

public partial class ChoosePurposeView
{
	public static ChoosePurposeView Create()
	{
		return Create<ChoosePurposeView>();
	}

	private readonly ChoosePurposeViewModel viewModel;

	public ChoosePurposeView(ChoosePurposeViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentViewModel = ViewModel;
		this.viewModel = ViewModel;
	}

	[RelayCommand]
	public void SelectPurpose(object o)
	{
		if (o is not PurposeUse Purpose)
		{
			return;
		}

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
				{
					VisualStateManager.GoToState(Element, VisualStateManager.CommonStates.Normal);
				}
			}
		}
	}

	[RelayCommand]
	public async Task ShowPurposeInfo(object o)
	{
		if (o is not PurposeUse Purpose)
		{
			return;
		}

		foreach (object Item in this.PurposesContainer)
		{
			if ((Item is VisualElement Element) &&
				(Element.BindingContext is PurposeInfo PurposeInfo) &&
				(Purpose == PurposeInfo.Purpose))
			{
				ShowInfoPage Page = new(PurposeInfo.LocalizedName, PurposeInfo.LocalizedDescription);
				await MopupService.Instance.PushAsync(Page);
				break;
			}
		}
	}
}
