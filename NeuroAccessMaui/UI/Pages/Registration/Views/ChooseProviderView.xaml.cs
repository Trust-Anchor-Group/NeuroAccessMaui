namespace NeuroAccessMaui.UI.Pages.Registration.Views;

public partial class ChooseProviderView
{
	public static ChooseProviderView Create()
	{
		return Create<ChooseProviderView>();
	}

	public ChooseProviderView(ChooseProviderViewModel ViewModel)
	{
		this.InitializeComponent();
		this.ContentViewModel = ViewModel;
	}
}
