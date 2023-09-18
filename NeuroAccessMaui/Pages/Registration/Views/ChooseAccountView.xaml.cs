namespace NeuroAccessMaui.Pages.Registration.Views;

public partial class ChooseAccountView
{
	public static ChooseAccountView? Create()
	{
		return Create<ChooseAccountView>();
	}

	public ChooseAccountView(ChooseAccountViewModel ViewModel)
	{
		this.InitializeComponent();
		this.InitializeObject(ViewModel);
	}
}
