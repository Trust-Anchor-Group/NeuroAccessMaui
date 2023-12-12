using CommunityToolkit.Mvvm.Input;

namespace NeuroAccessMaui.UI.Pages.Registration.Views
{
	public partial class ChooseProviderView
	{
		public static ChooseProviderView Create()
		{
			return Create<ChooseProviderView>();
		}

		private readonly ChooseProviderViewModel viewModel;

		public ChooseProviderView(ChooseProviderViewModel ViewModel)
		{
			this.InitializeComponent();
			this.ContentViewModel = ViewModel;
			this.viewModel = ViewModel;
		}

		[RelayCommand]
		public void SelectButton(object Control)
		{
			if (Control is not ButtonType Button)
				return;

			foreach (object Item in this.PurposesContainer)
			{
				if ((Item is VisualElement Element) &&
					(Element.BindingContext is ButtonInfo ButtonInfo))
				{
					if (Button == ButtonInfo.Button)
					{
						VisualStateManager.GoToState(Element, VisualStateManager.CommonStates.Selected);
						this.viewModel.SelectedButton = ButtonInfo;

						if (ButtonInfo.Button == ButtonType.Change)
						{
							// unselect it after the QR scan is open
							this.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
							{
								VisualStateManager.GoToState(Element, VisualStateManager.CommonStates.Normal);
								this.viewModel.SelectedButton = null;
							});
						}
					}
					else
						VisualStateManager.GoToState(Element, VisualStateManager.CommonStates.Normal);
				}
			}
		}
	}
}
