namespace NeuroAccessMaui.Pages.Registration.Views;

public partial class ChoosePurposeView
{
	public static ChoosePurposeView? Create()
	{
		return Create<ChoosePurposeView>();
	}

	public ChoosePurposeView(ChoosePurposeViewModel ViewModel)
	{
		this.InitializeComponent();
		this.InitializeObject(ViewModel);
	}
}
